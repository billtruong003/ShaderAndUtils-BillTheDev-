Shader "Custom/PortalShader"
{
    Properties
    {
        _MainTex ("Render Texture", 2D) = "white" {}
        _NoiseTex ("Distort Noise Texture", 2D) = "white" {}
        _EdgeNoiseTex ("Edge Noise Texture", 2D) = "white" {}
        _PortalSize ("Portal Size", Range(0,1)) = 0.5
        _Softness ("Softness", Range(0,0.1)) = 0.01
        _EdgeNoiseStrength ("Edge Noise Strength", Range(0,0.1)) = 0.01
        _EdgeNoiseScale ("Edge Noise Scale", Float) = 1.0
        _NoiseDistortStrength ("Noise Distort Strength", Range(0,0.1)) = 0.01
        _DistortNoiseScale ("Distort Noise Scale", Float) = 1.0
        _NoiseAnimSpeed ("Noise Animation Speed", Float) = 0.5
        _NoiseAnimDirection ("Noise Animation Direction", Vector) = (1,0,0,0)
        _GlowColor ("Glow Color", Color) = (1,1,1,1)
        _EdgeGradientColor ("Edge Gradient Color", Color) = (0.5,0.5,1,1)
        _GlowIntensity ("Glow Intensity", Range(0,1)) = 0.5
        _TintColor ("Tint Color", Color) = (1,1,1,1)
        _DepthPatternColor ("Depth Pattern Color", Color) = (0.7,0.9,1,1)
        [Toggle] _MovePatternDepth ("Move Pattern Depth", Float) = 0
        _DepthPatternSpeed ("Depth Pattern Speed", Float) = 1.0
        _DepthPatternNoiseStrength ("Depth Pattern Noise Strength", Range(0,1)) = 0.1
        _DepthPatternDistance ("Depth Pattern Distance", Range(1,50)) = 20.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_EdgeNoiseTex);
            SAMPLER(sampler_EdgeNoiseTex);

            float _PortalSize;
            float _Softness;
            float _EdgeNoiseStrength;
            float _EdgeNoiseScale;
            float _NoiseDistortStrength;
            float _DistortNoiseScale;
            float _NoiseAnimSpeed;
            float2 _NoiseAnimDirection;
            float4 _GlowColor;
            float4 _EdgeGradientColor;
            float _GlowIntensity;
            float4 _TintColor;
            float4 _DepthPatternColor;
            float _MovePatternDepth;
            float _DepthPatternSpeed;
            float _DepthPatternNoiseStrength;
            float _DepthPatternDistance;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float4 frag (Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float2 center = float2(0.5, 0.5);
                float dist = distance(uv, center);

                // Tính toán UV riêng cho edge noise và distort noise
                float2 edgeNoiseUV = uv * _EdgeNoiseScale + _NoiseAnimDirection * _Time.y * _NoiseAnimSpeed;
                float2 distortNoiseUV = uv * _DistortNoiseScale + _NoiseAnimDirection * _Time.y * _NoiseAnimSpeed;

                // Sample noise cho nhiễu động cạnh từ _EdgeNoiseTex
                float edgeNoise = SAMPLE_TEXTURE2D(_EdgeNoiseTex, sampler_EdgeNoiseTex, edgeNoiseUV).r;
                float noisyDist = dist + (edgeNoise - 0.5) * _EdgeNoiseStrength;

                // Tính mask cổng với độ mềm
                float mask = 1.0 - smoothstep(_PortalSize - _Softness, _PortalSize, noisyDist);

                // Tăng cường biến dạng gần rìa cho noise distort từ _NoiseTex
                float noiseX = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, distortNoiseUV + float2(0.1, 0.2)).r - 0.5;
                float noiseY = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, distortNoiseUV + float2(0.3, 0.4)).r - 0.5;
                float distortFactor = smoothstep(_PortalSize - _Softness, _PortalSize, dist);
                float2 noiseDistort = float2(noiseX, noiseY) * _NoiseDistortStrength * (1.0 + distortFactor * 5.0);

                // UV cuối cùng để lấy mẫu render texture
                float2 finalUV = uv + noiseDistort;

                // Lấy mẫu màu cổng từ render texture
                float4 portalColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, finalUV);

                // Áp dụng màu nhuộm
                portalColor *= _TintColor;

                // Tính toán hoa văn chiều sâu cố định theo rìa
                float depthPattern = 0.5 + 0.5 * sin(dist * _DepthPatternDistance);

                // Nếu bật chế độ di chuyển, thêm chuyển động và nhiễu từ _NoiseTex
                if (_MovePatternDepth > 0.5)
                {
                    float2 depthNoiseUV = uv * 2.0 + _Time.y * 0.1;
                    float depthNoise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, depthNoiseUV).r;
                    float movement = _Time.y * _DepthPatternSpeed + depthNoise * _DepthPatternNoiseStrength;
                    depthPattern = 0.5 + 0.5 * sin(dist * _DepthPatternDistance + movement);
                }

                // Áp dụng hoa văn chiều sâu với màu
                portalColor.rgb *= lerp(float3(1,1,1), _DepthPatternColor.rgb, depthPattern);

                // Tính ánh sáng rìa với gradient
                float glow = (1.0 - mask) * mask * 4.0 * _GlowIntensity;
                float4 edgeColor = lerp(_GlowColor, _EdgeGradientColor, dist / _PortalSize);

                // Kết hợp màu cổng và ánh sáng rìa
                float4 finalColor = portalColor * mask + edgeColor * glow;

                // Đặt alpha
                float finalAlpha = min(mask + glow, 1.0);

                return float4(finalColor.rgb, finalAlpha);
            }
            ENDHLSL
        }
    }
}