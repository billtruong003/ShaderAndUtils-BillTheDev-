Shader "TextMeshPro/Optimized/Mobile SDF (URP Opaque Cutout)"
{
    Properties
    {
        _FaceColor ("Face Color", Color) = (1,1,1,1)
        _FaceDilate ("Face Dilate", Range(-1,1)) = 0
        _MainTex ("Font Atlas", 2D) = "white" {}
        _Sharpness ("Sharpness", Range(-1,1)) = 0
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", Float) = 0
        [HideInInspector] _WeightNormal ("Weight Normal", float) = 0
        [HideInInspector] _WeightBold ("Weight Bold", float) = 0.5
        [HideInInspector] _GradientScale ("Gradient Scale", float) = 5.0
        [HideInInspector] _VertexOffsetX ("Vertex OffsetX", float) = 0
        [HideInInspector] _VertexOffsetY ("Vertex OffsetY", float) = 0
        [HideInInspector] _ShaderFlags ("Flags", float) = 0
        [HideInInspector] _ScaleRatioA ("Scale Ratio A", float) = 1
        [HideInInspector] _ScaleRatioB ("Scale Ratio B", float) = 1
        [HideInInspector] _ScaleRatioC ("Scale Ratio C", float) = 1
    }

    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Cull [_CullMode]
            ZWrite On
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex VertShader
            #pragma fragment PixShader
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _FaceColor;
                half _FaceDilate;
                half _WeightNormal;
                half _WeightBold;
                half _GradientScale;
                half _Sharpness;
                half _Cutoff;
                half _VertexOffsetX;
                half _VertexOffsetY;
                half _ScaleRatioA;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                half4  color        : COLOR;
                float4 texcoord0    : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                half4 faceColor     : COLOR;
                float2 texcoord0    : TEXCOORD0;
                half2 param         : TEXCOORD1;
                float4 worldPosition: TEXCOORD2;
                #ifdef UNITY_UI_CLIP_RECT
                float4 clipRect     : TEXCOORD3;
                #endif
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings VertShader(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float4 positionOS = input.positionOS;
                positionOS.x += _VertexOffsetX;
                positionOS.y += _VertexOffsetY;

                output.worldPosition = TransformObjectToWorld(positionOS.xyzw);
                output.positionCS = TransformWorldToHClip(output.worldPosition);

                half scale = abs(input.texcoord0.w) * _GradientScale * (_Sharpness + 1);
                half weight = lerp(_WeightNormal, _WeightBold, step(input.texcoord0.w, 0)) / 4.0;
                weight += _FaceDilate * _ScaleRatioA * 0.5;
                half bias = (0.5 - weight) * scale - 0.5;

                output.faceColor = input.color * _FaceColor;
                output.texcoord0 = input.texcoord0.xy;
                output.param = half2(scale, bias);

                #ifdef UNITY_UI_CLIP_RECT
                output.clipRect = float4(0, 0, 0, 0);
                #endif

                return output;
            }

            half4 PixShader(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half signedDistance = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord0).a;
                half fontAlpha = saturate(signedDistance * input.param.x + input.param.y);

                #ifdef UNITY_UI_CLIP_RECT
                float2 pos = input.worldPosition.xy;
                if (pos.x < input.clipRect.x || pos.x > input.clipRect.z || 
                    pos.y < input.clipRect.y || pos.y > input.clipRect.w)
                    discard;
                #endif

                clip(fontAlpha - _Cutoff);

                half4 finalColor = input.faceColor;
                finalColor.a *= fontAlpha;

                return finalColor;
            }
            ENDHLSL
        }
    }
}