#ifndef BILLS_TOON_CORE_HQ_INCLUDED
#define BILLS_TOON_CORE_HQ_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 uv           : TEXCOORD0;
    float4 color        : COLOR;
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float3 positionWS   : TEXCOORD0;
    float3 normalWS     : TEXCOORD1;
    float2 uv           : TEXCOORD2;
    float4 color        : COLOR;
};

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _BaseColor;
    float  _Cutoff;
    float4 _EmissionColor;
    float4 _FakeLightColor;
    float3 _FakeLightDirection;

    // Standard Toon
    float  _HighlightThreshold;
    float  _MidtoneThreshold;
    float  _ShadowThreshold;
    float  _RampSmoothness;
    float4 _HighlightColor;
    float4 _MidtoneColor;
    float4 _ShadowColor;

    // Metallic Toon
    float  _MetallicHotSpotThreshold;
    float  _MetallicSpecularThreshold;
    float  _MetallicReflectionThreshold;
    float  _MetallicRampSmoothness;
    float4 _MetallicBaseColor;
    float4 _MetallicReflectionColor;
    float4 _MetallicSpecularColor;
    float4 _MetallicHotSpotColor;
    float  _RimPower;
    float4 _RimColor;

    // Foliage
    float  _WindFrequency;
    float  _WindAmplitude;
    float3 _WindDirection;
    float3 _TranslucencyColor;
    float  _TranslucencyStrength;

    // Outline
    float4 _FresnelOutlineColor;
    float  _FresnelOutlineWidth;
    float  _FresnelOutlinePower;
    float  _FresnelOutlineSharpness;
    float4 _GlintColor;
    float  _GlintScale;
    float  _GlintSpeed;
    float  _GlintThreshold;
    
    // Advanced
    float4 _AmbientColor;
CBUFFER_END

TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
TEXTURE2D(_EmissionMap);    SAMPLER(sampler_EmissionMap);

#include "ToonUber_FunctionsHighQuality.hlsl"

void ApplyAlphaClip(float2 uv)
{
    #if defined(_ALPHACLIP_ON)
        half albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).a * _BaseColor.a;
        clip(albedoAlpha - _Cutoff);
    #endif
}

half3 ApplyEmission(half3 surfaceColor, float2 uv)
{
    #if defined(_EMISSION_ON)
        surfaceColor += SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb * _EmissionColor.rgb;
    #endif
    return surfaceColor;
}

half3 ApplyFresnelOutline(half3 surfaceColor, float3 normalWS, float3 viewDir, float3 worldPos)
{
    #if defined(_OUTLINEMODE_FRESNEL)
        float fresnelDot = dot(normalWS, viewDir);
        float fresnelTerm = 1.0 - saturate(fresnelDot);
        float fresnelPower = pow(fresnelTerm, _FresnelOutlinePower);
        
        float screenSpaceDerivative = fwidth(fresnelPower);
        float edgeWidth = screenSpaceDerivative * _FresnelOutlineSharpness;
        
        float outlineFactor = smoothstep(1.0 - _FresnelOutlineWidth - edgeWidth, 1.0 - _FresnelOutlineWidth, fresnelPower);
        
        half3 finalOutlineColor = _FresnelOutlineColor.rgb;

        #if defined(_OUTLINEGLINT_ON)
            float glintFactor = CalculateGlintFactor(worldPos);
            finalOutlineColor = lerp(finalOutlineColor, _GlintColor.rgb, glintFactor);
        #endif

        surfaceColor = lerp(surfaceColor, finalOutlineColor, outlineFactor);
    #endif
    return surfaceColor;
}

Light GetEffectiveMainLight(float3 positionWS)
{
    Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
    
    #if defined(_FAKELIGHT_ON)
        bool hasRealLight = dot(mainLight.color, mainLight.color) > 0.001;
        if (!hasRealLight)
        {
            mainLight.direction = normalize(_FakeLightDirection.xyz);
            mainLight.color = _FakeLightColor.rgb;
            mainLight.shadowAttenuation = 1.0;
        }
    #endif
    return mainLight;
}

#endif