Shader "CleanCode/ToonySlime"
{
    Properties
    {
        [Header(Base Properties)]
        _BaseMap("Texture (RGBA)", 2D) = "white" {}
        _ColorTint("Color Tint", Color) = (0.5, 1.0, 0.5, 0.75)

        [Header(Refraction and Transparency)]
        _RefractionStrength("Refraction Strength", Range(0.0, 0.1)) = 0.02
        _SurfaceTransparency("Surface Transparency", Range(0, 1)) = 0.25

        [Header(Depth Effect)]
        _DepthColor("Depth Color", Color) = (0.1, 0.5, 0.2, 1.0)
        _MaxDepth("Maximum Depth", Float) = 2.0
        _DepthTransparency("Depth Max Transparency", Range(0, 1)) = 0.9

        [Header(Slime Animation Noise)]
        _NoiseScale("Noise Scale", Float) = 2.0
        _NoiseSpeed("Noise Speed", Float) = 0.5
        _NoiseAmplitude("Noise Amplitude", Float) = 0.1

        [Header(Internal Bubbles)]
        _BubbleMap("Bubble Texture (RGB)", 2D) = "white" {}
        _BubbleScale("Bubble Scale", Float) = 5.0
        _BubbleSpeed("Bubble Scroll Speed", Vector) = (0.1, 0.2, 0.0, 0.0)
        _BubbleDensity("Bubble Density", Range(0, 1)) = 0.5

        [Header(Toon Style and Wetness)]
        _ToonThreshold("Toon Threshold", Range(0, 1)) = 0.1
        _SSSStrength("Subsurface Strength", Range(0, 1)) = 0.5
        _SpecularColor("Specular Color", Color) = (1, 1, 1, 1)
        _Shininess("Shininess", Range(1, 100)) = 20.0
        _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower("Rim Power", Range(0.1, 10.0)) = 3.0

        [Header(Emission)]
        [HDR]_EmissionColor("Bubble Emission Color", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/ToonySlime/SlimeAnimation.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BubbleMap); SAMPLER(sampler_BubbleMap);
            TEXTURE2D(_CameraOpaqueTexture); SAMPLER(sampler_CameraOpaqueTexture);
            TEXTURE2D(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _ColorTint;
                float _RefractionStrength;
                float _SurfaceTransparency;

                float4 _DepthColor;
                float _MaxDepth;
                float _DepthTransparency;
                
                float _NoiseScale;
                float _NoiseSpeed;
                float _NoiseAmplitude;
                
                float4 _BubbleMap_ST;
                float _BubbleScale;
                float4 _BubbleSpeed;
                float _BubbleDensity;

                float _ToonThreshold;
                float _SSSStrength;
                float4 _SpecularColor;
                float _Shininess;
                float4 _RimColor;
                float _RimPower;
                
                float4 _EmissionColor;
            CBUFFER_END

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
                float2 uv           : TEXCOORD2;
                float4 screenPos    : TEXCOORD3;
                float3 positionOS   : TEXCOORD4;
                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    float4 shadowCoord  : TEXCOORD5;
                #endif
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                output.positionOS = input.positionOS.xyz;
                float3 animatedPositionOS = AnimateVertexWithNoise(input.positionOS.xyz, input.normalOS, _NoiseScale, _NoiseSpeed, _NoiseAmplitude);

                VertexPositionInputs posInputs = GetVertexPositionInputs(animatedPositionOS);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.screenPos = ComputeScreenPos(output.positionCS);
                
                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    output.shadowCoord = GetShadowCoord(posInputs);
                #endif

                return output;
            }

            half3 SampleBubbles(float3 positionWS)
            {
                float2 uv1 = positionWS.xy * _BubbleScale + _Time.y * _BubbleSpeed.xy;
                float2 uv2 = positionWS.yz * _BubbleScale + _Time.y * _BubbleSpeed.xy;
                float2 uv3 = positionWS.xz * _BubbleScale + _Time.y * _BubbleSpeed.xy;

                half bubble1 = SAMPLE_TEXTURE2D(_BubbleMap, sampler_BubbleMap, uv1).r;
                half bubble2 = SAMPLE_TEXTURE2D(_BubbleMap, sampler_BubbleMap, uv2).r;
                half bubble3 = SAMPLE_TEXTURE2D(_BubbleMap, sampler_BubbleMap, uv3).r;
                
                return (bubble1 + bubble2 + bubble3) * _BubbleDensity;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = SafeNormalize(_WorldSpaceCameraPos - input.positionWS);
                
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float2 refractionOffset = normalWS.xy * _RefractionStrength;
                float sceneDepth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV + refractionOffset).r;
                float surfaceDepth = input.screenPos.w;
                
                float depthDifference = LinearEyeDepth(sceneDepth, _ZBufferParams) - surfaceDepth;
                float depthFade = saturate(depthDifference / _MaxDepth);

                half3 sceneColor = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + refractionOffset).rgb;

                float4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 albedo = texColor.rgb * _ColorTint.rgb;
                
                albedo = lerp(albedo, _DepthColor.rgb, depthFade * _DepthColor.a);
                
                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    Light mainLight = GetMainLight(input.shadowCoord);
                #else
                    Light mainLight = GetMainLight();
                #endif
                
                float sssDot = dot(normalWS, mainLight.direction) * 0.5 + 0.5;
                float sssFalloff = saturate(sssDot);
                half3 sssColor = lerp(1, sssFalloff, 1 - _SSSStrength);
                
                float ndotl = saturate(dot(normalWS, mainLight.direction));
                half toonIntensity = smoothstep(0, _ToonThreshold, ndotl) * mainLight.shadowAttenuation;
                half3 lightColor = mainLight.color * toonIntensity * sssColor;
                
                float3 halfVec = SafeNormalize(mainLight.direction + viewDirWS);
                float ndoth = saturate(dot(normalWS, halfVec));
                half3 specular = _SpecularColor.rgb * pow(ndoth, _Shininess) * toonIntensity;

                float rimDot = 1.0 - saturate(dot(viewDirWS, normalWS));
                half3 rimColor = _RimColor.rgb * pow(rimDot, _RimPower);
                
                half3 bubbles = SampleBubbles(input.positionWS);

                // --- LOGIC ĐÃ CẬP NHẬT ---
                // Tính màu cơ bản của slime không bao gồm các hiệu ứng cộng thêm
                half3 baseColor = albedo * lightColor + specular + rimColor;
                // Tạo hiệu ứng bong bóng phát sáng bằng cách nhân màu gốc của bong bóng với màu phát sáng
                half3 glowingBubbles = bubbles * (1.0 + _EmissionColor.rgb);
                // Kết hợp màu cơ bản và bong bóng phát sáng
                half3 finalRGB = baseColor + glowingBubbles;
                
                float surfaceAlpha = _ColorTint.a * texColor.a;
                float alphaFromSurface = lerp(1.0, surfaceAlpha, _SurfaceTransparency);
                float alphaFromDepth = lerp(alphaFromSurface, _DepthTransparency, depthFade);
                
                finalRGB = lerp(sceneColor, finalRGB, alphaFromDepth);

                return half4(finalRGB, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Shaders/ToonySlime/SlimeAnimation.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _NoiseScale;
                float _NoiseSpeed;
                float _NoiseAmplitude;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                
                float3 animatedPositionOS = AnimateVertexWithNoise(input.positionOS.xyz, input.normalOS, _NoiseScale, _NoiseSpeed, _NoiseAmplitude);
                VertexPositionInputs vertexInput = GetVertexPositionInputs(animatedPositionOS);
                output.positionCS = GetShadowPositionHClip(vertexInput);
                
                return output;
            }

            half4 frag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Transparent/VertexLit"
    CustomEditor "ToonySlimeShaderGUI"
}