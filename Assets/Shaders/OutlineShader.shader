Shader "Custom/URP Lightweight Outline"
{
    Properties
    {
        [HDR]_OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineThickness ("Outline Thickness", Range(-10, 10)) = 0.01
        [Toggle(_ENABLE_OUTLINE)] _EnableOutline ("Enable Outline", Float) = 1

        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _BaseMap ("Base Map", 2D) = "white" {}
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        // Pass 1: Đánh dấu mesh vào Stencil Buffer
        Pass
        {
            Name "StencilWrite"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            ColorMask 0 // Không ghi màu, chỉ ghi stencil
            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex StencilVert
            #pragma fragment StencilFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct StencilAppData
            {
                float4 vertex : POSITION;
            };

            struct StencilVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            StencilVaryings StencilVert (StencilAppData input)
            {
                StencilVaryings output;
                output.positionCS = TransformObjectToHClip(input.vertex.xyz);
                return output;
            }

            half4 StencilFrag (StencilVaryings input) : SV_Target
            {
                return half4(0, 0, 0, 0); // Không cần ghi màu
            }
            ENDHLSL
        }

        // Pass 2: Vẽ outline sử dụng Stencil Buffer
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite Off
            ZTest LEqual
            Stencil
            {
                Ref 1
                Comp NotEqual // Chỉ vẽ ở các pixel không thuộc mesh
            }

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #pragma multi_compile_local _ _ENABLE_OUTLINE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct OutlineAppData
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct OutlineVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineThickness;
                float _EnableOutline;
            CBUFFER_END

            OutlineVaryings OutlineVert (OutlineAppData input)
            {
                OutlineVaryings output;
                float3 extrudedPos = input.vertex.xyz + input.normal * _OutlineThickness;
                output.positionCS = TransformObjectToHClip(extrudedPos);
                return output;
            }

            half4 OutlineFrag (OutlineVaryings input) : SV_Target
            {
                #if defined(_ENABLE_OUTLINE)
                    return _OutlineColor;
                #else
                    discard;
                    return half4(0,0,0,0);
                #endif
            }
            ENDHLSL
        }

        // Pass 3: MainLit - Render vật liệu chính
        Pass
        {
            Name "MainLit"
            Tags { "LightMode" = "UniversalForward" }
            Stencil
            {
                Ref 1
                Comp Equal // Chỉ vẽ trên các pixel đã đánh dấu
            }

            HLSLPROGRAM
            #pragma vertex LitVert
            #pragma fragment LitFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct LitAppData
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct LitVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Metallic;
                float _Smoothness;
            CBUFFER_END

            float3 GetViewDirectionWS(float3 positionWS)
            {
                return normalize(_WorldSpaceCameraPos - positionWS);
            }

            LitVaryings LitVert (LitAppData input)
            {
                LitVaryings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 LitFrag (LitVaryings input) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                InputData inputData;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalize(input.normalWS);
                inputData.viewDirectionWS = GetViewDirectionWS(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.bakedGI = half3(0, 0, 0);
                inputData.fogCoord = 0.0;
                inputData.vertexLighting = half3(0, 0, 0);
                inputData.normalizedScreenSpaceUV = float2(0, 0);
                inputData.shadowMask = half4(0, 0, 0, 0);
                inputData.tangentToWorld = half3x3(0, 0, 0, 0, 0, 0, 0, 0, 0);

                SurfaceData surfaceData;
                surfaceData.albedo = baseColor.rgb;
                surfaceData.specular = half3(0, 0, 0);
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.emission = half3(0, 0, 0);
                surfaceData.occlusion = 1.0;
                surfaceData.alpha = baseColor.a;
                surfaceData.clearCoatMask = 0.0;
                surfaceData.clearCoatSmoothness = 0.0;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                return color;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}