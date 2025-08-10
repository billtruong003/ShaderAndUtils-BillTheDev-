Shader "Unlit/DepthFadeFog"
{
    Properties
    {
        _FogColor("Fog Color", Color) = (0.5, 0.5, 0.5, 0.5)
        _FogDensity("Fog Density", Range(0, 1)) = 1.0
        _FadeStartDistance("Fade Start Distance", Float) = 0.1
        _FadeEndDistance("Fade End Distance", Float) = 5.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Front
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // Dòng include bị thiếu đã được thêm vào đây
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            float4 _FogColor;
            float _FogDensity;
            float _FadeStartDistance;
            float _FadeEndDistance;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Giờ đây SampleSceneDepth sẽ được nhận dạng chính xác
                float sceneRawDepth = SampleSceneDepth(input.screenPos.xy / input.screenPos.w);
                float sceneLinearEyeDepth = LinearEyeDepth(sceneRawDepth, _ZBufferParams);
                
                float fragmentLinearEyeDepth = input.positionCS.w;
                
                float depthDifference = sceneLinearEyeDepth - fragmentLinearEyeDepth;

                float fadeFactor = saturate(smoothstep(_FadeStartDistance, _FadeEndDistance, depthDifference));

                half4 finalColor = _FogColor;
                finalColor.a *= fadeFactor * _FogDensity;

                return finalColor;
            }
            ENDHLSL
        }
    }
}