Shader "Stylized/Advanced Lake Water"
{
    Properties
    {
        [Header(Water Colors)]
        _ShallowColor("Shallow Color", Color) = (0.3, 0.7, 0.8, 1.0)
        _DeepColor("Deep Color", Color) = (0.1, 0.3, 0.5, 1.0)
        _DepthMax("Water Depth", Range(0, 50)) = 10
        _Opacity("Opacity", Range(0, 1)) = 0.8

        [Header(Surface Layer)]
        _SurfaceNoiseTex("Surface Noise Texture", 2D) = "white" {}
        _SurfaceNoiseScale("Scale", Range(0.1, 10)) = 1.5
        _SurfaceNoiseStrength("Normal Strength", Range(0, 1)) = 0.1
        _SurfaceFlowDirection("Flow Direction (X1,Y1,X2,Y2)", Vector) = (0.01, 0.01, -0.01, 0.02)

        [Header(Foam Layer)]
        _FoamNoiseTex("Foam Texture", 2D) = "white" {}
        _FoamNoiseScale("Scale", Range(0.1, 20)) = 5
        _FoamColor("Foam Color", Color) = (1, 1, 1, 1)
        _ShoreFoamDistance("Shore Foam Distance", Range(0, 5)) = 0.5
        _IntersectionThreshold("Wave Foam Threshold", Range(0, 1)) = 0.6

        [Header(Stylized Specular)]
        _SpecularColor("Specular Color", Color) = (1, 1, 1, 1)
        _SpecularGloss("Gloss", Range(1, 100)) = 30
        _SpecularThreshold("Threshold", Range(0, 1)) = 0.95

        [Header(Wave Animation)]
        _WaveAmplitude("Amplitude", Range(0, 1)) = 0.1
        _WaveFrequency("Frequency", Range(0, 5)) = 1.5
        _WaveSpeed("Speed", Range(0, 5)) = 0.5
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float4 screenPos    : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor, _DeepColor, _FoamColor, _SpecularColor;
                half _DepthMax, _Opacity, _SurfaceNoiseScale, _SurfaceNoiseStrength;
                half _ShoreFoamDistance, _IntersectionThreshold, _FoamNoiseScale;
                half _SpecularGloss, _SpecularThreshold;
                half _WaveAmplitude, _WaveFrequency, _WaveSpeed;
                float4 _SurfaceFlowDirection;
            CBUFFER_END

            TEXTURE2D(_SurfaceNoiseTex);    SAMPLER(sampler_SurfaceNoiseTex);
            TEXTURE2D(_FoamNoiseTex);       SAMPLER(sampler_FoamNoiseTex);
            TEXTURE2D(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);

            Varyings Vertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                float time = _Time.y * _WaveSpeed;
                float wave = sin(time + (input.positionOS.x + input.positionOS.z) * _WaveFrequency) * _WaveAmplitude;
                input.positionOS.y += wave;

                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float3 viewDir = SafeNormalize(_WorldSpaceCameraPos - input.positionWS);
                float sceneRawDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, input.screenPos.xy / input.screenPos.w).r;
                float sceneLinearEyeDepth = LinearEyeDepth(sceneRawDepth, _ZBufferParams);
                float surfaceLinearEyeDepth = input.positionCS.w;
                float depthDifference = sceneLinearEyeDepth - surfaceLinearEyeDepth;

                half depthFactor = saturate(depthDifference / _DepthMax);
                half4 waterColor = lerp(_ShallowColor, _DeepColor, depthFactor);

                float2 flowUV1 = input.positionWS.xz * _SurfaceNoiseScale / 10.0 + _Time.y * _SurfaceFlowDirection.xy;
                float2 flowUV2 = input.positionWS.xz * _SurfaceNoiseScale / 10.0 + _Time.y * _SurfaceFlowDirection.zw;
                half surfaceNoiseSample = (SAMPLE_TEXTURE2D(_SurfaceNoiseTex, sampler_SurfaceNoiseTex, flowUV1).r + SAMPLE_TEXTURE2D(_SurfaceNoiseTex, sampler_SurfaceNoiseTex, flowUV2).r) * 0.5;
                
                float3 perturbedNormal = SafeNormalize(input.normalWS + float3(surfaceNoiseSample, surfaceNoiseSample, surfaceNoiseSample) * _SurfaceNoiseStrength);

                half shoreFoam = 1.0 - saturate(depthDifference / _ShoreFoamDistance);
                
                float2 foamNoiseUV = input.positionWS.xz * _FoamNoiseScale / 10.0;
                half foamNoiseSample = SAMPLE_TEXTURE2D(_FoamNoiseTex, sampler_FoamNoiseTex, foamNoiseUV).r;
                half intersectionFoam = smoothstep(_IntersectionThreshold - 0.1, _IntersectionThreshold + 0.1, surfaceNoiseSample * foamNoiseSample);

                half totalFoamFactor = saturate(shoreFoam + intersectionFoam);
                half4 foamColor = totalFoamFactor * _FoamColor;

                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;
                float3 halfDir = SafeNormalize(lightDir + viewDir);

                half specularDot = saturate(dot(perturbedNormal, halfDir));
                half specularPower = pow(specularDot, _SpecularGloss);
                half specularMask = smoothstep(_SpecularThreshold - 0.01, _SpecularThreshold + 0.01, specularPower);
                half3 specular = specularMask * _SpecularColor.rgb * mainLight.color;
                
                half3 finalColor = waterColor.rgb + foamColor.rgb + specular;
                half finalAlpha = waterColor.a * _Opacity + totalFoamFactor;

                return half4(finalColor, saturate(finalAlpha));
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Transparent"
}