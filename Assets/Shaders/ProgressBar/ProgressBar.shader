// Đặt tên là "Unlit" để thể hiện nó không bị ảnh hưởng bởi ánh sáng, rất nhẹ.
Shader "Custom/SimpleSpriteProgress"
{
    Properties
    {
        // Sử dụng _MainTex như một sprite thông thường
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        _Progress ("Progress", Range(0, 1)) = 0.5
        _ColorStart ("Start Color (Full)", Color) = (0, 1, 0, 1) // Màu khi đầy (progress=1)
        _ColorEnd ("End Color (Empty)", Color) = (1, 0, 0, 1)   // Màu khi cạn (progress=0)
        _BackgroundColor ("Background Color", Color) = (0.2, 0.2, 0.2, 1)
        _SmoothEdge ("Smooth Edge", Range(0.0, 0.1)) = 0.01

        // Các thuộc tính Stencil để tương thích với UI Mask
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
    }

    SubShader
    {
        // Tags cần thiết cho UI
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            // Stencil block để hoạt động với UI Mask
            Stencil
            {
                Ref [_Stencil]
                Comp [_StencilComp]
                Pass [_StencilOp]
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR; // Lấy màu từ SpriteRenderer/Image
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Properties
            CBUFFER_START(UnityPerMaterial)
                float _Progress;
                float4 _ColorStart;
                float4 _ColorEnd;
                float4 _BackgroundColor;
                float _SmoothEdge;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color; // Truyền màu của component Image/SpriteRenderer
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Lấy alpha từ sprite để tạo hình dạng cho thanh máu
                half spriteAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).a;

                // Tính toán phần trăm fill, đã sửa lỗi logic ngược
                // saturate kẹp giá trị trong khoảng [0, 1]
                half fillAmount = saturate((_Progress - IN.uv.x) / max(_SmoothEdge, 0.0001));

                // Nội suy màu dựa trên progress, không phải vị trí uv.x
                // Điều này cho thanh máu đổi màu toàn bộ khi máu giảm dần.
                half4 fillColor = lerp(_ColorEnd, _ColorStart, _Progress);

                // Chọn màu cuối cùng: là màu fill hay màu nền, dựa trên fillAmount
                half4 finalColor = lerp(_BackgroundColor, fillColor, fillAmount);
                
                // Áp dụng alpha của sprite và màu từ component (để có thể fade out)
                finalColor.a = finalColor.a * spriteAlpha * IN.color.a;

                // Loại bỏ những pixel hoàn toàn trong suốt để tương thích với Mask
                clip(finalColor.a - 0.001);

                return finalColor;
            }
            ENDHLSL
        }
    }
}