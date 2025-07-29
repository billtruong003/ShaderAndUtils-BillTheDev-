Shader "Toon/Lit"
{
    Properties
    {
        [Header(Main Shading Ramp)]
        _BaseMap("Base Map (Albedo)", 2D) = "white" {}
        _HighlightColor("Highlight Color", Color) = (1,1,1,1)
        _MidtoneColor("Midtone Color", Color) = (0.8, 0.8, 0.8, 1)
        _ShadowColor("Shadow Color", Color) = (0.4, 0.4, 0.4, 1)
        _HighlightThreshold("Highlight Threshold", Range(0, 1)) = 0.8
        _ShadowThreshold("Shadow Threshold", Range(0, 1)) = 0.4
        _RampSmoothness("Ramp Smoothness", Range(0.001, 1)) = 0.05

        [Header(Lighting Control)]
        [Toggle(_USE_FAKE_LIGHT)] _UseFakeLight("Use Fake Light Direction", Float) = 0
        _FakeLightDirection("Fake Light Direction", Vector) = (0, 1, 0, 0)
        _CustomShadowColor("Main Light Custom Shadow Color", Color) = (0,0,0,1)
        _AmbientColor("Custom Ambient Color (RGB) & Intensity (A)", Color) = (0.1, 0.1, 0.2, 1)

        [Header(Additional Lights)]
        [Toggle(_ADDITIONAL_LIGHTS_ON)] _EnableAdditionalLights("Enable Additional Lights", Float) = 1
        _AdditionalLightInfluence("Additional Light Influence", Range(0, 1)) = 1.0

        [Header(Specular Reflection)]
        [Toggle(_SPECULAR_ON)] _EnableSpecular("Enable Specular", Float) = 1
        _SpecularColor("Specular Color", Color) = (1,1,1,1)
        _SpecularThreshold("Specular Threshold", Range(0, 1)) = 0.95
        _SpecularSmoothness("Specular Smoothness", Range(0.001, 1)) = 0.02

        [Header(Rim Light)]
        [Toggle(_RIM_LIGHT_ON)] _EnableRimLight("Enable Rim Light", Float) = 1
        _RimColor("Rim Color", Color) = (1,1,1,1)
        _RimPower("Rim Power", Range(1, 10)) = 3.0
        _RimThreshold("Rim Threshold", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma shader_feature_local_fragment _USE_FAKE_LIGHT
            #pragma shader_feature_local_fragment _ADDITIONAL_LIGHTS_ON
            #pragma shader_feature_local_fragment _SPECULAR_ON
            #pragma shader_feature_local_fragment _RIM_LIGHT_ON

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
                float2 uv           : TEXCOORD2;
                half4 shadowCoord   : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                sampler2D _BaseMap;
                float4 _BaseMap_ST;
                half4 _HighlightColor;
                half4 _MidtoneColor;
                half4 _ShadowColor;
                half _HighlightThreshold;
                half _ShadowThreshold;
                half _RampSmoothness;
                half4 _FakeLightDirection;
                half4 _CustomShadowColor;
                half4 _AmbientColor;
                half _AdditionalLightInfluence;
                half4 _SpecularColor;
                half _SpecularThreshold;
                half _SpecularSmoothness;
                half4 _RimColor;
                half _RimPower;
                half _RimThreshold;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;

                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.normalWS = normalInputs.normalWS;
                
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.shadowCoord = GetShadowCoord(positionInputs);
                return output;
            }

            half3 CalculateToonRamp(half NdotL, half3 lightColor)
            {
                half smoothness = _RampSmoothness * 0.5;
                half highlightFactor = smoothstep(_HighlightThreshold - smoothness, _HighlightThreshold + smoothness, NdotL);
                half shadowFactor = smoothstep(_ShadowThreshold - smoothness, _ShadowThreshold + smoothness, NdotL);

                half3 rampColor = lerp(_ShadowColor.rgb, _MidtoneColor.rgb, shadowFactor);
                rampColor = lerp(rampColor, _HighlightColor.rgb, highlightFactor);

                return rampColor * lightColor;
            }
            
            half3 CalculateSpecular(half3 normalWS, half3 lightDir, float3 viewDirWS, half3 lightColor)
            {
                #if _SPECULAR_ON
                    half3 halfDir = SafeNormalize(lightDir + viewDirWS);
                    half NdotH = saturate(dot(normalWS, halfDir));
                    half specIntensity = smoothstep(_SpecularThreshold, _SpecularThreshold + _SpecularSmoothness, NdotH);
                    return specIntensity * _SpecularColor.rgb * lightColor;
                #else
                    return 0;
                #endif
            }

            half3 CalculateRimLight(half3 normalWS, float3 viewDirWS)
            {
                #if _RIM_LIGHT_ON
                    half NdotV = 1.0 - saturate(dot(normalWS, viewDirWS));
                    half rimFactor = pow(NdotV, _RimPower);
                    rimFactor = smoothstep(_RimThreshold - 0.1, _RimThreshold + 0.1, rimFactor);
                    return rimFactor * _RimColor.rgb * _RimColor.a;
                #else
                    return 0;
                #endif
            }
            
            half4 Frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = SafeNormalize(_WorldSpaceCameraPos - input.positionWS);
                half4 baseColor = tex2D(_BaseMap, input.uv);
                half3 finalLighting = half3(0,0,0);
                
                // --- ÁNH SÁNG CHÍNH --- //
                // SỬA LỖI: GetMainLight yêu cầu shadowCoord từ Varyings.
                Light mainLight = GetMainLight(input.shadowCoord);
                
                #if _USE_FAKE_LIGHT
                    mainLight.direction = normalize(_FakeLightDirection.xyz);
                #endif
                
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                finalLighting += CalculateToonRamp(NdotL, mainLight.color);
                finalLighting += CalculateSpecular(normalWS, mainLight.direction, viewDirWS, mainLight.color);
                
                // --- ÁNH SÁNG PHỤ --- //
                #if _ADDITIONAL_LIGHTS_ON
                    int additionalLightsCount = GetAdditionalLightsCount();
                    for (int i = 0; i < additionalLightsCount; ++i)
                    {
                        // SỬA LỖI: GetAdditionalLight cần cả worldPos và shadowCoord.
                        Light addLight = GetAdditionalLight(i, input.positionWS, input.shadowCoord);
                        half addNdotL = saturate(dot(normalWS, addLight.direction));
                        half attenuation = addLight.distanceAttenuation * addLight.shadowAttenuation;

                        half3 addColor = CalculateToonRamp(addNdotL, addLight.color);
                        addColor += CalculateSpecular(normalWS, addLight.direction, viewDirWS, addLight.color);
                        finalLighting += addColor * attenuation * _AdditionalLightInfluence;
                    }
                #endif

                // --- MÔI TRƯỜNG & HIỆU ỨNG --- //
                half3 ambient = SampleSH(normalWS);
                half3 finalColor = finalLighting * baseColor.rgb;
                finalColor += lerp(ambient, _AmbientColor.rgb, _AmbientColor.a);
                finalColor += CalculateRimLight(normalWS, viewDirWS);
                
                // --- XỬ LÝ BÓNG ĐỔ --- //
                finalColor = lerp(finalColor * _CustomShadowColor.rgb, finalColor, mainLight.shadowAttenuation);

                return half4(finalColor, baseColor.a);
            }
            ENDHLSL
        }
        
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
    // CustomEditor "ToonLitShaderGUI"
}