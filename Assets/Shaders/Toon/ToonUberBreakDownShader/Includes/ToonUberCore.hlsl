#ifndef TOON_UBER_CORE_INCLUDED
#define TOON_UBER_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// ================== STRUCTS ==================
struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 uv           : TEXCOORD0;
    float4 color        : COLOR;
    float4 tangentOS    : TANGENT;
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float3 positionWS   : TEXCOORD0;
    float3 normalWS     : TEXCOORD1;
    float2 uv           : TEXCOORD2;
    float4 color        : COLOR;
    float4 screenPos    : TEXCOORD3;
};

// ================== CBUFFER ==================
CBUFFER_START(UnityPerMaterial)
    // Shared
    float4 _BaseMap_ST;
    float4 _BaseColor;
    float4 _EmissionColor;
    float4 _FakeLightColor;
    float3 _FakeLightDirection;
    float  _Cutoff;

    // Toon
    float  _ToonRampOffset;
    float  _ToonRampSmoothness;
    float4 _ShadowTint;

    // Metallic
    float  _Brightness;
    float  _Offset;
    float  _HighlightOffset;
    float  _RimPower;
    float4 _SpecuColor;
    float4 _HiColor;
    float4 _RimColor;

    // Transparent (Glass)
    float4 _GlassColor;
    float4 _FresnelColor;
    float  _FresnelPower;
    float  _RefractionStrength;
    float  _GlassSpecularPower;
    float  _GlassSpecularIntensity;

    // Foliage
    float  _WindFrequency;
    float  _WindAmplitude;
    float3 _WindDirection;
    float3 _TranslucencyColor;
    float  _TranslucencyStrength;

    // Fresnel Outline
    float4 _FresnelOutlineColor;
    float  _FresnelOutlineWidth;
    float  _FresnelOutlinePower;

    // Inverted Hull Outline
    float4 _OutlineColor;
    float  _OutlineWidth;
    float  _DistanceFadeStart;
    float  _DistanceFadeEnd;
CBUFFER_END

// ================== TEXTURES ==================
TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
TEXTURE2D(_EmissionMap);    SAMPLER(sampler_EmissionMap);
TEXTURE2D(_Ramp);           SAMPLER(sampler_Ramp);
TEXTURE2D_X_FLOAT(_CameraOpaqueTexture); SAMPLER(sampler_CameraOpaqueTexture);

// ================== HELPER FUNCTIONS ==================
void ApplyAlphaClip(float2 uv)
{
    #if defined(_ALPHACLIP_ON)
        half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
        clip(albedo.a - _Cutoff);
    #endif
}

half3 ApplyEmission(half3 surfaceColor, float2 uv)
{
    #if defined(_EMISSION_ON)
        surfaceColor += SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb * _EmissionColor.rgb;
    #endif
    return surfaceColor;
}

half3 ApplyFresnelOutline(half3 surfaceColor, float3 normalWS, float3 viewDir)
{
    #if defined(_OUTLINEMODE_FRESNEL)
        float fresnelDot = 1.0 - saturate(dot(normalWS, viewDir));
        float fresnelOutline = pow(fresnelDot, _FresnelOutlinePower);
        float outlineFactor = smoothstep(1.0 - _FresnelOutlineWidth, 1.0 - _FresnelOutlineWidth + 0.05, fresnelOutline);
        surfaceColor = lerp(surfaceColor, _FresnelOutlineColor.rgb, outlineFactor);
    #endif
    return surfaceColor;
}

Light GetEffectiveMainLight(float3 positionWS)
{
    Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
    
    #if defined(_FAKELIGHT_ON)
        // A more robust check for a "black" or non-existent light
        if (dot(mainLight.color, mainLight.color) < 0.001) 
        {
            mainLight.direction = normalize(_FakeLightDirection.xyz);
            mainLight.color = _FakeLightColor.rgb;
            mainLight.shadowAttenuation = 1.0;
        }
    #endif
    return mainLight;
}

#endif