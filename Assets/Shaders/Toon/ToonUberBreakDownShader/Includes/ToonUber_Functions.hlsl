#ifndef BILLS_TOON_FUNCTIONS_INCLUDED
#define BILLS_TOON_FUNCTIONS_INCLUDED

#include "Assets/Shaders/Toon/ToonUberBreakDownShader/Includes/MathUtils.hlsl"

#if defined(_OUTLINEGLINT_ON)
float CalculateGlintFactor(float3 worldPos)
{
    float noiseTime = _Time.y * _GlintSpeed;
    float2 noiseUV = worldPos.xy * _GlintScale * 0.1;
    noiseUV.y += noiseTime;

    float noise = MU_SimplexNoise(noiseUV) * 0.5 + 0.5;
    float glint = smoothstep(_GlintThreshold, _GlintThreshold + 0.05, noise);
    
    return glint;
}
#endif

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

half3 CalculateGlassLighting(Varyings i, Light mainLight, float3 viewDir, half3 ambient)
{
    float2 screenUV = i.screenPos.xy / i.screenPos.w;
    float2 distortion = i.normalWS.xy * _RefractionStrength;
    float3 sceneColor = SAMPLE_TEXTURE2D_X_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + distortion, 0).rgb;

    float fresnelDot = 1.0 - saturate(dot(i.normalWS, viewDir));
    float fresnel = MU_FastPow(fresnelDot, _FresnelPower);
                
    float3 reflectDir = reflect(-mainLight.direction, i.normalWS);
    float spec = MU_FastPow(saturate(dot(viewDir, reflectDir)), _GlassSpecularPower);
    half3 specularColor = mainLight.color * spec * _GlassSpecularIntensity * mainLight.shadowAttenuation;
    
    half3 tintedScene = sceneColor * _GlassColor.rgb;
    half3 finalColor = lerp(tintedScene, _FresnelColor.rgb, fresnel);
    finalColor += specularColor + ambient;

    return finalColor;
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