#ifndef TOON_MASTER_CORE_INCLUDED
#define TOON_MASTER_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// STRUCTS
struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 uv           : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float3 positionWS   : TEXCOORD0;
    float3 normalWS     : TEXCOORD1;
    float3 tangentWS    : TEXCOORD2;
    float3 bitangentWS  : TEXCOORD3;
    float2 uv           : TEXCOORD4;
};

struct VaryingsOutline { float4 positionCS : SV_POSITION; };
struct VaryingsShadow { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

// TEXTURES & SAMPLERS
TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
TEXTURE2D(_LightRamp);      SAMPLER(sampler_LightRamp);
TEXTURE2D(_EmissionMap);    SAMPLER(sampler_EmissionMap);
TEXTURE2D(_MatCapTexture);  SAMPLER(sampler_MatCapTexture);
TEXTURE2D(_HatchingTexture);SAMPLER(sampler_HatchingTexture);

// CBUFFER
CBUFFER_START(UnityPerMaterial)
    // Base
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half _Cutoff;
    // Lighting
    half4 _ShadowTint;
    half4 _AmbientColor;
    // Specular
    half4 _SpecularColor;
    half _SpecularStrength;
    half _SpecularToonSize;
    half _SpecularToonThreshold;
    half _SpecularSoftness;
    half _AnisotropicOffset;
    // Rim
    half4 _RimColor;
    half _RimPower;
    half _RimThreshold;
    half _RimMaskedByLight;
    // MatCap
    half _MatCapIntensity;
    // Hatching
    half _HatchingTiling;
    half4 _HatchingColor;
    half _HatchingShadowThreshold;
    // Outline
    half4 _OutlineColor;
    half _OutlineWidth;
    half _OutlineNoiseFrequency;
    half _OutlineNoiseAmplitude;
    half4 _FresnelOutlineColor;
    half _FresnelOutlineWidth;
    half _FresnelOutlinePower;
    // Effects
    half4 _EmissionColor;
    half4 _InteriorGlowColor;
    half _InteriorGlowPower;
    // Transparency
    half _Opacity;
CBUFFER_END

#endif