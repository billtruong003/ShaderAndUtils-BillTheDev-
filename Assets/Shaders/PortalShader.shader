Shader "Custom/ImprovedPortalShader"
{
    Properties
    {
        // Portal Type Dropdown
        [Enum(Swirl, 0, Fire, 1, Electric, 2, Water, 3, DarkVoid, 4)] _PortalType ("Portal Type", Int) = 0
        
        // Portal Shape Properties
        _PortalSize ("Portal Size", Range(0.1, 1.0)) = 0.4
        _PortalSoftness ("Portal Softness", Float) = 3.0

        // Swirl Properties (Used for Swirl Portal)
        _SwirlSpeed ("Swirl Speed", Float) = 1.0
        _SwirlTightness ("Swirl Tightness", Float) = 5.0
        _SwirlStrength ("Swirl Strength", Range(0.0, 1.0)) = 1.0

        // Wave Properties (Used for Water Portal)
        _WaveSpeed ("Wave Speed", Float) = 1.0
        _WaveFrequency ("Wave Frequency", Float) = 10.0
        _WaveAmplitude ("Wave Amplitude", Float) = 0.05

        // Texture Noise Properties
        [NoScaleOffset] _NoiseTexture ("Noise Texture (Grayscale)", 2D) = "white" {}
        _TextureNoiseScale ("Texture Noise Scale", Float) = 1.0
        _TextureNoiseSpeed ("Texture Noise Speed (XY)", Vector) = (0.1, 0.0, 0,0)
        _TextureNoiseStrength ("Texture Noise Strength (Inner)", Float) = 1.0

        // Edge Noise Properties
        _EdgeNoiseAmplitude ("Edge Noise Amplitude", Range(0.0, 1.0)) = 0.2

        // Color Properties
        [HDR] _MainColor ("Main Color", Color) = (0,0,1,0.7)
        [HDR] _DistortionColor ("Distortion Color", Color) = (0,1,1,0.9)
        [HDR] _GlowColor ("Glow Color", Color) = (1,1,1,1)
        
        // Edge Glow Properties
        _EdgeThickness ("Edge Thickness", Float) = 0.1
        _GlowIntensity ("Glow Intensity", Float) = 5.0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                int _PortalType;
                float _PortalSize;
                float _PortalSoftness;
                
                float _SwirlSpeed;
                float _SwirlTightness;
                float _SwirlStrength;

                float _WaveSpeed;
                float _WaveFrequency;
                float _WaveAmplitude;

                TEXTURE2D(_NoiseTexture); SAMPLER(sampler_NoiseTexture); 
                float _TextureNoiseScale;
                float2 _TextureNoiseSpeed;
                float _TextureNoiseStrength;
                float _EdgeNoiseAmplitude;

                float4 _MainColor;
                float4 _DistortionColor;
                float4 _GlowColor;
                float _EdgeThickness;
                float _GlowIntensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            // Hàm tính toán noiseUV dựa trên loại portal
            float2 ComputeNoiseUV(float2 uv, float time, int portalType)
            {
                float2 centeredUV = uv - float2(0.5, 0.5);
                float distFromCenter = length(centeredUV);
                float2 noiseUV;

                if (portalType == 0) { // Swirl
                    float angle = atan2(centeredUV.y, centeredUV.x);
                    angle += time * _SwirlSpeed + distFromCenter * _SwirlTightness;
                    float2 swirledCenteredUV = float2(cos(angle), sin(angle)) * distFromCenter;
                    noiseUV = lerp(centeredUV, swirledCenteredUV, _SwirlStrength) + float2(0.5, 0.5);
                } else if (portalType == 3) { // Water
                    float wave = sin(time * _WaveSpeed + uv.x * _WaveFrequency);
                    noiseUV = uv + float2(0, wave * _WaveAmplitude);
                } else { // Fire, Electric, DarkVoid
                    noiseUV = uv;
                }
                return noiseUV;
            }

            // Hàm tính toán edgeNoise dựa trên loại portal
            float ComputeEdgeNoise(float sampledNoise, int portalType)
            {
                float edgeNoise;
                if (portalType == 0) { // Swirl
                    edgeNoise = (sampledNoise - 0.5) * _EdgeNoiseAmplitude;
                } else if (portalType == 1) { // Fire
                    float noise = pow(sampledNoise, 2.0);
                    edgeNoise = (noise - 0.5) * _EdgeNoiseAmplitude * 2.0;
                } else if (portalType == 2) { // Electric
                    edgeNoise = step(0.5, sampledNoise) * _EdgeNoiseAmplitude;
                } else if (portalType == 3) { // Water
                    edgeNoise = (sampledNoise - 0.5) * _EdgeNoiseAmplitude * 0.5;
                } else if (portalType == 4) { // DarkVoid
                    edgeNoise = (sampledNoise > 0.7 ? 1.0 : 0.0) * _EdgeNoiseAmplitude;
                } else {
                    edgeNoise = 0.0;
                }
                return edgeNoise;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _Time.y;

                int portalType = (int)_PortalType;

                // Tính toán noiseUV
                float2 noiseUV = ComputeNoiseUV(uv, time, portalType);

                // Lấy mẫu texture noise
                float2 textureSampleUV = noiseUV * _TextureNoiseScale + _TextureNoiseSpeed * time;
                float sampledNoise = SAMPLE_TEXTURE2D(_NoiseTexture, sampler_NoiseTexture, textureSampleUV).r;

                // Tính toán edgeNoise
                float edgeNoise = ComputeEdgeNoise(sampledNoise, portalType);

                // Tính toán mask của portal
                float2 centeredUV = uv - float2(0.5, 0.5);
                float distFromCenter = length(centeredUV);
                float rawMask = distFromCenter / _PortalSize;
                float oneMinusRawMask = 1.0 - rawMask + edgeNoise;
                float portalAlphaMask = pow(saturate(oneMinusRawMask), _PortalSoftness);
                portalAlphaMask = saturate(portalAlphaMask);

                // Kết hợp noise cho phần bên trong
                float finalCombinedNoise = saturate(sampledNoise * _TextureNoiseStrength);

                // Hiệu ứng glow ở cạnh
                float edgeRaw = pow(1.0 - portalAlphaMask, _EdgeThickness);
                float edgeMask = saturate(edgeRaw);
                float4 edgeGlowColor = edgeMask * _GlowColor;

                // Tính toán màu cuối cùng
                float4 baseColor = lerp(_MainColor, _DistortionColor, finalCombinedNoise);
                float4 finalColorPreMask = baseColor + edgeGlowColor;
                float4 finalColor = finalColorPreMask * portalAlphaMask;
                finalColor.rgb *= _GlowIntensity;
                finalColor.a = portalAlphaMask;

                return finalColor;
            }
            ENDHLSL
        }
    }
}