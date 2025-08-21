Shader "Advanced/VolumetricPlaneFog"
{
    Properties
    {
        _FogColor ("Fog Color (Tint)", Color) = (0.0, 0.1, 0.2, 0.5)
        _FogStrength ("Fog Strength", Range(0.0, 5.0)) = 1.0
        _FogDepthPower ("Fog Depth Falloff", Range(0.1, 100)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float4 screenPos    : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float _FogStrength;
                float _FogDepthPower;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                // Lấy độ sâu của cảnh vật phía sau mặt phẳng sương mù
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);

                // Độ sâu của chính mặt phẳng sương mù tại điểm ảnh đang vẽ
                float surfaceDepth = input.screenPos.w;

                // Tính toán khoảng cách chênh lệch và tạo mật độ sương mù
                float depthDifference = sceneDepth - surfaceDepth;
                float fogDensity = saturate(depthDifference * _FogStrength);
                
                // Áp dụng falloff để kiểm soát độ mềm của cạnh sương mù
                fogDensity = pow(fogDensity, _FogDepthPower);
                
                // Màu cuối cùng là màu sương mù với độ trong suốt dựa trên mật độ
                float3 finalColor = _FogColor.rgb;
                float finalAlpha = _FogColor.a * fogDensity;

                return float4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}