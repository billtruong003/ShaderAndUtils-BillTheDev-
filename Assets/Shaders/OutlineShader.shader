Shader "Custom/URP Lightweight Outline"
{
    Properties
    {
        [HDR]_OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineThickness ("Outline Thickness", Range(-10, 100)) = 0.01 // Đổi lại về range hợp lý hơn
        [Toggle(_ENABLE_OUTLINE)] _EnableOutline ("Enable Outline", Float) = 1 // Thêm thuộc tính bật/tắt

        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _BaseMap ("Base Map", 2D) = "white" {}
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #pragma multi_compile_local _ _ENABLE_OUTLINE // Dùng multi_compile_local để Unity tạo biến thể shader

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
                float _EnableOutline; // Thêm biến vào CBUFFER
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
                #if _ENABLE_OUTLINE
                    return _OutlineColor;
                #else
                    // Nếu _EnableOutline không được kích hoạt, discard pixel
                    discard;
                #endif
            }
            ENDHLSL
        }

        Pass
        {
            Name "MainLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0

            #pragma vertex LitVert
            #pragma fragment LitFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UniversalFragmentPBR.hlsl"

            struct LitAppData
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
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

                SurfaceData surfaceData;
                InitializeStandardLitSurfaceData(baseColor.rgb, baseColor.a, input.normalWS,
                                                _Metallic, _Smoothness,
                                                0, 0, 0, 0,
                                                surfaceData);

                InputData inputData;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = input.normalWS;
                inputData.viewDirectionWS = GetViewDirectionWS(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = ComputeFogFactor(input.positionCS.z);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);

                return color;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}