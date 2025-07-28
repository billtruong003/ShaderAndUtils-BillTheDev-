#ifndef TOON_MASTER_FUNCTIONS_INCLUDED
#define TOON_MASTER_FUNCTIONS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

// LIGHTING MODULE
half3 CalculateMainLighting(float3 normalWS, float3 positionWS, Light mainLight)
{
    half NdotL = saturate(dot(normalWS, mainLight.direction));
    half lightIntensity = mainLight.shadowAttenuation;

    half rampCoord = NdotL * lightIntensity;
    half3 rampColor = SAMPLE_TEXTURE2D(_LightRamp, sampler_LightRamp, float2(rampCoord, 0.5)).rgb;
    
    half3 lightColor = lerp(_ShadowTint.rgb, mainLight.color, saturate(NdotL * 2));
    
    half3 finalLighting = rampColor * lightColor;
    
    #ifdef _ADDITIONAL_LIGHTS
        uint lightCount = GetAdditionalLightsCount();
        for (uint i = 0u; i < lightCount; ++i)
        {
            Light addLight = GetAdditionalLight(i, positionWS);
            half addNdotL = saturate(dot(normalWS, addLight.direction));
            half addRampCoord = addNdotL * addLight.shadowAttenuation * addLight.distanceAttenuation;
            half3 addRampColor = SAMPLE_TEXTURE2D(_LightRamp, sampler_LightRamp, float2(addRampCoord, 0.5)).rgb;
            finalLighting += addRampColor * addLight.color;
        }
    #endif

    return finalLighting;
}

// SPECULAR MODULE
half3 CalculateSpecular(Varyings i, Light mainLight, float3 normalWS, float3 viewDir)
{
    #if _SPECULARMODE_NONE
        return 0;
    #else
        half3 specular = 0;
        float3 lightDir = mainLight.direction;

        #if _SPECULARMODE_HARD_TOON || _SPECULARMODE_SOFT
            float3 halfDir = SafeNormalize(lightDir + viewDir);
            float NdotH = saturate(dot(normalWS, halfDir));
        #endif

        #if _SPECULARMODE_HARD_TOON
            float specMask = smoothstep(_SpecularToonThreshold, _SpecularToonThreshold + _SpecularToonSize, NdotH);
            specular = specMask * _SpecularColor.rgb;
        #elif _SPECULARMODE_SOFT
            specular = pow(NdotH, _SpecularSoftness) * _SpecularColor.rgb;
        #elif _SPECULARMODE_ANISOTROPIC
            float3 tangent = normalize(i.tangentWS);
            float3 bitangent = normalize(i.bitangentWS);
            float3 anisoDir = lightDir - _AnisotropicOffset * tangent;
            float3 halfDirAniso = SafeNormalize(anisoDir + viewDir);
            float anisoDot = dot(tangent, halfDirAniso);
            float anisoSin = sqrt(1.0f - anisoDot * anisoDot);
            float anisoSpec = pow(anisoSin, _SpecularSoftness);
            specular = anisoSpec * _SpecularColor.rgb;
        #endif

        return specular * _SpecularStrength * mainLight.color * mainLight.shadowAttenuation;
    #endif
}

// RIM LIGHT MODULE
half3 CalculateRimLight(float3 normalWS, float3 viewDir, float3 lightDir)
{
    #if defined(_RIMLIGHT_ON)
        float NdotV = 1.0 - saturate(dot(normalWS, viewDir));
        float rimRaw = pow(NdotV, _RimPower);
        
        #if defined(_RIMMASKEDBYLIGHT)
            float NdotL = saturate(dot(normalWS, lightDir) * 0.5 + 0.5);
            rimRaw *= NdotL;
        #endif
        
        float rimMask = smoothstep(_RimThreshold - 0.05, _RimThreshold + 0.05, rimRaw);
        return rimMask * _RimColor.rgb;
    #else
        return 0;
    #endif
}

// MATCAP MODULE
half3 ApplyMatCap(half3 currentColor, float4 positionCS, float3 normalWS, float3 viewDir)
{
    #if defined(_MATCAP_ON)
        float3 viewNormal = mul((float3x3)UNITY_MATRIX_V, normalWS);
        float2 matCapUV = viewNormal.xy * 0.5 + 0.5;
        half3 matCapColor = SAMPLE_TEXTURE2D(_MatCapTexture, sampler_MatCapTexture, matCapUV).rgb * _MatCapIntensity;

        #if _MATCAPBLENDMODE_ADD
            return currentColor + matCapColor;
        #elif _MATCAPBLENDMODE_MULTIPLY
            return currentColor * matCapColor;
        #elif _MATCAPBLENDMODE_LERP
            return lerp(currentColor, matCapColor, _MatCapIntensity);
        #endif
    #endif
    return currentColor;
}

// PAINTERLY HATCHING MODULE
half3 ApplyHatching(half3 currentColor, float4 positionCS, half3 lighting)
{
    #if defined(_HATCHING_ON)
        float2 screenUV = positionCS.xy / positionCS.w;
        float lightLevel = Luminance(lighting);
        
        if(lightLevel < _HatchingShadowThreshold)
        {
             half4 hatchingSample = SAMPLE_TEXTURE2D(_HatchingTexture, sampler_HatchingTexture, screenUV * _HatchingTiling);
             half3 hatchingColor = _HatchingColor.rgb * hatchingSample.rgb;
             return lerp(currentColor, hatchingColor, hatchingSample.a * _HatchingColor.a);
        }
    #endif
    return currentColor;
}

// FRESNEL OUTLINE MODULE
half3 ApplyFresnelOutline(half3 currentColor, float3 normalWS, float3 viewDir)
{
    #if _OUTLINEMODE_FRESNEL
        float NdotV = dot(normalWS, viewDir);
        float fresnel = 1.0 - saturate(NdotV);
        float powerFresnel = pow(fresnel, _FresnelOutlinePower);
        float fresnelEdge = step(_FresnelOutlineWidth, powerFresnel);
        return lerp(currentColor, _FresnelOutlineColor.rgb, fresnelEdge);
    #else
        return currentColor;
    #endif
}

// INTERIOR GLOW MODULE
half3 ApplyInteriorGlow(half3 currentColor, float3 normalWS, float3 viewDir)
{
    #if defined(_INTERIORGLOW_ON)
        float NdotV = saturate(dot(normalWS, viewDir));
        float glow = pow(NdotV, _InteriorGlowPower);
        return currentColor + glow * _InteriorGlowColor.rgb;
    #else
        return currentColor;
    #endif
}

#endif