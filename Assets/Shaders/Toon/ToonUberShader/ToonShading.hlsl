#ifndef TOON_SHADING_INCLUDED
#define TOON_SHADING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

float3 CalculateToonLighting(
    in float3 normalWS,
    in float toonRampSmoothness,
    in float3 worldPos,
    in float4 shadowTint,
    in float toonRampOffset,
    in Light mainLight)
{
    float NdotL = dot(normalWS, mainLight.direction) * 0.5 + 0.5;
    float toonRamp = smoothstep(toonRampOffset, toonRampOffset + toonRampSmoothness, NdotL);
    toonRamp *= mainLight.shadowAttenuation;
    float3 mainLightContribution = mainLight.color * lerp(shadowTint.rgb, 1.0, toonRamp);

    float3 additionalLightContribution = float3(0.0, 0.0, 0.0);
    #ifdef _ADDITIONAL_LIGHTS
        uint lightCount = GetAdditionalLightsCount();
        for (uint i = 0u; i < lightCount; ++i)
        {
            Light additionalLight = GetAdditionalLight(i, worldPos);
            float dAdd = dot(normalWS, additionalLight.direction) * 0.5 + 0.5;
            float toonRampAdd = smoothstep(toonRampOffset, toonRampOffset + toonRampSmoothness, dAdd);
            toonRampAdd *= additionalLight.shadowAttenuation * additionalLight.distanceAttenuation;
            additionalLightContribution += additionalLight.color * lerp(shadowTint.rgb, 1.0, toonRampAdd);
        }
    #endif

    return mainLightContribution + additionalLightContribution;
}

#endif