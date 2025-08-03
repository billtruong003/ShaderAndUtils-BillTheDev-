#ifndef UNIVERSAL_UNLIT_INPUT_INCLUDED
#define UNIVERSAL_UNLIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half _Cutoff;
    half _Surface;
    // BakeAO add begin
    int _AOTextureUV;
    int _MultiplyAlbedoAndOcclusion;
    float _OcclusionStrength;
    // BakeAO add end
    UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END

#ifdef UNITY_DOTS_INSTANCING_ENABLED
UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DOTS_INSTANCED_PROP(float , _Cutoff)
    UNITY_DOTS_INSTANCED_PROP(float , _Surface)
    // BakeAO add begin
    UNITY_DOTS_INSTANCED_PROP(int, _AOTextureUV)
    UNITY_DOTS_INSTANCED_PROP(int, _MultiplyAlbedoAndOcclusion)
    UNITY_DOTS_INSTANCED_PROP(float, _OcclusionStrength)
    // BakeAO add end
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

static float4 unity_DOTS_Sampled_BaseColor;
static float  unity_DOTS_Sampled_Cutoff;
static float  unity_DOTS_Sampled_Surface;
// BakeAO add begin
static float  unity_DOTS_Sampled_AOTextureUV;
static float  unity_DOTS_Sampled_MultiplyAlbedoAndOcclusion;
static float  unity_DOTS_Sampled_OcclusionStrength;
// BakeAO add end

void SetupDOTSUnlitMaterialPropertyCaches()
{
    unity_DOTS_Sampled_BaseColor     = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor);
    unity_DOTS_Sampled_Cutoff        = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Cutoff);
    unity_DOTS_Sampled_Surface       = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Surface);
    // BakeAO add begin
    unity_DOTS_Sampled_AOTextureUV = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AOTextureUV);
    unity_DOTS_Sampled_MultiplyAlbedoAndOcclusion = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MultiplyAlbedoAndOcclusion);
    unity_DOTS_Sampled_OcclusionStrength = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _OcclusionStrength);
    // BakeAO add end
}

#undef UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES
#define UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES() SetupDOTSUnlitMaterialPropertyCaches()

#define _BaseColor          unity_DOTS_Sampled_BaseColor
#define _Cutoff             unity_DOTS_Sampled_Cutoff
#define _Surface            unity_DOTS_Sampled_Surface
// BakeAO add begin
#define _AOTextureUV            unity_DOTS_Sampled_AOTextureUV
#define _MultiplyAlbedoAndOcclusion            unity_DOTS_Sampled_MultiplyAlbedoAndOcclusion
#define _OcclusionStrength            unity_DOTS_Sampled_OcclusionStrength
// BakeAO add end
#endif
// BakeAO add begin
TEXTURE2D(_OcclusionMap);
SAMPLER(sampler_OcclusionMap);
// BakeAO add end

#endif
