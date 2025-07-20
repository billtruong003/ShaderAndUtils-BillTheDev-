#ifndef BILLS_TOON_BLING_ENHANCED_CORE_INCLUDED
#define BILLS_TOON_BLING_ENHANCED_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Assets/Shaders/Toon/ToonUberBreakDownShader/Includes/ToonBlingMetallic_Functions.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 uv           : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float3 positionWS   : TEXCOORD0;
    half3  normalWS     : TEXCOORD1;
    float2 uv           : TEXCOORD2;
    half3  viewDirWS    : TEXCOORD3;
    UNITY_VERTEX_OUTPUT_STEREO
};

struct Varyings_ShadowPass
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4  _BaseColor;
    half   _Cutoff;

    half4  _EmissionColor;
    
    half   _ToonRampOffset;
    half   _ToonRampSmoothness;
    half4  _ShadowTint;

    half4  _SpecColor;
    half   _SpecSmoothness;
    half   _SpecOffset;

    half4  _RimColor;
    half   _RimPower;
    half   _RimMin;
    half   _RimMax;

    half4  _BlingColor;
    half   _BlingIntensity;
    half   _BlingScale;
    half   _BlingSpeed;
    half   _BlingFresnelPower;
    half   _BlingThreshold;
CBUFFER_END

TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
TEXTURE2D(_EmissionMap);    SAMPLER(sampler_EmissionMap);

Varyings BlingVertex(Attributes v)
{
    Varyings o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    
    o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
    o.positionCS = TransformWorldToHClip(o.positionWS);
    o.normalWS = TransformObjectToWorldNormal(v.normalOS);
    o.viewDirWS = GetWorldSpaceViewDir(o.positionWS);
    o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
    return o;
}

half4 BlingFragment(Varyings i) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    
    half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
    
    #if defined(_ALPHACLIP_ON)
        clip(albedo.a - _Cutoff);
    #endif

    half3 normalWS = normalize(i.normalWS);
    half3 viewDirWS = SafeNormalize(i.viewDirWS);
    
    Light mainLight = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
    
    half3 toonDiffuse = CalculateToonDiffuseContribution(normalWS, mainLight, _ToonRampOffset, _ToonRampSmoothness, _ShadowTint.rgb);
    half3 toonSpecular = CalculateToonSpecularContribution(normalWS, viewDirWS, mainLight, _SpecOffset, _SpecSmoothness, _SpecColor.rgb);
    
    #ifdef _ADDITIONAL_LIGHTS
        uint lightCount = GetAdditionalLightsCount();
        for (uint j = 0u; j < lightCount; ++j)
        {
            Light additionalLight = GetAdditionalLight(j, i.positionWS);
            toonDiffuse += CalculateToonDiffuseContribution(normalWS, additionalLight, _ToonRampOffset, _ToonRampSmoothness, _ShadowTint.rgb);
            toonSpecular += CalculateToonSpecularContribution(normalWS, viewDirWS, additionalLight, _SpecOffset, _SpecSmoothness, _SpecColor.rgb);
        }
    #endif

    half3 rim = CalculateRimLight(normalWS, viewDirWS, _RimColor.rgb, _RimMin, _RimMax, _RimPower);
    half3 bling = CalculateBlingEffect(i.positionCS, i.positionWS, normalWS, viewDirWS, _BlingScale, _BlingSpeed, _BlingThreshold, _BlingColor.rgb, _BlingIntensity, _BlingFresnelPower);
    half3 ambient = SampleSH(normalWS);
    
    half3 litColor = albedo.rgb * (toonDiffuse + ambient);
    half3 finalColor = litColor + toonSpecular + rim + bling;
    
    #if defined(_EMISSION_ON)
        finalColor += SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, i.uv).rgb * _EmissionColor.rgb;
    #endif

    return half4(finalColor, albedo.a);
}

#endif