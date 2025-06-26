#ifndef DEPTH_FADE_LOGIC_INCLUDED
#define DEPTH_FADE_LOGIC_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

float CalculateDepthFade(float4 positionCS, float4 screenPos, float fadeDistance)
{
    float thisPixelDepth = positionCS.w;
    float2 screenUV = screenPos.xy / screenPos.w;
    float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
    float depthDifference = sceneDepth - thisPixelDepth;
    float fadeFactor = saturate(depthDifference / (fadeDistance + 0.001));
    return fadeFactor;
}

#endif // DEPTH_FADE_LOGIC_INCLUDED