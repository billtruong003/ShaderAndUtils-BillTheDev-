#ifndef FOLIAGE_INCLUDED
#define FOLIAGE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

void ApplyWind(inout float3 positionOS, float3 normalOS, float4 vertexColor, float windFrequency, float windAmplitude, float3 windDirection)
{
    float3 worldPos = TransformObjectToWorld(positionOS);
    float windPhase = worldPos.x * 0.2 + worldPos.z * 0.1;
    float windSine = sin(_Time.y * windFrequency + windPhase);

    float3 windVector = normalize(windDirection) * windSine * windAmplitude;
    
    // Use vertex normal's length in object space as a mask to keep base of object still
    // This assumes the base vertices have normals pointing up/down (not sideways)
    // or use vertex color alpha as a mask
    float windMask = saturate(length(normalOS.xz)); // A simple mask
    windMask *= vertexColor.a; // Vertex Color Alpha is the best way to control wind influence

    positionOS.xyz += windVector * windMask;
}

float3 CalculateFoliageLighting(
    float3 normalWS,
    float3 worldPos,
    Light mainLight,
    float translucencyStrength,
    float3 translucencyColor)
{
    float NdotL = dot(normalWS, mainLight.direction) * 0.5 + 0.5;
    float3 lambert = mainLight.color * NdotL * mainLight.shadowAttenuation;

    float3 viewDir = SafeNormalize(_WorldSpaceCameraPos.xyz - worldPos);
    float3 halfDir = SafeNormalize(mainLight.direction + viewDir);
    float NdotH = dot(normalWS, halfDir);
    float specular = pow(saturate(NdotH), 32.0);
    float3 specularColor = mainLight.color * specular * mainLight.shadowAttenuation;

    float3 backLightDir = -mainLight.direction;
    float backNdotL = dot(normalWS, backLightDir) * 0.5 + 0.5;
    float3 translucency = pow(backNdotL, 2.0) * mainLight.color * translucencyStrength * translucencyColor;

    float3 additionalLightContribution = float3(0, 0, 0);
    #ifdef _ADDITIONAL_LIGHTS
        uint lightCount = GetAdditionalLightsCount();
        for (uint i = 0u; i < lightCount; ++i)
        {
            Light additionalLight = GetAdditionalLight(i, worldPos);
            float addNdotL = dot(normalWS, additionalLight.direction) * 0.5 + 0.5;
            additionalLightContribution += additionalLight.color * addNdotL * additionalLight.shadowAttenuation * additionalLight.distanceAttenuation;
        }
    #endif

    return lambert + specularColor + translucency + additionalLightContribution;
}

#endif