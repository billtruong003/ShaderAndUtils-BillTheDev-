// File: Assets/Shaders/Includes/ToonShading.hlsl
// (KHÔNG THAY ĐỔI, GIỮ NGUYÊN LOGIC CỦA BẠN)
#ifndef TOON_SHADING_INCLUDED
#define TOON_SHADING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

void ToonShading_float(in float3 Normal, in float ToonRampSmoothness, in float3 ClipSpacePos, in float3 WorldPos, in float4 ToonRampTinting,
in float ToonRampOffset, out float3 ToonRampOutput, out float3 Direction, out float Attenuation)
{
    #ifdef SHADERGRAPH_PREVIEW
        ToonRampOutput = float3(0.5, 0.5, 0.0);
        Direction = float3(0.5, 0.5, 0.0);
        Attenuation = 1.0;
    #else
        #if SHADOWS_SCREEN
            float4 shadowCoord = ComputeScreenPos(ClipSpacePos);
        #else
            float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
        #endif 

        // Main light
        #if _MAIN_LIGHT_SHADOWS_CASCADE || _MAIN_LIGHT_SHADOWS
            Light light = GetMainLight(shadowCoord);
        #else
            Light light = GetMainLight();
        #endif

        float d = dot(Normal, light.direction) * 0.5 + 0.5;
        float toonRamp = smoothstep(ToonRampOffset, ToonRampOffset + ToonRampSmoothness, d);
        toonRamp *= light.shadowAttenuation;
        float3 mainLightContribution = light.color * (toonRamp + ToonRampTinting.rgb);
        Direction = light.direction;
        Attenuation = light.shadowAttenuation;

        // Additional lights
        float3 additionalLightContribution = float3(0.0, 0.0, 0.0);
        #ifdef _ADDITIONAL_LIGHTS
            uint lightCount = GetAdditionalLightsCount();
            for (uint i = 0u; i < lightCount; ++i)
            {
                Light additionalLight = GetAdditionalLight(i, WorldPos);
                float dAdd = dot(Normal, additionalLight.direction) * 0.5 + 0.5;
                float toonRampAdd = smoothstep(ToonRampOffset, ToonRampOffset + ToonRampSmoothness, dAdd);
                toonRampAdd *= additionalLight.shadowAttenuation * additionalLight.distanceAttenuation;
                additionalLightContribution += additionalLight.color * (toonRampAdd + ToonRampTinting.rgb);
            }
        #endif

        // Combine contributions
        ToonRampOutput = mainLightContribution + additionalLightContribution;
    #endif
}

// Hàm Dither không liên quan, có thể giữ lại
void Dither(float2 screenPosition, float threshold)
{
    float2 uv = screenPosition.xy / _ScreenParams.xy;
    float dither = InterleavedGradientNoise(uv * _ScreenParams.xy, 0);
    clip(threshold - dither);
}


#endif