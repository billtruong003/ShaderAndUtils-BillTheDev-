Shader "Debug/ScopeZoomTest"
{
    Properties
    {
        _Zoom ("Zoom", Range(1, 30)) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Bắt buộc phải có 2 tệp include này cho URP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // Tệp include này định nghĩa SampleCameraOpaqueTexture - SỬA LỖI TẠI ĐÂY
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float _Zoom;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Logic zoom cốt lõi
                float2 centeredUV = IN.uv * 2.0 - 1.0;
                float2 zoomedUV = (centeredUV / _Zoom) * 0.5 + 0.5;

                // Kiểm tra xem có đang ở trong màn hình không
                if (zoomedUV.x < 0 || zoomedUV.x > 1 || zoomedUV.y < 0 || zoomedUV.y > 1)
                {
                    return half4(0, 0, 0, 0.5); // Vẽ màu đen ở viền ngoài
                }
                
                half3 sceneColor = 0;
                
                // Hiển thị màu báo lỗi nếu Opaque Texture không được bật
                #if defined(REQUIRES_OPAQUE_TEXTURE)
                    sceneColor = SampleCameraOpaqueTexture(zoomedUV).rgb;
                #else
                    sceneColor = half3(1, 0, 1); // Màu tím hồng
                #endif
                
                // Tạo một mask tròn mềm mại để trông giống ống ngắm
                half circleMask = 1.0 - saturate(length(centeredUV) * 1.01);

                return half4(sceneColor, circleMask);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}