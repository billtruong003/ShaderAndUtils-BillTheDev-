#ifndef TOON_SHADING_INCLUDED
#define TOON_SHADING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

float3 CalculateToonLighting(
    in float3 normalWS, 
    in float toonRampSmoothness, 
    in float4 clipPos, 
    in float3 worldPos, 
    in float4 shadowTint,
    in float toonRampOffset, 
    in Light mainLight)
{
    float d = dot(normalWS, mainLight.direction) * 0.5 + 0.5;
    float toonRamp = smoothstep(toonRampOffset, toonRampOffset + toonRampSmoothness, d);
    toonRamp *= mainLight.shadowAttenuation;
    float3 mainLightContribution = mainLight.color * (toonRamp + shadowTint.rgb);

    float3 additionalLightContribution = float3(0.0, 0.0, 0.0);
    #ifdef _ADDITIONAL_LIGHTS
        uint lightCount = GetAdditionalLightsCount();
        for (uint i = 0u; i < lightCount; ++i)
        {
            Light additionalLight = GetAdditionalLight(i, worldPos);
            float dAdd = dot(normalWS, additionalLight.direction) * 0.5 + 0.5;
            float toonRampAdd = smoothstep(toonRampOffset, toonRampOffset + toonRampSmoothness, dAdd);
            toonRampAdd *= additionalLight.shadowAttenuation * additionalLight.distanceAttenuation;
            additionalLightContribution += additionalLight.color * (toonRampAdd + shadowTint.rgb);
        }
    #endif

    return mainLightContribution + additionalLightContribution;
}

#endif