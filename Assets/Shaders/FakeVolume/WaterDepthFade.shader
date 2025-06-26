Shader "Example/WaterDepthFade"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.0, 0.5, 0.7, 1.0)
        _Fade ("Fade Distance", Float) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/Includes/DepthFadeLogic.hlslinc"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos  : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Fade;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float depthFadeFactor = CalculateDepthFade(IN.positionCS, IN.screenPos, _Fade);
                half4 finalColor = _BaseColor;
                finalColor.a *= 1.0 - depthFadeFactor; // Nước mờ dần khi sâu
                return finalColor;
            }
            ENDHLSL
        }
    }
}