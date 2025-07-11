#ifndef TOON_UBER_FOLIAGE_INCLUDED
#define TOON_UBER_FOLIAGE_INCLUDED

#include "Assets/Shaders/Toon/ToonUberBreakDownShader/Includes/ToonUberCore.hlsl"

void ApplyWind(inout float3 positionOS, float4 vertexColor)
{
    float3 worldPos = TransformObjectToWorld(positionOS);
    float windPhase = dot(worldPos.xz, float2(0.2, 0.1));
    float windSine = sin(_Time.y * _WindFrequency + windPhase);

    float3 windVector = normalize(_WindDirection) * windSine * _WindAmplitude;
    
    // Vertex Color Alpha is the best and most explicit way to control wind influence.
    float windMask = vertexColor.a;

    positionOS.xyz += windVector * windMask;
}

float3 CalculateFoliageLighting(float3 normalWS, float3 worldPos, Light mainLight)
{
    float NdotL = dot(normalWS, mainLight.direction) * 0.5 + 0.5;
    float3 lambert = mainLight.color * NdotL;

    float3 viewDir = SafeNormalize(_WorldSpaceCameraPos.xyz - worldPos);
    float3 backLightDir = -mainLight.direction;
    float backNdotL = dot(normalWS, backLightDir) * 0.5 + 0.5;
    float3 translucency = pow(backNdotL, 2.0) * mainLight.color * _TranslucencyStrength * _TranslucencyColor;

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