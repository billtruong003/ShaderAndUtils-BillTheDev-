#ifndef ADVANCED_TOON_DISSOLVE_CORE_INCLUDED
#define ADVANCED_TOON_DISSOLVE_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

half remap(half value, half from1, half to1, half from2, half to2)
{
    return from2 + (value - from1) * (to2 - from2) / (to1 - from1);
}

struct ToonSurfaceData
{
    half3 albedo;
    half3 normalWS;
    half  alpha;
    half3 emission;
};

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 uv           : TEXCOORD0;
    half4  color        : COLOR;
};

struct Varyings
{
    float4 positionCS      : SV_POSITION;
    float3 positionWS      : TEXCOORD0;
    float3 normalWS        : TEXCOORD1;
    float4 tangentWS       : TEXCOORD2;
    float2 uv              : TEXCOORD3;
    half4  color           : TEXCOORD4;
    float4 shadowCoord     : TEXCOORD5;
};

CBUFFER_START(UnityPerMaterial)
    sampler2D _BaseMap, _BumpMap, _DissolveMap, _SwapAlbedo, _SwapNormalMap;
    float4 _BaseMap_ST, _DissolveVector;
    half _BumpScale, _Cutoff;
    half4 _HighlightColor, _MidtoneColor, _ShadowColor, _DissolveEdgeColor, _RimColor, _SpecularColor;
    half _HighlightThreshold, _ShadowThreshold, _RampSmoothness;
    half _SpecularThreshold, _SpecularSmoothness;
    half _RimPower;
    float _DissolveType;
    half _DissolveProgress, _DissolveEdgeWidth, _DissolveMapTiling, _DissolveEdgeHardness;
    half _SwapProgress;
CBUFFER_END

half3 GetNormalFromMap(sampler2D normalMap, float2 uv, half scale, float3 normalWS, float4 tangentWS)
{
    half4 packedNormal = tex2D(normalMap, uv);
    half3 tangentNormal = UnpackNormalScale(packedNormal, scale);
    half3x3 TBN = half3x3(tangentWS.xyz, cross(normalWS, tangentWS.xyz) * tangentWS.w, normalWS);
    return normalize(mul(tangentNormal, TBN));
}

half GetDissolveStep(Varyings input)
{
    #if defined(_DISSOLVE_ON)
        half dissolveStep = 0;
        #if defined(_DISSOLVE_TYPE_NOISE)
            dissolveStep = tex2D(_DissolveMap, input.uv * _DissolveMapTiling).r;
        #elif defined(_DISSOLVE_TYPE_DIRECTIONAL)
            dissolveStep = dot(input.positionWS, normalize(_DissolveVector.xyz));
            dissolveStep = remap(dissolveStep, -_DissolveVector.w, _DissolveVector.w, 0, 1);
        #elif defined(_DISSOLVE_TYPE_SPHERICAL)
            float dist = distance(input.positionWS, _DissolveVector.xyz);
            dissolveStep = 1.0 - saturate(dist / _DissolveVector.w);
        #elif defined(_DISSOLVE_TYPE_MASK)
            dissolveStep = tex2D(_DissolveMap, input.uv).r;
        #elif defined(_DISSOLVE_TYPE_VERTEX_COLOR)
            dissolveStep = input.color.r;
        #endif
        return dissolveStep;
    #else
        return 1.0;
    #endif
}

void ApplyClipping(Varyings input, half dissolveStep)
{
    #if _ALPHATEST_ON
        half alpha = tex2D(_BaseMap, input.uv).a;
        clip(alpha - _Cutoff);
    #endif

    #if defined(_DISSOLVE_ON)
        clip(dissolveStep - _DissolveProgress);
    #endif
}

ToonSurfaceData GetSurfaceData(Varyings input, half dissolveStep)
{
    ToonSurfaceData surf;
    half4 albedoTex = tex2D(_BaseMap, input.uv);
    surf.alpha = albedoTex.a;
    surf.albedo = albedoTex.rgb;
    surf.emission = 0;

    #if defined(_SWAP_ON)
        half3 swapAlbedo = tex2D(_SwapAlbedo, input.uv).rgb;
        surf.albedo = lerp(surf.albedo, swapAlbedo, _SwapProgress);
    #endif

    half3 baseNormal = GetNormalFromMap(_BumpMap, input.uv, _BumpScale, input.normalWS, input.tangentWS);
    surf.normalWS = baseNormal;
    
    #if defined(_SWAP_NORMAL_ON) && defined(_SWAP_ON)
        half3 swapNormal = GetNormalFromMap(_SwapNormalMap, input.uv, _BumpScale, input.normalWS, input.tangentWS);
        surf.normalWS = normalize(lerp(baseNormal, swapNormal, _SwapProgress));
    #endif

    #if defined(_DISSOLVE_ON)
        half edgeZoneStart = _DissolveProgress;
        half edgeZoneEnd = _DissolveProgress + _DissolveEdgeHardness;
        half dissolveFactor = 1.0 - smoothstep(edgeZoneStart, edgeZoneEnd, dissolveStep);
        
        half edgeFactor = smoothstep(_DissolveProgress - _DissolveEdgeWidth, _DissolveProgress, dissolveStep) -
                          smoothstep(_DissolveProgress, _DissolveProgress + _DissolveEdgeWidth, dissolveStep);
        surf.emission = edgeFactor * _DissolveEdgeColor.rgb * _DissolveEdgeColor.a;
    #endif
    
    return surf;
}

Varyings MainVert(Attributes input)
{
    Varyings o;
    VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
    o.positionWS = posInputs.positionWS;
    o.positionCS = posInputs.positionCS;
    
    VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    o.normalWS = normalInputs.normalWS;
    o.tangentWS = float4(normalInputs.tangentWS, input.tangentOS.w * GetOddNegativeScale());

    o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    o.color = input.color;
    o.shadowCoord = GetShadowCoord(posInputs);
    return o;
}

half4 MainFrag(Varyings input) : SV_TARGET
{
    half dissolveStep = GetDissolveStep(input);
    ApplyClipping(input, dissolveStep);
    
    ToonSurfaceData surf = GetSurfaceData(input, dissolveStep);
    
    half3 normalWS = surf.normalWS;
    float3 viewDirWS = SafeNormalize(GetCameraPositionWS() - input.positionWS);
    
    Light mainLight = GetMainLight(input.shadowCoord);
    half shadowAttenuation = mainLight.shadowAttenuation;
    
    half NdotL = saturate(dot(normalWS, mainLight.direction));
    half smoothness = _RampSmoothness * 0.5;
    half highlightFactor = smoothstep(_HighlightThreshold - smoothness, _HighlightThreshold + smoothness, NdotL);
    half shadowFactor = smoothstep(_ShadowThreshold - smoothness, _ShadowThreshold + smoothness, NdotL);
    
    half3 rampColor = lerp(_ShadowColor.rgb, _MidtoneColor.rgb, shadowFactor);
    rampColor = lerp(rampColor, _HighlightColor.rgb, highlightFactor);
    
    half3 finalColor = surf.albedo * rampColor * mainLight.color;

    #if _SPECULAR_ON
        half3 halfDir = SafeNormalize(mainLight.direction + viewDirWS);
        half NdotH = saturate(dot(normalWS, halfDir));
        half specIntensity = smoothstep(_SpecularThreshold, _SpecularThreshold + _SpecularSmoothness, NdotH);
        finalColor += specIntensity * _SpecularColor.rgb * mainLight.color;
    #endif

    #if _RIM_LIGHT_ON
        half NdotV = 1.0 - saturate(dot(normalWS, viewDirWS));
        half rimFactor = pow(NdotV, _RimPower);
        finalColor += rimFactor * _RimColor.rgb * _RimColor.a;
    #endif
    
    half3 ambient = SampleSH(normalWS);
    finalColor += ambient * surf.albedo;
    finalColor = lerp(finalColor * _ShadowColor.rgb, finalColor, shadowAttenuation);
    finalColor += surf.emission;

    return half4(finalColor, surf.alpha);
}

void ShadowFrag(Varyings input)
{
    half dissolveStep = GetDissolveStep(input);
    ApplyClipping(input, dissolveStep);
}

#endif