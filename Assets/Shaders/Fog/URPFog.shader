Shader "Custom/URPObjectFog"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Float) = 1.0
        _NoiseSpeed ("Noise Speed", Float) = 0.1
        [KeywordEnum(Linear, Exponential, ExponentialSquared)] _FogMode ("Fog Mode", Float) = 0
        _FogColor ("Fog Color", Color) = (0.8, 0.9, 1, 1)
        _FogStart ("Fog Start", Float) = 0.0
        _FogEnd ("Fog End", Float) = 100.0
        _FogDensity ("Fog Density", Float) = 0.01
        _FogAlpha ("Fog Alpha", Range(0,1)) = 0.5
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend", Float) = 5 // SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend", Float) = 10 // OneMinusSrcAlpha
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2 // Back
        [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Float) = 1 // On
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4 // LEqual
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            Cull [_Cull]
            ZWrite [_ZWrite]
            ZTest [_ZTest]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _FOGMODE_LINEAR _FOGMODE_EXPONENTIAL _FOGMODE_EXPONENTIALSQUARED

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NoiseTex_ST;
                float4 _Color;
                float4 _FogColor;
                float _FogStart;
                float _FogEnd;
                float _FogDensity;
                float _FogAlpha;
                float _NoiseSpeed;
                float _NoiseScale;
                float _SrcBlend;
                float _DstBlend;
                float _Cull;
                float _ZWrite;
                float _ZTest;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float distance = length(positionWS - _WorldSpaceCameraPos);
                
                #if _FOGMODE_LINEAR
                    OUT.fogFactor = saturate((distance - _FogStart) / (_FogEnd - _FogStart));
                #elif _FOGMODE_EXPONENTIAL
                    OUT.fogFactor = 1.0 - exp(-_FogDensity * distance);
                #elif _FOGMODE_EXPONENTIALSQUARED
                    OUT.fogFactor = 1.0 - exp(-_FogDensity * distance * distance);
                #else
                    OUT.fogFactor = 0.0;
                #endif
                
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _Color;
                
                // Tính noiseUV với noise scale và chuyển động theo thời gian
                float2 noiseUV = IN.uv * _NoiseScale + float2(sin(_Time.y * _NoiseSpeed), cos(_Time.y * _NoiseSpeed)) * 0.1;
                half4 noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV * _NoiseTex_ST.xy + _NoiseTex_ST.zw);
                
                // Sử dụng noise để điều chỉnh fogFactor
                half fogFactor = saturate(IN.fogFactor + (noise.r - 0.5) * 0.2);
                
                half4 fogColor = _FogColor;
                half4 finalColor = lerp(texColor, fogColor, fogFactor);
                finalColor.a = texColor.a * (1.0 - fogFactor * _FogAlpha);
                return finalColor;
            }
            ENDHLSL
        }
    }
}