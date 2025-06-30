Shader "Custom/Unlit Fog Plane URP (With Noise)"
{
    Properties
    {
        [MainColor] _FogColor ("Fog Color", Color) = (0.5, 0.5, 0.5, 1)
        _FogDensity ("Fog Density", Range(0.0, 5.0)) = 1.0

        // --- Các thuộc tính mới cho hiệu ứng Noise ---
        [Header(Noise Settings)]
        _NoiseTex ("Noise Texture (Seamless)", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Float) = 1.0
        _NoiseSpeedX ("Noise Scroll Speed X", Float) = 0.1
        _NoiseSpeedY ("Noise Scroll Speed Y", Float) = 0.1
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            // --- Khai báo các biến mới cho Noise ---
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _FogColor;
                float _FogDensity;
                // --- Khai báo các biến mới cho Noise ---
                float4 _NoiseTex_ST; // Dùng cho Tiling/Offset của texture
                float _NoiseScale;
                float _NoiseSpeedX;
                float _NoiseSpeedY;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                // --- Thêm vị trí thế giới để tính UV cho noise ---
                float3 worldPos     : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                // --- Tính toán vị trí thế giới của đỉnh ---
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // --- Phần tính toán độ sâu sương mù (giữ nguyên) ---
                float planeDepth = IN.positionCS.w;
                float2 screenUV = IN.positionCS.xy / _ScreenParams.xy;
                float rawSceneDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV).r;
                float sceneDepth = LinearEyeDepth(rawSceneDepth, _ZBufferParams);
                float depthDifference = sceneDepth - planeDepth;

                // --- Phần tính toán Noise ---
                // 1. Tạo UV cho noise từ vị trí thế giới (dùng xz cho mặt phẳng nằm ngang)
                float2 noiseUV = IN.worldPos.xz * _NoiseScale;

                // 2. Làm cho noise di chuyển theo thời gian
                float2 noiseSpeed = float2(_NoiseSpeedX, _NoiseSpeedY);
                noiseUV += _Time.y * noiseSpeed;

                // 3. Lấy giá trị từ noise texture (chỉ cần kênh R là đủ)
                float noiseValue = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;

                // --- Điều chỉnh hệ số sương mù với Noise ---
                // Nhân hệ số sương mù gốc với giá trị noise
                // Kết quả là sương mù sẽ dày hơn ở những vùng noise sáng và ngược lại
                float fogFactor = saturate(depthDifference * _FogDensity * noiseValue);

                // --- Kết hợp màu sắc (giữ nguyên) ---
                half3 finalColor = _FogColor.rgb;
                half finalAlpha = _FogColor.a * fogFactor;

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}