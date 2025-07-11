#ifndef TOON_UBER_LIGHTING_INCLUDED
#define TOON_UBER_LIGHTING_INCLUDED

#include "Assets\Shaders\Toon\ToonUberBreakDownShader\Includes\ToonUberCore.hlsl"

float3 CalculateToonLighting(float3 normalWS, float3 worldPos, Light mainLight)
{
    float NdotL = dot(normalWS, mainLight.direction) * 0.5 + 0.5;
    float toonRamp = smoothstep(_ToonRampOffset, _ToonRampOffset + _ToonRampSmoothness, NdotL);
    float3 mainLightContribution = mainLight.color * lerp(_ShadowTint.rgb, 1.0, toonRamp) * mainLight.shadowAttenuation;
    
    float3 additionalLightContribution = float3(0.0, 0.0, 0.0);
    #ifdef _ADDITIONAL_LIGHTS
        uint lightCount = GetAdditionalLightsCount();
        for (uint i = 0u; i < lightCount; ++i)
        {
            Light additionalLight = GetAdditionalLight(i, worldPos);
            float dAdd = dot(normalWS, additionalLight.direction) * 0.5 + 0.5;
            float toonRampAdd = smoothstep(_ToonRampOffset, _ToonRampOffset + _ToonRampSmoothness, dAdd);
            additionalLightContribution += additionalLight.color * lerp(_ShadowTint.rgb, 1.0, toonRampAdd) * additionalLight.distanceAttenuation * additionalLight.shadowAttenuation;
        }
    #endif

    return mainLightContribution + additionalLightContribution;
}

float3 CalculateMetallicLighting(float3 normalWS, float3 viewDir, Light mainLight)
{
    float NdotL = dot(normalWS, mainLight.direction) * 0.5 + 0.5;
    half3 ramp = SAMPLE_TEXTURE2D(_Ramp, sampler_Ramp, float2(NdotL, NdotL)).rgb;
    float3 diffuse = mainLight.color * ramp * (mainLight.shadowAttenuation * 2.0);

    float3 halfVec = SafeNormalize(viewDir + mainLight.direction);
    float specDot = saturate(dot(halfVec, normalWS));
    float3 specular = step(_Offset, specDot) * _SpecuColor.rgb * _Brightness * mainLight.color * mainLight.shadowAttenuation;
    
    float highlightDot = saturate(dot(normalWS, mainLight.direction));
    float3 highlight = step(_HighlightOffset, highlightDot) * _HiColor.rgb * mainLight.color * mainLight.shadowAttenuation;
    
    float rimDot = 1.0 - saturate(dot(viewDir, normalWS));
    float3 rim = _RimColor.rgb * pow(rimDot, _RimPower);
    
    return diffuse + specular + highlight + rim;
}

#endif