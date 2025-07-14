#ifndef BILLS_TOON_CORE_INCLUDED
#define BILLS_TOON_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _BaseColor;
    float  _Cutoff;

    float4 _EmissionColor;
    
    float4 _FakeLightColor;
    float3 _FakeLightDirection;

    float  _ToonRampOffset;
    float  _ToonRampSmoothness;
    float4 _ShadowTint;

    float  _Brightness;
    float  _Offset;
    float  _HighlightOffset;
    float  _RimPower;
    float4 _SpecuColor;
    float4 _HiColor;
    float4 _RimColor;

    float4 _GlassColor;
    float4 _FresnelColor;
    float  _FresnelPower;
    float  _RefractionStrength;
    float  _GlassSpecularPower;
    float  _GlassSpecularIntensity;

    float  _WindFrequency;
    float  _WindAmplitude;
    float3 _WindDirection;
    float3 _TranslucencyColor;
    float  _TranslucencyStrength;

    float4 _FresnelOutlineColor;
    float  _FresnelOutlineWidth;
    float  _FresnelOutlinePower;

    float4 _OutlineColor;
    float  _OutlineWidth;
    float  _DistanceFadeStart;
    float  _DistanceFadeEnd;
CBUFFER_END

TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
TEXTURE2D(_EmissionMap);    SAMPLER(sampler_EmissionMap);
TEXTURE2D(_Ramp);           SAMPLER(sampler_Ramp);
TEXTURE2D_X_FLOAT(_CameraOpaqueTexture); SAMPLER(sampler_CameraOpaqueTexture);

#include "Assets/Shaders/Toon/ToonUberBreakDownShader/Includes/ToonUber_Functions.hlsl"

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

half3 ApplyFresnelOutline(half3 surfaceColor, float3 normalWS, float3 viewDir)
{
    #if defined(_OUTLINEMODE_FRESNEL)
        float fresnelDot = 1.0 - saturate(dot(normalWS, viewDir));
        float outlineFactor = FastPow(fresnelDot, _FresnelOutlinePower);
        float outlineEdge = FastSmoothstep(1.0 - _FresnelOutlineWidth, 1.0 - _FresnelOutlineWidth + 0.05, outlineFactor);
        surfaceColor = lerp(surfaceColor, _FresnelOutlineColor.rgb, outlineEdge);
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