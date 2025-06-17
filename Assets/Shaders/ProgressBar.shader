Shader "Custom/ProgressBarURP"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ColorStart ("Start Color", Color) = (1, 0, 0, 1) // Màu bắt đầu (đỏ)
        _ColorEnd ("End Color", Color) = (0, 1, 0, 1) // Màu kết thúc (xanh)
        _InsideTex ("Inside (RGBA)", 2D) = "white" {}
        _OutsideTex ("Outside (RGBA)", 2D) = "black" {}
        _Progress ("Progress", Range(0.0, 1.0)) = 0.5
        _GlowColor ("Glow Color", Color) = (1, 1, 1, 1) // Màu phát sáng
        _GlowIntensity ("Glow Intensity", Range(0.0, 2.0)) = 1.0 // Cường độ glow
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1) // Màu viền
        _OutlineWidth ("Outline Width", Range(0.0, 0.1)) = 0.02 // Chiều rộng viền (trục X)
        _OutlineHeight ("Outline Height", Range(0.0, 0.1)) = 0.02 // Chiều cao viền (trục Y)
        _SmoothEdge ("Smooth Edge", Range(0.0, 0.1)) = 0.02 // Độ mượt của cạnh
        [Toggle]_UseURP ("Use URP", Float) = 1 // Chuyển đổi URP

        // Stencil properties cho UI Mask
        [IntRange] _Stencil ("Stencil ID", Range(0, 255)) = 1 // Mặc định là 1 cho UI Mask
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comparison", Float) = 8 // Equal
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilOp ("Stencil Operation", Float) = 0 // Keep
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    CBUFFER_START(UnityPerMaterial)
        float4 _ColorStart;
        float4 _ColorEnd;
        float _Progress;
        float4 _GlowColor;
        float _GlowIntensity;
        float4 _OutlineColor;
        float _OutlineWidth;
        float _OutlineHeight;
        float _SmoothEdge;
        float _UseURP;
        float _Stencil;
        float _StencilComp;
        float _StencilOp;
    CBUFFER_END

    TEXTURE2D(_InsideTex);
    SAMPLER(sampler_InsideTex);
    TEXTURE2D(_OutsideTex);
    SAMPLER(sampler_OutsideTex);

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
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            // Stencil settings để tương thích với UI Mask
            Stencil
            {
                Ref [_Stencil]
                Comp Equal // Chỉ vẽ khi stencil khớp với mask
                Pass Keep
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

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
                // Lấy texture bên trong và bên ngoài
                half4 inside = SAMPLE_TEXTURE2D(_InsideTex, sampler_InsideTex, IN.uv);
                half4 outside = SAMPLE_TEXTURE2D(_OutsideTex, sampler_OutsideTex, IN.uv);

                // Gradient màu từ _ColorStart đến _ColorEnd
                half4 progressColor = lerp(_ColorStart, _ColorEnd, IN.uv.x);

                // Áp dụng màu gradient cho phần đã fill
                inside *= progressColor;

                // Tính toán phần đã fill với hiệu ứng mượt
                half fill = smoothstep(_Progress - _SmoothEdge, _Progress + _SmoothEdge, IN.uv.x);
                inside.a *= fill;

                // Kết hợp texture bên ngoài và bên trong
                half4 c = (1.0 - outside.a) * inside + outside * outside.a;

                // Hiệu ứng glow cho phần đã fill, giới hạn bởi fill
                half glow = fill * _GlowIntensity;
                c.rgb += _GlowColor.rgb * glow * fill; // Chỉ glow trong vùng fill

                // Tính toán viền phát sáng (outline glow) dựa trên Width và Height
                half outline = 0.0;
                if (IN.uv.x < _OutlineWidth || IN.uv.x > (1.0 - _OutlineWidth) ||
                    IN.uv.y < _OutlineHeight || IN.uv.y > (1.0 - _OutlineHeight))
                {
                    outline = 1.0;
                }
                c.rgb += _OutlineColor.rgb * outline * fill; // Chỉ outline trong vùng fill
                c.a += outline * _OutlineColor.a * fill; // Giới hạn alpha của outline

                // Loại bỏ pixel nếu alpha không đủ (tôn trọng mask)
                clip(c.a - 0.01); // Loại bỏ nếu alpha gần 0

                return c;
            }
            ENDHLSL
        }
    }
    Fallback Off
}