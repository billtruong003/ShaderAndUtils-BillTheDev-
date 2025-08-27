#ifndef BILLS_TOON_FUNCTIONS_HQ_INCLUDED
#define BILLS_TOON_FUNCTIONS_HQ_INCLUDED

#include "Assets/Shaders/Toon/ToonUberBreakDownShader/Includes/MathUtils.hlsl"

#if defined(_OUTLINEGLINT_ON)
float CalculateGlintFactor(float3 worldPos)
{
    float noiseTime = _Time.y * _GlintSpeed;
    float2 noiseUV = worldPos.xy * _GlintScale * 0.1;
    noiseUV.y += noiseTime;
    float noise = MU_SimplexNoise(noiseUV) * 0.5 + 0.5;
    return smoothstep(_GlintThreshold, _GlintThreshold + 0.05, noise);
}
#endif

float3 GetToonRamp(float rampInput, float3 albedo)
{
    float highlightStep = smoothstep(_HighlightThreshold - _RampSmoothness, _HighlightThreshold + _RampSmoothness, rampInput);
    float midtoneStep = smoothstep(_MidtoneThreshold - _RampSmoothness, _MidtoneThreshold + _RampSmoothness, rampInput);
    float shadowStep = smoothstep(_ShadowThreshold - _RampSmoothness, _ShadowThreshold + _RampSmoothness, rampInput);
    
    float3 shadowColor = _ShadowColor.rgb * albedo;
    float3 midtoneColor = _MidtoneColor.rgb * albedo;
    float3 highlightColor = _HighlightColor.rgb * albedo;

    float3 ramp = lerp(shadowColor, midtoneColor, shadowStep);
    ramp = lerp(ramp, albedo, midtoneStep);
    ramp = lerp(ramp, highlightColor, highlightStep);
    
    return ramp;
}

float3 GetMetallicRamp(float rampInput)
{
    float hotSpotStep = smoothstep(_MetallicHotSpotThreshold - _MetallicRampSmoothness, _MetallicHotSpotThreshold + _MetallicRampSmoothness, rampInput);
    float specularStep = smoothstep(_MetallicSpecularThreshold - _MetallicRampSmoothness, _MetallicSpecularThreshold + _MetallicRampSmoothness, rampInput);
    float reflectionStep = smoothstep(_MetallicReflectionThreshold - _MetallicRampSmoothness, _MetallicReflectionThreshold + _MetallicRampSmoothness, rampInput);

    float3 ramp = lerp(_MetallicBaseColor.rgb, _MetallicReflectionColor.rgb, reflectionStep);
    ramp = lerp(ramp, _MetallicSpecularColor.rgb, specularStep);
    ramp = lerp(ramp, _MetallicHotSpotColor.rgb, hotSpotStep);

    return ramp;
}

float3 CalculateHighQualityToonLighting(float3 normalWS, float3 worldPos, Light mainLight, float3 albedo)
{
    float NdotL = dot(normalWS, mainLight.direction) * 0.5 + 0.5;
    float3 mainLightContribution = GetToonRamp(NdotL, albedo) * mainLight.color * mainLight.shadowAttenuation;
    
    float3 additionalLightContribution = 0.0;
    #ifdef _ADDITIONAL_LIGHTS
        uint lightCount = GetAdditionalLightsCount();
        for (uint i = 0u; i < lightCount; ++i)
        {
            Light additionalLight = GetAdditionalLight(i, worldPos);
            float addNdotL = dot(normalWS, additionalLight.direction) * 0.5 + 0.5;
            additionalLightContribution += GetToonRamp(addNdotL, albedo) * additionalLight.color * additionalLight.shadowAttenuation * additionalLight.distanceAttenuation;
        }
    #endif

    return mainLightContribution + additionalLightContribution;
}

float3 CalculateHighQualityMetallicLighting(float3 normalWS, float3 viewDir, float3 worldPos, Light mainLight)
{
    float3 halfVec = SafeNormalize(viewDir + mainLight.direction);
    float NdotH = saturate(dot(normalWS, halfVec));

    float3 ramp = GetMetallicRamp(NdotH);
    float3 lighting = ramp * mainLight.color * mainLight.shadowAttenuation;

    float NdotV = saturate(dot(normalWS, viewDir));
    float3 rim = pow(1.0 - NdotV, _RimPower) * _RimColor.rgb;
    lighting += rim;
    
    return lighting;
}

float3 CalculateFoliageLighting(float3 normalWS, float3 worldPos, Light mainLight)
{
    float NdotL = dot(normalWS, mainLight.direction) * 0.5 + 0.5;
    float3 lambert = mainLight.color * NdotL;

    float3 backLightDir = -mainLight.direction;
    float backNdotL = dot(normalWS, backLightDir) * 0.5 + 0.5;
    float3 translucency = pow(backNdotL, 2) * mainLight.color * _TranslucencyStrength * _TranslucencyColor;
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

void ApplyWind(inout float3 positionOS, float4 vertexColor)
{
    float3 worldPos = TransformObjectToWorld(positionOS);
    float windPhase = dot(worldPos.xz, float2(0.2, 0.1));
    float windSine = MU_FastSin(_Time.y * _WindFrequency + windPhase);
    float3 windVector = normalize(_WindDirection) * windSine * _WindAmplitude;
    float windMask = vertexColor.a;
    positionOS.xyz += windVector * windMask;
}

#endif