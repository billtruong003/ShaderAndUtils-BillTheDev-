#ifndef TOON_AURA_FUNCTIONS_INCLUDED
#define TOON_AURA_FUNCTIONS_INCLUDED

// Include các tiện ích toán học cho các hàm như FastSin, FastPow, v.v.
#include "Assets/Shaders/Toon/ToonUberBreakDownShader/Includes/MathUtils.hlsl"

// Áp dụng hiệu ứng gió dựa trên sóng sin đơn giản vào vị trí các đỉnh.
// Hiệu ứng được che (mask) bằng kênh alpha của màu đỉnh.
void ApplyWind(inout float3 positionOS, float4 vertexColor)
{
    float3 worldPos = TransformObjectToWorld(positionOS);
    float windPhase = dot(worldPos.xz, float2(0.2, 0.1));
    float windSine = MU_FastSin(_Time.y * _WindFrequency + windPhase);
    float3 windVector = normalize(_WindDirection) * windSine * _WindAmplitude;
    float windMask = vertexColor.a; // Sử dụng alpha của màu đỉnh làm mask
    positionOS.xyz += windVector * windMask;
}

// Tính toán ánh sáng theo kiểu ramp hai tông màu cho vẻ ngoài hoạt hình cổ điển.
float3 CalculateToonLighting(float3 normalWS, float3 worldPos, Light mainLight)
{
    float NdotL = dot(normalWS, mainLight.direction) * 0.5 + 0.5;
    float toonRamp = MU_FastSmoothstep(_ToonRampOffset, _ToonRampOffset + _ToonRampSmoothness, NdotL);
    float3 mainLightContribution = mainLight.color * lerp(_ShadowTint.rgb, 1.0, toonRamp) * mainLight.shadowAttenuation;

    float3 additionalLightContribution = 0.0h;
    #ifdef _ADDITIONAL_LIGHTS
        uint lightCount = GetAdditionalLightsCount();
        for (uint i = 0u; i < lightCount; ++i)
        {
            Light additionalLight = GetAdditionalLight(i, worldPos);
            float dAdd = dot(normalWS, additionalLight.direction) * 0.5 + 0.5;
            float toonRampAdd = MU_FastSmoothstep(_ToonRampOffset, _ToonRampOffset + _ToonRampSmoothness, dAdd);
            additionalLightContribution += additionalLight.color * lerp(_ShadowTint.rgb, 1.0, toonRampAdd) * additionalLight.distanceAttenuation * additionalLight.shadowAttenuation;
        }
    #endif

    return mainLightContribution + additionalLightContribution;
}

// Tính toán ánh sáng cho bề mặt kim loại cách điệu.
float3 CalculateMetallicLighting(float3 normalWS, float3 viewDir, Light mainLight)
{
    float3 halfVec = SafeNormalize(viewDir + mainLight.direction);
    float NdotH = saturate(dot(normalWS, halfVec));
    float NdotL = saturate(dot(normalWS, mainLight.direction));
    float NdotV = saturate(dot(normalWS, viewDir));

    half3 rampColor = SAMPLE_TEXTURE2D(_Ramp, sampler_Ramp, half2(NdotL, 0.5h)).rgb;
    half specularRamp = MU_FastSmoothstep(_Offset, _Offset + 0.05, NdotH);
    half highlightRamp = MU_FastSmoothstep(_HighlightOffset, _HighlightOffset + 0.05, NdotH);

    half3 specular = specularRamp * _SpecuColor.rgb;
    half3 highlight = highlightRamp * _HiColor.rgb;
    float3 rim = MU_FastPow(1.0h - NdotV, _RimPower) * _RimColor.rgb;

    float3 lighting = (rampColor + specular + highlight) * _Brightness * mainLight.color * mainLight.shadowAttenuation;
    lighting += rim;

    return lighting;
}

// Tính toán ánh sáng cho cây cỏ, bao gồm hiệu ứng mờ cho ánh sáng ngược.
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

#endif // TOON_AURA_FUNCTIONS_INCLUDED