Shader "Example/GlowOutline"
{
    Properties
    {
        [HDR]_GlowColor ("Glow Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Fade ("Fade Distance", Float) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Blend One One // Additive blending for glow
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
                half4 _GlowColor;
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
                half4 finalColor = _GlowColor;
                finalColor.a *= depthFadeFactor;
                return finalColor;
            }
            ENDHLSL
        }
    }
}