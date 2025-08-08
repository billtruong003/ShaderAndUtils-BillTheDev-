#ifndef UBER_MASTER_CORE_INCLUDED
#define UBER_MASTER_CORE_INCLUDED

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
    float3 positionOS       : TEXCOORD6;
};

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _SecondaryAlbedoMap_ST;
    float4 _NoiseMap_ST;
    half4  _BaseColor;
    half4  _AmbientColor;
    float  _CullMode;
    half   _Cutoff;

    int    _SwapShape;
    int    _SwapType;
    float3 _SwapEffectCenter;
    float3 _SwapEffectExtents;
    float  _SwapEffectRadius;
    float4 _SwapLineColor;
    float2 _SwapNoiseScrollSpeed;
    float  _SwapProgress;
    float  _SwapLineWidth;
    float  _SwapNoiseScale;
    float  _SwapNoiseStrength;
    float  _SwapTransitionHardness;
    float4 _SwapDirection;
    int    _SwapPatternType;
    float  _SwapPatternFrequency;

    float4 _DissolveNoiseTex_ST;
    float  _DissolveNoiseScale;
    half   _DissolveThreshold;
    half   _DissolveEdgeWidth;
    half4  _DissolveEdgeColor;
    half   _DissolveNoiseStrength;
    float  _TimeScale;
    float  _UseTimeAnimation;
    int    _DissolveType;
    float4 _DissolveDirection;
    float  _RadialDirection;
    int    _PatternType;
    float  _PatternFrequency;
    half   _AlphaFadeRange;
    float  _VertexDisplacement;
    float  _BounceWaveWidth;
    float  _UseSaturateDisplacement;
    float  _ShatterStrength;
    float  _ShatterLiftSpeed;
    float  _ShatterOffsetStrength;
    float  _ShatterTriggerRange;

    half   _ToonRampOffset;
    half   _ToonRampSmoothness;
    half4  _ShadowTint;
    half4  _Toon_SpecColor;
    half   _Toon_SpecSmoothness;
    half   _Toon_SpecOffset;
    half4  _Toon_RimColor;
    half   _Toon_RimPower;
    half   _Toon_RimMin;
    half   _Toon_RimMax;

    half   _Metal_Brightness;
    half   _Metal_Offset;
    half4  _Metal_SpecuColor;
    half   _Metal_HighlightOffset;
    half4  _Metal_HiColor;
    half4  _Metal_RimColor;
    half   _Metal_RimPower;

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

float3 _SwapWorldPosition;

TEXTURE2D(_BaseMap);                SAMPLER(sampler_BaseMap);
TEXTURE2D(_SecondaryAlbedoMap);     SAMPLER(sampler_SecondaryAlbedoMap);
TEXTURE2D(_SwapNoiseMap);           SAMPLER(sampler_SwapNoiseMap);
TEXTURE2D(_SwapShapeMask);          SAMPLER(sampler_SwapShapeMask);
TEXTURE2D(_DissolveNoiseTex);       SAMPLER(sampler_DissolveNoiseTex);
TEXTURE2D(_Metal_Ramp);             SAMPLER(sampler_Metal_Ramp);

#include "UberMaster_Functions.hlsl"

#endif