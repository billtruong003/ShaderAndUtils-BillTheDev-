

Shader "Custom/ValorantSmoke_Gameplay"
{
    Properties
    {
        [Header(Main Settings)]
        _BaseColor("Base Color", Color) = (0.2, 0.2, 0.25, 1)
        [PowerSlider(5.0)]_Density("Density", Range(0.8, 1)) = 0.98

        [Header(Surface Noise)]
        _NoiseTex("Surface Noise Texture", 2D) = "white" {}
        _NoiseSpeedU("Noise Speed U", Float) = 0.05
        _NoiseSpeedV("Noise Speed V", Float) = 0.05
        _NoiseScale("Noise Scale", Float) = 1.5
        _NoiseColorInfluence("Noise Color Influence", Range(0, 1)) = 0.3

        [Header(Fresnel Rim)]
        _FresnelColor("Fresnel Rim Color", Color) = (0.4, 0.4, 0.5, 1)
        _FresnelPower("Fresnel Power", Range(0.1, 10)) = 3.0

        [Header(Intersection)]
        _IntersectionColor("Intersection Color", Color) = (0.6, 0.6, 0.7, 1)
        [HDR]_IntersectionEmission("Intersection Emission", Color) = (0.1, 0.1, 0.15, 1)
        _IntersectionFadePower("Intersection Power", Range(1, 10)) = 4.0
        _IntersectionThickness("Intersection Thickness", Range(-50, 50.0)) = 1.5

        [Header(3D Distortion)]
        _DistortionNoiseTex("3D Distortion Noise", 2D) = "black" {}
        _DistortionStrength("Distortion Strength", Float) = 0.15
        _DistortionSpeed("Distortion Speed", Vector) = (0.1, 0.1, 0.1, 0)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite On

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
                float3 normalWS     : TEXCOORD2;
                float4 screenPos    : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Density;
                float4 _NoiseTex_ST;
                half _NoiseSpeedU;
                half _NoiseSpeedV;
                half _NoiseScale;
                half _NoiseColorInfluence;
                half4 _FresnelColor;
                half _FresnelPower;
                half4 _IntersectionColor;
                half4 _IntersectionEmission;
                half _IntersectionFadePower;
                half _IntersectionThickness;
                half _DistortionStrength;
                float4 _DistortionSpeed;
            CBUFFER_END
            
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            TEXTURE3D(_DistortionNoiseTex); SAMPLER(sampler_DistortionNoiseTex);
            TEXTURE2D_X(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 distortionCoord = input.positionOS.xyz + _Time.y * _DistortionSpeed.xyz;
                float3 distortion = SAMPLE_TEXTURE3D_LOD(_DistortionNoiseTex, sampler_DistortionNoiseTex, distortionCoord, 0).rgb;
                distortion = (distortion * 2.0) - 1.0;
                input.positionOS.xyz += distortion * _DistortionStrength;

                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _NoiseTex);
                output.screenPos = ComputeScreenPos(output.positionCS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Depth Calculation
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float rawSceneDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV).r;
                float sceneDepth = LinearEyeDepth(rawSceneDepth, _ZBufferParams);
                float pixelDepth = input.positionCS.w;
                
                // Intersection and Soft Edge calculation
                float depthDifference = sceneDepth - pixelDepth;
                float intersection = 1.0 - saturate(depthDifference / _IntersectionThickness);
                intersection = pow(intersection, _IntersectionFadePower);
                
                float depthFade = saturate(depthDifference / _IntersectionThickness);

                // Animated Noise
                float2 animatedUV = input.uv + float2(_Time.y * _NoiseSpeedU, _Time.y * _NoiseSpeedV);
                half noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, animatedUV * _NoiseScale).r;
                
                // Fresnel
                float3 viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
                float fresnelDot = 1.0 - saturate(dot(viewDirection, input.normalWS));
                half fresnel = pow(fresnelDot, _FresnelPower);
                
                // --- COLOR CALCULATION ---
                // Start with base color
                half3 finalColor = _BaseColor.rgb;
                // Use noise to vary the color, not alpha. This creates a "churning" surface.
                finalColor = lerp(finalColor, finalColor * (1-noise), _NoiseColorInfluence);
                // Add fresnel rim light
                finalColor = lerp(finalColor, _FresnelColor.rgb, fresnel * _FresnelColor.a);
                // Add intersection color and emission
                half3 intersectionCombinedColor = _IntersectionColor.rgb + _IntersectionEmission.rgb;
                finalColor = lerp(finalColor, intersectionCombinedColor, intersection * _IntersectionColor.a);

                // --- ALPHA CALCULATION (GAMEPLAY FOCUSED) ---
                // The smoke is now fundamentally OPAQUE.
                half finalAlpha = _Density;
                
                // The intersection area should be fully opaque.
                finalAlpha = lerp(finalAlpha, 1.0, intersection);
                
                // ONLY the depth fade (soft edges) can reduce the alpha.
                finalAlpha *= depthFade;

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack "Transparent/VertexLit"
}