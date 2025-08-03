#ifndef BAKE_AO_CG_INCLUDED
#define BAKE_AO_CG_INCLUDED

float2 BakeAO_FilterAOTextureUV(float2 uv0, float2 uv1, float2 uv2, float2 uv3, float aoTextureUV)
{
    [flatten]
    if (aoTextureUV == 0)
        return uv0;
    else if (aoTextureUV == 1)
        return uv1;
    else if (aoTextureUV == 2)
        return uv2;
    else
        return uv3;
}

float2 BakeAO_FilterAOTextureUV(float2 uv0, float2 uv1, float2 uv2, float aoTextureUV)
{
    [flatten]
    if (aoTextureUV == 0)
        return uv0;
    else if (aoTextureUV == 1)
        return uv1;
    else
        return uv2;
}

float2 BakeAO_FilterAOTextureUV(float2 uv0, float2 uv1, float aoTextureUV)
{
    [flatten]
    if (aoTextureUV == 0)
        return uv0;
    else
        return uv1;
}

float4 BakeAO_ModifyBaseColor(float4 baseColor, float occlusion, float multiplyAlbedoAndOcclusion)
{
    if (multiplyAlbedoAndOcclusion > 0.5)
        return baseColor * occlusion;
    else
        return baseColor;
}

float4 BakeAO_ApplyOcclusionStrength(float occlusion, float occlusionStrength)
{
    return saturate(lerp(1.0, occlusion, occlusionStrength));
}

#endif