#ifndef BILLTHEEV_VAT_LIBRARY_INCLUDED
#define BILLTHEEV_VAT_LIBRARY_INCLUDED

TEXTURE2D(_PositionTexture);
SAMPLER(sampler_PositionTexture);
TEXTURE2D(_NormalTexture);
SAMPLER(sampler_NormalTexture);

// Các biến này được khai báo toàn cục. URP sẽ tự động đưa chúng vào CBUFFER UnityPerMaterial.
// Đây là cách làm đúng để tránh lỗi "Duplicate constant buffer".
float4 _PositionMin;
float4 _PositionMax;

UNITY_INSTANCING_BUFFER_START(VATProps)
    UNITY_DEFINE_INSTANCED_PROP(float, _CurrentAnimNormalizedTime)
    UNITY_DEFINE_INSTANCED_PROP(float, _PreviousAnimNormalizedTime)
    UNITY_DEFINE_INSTANCED_PROP(float, _AnimationBlendWeight)
UNITY_INSTANCING_BUFFER_END(VATProps)

float3 slerp_robust(float3 a, float3 b, float t)
{
    float dot_val = dot(a, b);
    dot_val = clamp(dot_val, -1.0, 1.0);
    if (abs(dot_val) > 0.9995)
    {
        return normalize(lerp(a, b, t));
    }
    float theta_0 = acos(dot_val);
    return (sin((1.0 - t) * theta_0) * a + sin(t * theta_0) * b) / sin(theta_0);
}

float3 DecodePosition(float u, float v)
{
    float3 encodedPos = SAMPLE_TEXTURE2D_LOD(_PositionTexture, sampler_PositionTexture, float2(u, v), 0).xyz;
    return lerp(_PositionMin.xyz, _PositionMax.xyz, encodedPos);
}

float3 DecodeNormal(float u, float v)
{
    float3 encodedNormal = SAMPLE_TEXTURE2D_LOD(_NormalTexture, sampler_NormalTexture, float2(u, v), 0).xyz;
    return normalize(encodedNormal * 2.0 - 1.0);
}

void ApplyVAT(inout float3 positionOS, inout float3 normalOS, float2 vertexIdUV)
{
    float u = vertexIdUV.x;
    float currentV = UNITY_ACCESS_INSTANCED_PROP(VATProps, _CurrentAnimNormalizedTime);
    float blendW = UNITY_ACCESS_INSTANCED_PROP(VATProps, _AnimationBlendWeight);

    float3 decodedPos = DecodePosition(u, currentV);
    float3 decodedNorm = DecodeNormal(u, currentV);

    if (blendW > 0.001)
    {
        float previousV = UNITY_ACCESS_INSTANCED_PROP(VATProps, _PreviousAnimNormalizedTime);
        decodedPos = lerp(DecodePosition(u, previousV), decodedPos, blendW);
        decodedNorm = slerp_robust(DecodeNormal(u, previousV), decodedNorm, blendW);
    }

    positionOS = decodedPos;
    normalOS = decodedNorm;
}

#endif