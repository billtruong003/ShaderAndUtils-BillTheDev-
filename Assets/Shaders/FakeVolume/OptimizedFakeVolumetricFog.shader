Shader "Example/OptimizedFakeVolumetricFog"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.5, 0.5, 0.5, 0.5)
        _Density ("Fog Density", Range(0, 1)) = 0.2
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Float) = 1.0
        _NoiseSpeed ("Noise Speed", Float) = 0.05
        _HeightFade ("Height Fade", Float) = 2.0
        _LightAbsorption ("Light Absorption", Range(0, 1)) = 0.6
        _MaxDistance ("Max Distance", Float) = 50.0
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos  : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);

            CBUFFER_START(UnityPerMaterial)
                half4 _FogColor;
                float _Density;
                float _NoiseScale;
                float _NoiseSpeed;
                float _HeightFade;
                float _LightAbsorption;
                float _MaxDistance;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                OUT.uv = IN.uv;
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Depth-based distance calculation
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV).r;
                float linearDepth = LinearEyeDepth(depth, _ZBufferParams);
                float depthFadeFactor = saturate(linearDepth / _MaxDistance);

                // Height-based fog
                float heightFactor = saturate(1.0 - (IN.worldPos.y - _WorldSpaceCameraPos.y) / _HeightFade);

                // Noise with animation
                float2 noiseUV = IN.uv * _NoiseScale + _Time.y * _NoiseSpeed;
                half noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;

                // Fog density calculation
                float fogDensity = _Density * depthFadeFactor * heightFactor * (0.5 + 0.5 * noise);

                // Light interaction
                Light mainLight = GetMainLight();
                float lightAtten = saturate(dot(normalize(mainLight.direction), normalize(IN.worldPos - _WorldSpaceCameraPos)));
                float absorption = exp(-fogDensity * _LightAbsorption);

                // Final color
                half4 finalColor = _FogColor;
                finalColor.a = 1.0 - absorption;
                finalColor.rgb *= lerp(0.5, 1.0, lightAtten);

                return finalColor;
            }
            ENDHLSL
        }
    }
}