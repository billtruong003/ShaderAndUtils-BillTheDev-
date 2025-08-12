#ifndef VALORANT_SMOKE_CORE_INCLUDED
#define VALORANT_SMOKE_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

CBUFFER_START(UnityPerMaterial)
    // Formation and Quality
    float _Progress;
    float _SphereRadius;
    int _RaymarchSteps;
    float _DensityMultiplier;

    // Shell Layer
    float _ShellThickness;
    float _ShellEdgeSoftness;
    float _ShellDensity;
    float4 _ShellColor;
    float _ShellNoiseScale;
    float4 _ShellScrollSpeed;

    // Core Layer
    float _CoreFalloff;
    float _CoreNoiseScale;
    float4 _CoreScrollSpeed;
    
    // Proximity Effect
    float _ProximityDetailBoost;
    float _ProximityDensityMultiplier;

    // Warp Effect
    float _WarpScale;
    float _WarpStrength;

    // Lighting & Color
    float _LightAbsorption;
    float4 _RimColor;
    float _RimPower;
    
    // Edge & Intersection
    float4 _EdgeColor;
    float _EdgeHardness;
    float _EdgeSoftness;
    float _DepthFadeDistance;
CBUFFER_END

TEXTURE3D(_NoiseTexture); SAMPLER(sampler_NoiseTexture);
TEXTURE3D(_WarpTexture);
TEXTURE2D(_LitColorRamp); SAMPLER(sampler_LitColorRamp);
TEXTURE2D(_ShadowColorRamp); SAMPLER(sampler_ShadowColorRamp);
TEXTURE2D(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);

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

struct SmokeSample
{
    float density;
    float3 color;
};

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

bool raySphereIntersect(float3 rayOriginOS, float3 rayDirectionOS, out float entryDist, out float exitDist)
{
    float3 oc = rayOriginOS;
    float a = dot(rayDirectionOS, rayDirectionOS);
    float b = 2.0 * dot(oc, rayDirectionOS);
    float c = dot(oc, oc) - _SphereRadius * _SphereRadius;
    return solveQuadratic(a, b, c, entryDist, exitDist);
}

float GetCameraProximityFactor(float3 cameraPosOS)
{
    float distFromCenter = length(cameraPosOS);
    float proximity = 1.0 - saturate(distFromCenter / _SphereRadius);
    return smoothstep(0.0, 1.0, proximity);
}

float SampleFractalNoise(float3 position, float scale, float4 scrollSpeed)
{
    float3 timeShiftedPos = position + _Time.y * scrollSpeed.xyz;
    float3 mainNoiseCoords = timeShiftedPos * scale;

    #if _ENABLE_WARP
        float3 warpOffsetCoords = timeShiftedPos * _WarpScale;
        float3 warpVector = SAMPLE_TEXTURE3D_LOD(_WarpTexture, sampler_NoiseTexture, warpOffsetCoords, 0).rgb * 2.0 - 1.0;
        mainNoiseCoords += warpVector * _WarpStrength * _Progress;
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

    float formationCarve = 1.0 - _Progress;
    float remappedNoise = saturate((fbm - formationCarve) / (1.0 - formationCarve));
    return remappedNoise;
}

SmokeSample GetSmokeLayerPropertiesAtPosition(float3 positionOS, float3 worldPos, Light mainLight, float proximityFactor)
{
    SmokeSample finalSample = (SmokeSample)0;
    float distFromCenter = length(positionOS);
    float proximityDensityBoost = 1.0 + proximityFactor * _ProximityDensityMultiplier;
    
    #if _ENABLE_SHELL
        float shellOuterEdge = _SphereRadius;
        float shellInnerEdge = _SphereRadius - _ShellThickness;
        float shellFalloff = smoothstep(shellOuterEdge, shellOuterEdge - _ShellEdgeSoftness, distFromCenter) *
                             smoothstep(shellInnerEdge, shellInnerEdge + _ShellEdgeSoftness, distFromCenter);

        if (shellFalloff > 0.01)
        {
            float dynamicShellNoiseScale = lerp(_ShellNoiseScale, _ShellNoiseScale + _ProximityDetailBoost, proximityFactor);
            float shellNoise = SampleFractalNoise(positionOS, dynamicShellNoiseScale, _ShellScrollSpeed);
            finalSample.density = shellNoise * shellFalloff * _ShellDensity * proximityDensityBoost;
            finalSample.color = _ShellColor.rgb;
        }
    #endif

    float coreBoundary = _SphereRadius - _ShellThickness * _ENABLE_SHELL;
    if (distFromCenter < coreBoundary)
    {
        float coreShapeFalloff = pow(saturate(1.0 - distFromCenter / coreBoundary), _CoreFalloff);
        if (coreShapeFalloff > 0.01)
        {
            float dynamicCoreNoiseScale = lerp(_CoreNoiseScale, _CoreNoiseScale + _ProximityDetailBoost, proximityFactor);
            float coreNoise = SampleFractalNoise(positionOS, dynamicCoreNoiseScale, _CoreScrollSpeed);
            float coreDensity = coreNoise * coreShapeFalloff * proximityDensityBoost;

            float3 worldLightDir = mainLight.direction;
            float lightDot = saturate(dot(normalize(worldPos), worldLightDir));
            
            float3 litColor = SAMPLE_TEXTURE2D_LOD(_LitColorRamp, sampler_LitColorRamp, float2(lightDot, 0.5), 0).rgb;
            float3 shadowColor = SAMPLE_TEXTURE2D_LOD(_ShadowColorRamp, sampler_ShadowColorRamp, float2(lightDot, 0.5), 0).rgb;
            float3 coreColor = lerp(shadowColor, litColor, lightDot);
            
            finalSample.density += coreDensity;
            finalSample.color = lerp(finalSample.color, coreColor, saturate(coreDensity / max(finalSample.density, 0.001)));
        }
    }
    
    return finalSample;
}

float4 ValorantSmokeFragment(Varyings input)
{
    float3 rayOriginOS = mul(GetWorldToObjectMatrix(), float4(_WorldSpaceCameraPos, 1.0)).xyz;
    float3 rayDirectionOS = normalize(input.objectPositionOS - rayOriginOS);

    float entryPoint, exitPoint;
    if (!raySphereIntersect(rayOriginOS, rayDirectionOS, entryPoint, exitPoint) || exitPoint < 0.0)
    {
        discard;
    }
    
    entryPoint = max(0.0, entryPoint);
    float rayLength = max(0.0, exitPoint - entryPoint);
    if (rayLength <= 0.001)
    {
        discard;
    }

    int dynamicSteps = max(4, (int)lerp(4, _RaymarchSteps, saturate(rayLength / (_SphereRadius * 2.0))));
    float stepSize = rayLength / dynamicSteps;
    
    float accumulatedDensity = 0;
    float3 lightEnergy = 0;
    Light mainLight = GetMainLight();
    float proximityFactor = GetCameraProximityFactor(rayOriginOS);
    
    [loop]
    for (int i = 0; i < dynamicSteps; i++)
    {
        float currentDistOnRay = entryPoint + stepSize * (i + 0.5);
        float3 currentPosOS = rayOriginOS + rayDirectionOS * currentDistOnRay;
        
        float3 worldPos = TransformObjectToWorld(currentPosOS);
        SmokeSample currentSample = GetSmokeLayerPropertiesAtPosition(currentPosOS, worldPos, mainLight, proximityFactor);
        
        if (currentSample.density > 0.01)
        {
            float transmittance = exp(-accumulatedDensity * _LightAbsorption);
            lightEnergy += currentSample.color * currentSample.density * stepSize * transmittance;
            accumulatedDensity += currentSample.density * stepSize;
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
    float sceneRawDepth = SAMPLE_TEXTURE2D_LOD(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV, 0).r;
    float sceneLinearDepth = LinearEyeDepth(sceneRawDepth, _ZBufferParams);
    float pixelLinearDepth = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
    float depthDifference = abs(sceneLinearDepth - pixelLinearDepth);
    float depthFade = saturate(depthDifference / _DepthFadeDistance);

    return float4(finalColor, finalAlpha * depthFade);
}
#endif