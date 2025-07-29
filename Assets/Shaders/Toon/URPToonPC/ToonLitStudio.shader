Shader "Toon/Lit Studio"
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
        _ShadowTintInfluence("Light Color On Shadow", Range(0, 1)) = 0.2

        [Header(Gradient Ambient)]
        [Toggle(_GRADIENT_AMBIENT_ON)] _EnableGradientAmbient("Enable Gradient Ambient", Float) = 0
        _SkyColor("Sky Color", Color) = (0.2, 0.3, 0.4, 1)
        _GroundColor("Ground Color", Color) = (0.1, 0.1, 0.1, 1)
        _AmbientGradientPower("Ambient Gradient Power", Range(0.1, 5)) = 1.0

        [Header(Additional Lights)]
        [Toggle(_ADDITIONAL_LIGHTS_ON)] _EnableAdditionalLights("Enable Additional Lights", Float) = 1
        _AdditionalLightInfluence("Additional Light Influence", Range(0, 1)) = 1.0

        [Header(Artistic Effects)]
        [Toggle(_HATCHING_ON)] _EnableHatching("Enable Shadow Hatching", Float) = 0
        _HatchingMap("Hatching Map", 2D) = "gray" {}
        _HatchingTiling("Hatching Tiling", Float) = 1.0
        _HatchingVisibility("Hatching Visibility", Range(0, 1)) = 1.0
        
        [Space(10)]
        [Toggle(_MATCAP_ON)] _EnableMatcap("Enable MatCap", Float) = 0
        _MatcapMap("MatCap Map", 2D) = "gray" {}
        _MatcapBlendMode("MatCap Blend Mode", Float) = 0.0 // 0: Add, 1: Multiply, 2: Lerp
        _MatcapTint("MatCap Tint", Color) = (1,1,1,1)
        _MatcapIntensity("MatCap Intensity", Range(0, 5)) = 1.0

        [Header(Surface Effects)]
        [Toggle(_SPECULAR_ON)] _EnableSpecular("Enable Specular", Float) = 1
        _SpecularColor("Specular Color", Color) = (1,1,1,1)
        _SpecularThreshold("Specular Threshold", Range(0, 1)) = 0.95
        _SpecularSmoothness("Specular Smoothness", Range(0.001, 1)) = 0.02
        
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
            #pragma shader_feature_local_fragment _GRADIENT_AMBIENT_ON
            #pragma shader_feature_local_fragment _ADDITIONAL_LIGHTS_ON
            #pragma shader_feature_local_fragment _HATCHING_ON
            #pragma shader_feature_local_fragment _MATCAP_ON
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
                sampler2D _BaseMap; float4 _BaseMap_ST;
                half4 _HighlightColor, _MidtoneColor, _ShadowColor;
                half _HighlightThreshold, _ShadowThreshold, _RampSmoothness;
                half4 _FakeLightDirection;
                half4 _CustomShadowColor; half _ShadowTintInfluence;
                half4 _SkyColor, _GroundColor; half _AmbientGradientPower;
                half _AdditionalLightInfluence;
                sampler2D _HatchingMap; half _HatchingTiling; half _HatchingVisibility;
                sampler2D _MatcapMap; half _MatcapBlendMode; half4 _MatcapTint; half _MatcapIntensity;
                half4 _SpecularColor; half _SpecularThreshold, _SpecularSmoothness;
                half4 _RimColor; half _RimPower, _RimThreshold;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
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
            
            half4 Frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = SafeNormalize(_WorldSpaceCameraPos - input.positionWS);
                half4 baseColor = tex2D(_BaseMap, input.uv);
                half3 finalLighting = half3(0,0,0);
                
                Light mainLight = GetMainLight(input.shadowCoord);
                #if _USE_FAKE_LIGHT
                    mainLight.direction = normalize(_FakeLightDirection.xyz);
                #endif
                
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                finalLighting += CalculateToonRamp(NdotL, mainLight.color);

                #if _SPECULAR_ON
                    half3 halfDir = SafeNormalize(mainLight.direction + viewDirWS);
                    half NdotH = saturate(dot(normalWS, halfDir));
                    half specIntensity = smoothstep(_SpecularThreshold, _SpecularThreshold + _SpecularSmoothness, NdotH);
                    finalLighting += specIntensity * _SpecularColor.rgb * mainLight.color;
                #endif

                #if _ADDITIONAL_LIGHTS_ON
                    int additionalLightsCount = GetAdditionalLightsCount();
                    for (int i = 0; i < additionalLightsCount; ++i)
                    {
                        Light addLight = GetAdditionalLight(i, input.positionWS, input.shadowCoord);
                        half addNdotL = saturate(dot(normalWS, addLight.direction));
                        half attenuation = addLight.distanceAttenuation * addLight.shadowAttenuation;
                        half3 addColor = CalculateToonRamp(addNdotL, addLight.color);
                        finalLighting += addColor * attenuation * _AdditionalLightInfluence;
                    }
                #endif

                half3 finalColor = finalLighting * baseColor.rgb;
                
                half3 ambient = SampleSH(normalWS);
                #if _GRADIENT_AMBIENT_ON
                    half ambientFactor = pow(saturate(normalWS.y * 0.5 + 0.5), _AmbientGradientPower);
                    ambient = lerp(_GroundColor.rgb, _SkyColor.rgb, ambientFactor);
                #endif
                finalColor += ambient;

                #if _HATCHING_ON
                    half shadowRegionMask = smoothstep(_ShadowThreshold + _RampSmoothness, _ShadowThreshold - _RampSmoothness, NdotL);
                    if (shadowRegionMask > 0.01)
                    {
                        float2 screenUV = input.positionCS.xy / _ScreenParams.xy;
                        half hatchingPattern = tex2D(_HatchingMap, screenUV * _HatchingTiling).r;
                        hatchingPattern = lerp(1.0, hatchingPattern, _HatchingVisibility);
                        finalColor = lerp(finalColor, finalColor * hatchingPattern, shadowRegionMask);
                    }
                #endif
                
                #if _MATCAP_ON
                    float3 viewNormal = mul((float3x3)UNITY_MATRIX_V, normalWS);
                    float2 matcapUV = viewNormal.xy * 0.5 + 0.5;
                    half3 matcapColor = tex2D(_MatcapMap, matcapUV).rgb * _MatcapTint.rgb * _MatcapIntensity;
                    if (_MatcapBlendMode < 0.5) finalColor += matcapColor; // Add
                    else if (_MatcapBlendMode < 1.5) finalColor *= matcapColor; // Multiply
                    else finalColor = lerp(finalColor, matcapColor, _MatcapTint.a); // Lerp
                #endif

                #if _RIM_LIGHT_ON
                    half NdotV = 1.0 - saturate(dot(normalWS, viewDirWS));
                    half rimFactor = pow(NdotV, _RimPower);
                    rimFactor = smoothstep(_RimThreshold - 0.1, _RimThreshold + 0.1, rimFactor);
                    finalColor += rimFactor * _RimColor.rgb * _RimColor.a;
                #endif
                
                half3 shadowTint = lerp(_CustomShadowColor.rgb, mainLight.color, _ShadowTintInfluence);
                finalColor = lerp(finalColor * shadowTint, finalColor, mainLight.shadowAttenuation);

                return half4(finalColor, baseColor.a);
            }

            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag

            #pragma shader_feature_local _ALPHACLIP_ON
            #pragma shader_feature_local _SURFACETYPE_FOLIAGE
            
            #include "Assets/Shaders/Toon/ToonUberBreakDownShader/Includes/ToonUberCore.hlsl"

            struct VaryingsDepthNormals
            {
                float4 positionCS   : SV_POSITION;
                float3 normalWS     : TEXCOORD0;
                float2 uv           : TEXCOORD1;
            };
            
            VaryingsDepthNormals DepthNormalsVert(Attributes v)
            {
                VaryingsDepthNormals o;
                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(v.positionOS.xyz, v.color);
                #endif
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            half4 DepthNormalsFrag(VaryingsDepthNormals i) : SV_TARGET
            {
                ApplyAlphaClip(i.uv);
                
                float3 normalWS = normalize(i.normalWS);
                return float4(normalWS * 0.5 + 0.5, 1.0);
            }
            ENDHLSL
        }
        
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
    CustomEditor "ToonLitStudioShaderGUI"
}