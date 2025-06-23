Shader "Custom/URP/ToonShader"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _ToonRampSmoothness ("Toon Ramp Smoothness", Range(0,1)) = 0.1
        _ToonRampOffset ("Toon Ramp Offset", Range(0,1)) = 0.5
        [HDR] _ToonRampTinting ("Toon Ramp Tinting", Color) = (1,1,1,1)
        _AmbientColor ("Ambient Color", Color) = (0.2, 0.2, 0.2, 1)
        [IntRange] _RenderingLayerMask ("Rendering Layer Mask", Range(0, 31)) = 0 // Support for rendering layer mask
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }


            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _ToonRampSmoothness;
                float _ToonRampOffset;
                float4 _ToonRampTinting;
                float4 _AmbientColor;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            void ToonShading_float(in float3 Normal, in float ToonRampSmoothness, in float3 ClipSpacePos, in float3 WorldPos, in float4 ToonRampTinting,
                in float ToonRampOffset, out float3 ToonRampOutput, out float3 Direction)
            {
                #ifdef SHADERGRAPH_PREVIEW
                    ToonRampOutput = float3(0.5, 0.5, 0);
                    Direction = float3(0.5, 0.5, 0);
                #else
                    #if SHADOWS_SCREEN
                        half4 shadowCoord = ComputeScreenPos(ClipSpacePos);
                    #else
                        half4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
                    #endif 

                    #if _MAIN_LIGHT_SHADOWS_CASCADE || _MAIN_LIGHT_SHADOWS
                        Light light = GetMainLight(shadowCoord);
                    #else
                        Light light = GetMainLight();
                    #endif

                    half d = dot(Normal, light.direction) * 0.5 + 0.5;
                    half toonRamp = smoothstep(ToonRampOffset, ToonRampOffset + ToonRampSmoothness, d);
                    toonRamp *= light.shadowAttenuation;
                    ToonRampOutput = light.color * (toonRamp + ToonRampTinting);
                    Direction = light.direction;

                    // Fallback for no light
                    if (dot(light.color, light.color) == 0)
                    {
                        ToonRampOutput = float3(1, 1, 1);
                    }
                #endif
            }

            float4 frag(Varyings input) : SV_TARGET
            {
                float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float3 toonRampOutput;
                float3 lightDirection;
                ToonShading_float(input.normalWS, _ToonRampSmoothness, input.positionCS.xyz, input.positionWS, _ToonRampTinting, _ToonRampOffset, toonRampOutput, lightDirection);
                color.rgb *= toonRampOutput;
                color.rgb += _AmbientColor.rgb;
                return color;
            }

            ENDHLSL
        }
    }
}