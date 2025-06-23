Shader "Custom/URP/MapReveal"
{
    Properties
    {
        _MainTex("Map Gốc (RGB)", 2D) = "white" {}
        [NoScaleOffset] _PaintMap("Map Che (R)", 2D) = "white" {}
        _FadeRadius("Bán kính Fade", Range(0, 1)) = 0.1
        [NoScaleOffset] _PlayerIcon("Icon Nhân Vật", 2D) = "white" {}
        _PlayerPosition("Vị trí Nhân Vật (UV)", Vector) = (0, 0, 0, 0)
        _PlayerIconSize("Kích thước Icon Nhân Vật", Range(0, 0.2)) = 0.05
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uvMain : TEXCOORD0;
                float2 uvPaint : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            TEXTURE2D(_PaintMap);
            SAMPLER(sampler_PaintMap);
            float _FadeRadius;

            TEXTURE2D(_PlayerIcon);
            SAMPLER(sampler_PlayerIcon);
            float4 _PlayerPosition;
            float _PlayerIconSize;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uvMain = IN.uv0 * _MainTex_ST.xy + _MainTex_ST.zw;
                OUT.uvPaint = IN.uv1;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 mainColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvMain);
                half paintValue = SAMPLE_TEXTURE2D(_PaintMap, sampler_PaintMap, IN.uvPaint).r;

                // Fade effect: paintValue = 1 (red) -> hidden, paintValue = 0 (black) -> revealed
                half fade = smoothstep(0, _FadeRadius, paintValue);
                mainColor.a = 1.0 - fade;

                half4 finalColor = mainColor;
                float2 uvIcon = (IN.uvPaint - _PlayerPosition.xy) / _PlayerIconSize + 0.5;
                if (uvIcon.x >= 0 && uvIcon.x <= 1 && uvIcon.y >= 0 && uvIcon.y <= 1)
                {
                    half4 iconColor = SAMPLE_TEXTURE2D(_PlayerIcon, sampler_PlayerIcon, uvIcon);
                    finalColor = lerp(finalColor, iconColor, iconColor.a);
                }

                return finalColor;
            }
            ENDHLSL
        }
    }
}