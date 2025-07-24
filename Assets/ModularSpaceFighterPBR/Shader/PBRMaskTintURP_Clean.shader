Shader "PBRMaskTintURP_Final"
{
    Properties
    {
        [Header(Textures)]
        _Albedo("Albedo", 2D) = "white" {}
        _MetallicSmoothness("Metallic (R), Smoothness (A)", 2D) = "white" {}
        _Emission("Emission", 2D) = "black" {}
        _Mask01("Mask 01 (RGB)", 2D) = "white" {}
        _Mask02("Mask 02 (RGB)", 2D) = "white" {}

        [Header(Color Tints)]
        _Color01("Color 01 (Mask01.R)", Color) = (1, 0, 0, 1)
        _Color02("Color 02 (Mask01.G)", Color) = (0, 1, 0, 1)
        _Color03("Color 03 (Mask01.B)", Color) = (0, 0, 1, 1)
        _Color04("Color 04 (Mask02.R)", Color) = (1, 1, 0, 1)
        _Color05("Color 05 (Mask02.G)", Color) = (0, 1, 1, 1)
        _Color06("Color 06 (Mask02.B)", Color) = (1, 0, 1, 1)

        [Header(Color Powers)]
        _Color01Power("Color 01 Power", Range(0, 20)) = 1
        _Color02Power("Color 02 Power", Range(0, 20)) = 1
        _Color03Power("Color 03 Power", Range(0, 20)) = 1
        _Color04Power("Color 04 Power", Range(0, 20)) = 1
        _Color05Power("Color 05 Power", Range(0, 20)) = 1
        _Color06Power("Color 06 Power", Range(0, 20)) = 1

        [Header(Emission)]
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _MIXED_LIGHTING_SUBTRACTIVE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
                float3 normalWS     : TEXCOORD2;
            };

            TEXTURE2D(_Albedo);                 SAMPLER(sampler_Albedo);
            TEXTURE2D(_MetallicSmoothness);     SAMPLER(sampler_MetallicSmoothness);
            TEXTURE2D(_Emission);               SAMPLER(sampler_Emission);
            TEXTURE2D(_Mask01);                 SAMPLER(sampler_Mask01);
            TEXTURE2D(_Mask02);                 SAMPLER(sampler_Mask02);

            CBUFFER_START(UnityPerMaterial)
                float4 _Albedo_ST;
                half4 _Color01, _Color02, _Color03, _Color04, _Color05, _Color06;
                half _Color01Power, _Color02Power, _Color03Power, _Color04Power, _Color05Power, _Color06Power;
                half4 _EmissionColor;
            CBUFFER_END

            Varyings Vertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _Albedo);
                return output;
            }

            half3 CalculateTintContribution(half maskValue, half3 tintColor, half power)
            {
                return maskValue * tintColor * power;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half4 albedoMap = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, input.uv);
                half4 mask01 = SAMPLE_TEXTURE2D(_Mask01, sampler_Mask01, input.uv);
                half4 mask02 = SAMPLE_TEXTURE2D(_Mask02, sampler_Mask02, input.uv);
                half4 metallicSmoothnessMap = SAMPLE_TEXTURE2D(_MetallicSmoothness, sampler_MetallicSmoothness, input.uv);
                half3 emissionMap = SAMPLE_TEXTURE2D(_Emission, sampler_Emission, input.uv).rgb;

                half3 totalTintColor =
                    CalculateTintContribution(mask01.r, _Color01.rgb, _Color01Power) +
                    CalculateTintContribution(mask01.g, _Color02.rgb, _Color02Power) +
                    CalculateTintContribution(mask01.b, _Color03.rgb, _Color03Power) +
                    CalculateTintContribution(mask02.r, _Color04.rgb, _Color04Power) +
                    CalculateTintContribution(mask02.g, _Color05.rgb, _Color05Power) +
                    CalculateTintContribution(mask02.b, _Color06.rgb, _Color06Power);

                half totalMask = saturate(mask01.r + mask01.g + mask01.b + mask02.r + mask02.g + mask02.b);
                half3 finalAlbedo = lerp(albedoMap.rgb, totalTintColor, totalMask);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalize(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = GetShadowCoord(GetVertexPositionInputs(input.positionWS));

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = finalAlbedo;
                surfaceData.metallic = metallicSmoothnessMap.r;
                surfaceData.specular = half3(0,0,0);
                surfaceData.smoothness = metallicSmoothnessMap.a;
                surfaceData.emission = emissionMap * _EmissionColor.rgb;
                surfaceData.occlusion = 1.0;
                surfaceData.alpha = 1.0;
                
                return UniversalFragmentPBR(inputData, surfaceData);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = GetShadowCoord(normalWS);
                return output;
            }

            half4 Fragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}