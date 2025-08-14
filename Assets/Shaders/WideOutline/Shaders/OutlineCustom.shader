Shader "Hidden/CleanCode/WideOutlineEffect"
{
    Properties
    {
        _OuterColor("Outer Color", Color) = (1, 1, 1, 1)
        _InnerColor("Inner Color", Color) = (1, 1, 1, 0)
        _OutlineWidth("Outline Width", Range(1, 50)) = 10.0
        _Gap("Gap", Range(0, 0.99)) = 0.5
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        
        Pass
        {
            Name "Wide Outline Pass"
            Cull Off
            ZTest Always
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/dev.ameye.linework/Runtime/Common/Shaders/DecodePosition.hlsl"

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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _OuterColor;
                float4 _InnerColor;
                float _OutlineWidth;
                float _Gap;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // SỬA LỖI: Sử dụng _ScreenParams.xy để có được kích thước pixel vật lý chính xác,
                // thay vì _MainTex_TexelSize.zw có thể không đáng tin cậy khi có Render Scale.
                float2 physicalResolution = _ScreenParams.xy;
                float2 physicalPixelCoords = input.uv * physicalResolution;

                UnityTexture2D mainTexStruct = UnityBuildTexture2DStructNoScale(_MainTex);

                float2 nearestEdgePhysicalPixel;
                DecodePosition_float(
                    physicalPixelCoords, 
                    physicalResolution, 
                    mainTexStruct, 
                    nearestEdgePhysicalPixel
                );

                float rawDistance = distance(physicalPixelCoords, nearestEdgePhysicalPixel);

                // Thêm 0.5 để căn giữa pixel, cho kết quả mượt hơn
                float relativeDistance = saturate(1.0 - ((rawDistance - 0.5) / _OutlineWidth));

                float4 outlineGradient = lerp(_OuterColor, _InnerColor, relativeDistance);

                // +1.0 thay vì +0.5 vì JFA distance field đã được bù trừ
                float outlineMask = saturate(1.0 + _OutlineWidth - rawDistance);
                float gapMask = step(1.0 - _Gap, relativeDistance);

                float finalAlpha = outlineMask * (1.0 - gapMask);

                return float4(outlineGradient.rgb, outlineGradient.a * finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}