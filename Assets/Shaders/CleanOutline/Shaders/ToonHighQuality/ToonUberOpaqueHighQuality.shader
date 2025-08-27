Shader "Bill's Toon/High Quality Opaque"
{
    Properties
    {
        [HideInInspector] _SurfaceType("Surface Type", Float) = 0

        [Header(Base Properties)]
        _BaseMap("Albedo (RGB) Alpha (A)", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [Header(Alpha Clipping)]
        [Toggle(_ALPHACLIP_ON)] _AlphaClipMode("Enable Alpha Clip", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        
        [Header(Emission)]
        [Toggle(_EMISSION_ON)] _EmissionMode("Enable Emission", Float) = 0
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,1)
        _EmissionMap("Emission Map", 2D) = "black" {}
        
        [Header(Render States)]
        [Enum(Off, 0, Front, 1, Back, 2)] _CullMode ("Culling Mode", Float) = 2

        [Header(Fake Light)]
        [Toggle(_FAKELIGHT_ON)] _FakeLightMode("Enable Fake Light", Float) = 1
        _FakeLightColor("Fake Light Color", Color) = (0.8, 0.8, 0.8, 1)
        _FakeLightDirection("Fake Light Direction", Vector) = (0.5, 0.5, -0.5, 0)

        [Header(Standard Toon Shading)]
        _HighlightThreshold("Highlight Threshold", Range(0.0, 1.0)) = 0.9
        _MidtoneThreshold("Midtone Threshold", Range(0.0, 1.0)) = 0.7
        _ShadowThreshold("Shadow Threshold", Range(0.0, 1.0)) = 0.5
        _RampSmoothness("Ramp Smoothness", Range(0.001, 0.2)) = 0.05
        [HDR] _HighlightColor("Highlight Color", Color) = (1.2, 1.2, 1.2, 1)
        [HDR] _MidtoneColor("Midtone Color", Color) = (0.8, 0.8, 0.8, 1)
        [HDR] _ShadowColor("Shadow Color", Color) = (0.4, 0.4, 0.5, 1)

        [Header(Stylized Metallic Shading)]
        _MetallicHotSpotThreshold("Hot Spot Threshold", Range(0.0, 1.0)) = 0.95
        _MetallicSpecularThreshold("Specular Threshold", Range(0.0, 1.0)) = 0.8
        _MetallicReflectionThreshold("Reflection Threshold", Range(0.0, 1.0)) = 0.6
        _MetallicRampSmoothness("Metallic Ramp Smoothness", Range(0.001, 0.2)) = 0.05
        [HDR] _MetallicBaseColor("Base Color", Color) = (0.1, 0.1, 0.1, 1)
        [HDR] _MetallicReflectionColor("Broad Reflection Color", Color) = (0.5, 0.45, 0.4, 1)
        [HDR] _MetallicSpecularColor("Specular Color", Color) = (0.9, 0.7, 0.5, 1)
        [HDR] _MetallicHotSpotColor("Hot Spot Color", Color) = (1.5, 1.5, 1.5, 1)
        [HDR] _RimColor("Rim Color", Color) = (1, 0.3, 0.3, 1)
        _RimPower("Rim Power", Range(0.1, 20)) = 6.0
        
        [Header(Foliage)]
        _WindFrequency("Wind Frequency", Range(0.1, 10)) = 2.0
        _WindAmplitude("Wind Amplitude", Range(0, 1)) = 0.1
        _WindDirection("Wind Direction", Vector) = (1, 0, 0.5, 0)
        [HDR] _TranslucencyColor("Translucency Color", Color) = (0.7, 0.9, 0.3, 1)
        _TranslucencyStrength("Translucency Strength", Range(0, 5)) = 1.0
        
        [Header(Outline Properties (Fresnel))]
        [Toggle(_OUTLINEMODE_FRESNEL)] _FresnelOutlineToggle("Enable Fresnel Outline", Float) = 1
        [HDR] _FresnelOutlineColor("Color", Color) = (0, 0, 0, 1)
        _FresnelOutlineWidth("Width", Range(0.001, 1.0)) = 0.1
        _FresnelOutlinePower("Power", Range(1.0, 100.0)) = 5.0
        _FresnelOutlineSharpness("Sharpness", Range(0.1, 10.0)) = 2.0

        [Toggle(_OUTLINEGLINT_ON)] _GlintToggle("Enable Glint Effect", Float) = 0
        [HDR] _GlintColor("Glint Color", Color) = (1, 1, 0.5, 1)
        _GlintScale("Glint Scale", Float) = 20.0
        _GlintSpeed("Glint Speed", Range(0.1, 10.0)) = 2.0
        _GlintThreshold("Glint Threshold", Range(0.5, 0.99)) = 0.95

        [Header(Advanced)]
        _AmbientColor("Ambient Color", Color) = (0.5, 0.5, 0.5, 0)
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            Cull [_CullMode]
            ZWrite On
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile_local _SURFACETYPE_OPAQUE _SURFACETYPE_METALLIC _SURFACETYPE_FOLIAGE
            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local_fragment _EMISSION_ON
            #pragma shader_feature_local_fragment _FAKELIGHT_ON
            #pragma shader_feature_local_fragment _OUTLINEMODE_FRESNEL
            #pragma shader_feature_local_fragment _OUTLINEGLINT_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            
            #include "Includes/ToonUberCoreHighQuality.hlsl"

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(v.positionOS.xyz, v.color);
                #endif
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i, half frontFace : VFACE) : SV_Target
            {
                ApplyAlphaClip(i.uv);
                
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                float3 viewDir = SafeNormalize(_WorldSpaceCameraPos.xyz - i.positionWS);
                float3 normalWS = normalize(i.normalWS * sign(frontFace));
                
                Light mainLight = GetEffectiveMainLight(i.positionWS);

                half3 sceneAmbient = SampleSH(normalWS); 
                half3 ambient = lerp(sceneAmbient, _AmbientColor.rgb, _AmbientColor.a);
                
                half3 lighting = 0;
                #if defined(_SURFACETYPE_OPAQUE)
                    lighting = CalculateHighQualityToonLighting(normalWS, i.positionWS, mainLight, albedo.rgb);
                #elif defined(_SURFACETYPE_METALLIC)
                    lighting = CalculateHighQualityMetallicLighting(normalWS, viewDir, i.positionWS, mainLight);
                #elif defined(_SURFACETYPE_FOLIAGE)
                    lighting = CalculateFoliageLighting(normalWS, i.positionWS, mainLight);
                #endif

                half3 surfaceColor = lighting + (albedo.rgb * ambient);
                surfaceColor = ApplyEmission(surfaceColor, i.uv);
                surfaceColor = ApplyFresnelOutline(surfaceColor, normalWS, viewDir, i.positionWS);

                return half4(surfaceColor, albedo.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On 
            ZTest LEqual 
            ColorMask 0
            Cull [_CullMode]

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local _SURFACETYPE_FOLIAGE
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Includes/ToonUberCoreHighQuality.hlsl"

            struct ShadowVaryings 
            { 
                float4 positionCS : SV_POSITION; 
                float2 uv : TEXCOORD0; 
            };

            ShadowVaryings ShadowVert(Attributes input)
            {
                ShadowVaryings o;
                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(input.positionOS.xyz, input.color);
                #endif
                o.positionCS = GetShadowCoord(GetVertexPositionInputs(input.positionOS.xyz));
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }

            half4 ShadowFrag(ShadowVaryings i) : SV_Target
            {
                ApplyAlphaClip(i.uv);
                return 0;
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
            
            #include "Assets/Shaders/CleanOutline/Shaders/ToonHighQuality/Includes/ToonUberCoreHighQuality.hlsl"

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
    }
    CustomEditor "ToonOpaqueHighQualityShaderGUI"
}