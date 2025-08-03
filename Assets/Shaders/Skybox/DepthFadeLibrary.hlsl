#ifndef URP_DEPTH_FADE_LIBRARY_INCLUDED
#define URP_DEPTH_FADE_LIBRARY_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

// Các biến này phải được khai báo trong tệp HLSL
// và được định nghĩa trong khối Properties của shader chính để có thể chỉnh sửa trong Inspector.
float _FadeStartDistance;
float _FadeEndDistance;
float _FadeExponent;

// Tính toán hệ số fade dựa trên khoảng cách tuyến tính từ camera (Linear Eye Depth).
// Nhanh và hiệu quả cho hầu hết các trường hợp.
// Yêu cầu: screenPosition từ vertex shader.
float ComputeLinearEyeDepthFade(float4 screenPosition)
{
    float rawSceneDepth = SampleSceneDepth(screenPosition.xy / screenPosition.w);
    float linearSceneDepth = LinearEyeDepth(rawSceneDepth, _ZBufferParams);

    float fadeFactor = saturate((linearSceneDepth - _FadeStartDistance) / (_FadeEndDistance - _FadeStartDistance));
    
    return pow(fadeFactor, _FadeExponent);
}

// Tính toán hệ số fade dựa trên khoảng cách thực tế trong không gian thế giới (World Space).
// Chính xác hơn nhưng yêu cầu tính toán nhiều hơn một chút.
// Yêu cầu: worldPosition từ vertex shader.
float ComputeWorldSpaceDistanceFade(float3 worldPosition)
{
    float sceneDistance = distance(GetCameraPositionWS(), worldPosition);

    float fadeFactor = saturate((sceneDistance - _FadeStartDistance) / (_FadeEndDistance - _FadeStartDistance));

    return pow(fadeFactor, _FadeExponent);
}

#endif // URP_DEPTH_FADE_LIBRARY_INCLUDED