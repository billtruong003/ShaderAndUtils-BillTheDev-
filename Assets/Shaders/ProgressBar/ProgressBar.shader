Shader "Custom/ProgressBarURP_Fixed"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ColorStart ("Start Color", Color) = (1, 0, 0, 1)
        _ColorEnd ("End Color", Color) = (0, 1, 0, 1)
        _InsideTex ("Inside (RGBA)", 2D) = "white" {}
        _OutsideTex ("Outside (RGBA)", 2D) = "black" {} // Thường là hình dạng của thanh progress rỗng
        _Progress ("Progress", Range(0.0, 1.0)) = 0.5
        _GlowColor ("Glow Color", Color) = (1, 1, 1, 1)
        _GlowIntensity ("Glow Intensity", Range(0.0, 2.0)) = 1.0
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Outline Width", Range(0.0, 0.5)) = 0.02 // Range đến 0.5 để không bị lật
        _OutlineHeight ("Outline Height", Range(0.0, 0.5)) = 0.02
        _SmoothEdge ("Smooth Edge", Range(0.0, 0.1)) = 0.01

        [IntRange] _Stencil ("Stencil ID", Range(0, 255)) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comparison", Float) = 8 // Equal
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            Stencil
            {
                Ref [_Stencil]
                Comp [_StencilComp]
                Pass Keep
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorStart, _ColorEnd;
                float _Progress;
                float4 _GlowColor;
                float _GlowIntensity;
                float4 _OutlineColor;
                float _OutlineWidth, _OutlineHeight;
                float _SmoothEdge;
            CBUFFER_END

            TEXTURE2D(_InsideTex);      SAMPLER(sampler_InsideTex);
            TEXTURE2D(_OutsideTex);     SAMPLER(sampler_OutsideTex);
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionHCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Lấy texture bên ngoài (khung) và bên trong (phần tô)
                half4 outside = SAMPLE_TEXTURE2D(_OutsideTex, sampler_OutsideTex, IN.uv);
                half4 inside = SAMPLE_TEXTURE2D(_InsideTex, sampler_InsideTex, IN.uv);

                // =======================================================================
                // SỬA LỖI LOGIC: Đảo ngược tham số của smoothstep
                // =======================================================================
                // Giờ đây, fill = 1 khi IN.uv.x < _Progress, và fill = 0 khi IN.uv.x > _Progress
                // Điều này tạo ra hiệu ứng fill từ trái qua phải.
                half fill = smoothstep(_Progress + _SmoothEdge, _Progress - _SmoothEdge, IN.uv.x);
                
                // Gradient màu từ _ColorStart đến _ColorEnd
                half4 progressColor = lerp(_ColorStart, _ColorEnd, IN.uv.x);

                // Áp dụng màu gradient và mask của texture "inside"
                half4 filledPart = progressColor * inside;

                // Kết hợp phần nền (outside) và phần đã tô (filledPart) dựa trên giá trị fill
                half4 finalColor = lerp(outside, filledPart, fill);
                
                // Alpha của màu cuối cùng được quyết định bởi alpha của texture nền và phần đã tô
                finalColor.a = lerp(outside.a, inside.a, fill);
                
                // =======================================================================
                // SỬA LỖI LOGIC: Outline và Glow áp dụng cho toàn bộ thanh progress
                // =======================================================================

                // Tính toán viền (outline) dựa trên UV, độc lập với _Progress
                // smoothstep tạo ra viền mượt hơn
                half outlineX = 1.0 - smoothstep(_OutlineWidth, _OutlineWidth + 0.01, IN.uv.x) + smoothstep(1.0 - _OutlineWidth - 0.01, 1.0 - _OutlineWidth, IN.uv.x);
                half outlineY = 1.0 - smoothstep(_OutlineHeight, _OutlineHeight + 0.01, IN.uv.y) + smoothstep(1.0 - _OutlineHeight - 0.01, 1.0 - _OutlineHeight, IN.uv.y);
                half outlineMask = saturate(outlineX + outlineY);

                // Áp dụng màu viền. Dùng lerp để pha trộn thay vì cộng để tránh màu bị cháy.
                finalColor.rgb = lerp(finalColor.rgb, _OutlineColor.rgb, outlineMask * _OutlineColor.a);
                finalColor.a = max(finalColor.a, outlineMask * _OutlineColor.a);

                // Áp dụng hiệu ứng glow cho phần đã fill
                finalColor.rgb += _GlowColor.rgb * _GlowIntensity * fill;

                // Loại bỏ pixel nếu alpha quá thấp để tương thích với UI Mask
                clip(finalColor.a - 0.01);

                return finalColor;
            }
            ENDHLSL
        }
    }
    Fallback Off
}