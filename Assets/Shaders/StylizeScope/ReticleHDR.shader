Shader "Unlit/ReticleHDR_TransparentEmissive"
{
    Properties
    {
        [Header(Reticle Properties)]
        _ReticleTexture("Reticle Texture", 2D) = "white" {}
        [HDR]_Color("HDR Color", Color) = (1, 0, 0, 1)
        _EmissionStrength("Emission Strength", Float) = 2.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_ReticleTexture); SAMPLER(sampler_ReticleTexture);
            float4 _ReticleTexture_ST;
            half4 _Color;
            half _EmissionStrength;

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = TRANSFORM_TEX(i.uv, _ReticleTexture);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_ReticleTexture, sampler_ReticleTexture, i.uv);
                
                half3 emissiveColor = _Color.rgb * _EmissionStrength;
                half alpha = tex.a * _Color.a;

                return half4(emissiveColor, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}