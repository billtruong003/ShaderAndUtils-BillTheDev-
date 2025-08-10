#ifndef SHMACKLE_UBER_DISSOLVE_CORE_INCLUDED
#define SHMACKLE_UBER_DISSOLVE_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;
    float2 uv           : TEXCOORD0;
    float3 normalOS     : NORMAL;
};

struct Varyings
{
    float4 positionCS       : SV_POSITION;
    float2 uv               : TEXCOORD0;
    float3 positionWS       : TEXCOORD1;
    float3 normalWS         : TEXCOORD2;
    float3 viewDirWS        : TEXCOORD3;
    float  dissolveValue    : TEXCOORD4;
    float  perVertexNoise   : TEXCOORD5;
};

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4  _BaseColor;
    float  _CullMode;
    half   _Cutoff;
    half4  _EmissionColor;
    float4 _NoiseTex_ST;
    float  _NoiseScale;
    half   _DissolveThreshold;
    half   _DissolveEdgeWidth;
    half4  _DissolveEdgeColor;
    half   _NoiseStrength;
    float  _TimeScale;
    float  _UseTimeAnimation;
    int    _DissolveType;
    float4 _DissolveDirection;
    float  _RadialDirection;
    int    _PatternType;
    float  _PatternFrequency;
    half   _AlphaFadeRange;
    float  _VertexDisplacement;
    float  _DisplacementWaveWidth;
    float  _ShatterStrength;
    float  _ShatterLiftSpeed;
    float  _ShatterOffsetStrength;
    float  _ShatterTriggerRange;
    half   _ToonRampOffset;
    half   _ToonRampSmoothness;
    half4  _ShadowTint;
    half4 _StudioToon_HighlightColor, _StudioToon_MidtoneColor, _StudioToon_ShadowColor;
    half _StudioToon_HighlightThreshold, _StudioToon_ShadowThreshold, _StudioToon_RampSmoothness;
    half4 _StudioToon_SkyColor, _StudioToon_GroundColor;
    half _StudioToon_AmbientGradientPower;
    half4 _StudioToon_SpecularColor;
    half _StudioToon_SpecularThreshold, _StudioToon_SpecularSmoothness;
    half4 _StudioToon_RimColor;
    half _StudioToon_RimPower, _StudioToon_RimThreshold;
    half4  _Bling_SpecColor;
    half   _Bling_SpecSmoothness;
    half   _Bling_SpecOffset;
    half4  _Bling_RimColor;
    half   _Bling_RimPower;
    half   _Bling_RimMin;
    half   _Bling_RimMax;
    half4  _BlingColor;
    half   _BlingIntensity;
    float  _BlingScale;
    float  _BlingSpeed;
    half   _BlingFresnelPower;
    half   _BlingThreshold;
CBUFFER_END

TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
TEXTURE2D(_EmissionMap);    SAMPLER(sampler_EmissionMap);
TEXTURE2D(_NoiseTex);       SAMPLER(sampler_NoiseTex);

#include "UberDissolve_Functions.hlsl"

#endif