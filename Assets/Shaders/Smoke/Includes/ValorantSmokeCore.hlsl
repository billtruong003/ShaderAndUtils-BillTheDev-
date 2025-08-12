#ifndef VALORANT_SMOKE_CORE_INCLUDED
#define VALORANT_SMOKE_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// CBUFFER definition - nó sẽ được điền dữ liệu từ shader chính
CBUFFER_START(UnityPerMaterial)
    float _ShellThickness;
    float _DensityMultiplier;
    int _RaymarchSteps;
    float _NoiseScale;
    float4 _NoiseScrollSpeed;
    float _WarpScale;
    float _WarpStrength;
    float _LightAbsorption;
    float4 _RimColor;
    float _RimPower;
    float4 _EdgeColor;
    float _EdgeHardness;
    float _EdgeSoftness;
    float _DepthFadeDistance;
CBUFFER_END

// Texture definitions - chúng cũng sẽ được liên kết từ shader chính
TEXTURE3D(_NoiseTexture); SAMPLER(sampler_NoiseTexture);
TEXTURE3D(_WarpTexture);
TEXTURE2D(_LitColorRamp); SAMPLER(sampler_LitColorRamp);
TEXTURE2D(_ShadowColorRamp); SAMPLER(sampler_ShadowColorRamp);
TEXTURE2D(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);

// -- STRUCTS --
struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
};

struct Varyings
{
    float4 positionCS               : SV_POSITION;
    float3 worldPosition            : TEXCOORD0;
    float3 viewDirection            : TEXCOORD1;
    float3 worldNormal              : TEXCOORD2;
    float4 screenPosition           : TEXCOORD3;
    float3 objectPositionOS         : TEXCOORD4;
};

// -- HELPER FUNCTIONS --
bool solveQuadratic(float a, float b, float c, out float t0, out float t1)
{
    t0 = 0.0; t1 = 0.0;
    float discriminant = b * b - 4.0 * a * c;
    if (discriminant < 0.0) return false;
    float sqrtDiscriminant = sqrt(discriminant);
    t0 = (-b - sqrtDiscriminant) / (2.0 * a);
    t1 = (-b + sqrtDiscriminant) / (2.0 * a);
    return true;
}

bool raySphereIntersect(float3 rayOriginOS, float3 rayDirectionOS, float sphereRadius, out float entryDist, out float exitDist)
{
    float3 oc = rayOriginOS;
    float a = dot(rayDirectionOS, rayDirectionOS);
    float b = 2.0 * dot(oc, rayDirectionOS);
    float c = dot(oc, oc) - sphereRadius * sphereRadius;
    return solveQuadratic(a, b, c, entryDist, exitDist);
}

// -- CORE SAMPLING LOGIC --
float sampleDensity(float3 positionOS)
{
    float3 timeShiftedPos = positionOS + _Time.y * _NoiseScrollSpeed.xyz;
    float3 mainNoiseCoords = timeShiftedPos * _NoiseScale;

    #if _ENABLE_WARP
        float3 warpOffsetCoords = timeShiftedPos * _WarpScale;
        float3 warpVector = SAMPLE_TEXTURE3D_LOD(_WarpTexture, sampler_NoiseTexture, warpOffsetCoords, 0).rgb * 2.0 - 1.0;
        mainNoiseCoords += warpVector * _WarpStrength;
    #endif
    
    float fbm = 0;
    float amplitude = 0.5;
    float frequency = 1.0;
    [unroll]
    for(int i = 0; i < 4; i++)
    {
        fbm += amplitude * SAMPLE_TEXTURE3D_LOD(_NoiseTexture, sampler_NoiseTexture, mainNoiseCoords * frequency, 0).r;
        amplitude *= 0.5;
        frequency *= 2.0;
    }
    return saturate(fbm);
}


// -- MAIN FRAGMENT FUNCTION --
float4 ValorantSmokeFragment(Varyings input)
{
    float3 rayOriginOS = mul(GetWorldToObjectMatrix(), float4(_WorldSpaceCameraPos, 1.0)).xyz;
    float3 rayDirectionOS = normalize(input.objectPositionOS - rayOriginOS);

    float entryPoint, exitPoint;
    float sphereRadius = 0.5;
    if (!raySphereIntersect(rayOriginOS, rayDirectionOS, sphereRadius, entryPoint, exitPoint) || exitPoint < 0.0)
    {
        discard;
    }
    
    entryPoint = max(0.0, entryPoint);
    float rayLength = exitPoint - entryPoint;
    if (rayLength <= 0.001)
    {
        discard;
    }

    float stepSize = rayLength / _RaymarchSteps;
    float accumulatedDensity = 0;
    float3 lightEnergy = 0;
    Light mainLight = GetMainLight();
    
    [loop]
    for (int i = 0; i < _RaymarchSteps; i++)
    {
        float currentDistOnRay = entryPoint + stepSize * (i + 0.5);
        float3 currentPosOS = rayOriginOS + rayDirectionOS * currentDistOnRay;
        
        float distFromCenter = length(currentPosOS);
        float shellFalloff = 1.0 - smoothstep(sphereRadius - _ShellThickness, sphereRadius, distFromCenter);
        if (shellFalloff < 0.01) continue;
        
        float noiseDensity = sampleDensity(currentPosOS);
        float finalStepDensity = noiseDensity * shellFalloff;
        
        if (finalStepDensity > 0.01)
        {
            float transmittance = exp(-accumulatedDensity * _LightAbsorption);
            
            float3 worldPos = TransformObjectToWorld(currentPosOS);
            float lightDot = saturate(dot(normalize(worldPos - input.worldPosition), mainLight.direction));
            
            float3 litColor = SAMPLE_TEXTURE2D_LOD(_LitColorRamp, sampler_LitColorRamp, float2(lightDot, 0.5), 0).rgb;
            float3 shadowColor = SAMPLE_TEXTURE2D_LOD(_ShadowColorRamp, sampler_ShadowColorRamp, float2(lightDot, 0.5), 0).rgb;
            float3 stepColor = lerp(shadowColor, litColor, lightDot);
            
            lightEnergy += stepColor * finalStepDensity * stepSize * transmittance;
            accumulatedDensity += finalStepDensity * stepSize;
        }
    }

    if (accumulatedDensity <= 0.0)
    {
        discard;
    }
    
    float totalDensity = accumulatedDensity * _DensityMultiplier;
    float finalAlpha = 1.0 - exp(-totalDensity);
    float3 finalColor = lightEnergy;

    float edge = 1.0 - smoothstep(_EdgeHardness, _EdgeHardness + _EdgeSoftness, finalAlpha);
    finalColor = lerp(finalColor, _EdgeColor.rgb, edge * _EdgeColor.a);
    
    float rim = pow(1.0 - saturate(dot(input.viewDirection, normalize(input.worldNormal))), _RimPower);
    finalColor += _RimColor.rgb * rim * finalAlpha * _RimColor.a;

    float2 screenUV = input.screenPosition.xy / input.screenPosition.w;
    float sceneRawDepth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV).r;
    float sceneLinearDepth = LinearEyeDepth(sceneRawDepth, _ZBufferParams);
    float pixelLinearDepth = input.positionCS.w;
    float depthDifference = abs(sceneLinearDepth - pixelLinearDepth);
    float depthFade = saturate(depthDifference / _DepthFadeDistance);

    return float4(finalColor, finalAlpha * depthFade);
}

#endif