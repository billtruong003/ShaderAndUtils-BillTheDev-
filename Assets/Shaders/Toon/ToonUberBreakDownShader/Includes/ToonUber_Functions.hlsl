#ifndef BILLS_TOON_FUNCTIONS_INCLUDED
#define BILLS_TOON_FUNCTIONS_INCLUDED

inline half FastSmoothstep(half a, half b, half x)
{
    half t = saturate((x - a) / (b - a + 1e-5h));
    return t * t * (3.0h - 2.0h * t);
}

inline half Pow2(half x) 
{
    return x * x;
}

inline half FastPow(half base, half exp)
{
    return exp2(log2(saturate(base)) * exp);
}

inline half FastSin(half x)
{
    x = frac(x * 0.15915494309); // Tương đương x / (2 * PI)
    x = x * 2.0h - 1.0h;
    return 1.27323954474 * x - 0.40528473456 * x * abs(x);
}

float3 CalculateToonLighting(float3 normalWS, float3 worldPos, Light mainLight)
{
    float NdotL = dot(normalWS, mainLight.direction) * 0.5 + 0.5;
    float toonRamp = FastSmoothstep(_ToonRampOffset, _ToonRampOffset + _ToonRampSmoothness, NdotL);
    float3 mainLightContribution = mainLight.color * lerp(_ShadowTint.rgb, 1.0, toonRamp) * mainLight.shadowAttenuation;

    float3 additionalLightContribution = 0.0h;
    #ifdef _ADDITIONAL_LIGHTS
        uint lightCount = GetAdditionalLightsCount();
        for (uint i = 0u; i < lightCount; ++i)
        {
            Light additionalLight = GetAdditionalLight(i, worldPos);
            float dAdd = dot(normalWS, additionalLight.direction) * 0.5 + 0.5;
            float toonRampAdd = FastSmoothstep(_ToonRampOffset, _ToonRampOffset + _ToonRampSmoothness, dAdd);
            additionalLightContribution += additionalLight.color * lerp(_ShadowTint.rgb, 1.0, toonRampAdd) * additionalLight.distanceAttenuation * additionalLight.shadowAttenuation;
        }
    #endif

    return mainLightContribution + additionalLightContribution;
}

float3 CalculateMetallicLighting(float3 normalWS, float3 viewDir, Light mainLight)
{
    float3 halfVec = SafeNormalize(viewDir + mainLight.direction);
    float NdotH = saturate(dot(normalWS, halfVec));
    float NdotL = saturate(dot(normalWS, mainLight.direction));
    float NdotV = saturate(dot(normalWS, viewDir));

    half2 rampUV = half2(NdotL, 0.5h);
    half3 rampColor = SAMPLE_TEXTURE2D(_Ramp, sampler_Ramp, rampUV).rgb;

    half specularRamp = FastSmoothstep(_Offset, _Offset + 0.05, NdotH);
    half highlightRamp = FastSmoothstep(_HighlightOffset, _HighlightOffset + 0.05, NdotH);
    
    half3 specular = specularRamp * _SpecuColor.rgb;
    half3 highlight = highlightRamp * _HiColor.rgb;
    
    float3 rim = FastPow(1.0h - NdotV, _RimPower) * _RimColor.rgb;

    float3 lighting = (rampColor + specular + highlight) * _Brightness * mainLight.color * mainLight.shadowAttenuation;
    lighting += rim;

    return lighting;
}

void ApplyWind(inout float3 positionOS, float4 vertexColor)
{
    float3 worldPos = TransformObjectToWorld(positionOS);
    float windPhase = dot(worldPos.xz, float2(0.2, 0.1));
    float windSine = FastSin(_Time.y * _WindFrequency + windPhase);
    float3 windVector = normalize(_WindDirection) * windSine * _WindAmplitude;
    float windMask = vertexColor.a;
    positionOS.xyz += windVector * windMask;
}

float3 CalculateFoliageLighting(float3 normalWS, float3 worldPos, Light mainLight)
{
    float NdotL = dot(normalWS, mainLight.direction) * 0.5 + 0.5;
    float3 lambert = mainLight.color * NdotL;

    float3 backLightDir = -mainLight.direction;
    float backNdotL = dot(normalWS, backLightDir) * 0.5 + 0.5;
    float3 translucency = Pow2(backNdotL) * mainLight.color * _TranslucencyStrength * _TranslucencyColor;
    float3 totalLight = (lambert + translucency) * mainLight.shadowAttenuation;

    #ifdef _ADDITIONAL_LIGHTS
        uint lightCount = GetAdditionalLightsCount();
        for (uint i = 0u; i < lightCount; ++i)
        {
            Light additionalLight = GetAdditionalLight(i, worldPos);
            float addNdotL = dot(normalWS, additionalLight.direction) * 0.5 + 0.5;
            totalLight += additionalLight.color * addNdotL * additionalLight.distanceAttenuation * additionalLight.shadowAttenuation;
        }
    #endif

    return totalLight;
}

#endif