#ifndef TOON_VAT_UBER_CORE_INCLUDED
#define TOON_VAT_UBER_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "MathUtils.hlsl"

struct Attributes {
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 uv           : TEXCOORD0;
    float4 color        : COLOR;
    float4 tangentOS    : TANGENT;
#if defined(_VAT_ON)
    float2 vertexIdUV   : TEXCOORD1;
#endif
#if defined(_VAT_INSTANCING_ON)
    UNITY_VERTEX_INPUT_INSTANCE_ID
#endif
};

struct Varyings {
    float4 positionCS   : SV_POSITION;
    float3 positionWS   : TEXCOORD0;
    float3 normalWS     : TEXCOORD1;
    float2 uv           : TEXCOORD2;
    float4 color        : COLOR;
    float4 screenPos    : TEXCOORD3;
    float3 positionOS   : TEXCOORD4;
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
float4 _AmbientColor;
float  _Brightness;
float  _Offset;
float  _HighlightOffset;
float  _RimPower;
float4 _SpecuColor;
float4 _HiColor;
float4 _RimColor;
float  _WindFrequency;
float  _WindAmplitude;
float3 _WindDirection;
float3 _TranslucencyColor;
float  _TranslucencyStrength;
float4 _GlassColor;
float4 _FresnelColor;
float  _FresnelPower;
float  _RefractionStrength;
float  _GlassSpecularPower;
float  _GlassSpecularIntensity;

float4 _OutlineColor;
float  _OutlineWidth;
float  _DistanceFadeStart;
float  _DistanceFadeEnd;

float4 _FresnelRampTexture_ST;
float4 _FresnelOutlineColor;
float  _FresnelOutlineWidth;
float  _FresnelOutlinePower;
float  _FresnelOutlineSharpness;

float4 _GlintColor;
float  _GlintScale;
float  _GlintSpeed;
float  _GlintThreshold;

float4 _PositionMin;
float4 _PositionMax;
float _CurrentAnimNormalizedTime;
float _PreviousAnimNormalizedTime;
float _AnimationBlendWeight;

float4 _StarfieldColor;
float  _StarfieldScale;
float4 _DustColor1;
float4 _DustColor2;
float4 _DustColor3;
float  _NoiseScale;
float  _ParallaxStrength;
float  _NoiseSpeed1;
float  _NoiseSpeed2;
float4 _GalaxyRimColor;
float  _GalaxyRimPower;
CBUFFER_END

TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
TEXTURE2D(_EmissionMap);    SAMPLER(sampler_EmissionMap);
TEXTURE2D(_Ramp);           SAMPLER(sampler_Ramp);
TEXTURE2D(_StarfieldMap);   SAMPLER(sampler_StarfieldMap);
TEXTURE2D(_NoiseMap);       SAMPLER(sampler_NoiseMap); 
TEXTURE2D(_GalaxyRimRampTexture);   SAMPLER(sampler_GalaxyRimRampTexture);
TEXTURE2D(_FresnelRampTexture);     SAMPLER(sampler_FresnelRampTexture);

#ifndef SHADER_PASS_SHADOWCASTER
TEXTURE2D_X_FLOAT(_CameraOpaqueTexture); SAMPLER(sampler_CameraOpaqueTexture);
#endif

#if defined(_VAT_ON)
TEXTURE2D(_PositionTexture); SAMPLER(sampler_PositionTexture);
#endif

#if defined(_VAT_INSTANCING_ON)
UNITY_INSTANCING_BUFFER_START(VATProps)
UNITY_DEFINE_INSTANCED_PROP(float4, _AnimationData)
UNITY_INSTANCING_BUFFER_END(VATProps)
#endif

#include "ToonUber_Functions.hlsl"

#if defined(_VAT_ON)
float3 DecodeVatPosition(float vertexU, float timeV) {
    float4 encodedPos = SAMPLE_TEXTURE2D_LOD(_PositionTexture, sampler_PositionTexture, float2(vertexU, timeV), 0);
    return lerp(_PositionMin.xyz, _PositionMax.xyz, encodedPos.xyz);
}
#endif

float3 GetPositionOS(Attributes input) {
#if defined(_VAT_ON)
#if defined(_VAT_INSTANCING_ON)
    UNITY_SETUP_INSTANCE_ID(input);
    float4 animData = UNITY_ACCESS_INSTANCED_PROP(VATProps, _AnimationData);
    float currentV = animData.x;
    float previousV = animData.y;
    float blendW = animData.z;
#else
    float currentV = _CurrentAnimNormalizedTime;
    float previousV = _PreviousAnimNormalizedTime;
    float blendW = _AnimationBlendWeight;
#endif

    float vertexU = input.vertexIdUV.x;
    float3 currentPos = DecodeVatPosition(vertexU, currentV);

    if (blendW > 0.001) {
        float3 previousPos = DecodeVatPosition(vertexU, previousV);
        return lerp(previousPos, currentPos, blendW);
    }
    return currentPos;
#else
    return input.positionOS.xyz;
#endif
}

void ApplyAlphaClip(float2 uv) {
#if defined(_ALPHACLIP_ON)
    half albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).a * _BaseColor.a;
    clip(albedoAlpha - _Cutoff);
#endif
}

half3 ApplyEmission(half3 surfaceColor, float2 uv) {
#if defined(_EMISSION_ON)
    surfaceColor += SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb * _EmissionColor.rgb;
#endif
    return surfaceColor;
}

half3 ApplyFresnelOutline(half3 surfaceColor, float3 normalWS, float3 viewDir, float3 worldPos) {
#if defined(_OUTLINEMODE_FRESNEL)
    float fresnelDot = dot(normalWS, viewDir);
    float fresnelTerm = 1.0 - saturate(fresnelDot);
    float fresnelPower = MU_FastPow(fresnelTerm, _FresnelOutlinePower);

    float screenSpaceDerivative = fwidth(fresnelPower);
    float edgeWidth = screenSpaceDerivative * _FresnelOutlineSharpness;

    float outlineFactor = smoothstep(1.0 - _FresnelOutlineWidth - edgeWidth, 1.0 - _FresnelOutlineWidth, fresnelPower);

    half3 finalOutlineColor = _FresnelOutlineColor.rgb;
#if defined(_FRESNEL_RAMP_ON)
    half2 rampUV = float2(fresnelTerm, 0.5);
    finalOutlineColor *= SAMPLE_TEXTURE2D(_FresnelRampTexture, sampler_FresnelRampTexture, rampUV).rgb;
#endif

#if defined(_OUTLINEGLINT_ON)
    float noiseTime = _Time.y * _GlintSpeed;
    float2 noiseUV = worldPos.xy * _GlintScale * 0.1;
    noiseUV.y += noiseTime;
    float noise = MU_SimplexNoise(noiseUV) * 0.5 + 0.5;
    float glint = smoothstep(_GlintThreshold, _GlintThreshold + 0.05, noise);
    finalOutlineColor = lerp(finalOutlineColor, _GlintColor.rgb, glint);
#endif

    surfaceColor = lerp(surfaceColor, finalOutlineColor, outlineFactor);
#endif
    return surfaceColor;
}

Light GetEffectiveMainLight(float3 positionWS) {
    Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
#if defined(_FAKELIGHT_ON)
    bool hasRealLight = dot(mainLight.color, mainLight.color) > 0.001;
    if (!hasRealLight) {
        mainLight.direction = normalize(_FakeLightDirection.xyz);
        mainLight.color = _FakeLightColor.rgb;
        mainLight.shadowAttenuation = 1.0;
    }
#endif
    return mainLight;
}

float FBM_FromTexture(float2 uv) {
    float value = 0.0;
    float amplitude = 0.5;

    value += amplitude * SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, uv).r;
    amplitude *= 0.5;

    value += amplitude * SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, uv * 2.0).r;
    amplitude *= 0.5;

    value += amplitude * SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, uv * 4.0).r;
    amplitude *= 0.5;

    value += amplitude * SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, uv * 8.0).r;

    return value;
}

float3 CalculateGalaxyLighting(float3 normalWS, float3 viewDir, float3 worldPos, float3 positionOS) {
    float2 parallaxOffset = viewDir.xy * _ParallaxStrength;

    float2 coordBase = positionOS.xz;

    float2 starUV = coordBase * _StarfieldScale + parallaxOffset * 3.0;
    half3 starfieldColor = SAMPLE_TEXTURE2D(_StarfieldMap, sampler_StarfieldMap, starUV).rgb * _StarfieldColor.rgb;

    float2 dustUV1 = coordBase * _NoiseScale * 0.1;
    dustUV1 += parallaxOffset * 2.0;
    dustUV1.y += _Time.y * _NoiseSpeed1;
    float dustNoise1 = FBM_FromTexture(dustUV1);
    half3 dustColor1 = _DustColor1.rgb * MU_SmootherStep(0.5, 0.7, dustNoise1);

    float2 dustUV2 = coordBase * _NoiseScale * 0.1;
    dustUV2 += float2(0.23, 0.41);
    dustUV2 += parallaxOffset * 1.0;
    dustUV2.y += _Time.y * _NoiseSpeed2;
    float dustNoise2 = FBM_FromTexture(dustUV2);
    half3 dustColor2 = _DustColor2.rgb * MU_SmootherStep(0.4, 0.6, dustNoise2);

    half3 innerUniverseColor = starfieldColor + dustColor1 + dustColor2;

    half NdotV = 1.0 - saturate(dot(normalWS, viewDir));
    half rimFactor = pow(NdotV, _GalaxyRimPower);

    half3 rimColor = _GalaxyRimColor.rgb;
#if defined(_GALAXY_RIM_RAMP_ON)
    rimColor *= SAMPLE_TEXTURE2D(_GalaxyRimRampTexture, sampler_GalaxyRimRampTexture, float2(NdotV, 0.5)).rgb;
#endif

    half3 rimLight = rimFactor * rimColor;

    return innerUniverseColor + rimLight;
}

#endif