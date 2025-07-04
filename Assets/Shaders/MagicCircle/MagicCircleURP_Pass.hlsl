#ifndef MAGIC_CIRCLE_UBER_PASS_INCLUDED
#define MAGIC_CIRCLE_UBER_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

CBUFFER_START(UnityPerMaterial)
    // Base
    float4 _Ring1Tex_ST; float4 _Ring1Color; float _Ring1Speed;
    float4 _Ring2Tex_ST; float4 _Ring2Color; float _Ring2Speed;
    float4 _EmissionColor; float _Activation;
    float _PulseSpeed; float _PulseStrength;
    // Sci-Fi
    float _ScanlineDensity; float _ScanlineSpeed;
    float _GlitchStrength; float _GlitchFrequency;
    // Corrupted
    float4 _DistortionTex_ST; float _DistortionStrength; float _DistortionSpeed;
    // Nature
    float _BreatheSpeed; float _BreatheAmount;
CBUFFER_END

TEXTURE2D(_Ring1Tex);       SAMPLER(sampler_Ring1Tex);
TEXTURE2D(_Ring2Tex);       SAMPLER(sampler_Ring2Tex);
TEXTURE2D(_DistortionTex);  SAMPLER(sampler_DistortionTex);

struct Attributes
{
    float4 positionOS   : POSITION;
    float2 uv           : TEXCOORD0;
};

struct Varyings
{
    float4 positionHCS  : SV_POSITION;
    float2 uv           : TEXCOORD0;
};

float2 RotateUV(float2 uv, float2 center, float angle)
{
    uv -= center;
    float s = sin(angle); float c = cos(angle);
    uv = mul(uv, float2x2(c, -s, s, c));
    uv += center;
    return uv;
}

Varyings vert(Attributes i)
{
    Varyings o;
    o.positionHCS = TransformObjectToHClip(i.positionOS.xyz);
    o.uv = TRANSFORM_TEX(i.uv, _Ring1Tex);
    return o;
}

float4 frag(Varyings i) : SV_Target
{
    float2 center = float2(0.5, 0.5);
    float time = _Time.y;
    float2 uv = i.uv;

    // --- NATURE: Hiệu ứng 'Thở' (thay đổi kích thước UV) ---
    #if defined(ENABLE_BREATHING)
        float breatheFactor = 1.0 + (sin(time * _BreatheSpeed) * _BreatheAmount);
        uv = (uv - center) * breatheFactor + center;
    #endif

    // --- CORRUPTED: Hiệu ứng biến dạng (thay đổi UV bằng noise) ---
    #if defined(ENABLE_DISTORTION)
        float2 distortionUV = uv + float2(time * _DistortionSpeed, 0);
        float2 distortionOffset = (SAMPLE_TEXTURE2D(_DistortionTex, sampler_DistortionTex, distortionUV).rg * 2.0 - 1.0) * _DistortionStrength;
        uv += distortionOffset;
    #endif

    // --- SCI-FI: Hiệu ứng Glitch (thay đổi UV đột ngột) ---
    #if defined(ENABLE_GLITCH)
        float glitch = sin(time * _GlitchFrequency) * cos(time * _GlitchFrequency * 2.7);
        if (glitch > 0.95) // Chỉ glitch khi đạt đỉnh
        {
            uv.x += (step(0.5, frac(time * 10)) - 0.5) * 2 * _GlitchStrength;
        }
    #endif

    // --- Chuyển động xoay cơ bản ---
    float angle1 = time * _Ring1Speed;
    float angle2 = time * _Ring2Speed;
    float2 rotatedUV1 = RotateUV(uv, center, angle1);
    float2 rotatedUV2 = RotateUV(uv, center, angle2);

    // --- Lấy mẫu & kết hợp texture ---
    float4 ring1Color = SAMPLE_TEXTURE2D(_Ring1Tex, sampler_Ring1Tex, rotatedUV1) * _Ring1Color;
    float4 ring2Color = SAMPLE_TEXTURE2D(_Ring2Tex, sampler_Ring2Tex, rotatedUV2) * _Ring2Color;
    float3 combinedColor = ring1Color.rgb + ring2Color.rgb;
    float combinedAlpha = max(ring1Color.a, ring2Color.a);
    
    // --- Các hiệu ứng mặt nạ ---
    float dist = distance(uv, center);
    float edgeFade = 1.0 - smoothstep(0.45, 0.5, dist);
    
    float2 dir = uv - center;
    float angle = atan2(dir.y, dir.x);
    float anglePercent = (angle / (2 * PI)) + 0.5;
    float activationMask = step(anglePercent, _Activation);

    // --- Rung động & Phát sáng ---
    float pulse = (sin(time * _PulseSpeed) * 0.5 + 0.5) * _PulseStrength + 1.0;
    float3 finalColor = combinedColor * _EmissionColor.rgb * pulse;

    // --- SCI-FI: Hiệu ứng Dòng quét (Scanlines) ---
    #if defined(ENABLE_SCANLINES)
        float scanline = sin((uv.y + time * _ScanlineSpeed) * _ScanlineDensity) * 0.5 + 0.5;
        scanline = pow(scanline, 4.0); // Làm cho dòng quét mỏng và sắc nét hơn
        finalColor *= scanline;
    #endif

    // --- Tổng hợp kết quả ---
    float finalAlpha = combinedAlpha * activationMask * edgeFade;

    return float4(finalColor, finalAlpha);
}

#endif