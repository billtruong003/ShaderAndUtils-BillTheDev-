#ifndef TOON_LIT_STUDIO_CORE_INCLUDED
#define TOON_LIT_STUDIO_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
    float2 uv           : TEXCOORD2;
    half4 shadowCoord   : TEXCOORD3;
    float3 tangentWS    : TEXCOORD4;
    float3 bitangentWS  : TEXCOORD5;
};

CBUFFER_START(UnityPerMaterial)
    sampler2D _BaseMap; float4 _BaseMap_ST;
    sampler2D _BumpMap; float4 _BumpMap_ST;
    half _BumpScale;
    half _Cutoff;

    half4 _HighlightColor, _MidtoneColor, _ShadowColor;
    half _HighlightThreshold, _ShadowThreshold, _RampSmoothness;
    sampler2D _RampMap;

    half4 _FakeLightDirection;
    half4 _CustomShadowColor; half _ShadowTintInfluence;
    
    half4 _SkyColor, _GroundColor; half _AmbientGradientPower;
    
    half _AdditionalLightInfluence;

    sampler2D _HatchingMap; half _HatchingTiling; half _HatchingVisibility;
    sampler2D _MatcapMap; half _MatcapBlendMode; half4 _MatcapTint; half _MatcapIntensity;
    
    half4 _SpecularColor; half _SpecularThreshold, _SpecularSmoothness;
    half4 _RimColor; half _RimPower, _RimThreshold;
CBUFFER_END

void ApplyAlphaClip(float2 uv)
{
    #if _ALPHATEST_ON
        half alpha = tex2D(_BaseMap, uv).a;
        clip(alpha - _Cutoff);
    #endif
}

half3 GetWorldNormal(Varyings input)
{
    half3 normalWS = normalize(input.normalWS);
    #if _NORMALMAP_ON
        half4 packedNormal = tex2D(_BumpMap, input.uv);
        half3 tangentNormal = UnpackNormalScale(packedNormal, _BumpScale);
        half3x3 TBN = half3x3(normalize(input.tangentWS), normalize(input.bitangentWS), normalWS);
        normalWS = normalize(mul(tangentNormal, TBN));
    #endif
    return normalWS;
}

half3 EncodeNormal(half3 normalWS)
{
    return normalWS * 0.5 + 0.5;
}

half3 CalculateToonRamp(half NdotL, half3 lightColor)
{
    #if _RAMP_TEXTURE_ON
        half3 rampColor = tex2D(_RampMap, float2(NdotL, 0.5)).rgb;
    #else
        half smoothness = _RampSmoothness * 0.5;
        half highlightFactor = smoothstep(_HighlightThreshold - smoothness, _HighlightThreshold + smoothness, NdotL);
        half shadowFactor = smoothstep(_ShadowThreshold - smoothness, _ShadowThreshold + smoothness, NdotL);
        half3 rampColor = lerp(_ShadowColor.rgb, _MidtoneColor.rgb, shadowFactor);
        rampColor = lerp(rampColor, _HighlightColor.rgb, highlightFactor);
    #endif
    
    return rampColor * lightColor;
}

Varyings MainVert(Attributes input)
{
    Varyings output;
    VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.positionCS = positionInputs.positionCS;
    output.positionWS = positionInputs.positionWS;
    output.normalWS = normalInputs.normalWS;
    output.tangentWS = normalInputs.tangentWS;
    output.bitangentWS = normalInputs.bitangentWS;
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    output.shadowCoord = GetShadowCoord(positionInputs);
    return output;
}

#endif