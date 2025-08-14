// SHADER ĐÃ ĐƯỢC SỬA LỖI LOGIC ĐỂ GIỐNG 100% VỚI LINEWORK
Shader "Hidden/CleanCode/WideOutlineEffect"
{
    Properties
    {
        // Các thuộc tính này giờ sẽ được điều khiển bởi ScriptableObject Settings
        // nhưng vẫn giữ lại để tương thích với Material Inspector.
        _OutlineColor("Outline Color", Color) = (1, 0, 0, 1)
        _OutlineOccludedColor("Occluded Color", Color) = (0, 0, 1, 1)
        _OutlineWidth("Outline Width", Range(0, 100)) = 10.0
        _OutlineGap("Outline Gap", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType"="Opaque" }
        
        Pass
        {
            Name "Wide Outline Pass"
            Cull Off
            ZTest Always // ZTest sẽ được ghi đè bởi C#
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha // Blend mode sẽ được ghi đè bởi C#

            HLSLINCLUDE
            #define SNORM16_MAX_FLOAT_MINUS_EPSILON ((float)(32768-2) / (float)(32768-1))
            #define FLOOD_ENCODE_OFFSET float2(1.0, SNORM16_MAX_FLOAT_MINUS_EPSILON)
            #define FLOOD_ENCODE_SCALE float2(2.0, 1.0 + SNORM16_MAX_FLOAT_MINUS_EPSILON)
            ENDHLSL

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Thêm các multi_compile cần thiết để khớp với shader gốc
            #pragma multi_compile_local _ CUSTOM_DEPTH
            #pragma multi_compile_local _ INFORMATION_BUFFER

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // Các biến và Texture được truyền từ C# (tên phải khớp với file gốc)
            TEXTURE2D(_BlitTexture); // Đây là JFA buffer, không phải _MainTex
            SAMPLER(sampler_BlitTexture);

            TEXTURE2D(_SilhouetteBuffer);
            SAMPLER(sampler_SilhouetteBuffer);

            TEXTURE2D(_InformationBuffer);
            SAMPLER(sampler_InformationBuffer);

            #if defined(CUSTOM_DEPTH)
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
                TEXTURE2D(_SilhouetteDepthBuffer);
                SAMPLER(sampler_SilhouetteDepthBuffer);
                half4 _OutlineOccludedColor;
            #endif

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineGap;
                float _RenderScale; // Cần biến này để xử lý Render Scale
            CBUFFER_END
            
            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // ---- BẮT ĐẦU SAO CHÉP LOGIC TỪ SHADER GỐC ----

                int2 uvInt = int2(IN.positionCS.xy);
                float2 encodedPos = _BlitTexture.Load(int3(uvInt, 0)).rg;

                if (encodedPos.y == -1) {
                    return half4(0, 0, 0, 0);
                }

                float2 nearestPos = (encodedPos + FLOOD_ENCODE_OFFSET) * abs(_ScreenParams.xy) / FLOOD_ENCODE_SCALE;
                float2 currentPos = IN.positionCS.xy * (1.0 / _RenderScale);
                half dist = length(nearestPos - currentPos);

                half width = _OutlineWidth;
                #if defined(INFORMATION_BUFFER)
                    // Sample từ information buffer nếu dùng chế độ Per-Outline
                    width = SAMPLE_TEXTURE2D(_InformationBuffer, sampler_InformationBuffer, nearestPos / _ScreenParams.xy).r * 100.0f;
                #endif
                
                half outlineMask = saturate(width - dist + 1.0) - saturate((_OutlineGap * width) - dist + 1.0);

                #if defined(CUSTOM_DEPTH)
                    half depth1 = SAMPLE_TEXTURE2D(_SilhouetteDepthBuffer, sampler_SilhouetteDepthBuffer, nearestPos / _ScreenParams.xy).r;
                    half depth2 = SampleSceneDepth(IN.uv);
                    half isOccluded = LinearEyeDepth(depth1, _ZBufferParams) - LinearEyeDepth(depth2, _ZBufferParams) > 0.001;
                    
                    half4 objectColor = SAMPLE_TEXTURE2D(_SilhouetteBuffer, sampler_SilhouetteBuffer, nearestPos / _ScreenParams.xy);
                    half4 finalColor = isOccluded > 0 ? _OutlineOccludedColor : objectColor;
                    
                    finalColor.a *= outlineMask;
                    return finalColor;
                #else
                    half4 finalColor = SAMPLE_TEXTURE2D(_SilhouetteBuffer, sampler_SilhouetteBuffer, nearestPos / _ScreenParams.xy);
                    finalColor.a *= outlineMask;
                    return finalColor;
                #endif

                // ---- KẾT THÚC SAO CHÉP LOGIC ----
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}