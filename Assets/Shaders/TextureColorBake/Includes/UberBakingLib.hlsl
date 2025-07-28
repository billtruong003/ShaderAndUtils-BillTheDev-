// Đặt tại: Assets/Shaders/Includes/UberBakingLib.hlsl
#ifndef UBER_BAKING_LIB_HLSL
#define UBER_BAKING_LIB_HLSL

//--- KHAI BÁO BIẾN TOÀN CỤC ---
// Các biến này sẽ được khai báo trong CBUFFER của file .shader
// nhưng chúng ta tham chiếu chúng ở đây.
CBUFFER_START(UnityPerMaterial)
    float _AOIntensity;
    float _AORadius;
CBUFFER_END

TEXTURE2D(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);
float4 _CameraDepthTexture_TexelSize;

//--- STRUCTS ---
struct Attributes
{
    float4 positionOS   : POSITION;
    float2 uv           : TEXCOORD0;
    float3 normalOS     : NORMAL;
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float2 uv           : TEXCOORD0;
    float3 normalWS     : TEXCOORD1;
    float3 normalVS     : TEXCOORD2;
    float3 normalOS     : NORMAL;
};

//--- LOGIC TÍNH TOÁN (HELPER FUNCTIONS) ---

float2 GetScreenUV(float4 positionCS) { return positionCS.xy / positionCS.w; }
float GetDepth(float2 screenUV) { return SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV).r; }

float3 GetNormalFromDepth(float2 screenUV)
{
    float d = GetDepth(screenUV);
    float dx = GetDepth(screenUV + float2(_CameraDepthTexture_TexelSize.x, 0)) - d;
    float dy = GetDepth(screenUV + float2(0, _CameraDepthTexture_TexelSize.y)) - d;
    float3 normalVS = normalize(float3(-dx * _ProjectionParams.x, -dy * _ProjectionParams.y, 1));
    return normalVS;
}

float GetCurvatureFromNormal(float3 normalOS)
{
    float3 ddx_normal = ddx(normalOS);
    float3 ddy_normal = ddy(normalOS);
    float curvature = sqrt(dot(ddx_normal, ddx_normal) + dot(ddy_normal, ddy_normal));
    return saturate(curvature * 50.0);
}

float GetAO(float2 screenUV)
{
    float totalAO = 0.0;
    float centerDepth = GetDepth(screenUV);
    for (float angle = 0.0; angle < TWO_PI; angle += TWO_PI / 16.0)
    {
        float2 offset = float2(cos(angle), sin(angle)) * _AORadius;
        float sampleDepth = GetDepth(screenUV + offset);
        totalAO += step(0.001, centerDepth - sampleDepth);
    }
    return 1.0 - saturate((totalAO / 16.0) * _AOIntensity);
}

float3 GetBentNormal(float2 screenUV, float3 originalNormalVS)
{
    float3 bentNormal = float3(0, 0, 0);
    int validSamples = 0;
    float centerDepth = GetDepth(screenUV);
    for (float angle = 0.0; angle < TWO_PI; angle += TWO_PI / 8.0)
    {
        for(float r = 0.1; r < 1.0; r += 0.3)
        {
            float2 offsetDir = float2(cos(angle), sin(angle));
            float2 sampleUV = screenUV + offsetDir * _AORadius * r;
            float sampleDepth = GetDepth(sampleUV);
            if (sampleDepth >= centerDepth - 0.001)
            {
                float3 viewDir = float3(offsetDir * _AORadius * r, sampleDepth - centerDepth);
                bentNormal += normalize(viewDir);
                validSamples++;
            }
        }
    }
    if (validSamples > 0) {
        return normalize(lerp(originalNormalVS, normalize(bentNormal), 0.5));
    }
    return originalNormalVS;
}

#endif // UBER_BAKING_LIB_HLSL