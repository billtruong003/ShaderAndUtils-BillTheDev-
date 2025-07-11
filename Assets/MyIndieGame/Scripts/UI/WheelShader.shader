Shader "MyShaders/UI/URP_SelectionWheel_Donut"
{
    Properties
    {
        [Header(Wheel Settings)]
        _Segments("Segments", Int) = 8
        _InnerRadius("Inner Radius", Range(0, 10)) = 0.3 // Bán kính của lỗ hổng ở giữa (0 -> 0.5)
        _BorderWidth("Border Width", Range(0, 10)) = 0.005 // Độ rộng viền (đơn vị UV)

        [Header(Colors)]
        _Color1("Color 1", Color) = (1, 1, 1, 0.5)
        _Color2("Color 2", Color) = (0.8, 0.8, 0.8, 0.5)
        _BorderColor("Border Color", Color) = (0, 0, 0, 1)
        _HighlightColor("Highlight Color", Color) = (1, 0.8, 0, 1)

        [Header(Selection Effect)]
        _HighlightScale("Highlight Scale", Range(1.0, 1.5)) = 1.1

        [HideInInspector] _SelectedSegment("Selected Segment", Int) = -1

        // Thuộc tính UI bắt buộc
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalRenderPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
            Stencil { Ref[_Stencil] Comp[_StencilComp] Pass[_StencilOp] ReadMask[_StencilReadMask] WriteMask[_StencilWriteMask] }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define PI 3.14159265359
            #define TWO_PI 6.28318530718

            int _Segments;
            float _InnerRadius, _BorderWidth;
            half4 _Color1, _Color2, _BorderColor, _HighlightColor;
            float _HighlightScale;
            int _SelectedSegment;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            v2f vert(appdata v) { v2f o; o.vertex = TransformObjectToHClip(v.vertex.xyz); o.uv = v.uv; o.color = v.color; return o; }

            half4 frag(v2f i) : SV_Target
            {
                // 1. Chuẩn bị các biến
                float2 centeredUV = i.uv - 0.5;
                float dist = length(centeredUV);
                float angle = (atan2(centeredUV.y, centeredUV.x) / TWO_PI) + 0.5;
                int segmentIndex = floor(angle * _Segments);

                // 2. Xác định bán kính ngoài và trong của bánh xe
                float outerRadius = 0.5;
                // Áp dụng hiệu ứng phồng to cho lát cắt được chọn
                if (segmentIndex == _SelectedSegment)
                {
                    outerRadius *= _HighlightScale;
                }

                // *** LOGIC MỚI: Tạo lỗ hổng ở giữa và loại bỏ pixel bên ngoài ***
                // Nếu pixel nằm ngoài bán kính ngoài hoặc trong bán kính trong, loại bỏ nó
                if (dist > outerRadius || dist < _InnerRadius)
                {
                    discard;
                }

                // 3. Chọn màu nền cho lát cắt
                half4 finalColor = (fmod(segmentIndex, 2.0) == 0.0) ? _Color1 : _Color2;
                if (segmentIndex == _SelectedSegment)
                {
                    finalColor = _HighlightColor;
                }

                // *** LOGIC MỚI: Vẽ đường viền cong ***
                // Viền ngoài
                if (dist > outerRadius - _BorderWidth)
                {
                    finalColor = _BorderColor;
                }
                // Viền trong
                else if (dist < _InnerRadius + _BorderWidth)
                {
                    // Chỉ vẽ viền trong nếu lát cắt đó được chọn (để tạo hiệu ứng đẹp hơn)
                    // Hoặc bạn có thể bỏ if này để vẽ tất cả các viền trong
                    if (segmentIndex == _SelectedSegment) {
                        finalColor = _BorderColor;
                    }
                }
                // Viền thẳng phân cách các lát cắt
                else
                {
                    float segmentPos = frac(angle * _Segments);
                    float radialBorderWidth = _BorderWidth / (dist * TWO_PI); // Độ rộng viền thay đổi theo khoảng cách để đều hơn
                    if (segmentPos < radialBorderWidth || segmentPos > 1.0 - radialBorderWidth)
                    {
                        finalColor = _BorderColor;
                    }
                }
                
                return finalColor * i.color;
            }
            ENDHLSL
        }
    }
}