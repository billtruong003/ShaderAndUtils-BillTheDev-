#ifndef URP_VAT_TOON_INSTANCED_CORE_INCLUDED
#define URP_VAT_TOON_INSTANCED_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct Attributes {
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 uv           : TEXCOORD0;
    float2 vertexIdUV   : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings {
    float2 uv           : TEXCOORD0;
    float3 worldNormal  : TEXCOORD1;
    float3 worldTangent : TEXCOORD2;
    float3 worldBitangent : TEXCOORD3;
    float3 worldPosition : TEXCOORD4;
    float4 positionCS   : SV_POSITION;
};

TEXTURE2D(_PositionTexture);    SAMPLER(sampler_PositionTexture);
TEXTURE2D(_MainTex);            SAMPLER(sampler_MainTex);
TEXTURE2D(_BumpMap);            SAMPLER(sampler_BumpMap);

CBUFFER_START(UnityPerMaterial)
float4 _PositionMin;
float4 _PositionMax;
half4 _BaseColor;
half4 _HighlightColor;
half4 _MidtoneColor;
half4 _ShadowColor;
half _HighlightThreshold;
half _MidtoneThreshold;
half _Smoothness;
float3 _FakeLightDirection;
half _LightIntensity;
half4 _RimColor;
half _RimPower;
half _RimThreshold;
CBUFFER_END

UNITY_INSTANCING_BUFFER_START(Props)
UNITY_DEFINE_INSTANCED_PROP(float4, _AnimationData)
UNITY_INSTANCING_BUFFER_END(Props)

float3 DecodeLocalPosition(float vertexU, float timeV) {
    float4 encodedPosition = SAMPLE_TEXTURE2D_LOD(_PositionTexture, sampler_PositionTexture, float2(vertexU, timeV), 0);
    return lerp(_PositionMin.xyz, _PositionMax.xyz, encodedPosition.xyz);
}

Varyings vert (Attributes v) {
    Varyings o;
    UNITY_SETUP_INSTANCE_ID(v);

    float vertexU = v.vertexIdUV.x;

    float4 animData = UNITY_ACCESS_INSTANCED_PROP(Props, _AnimationData);
    float currentAnimV = animData.x;
    float previousAnimV = animData.y;
    float blendWeight = animData.z;

    float3 localPosition = DecodeLocalPosition(vertexU, currentAnimV);

    if (blendWeight > 0.001) {
        float3 previousLocalPosition = DecodeLocalPosition(vertexU, previousAnimV);
        localPosition = lerp(previousLocalPosition, localPosition, blendWeight);
    }

    o.worldPosition = TransformObjectToWorld(localPosition);
    o.worldNormal = TransformObjectToWorldNormal(v.normalOS);
    o.worldTangent = TransformObjectToWorldDir(v.tangentOS.xyz);
    o.worldBitangent = cross(o.worldNormal, o.worldTangent) * v.tangentOS.w * GetOddNegativeScale();

    o.positionCS = TransformWorldToHClip(o.worldPosition);
    o.uv = v.uv;

    return o;
}

half4 frag (Varyings i) : SV_Target {
    half4 albedoMap = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    half4 albedo = albedoMap * _BaseColor;

    float3 tangentNormal = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv));
    float3x3 tbn = float3x3(normalize(i.worldTangent), normalize(i.worldBitangent), normalize(i.worldNormal));
    half3 worldNormal = normalize(mul(tangentNormal, tbn));

    half3 lightDirection = normalize(_FakeLightDirection.xyz);
    half NdotL = saturate(dot(worldNormal, lightDirection));

    half smoothnessFactor = _Smoothness * 0.5;

    half midtoneRamp = smoothstep(_MidtoneThreshold - smoothnessFactor, _MidtoneThreshold + smoothnessFactor, NdotL);
    half highlightRamp = smoothstep(_HighlightThreshold - smoothnessFactor, _HighlightThreshold + smoothnessFactor, NdotL);

    half3 rampColor = lerp(_ShadowColor.rgb, _MidtoneColor.rgb, midtoneRamp);
    rampColor = lerp(rampColor, _HighlightColor.rgb, highlightRamp);

    half3 finalColor = rampColor * albedo.rgb * _LightIntensity;

#if _RIM_LIGHT_ON
    half3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.worldPosition.xyz);
    half rimDot = 1.0 - saturate(dot(viewDirection, worldNormal));
    half rimFactor = smoothstep(_RimThreshold - 0.01, _RimThreshold + 0.01, rimDot);
    half rim = pow(rimDot, _RimPower) * rimFactor;
    finalColor += _RimColor.rgb * rim;
#endif

    return half4(finalColor, albedo.a);
}

#endif