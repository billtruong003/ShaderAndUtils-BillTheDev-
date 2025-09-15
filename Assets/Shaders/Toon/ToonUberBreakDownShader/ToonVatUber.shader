Shader "Bill's Toon/Toon VAT Uber"
{
    Properties
    {
        [HideInInspector] 
        [Enum(Opaque, 0, Metallic, 1, Foliage, 2, Glass, 3 , Galaxy, 4)] 
        _SurfaceType("Surface Type", Float) = 0
        [HideInInspector] _SrcBlend ("Src Blend Mode", Float) = 1.0
        [HideInInspector] _DstBlend ("Dst Blend Mode", Float) = 0.0
        [HideInInspector] _ZWrite ("ZWrite", Float) = 1.0

        [Header(VAT Settings)]
        [Toggle(_VAT_ON)] _VatMode ("Enable VAT", Float) = 0
        [Toggle(_VAT_INSTANCING_ON)] _EnableVatInstancing("Enable VAT Instancing", Float) = 0
        
        [Header(VAT Data (Required if Enabled))]
        [NoScaleOffset] _PositionTexture ("Position Texture (VAT)", 2D) = "white" {}
        _PositionMin ("Position Min (Local Space)", Vector) = (0,0,0,0)
        _PositionMax ("Position Max (Local Space)", Vector) = (0,0,0,0)

        [Header(VAT Animation Control (Driven by Script))]
        _CurrentAnimNormalizedTime ("Current Anim Time (0-1)", Range(0,1)) = 0
        _PreviousAnimNormalizedTime ("Previous Anim Time (0-1)", Range(0,1)) = 0
        _AnimationBlendWeight ("Animation Blend Weight", Range(0,1)) = 0

        [Header(Base Properties)]
        _BaseMap("Albedo (RGB) Alpha (A)", 2D) = "white" {}
        [HDR] _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [Header(Alpha Clipping)]
        [Toggle(_ALPHACLIP_ON)] _AlphaClipMode("Enable Alpha Clip", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        
        [Header(Emission)]
        [Toggle(_EMISSION_ON)] _EmissionMode("Enable Emission", Float) = 0
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,1)
        _EmissionMap("Emission Map", 2D) = "black" {}

        [Header(Lighting)]
        [Toggle(_FAKELIGHT_ON)] _FakeLightMode("Enable Fake Light", Float) = 1
        [HDR] _FakeLightColor("Fake Light Color", Color) = (0.8, 0.8, 0.8, 1)
        _FakeLightDirection("Fake Light Direction", Vector) = (0.5, 0.5, -0.5, 0)

        [Header(Toon Shading Opaque)]
        _ToonRampOffset("Ramp Offset", Range(0.0, 1.0)) = 0.5
        _ToonRampSmoothness("Ramp Smoothness", Range(0.001, 1.0)) = 0.05
        [HDR] _ShadowTint("Shadow Tint", Color) = (0.1, 0.1, 0.2, 1.0)
        [HDR] _AmbientColor("Ambient Color", Color) = (0.5, 0.5, 0.5, 0)

        [Header(Stylized Metal Metallic)]
        _Ramp("Toon Ramp (RGB)", 2D) = "white" {} 
        _Brightness("Specular Brightness", Range(0, 2)) = 1.3  
        _Offset("Specular Size", Range(0, 1)) = 0.8
        [HDR] _SpecuColor("Specular Color", Color) = (0.8,0.45,0.2,1)
        _HighlightOffset("Highlight Size", Range(0, 1)) = 0.9  
        [HDR] _HiColor("Highlight Color", Color) = (1,1,1,1)
        [HDR] _RimColor("Rim Color", Color) = (1,0.3,0.3,1)
        _RimPower("Rim Power", Range(0, 20)) = 6
        
        [Header(Foliage Settings)]
        _WindFrequency("Wind Frequency", Range(0.1, 10)) = 2.0
        _WindAmplitude("Wind Amplitude", Range(0, 1)) = 0.1
        _WindDirection("Wind Direction", Vector) = (1, 0, 0.5, 0)
        [HDR] _TranslucencyColor("Translucency Color", Color) = (0.7, 0.9, 0.3, 1)
        _TranslucencyStrength("Translucency Strength", Range(0, 5)) = 1.0
        
        [Header(Glass Settings)]
        [HDR] _GlassColor("Glass Color & Transparency", Color) = (0.8, 0.9, 1.0, 0.5)
        [HDR] _FresnelColor("Fresnel Color", Color) = (1,1,1,1)
        _FresnelPower("Fresnel Power", Range(0.1, 20)) = 5.0
        _RefractionStrength("Refraction Strength", Range(0, 0.1)) = 0.02
        _GlassSpecularPower("Specular Power", Range(1, 100)) = 30
        _GlassSpecularIntensity("Specular Intensity", Range(0, 5)) = 1

        [Header(Outline)]
        [Enum(None, 0, Inverted Hull, 1, Fresnel, 2)] _OutlineMode("Mode", Float) = 1
        
        [HDR] _OutlineColor("Color", Color) = (0, 0, 0, 1)
        _OutlineWidth("Width", Range(0.0, 10)) = 1.0
        [Toggle] _OutlineScaleWithDistance("Screen-Space Scaling", Float) = 1
        _DistanceFadeStart("Distance Fade Start", Float) = 20
        _DistanceFadeEnd("Distance Fade End", Float) = 30
        
        [Toggle(_FRESNEL_RAMP_ON)] _FresnelRampToggle("Use Fresnel Ramp", Float) = 0
        _FresnelRampTexture("Fresnel Ramp", 2D) = "white" {}
        [HDR] _FresnelOutlineColor("Fresnel Color", Color) = (0.2, 0.8, 1, 1)
        _FresnelOutlineWidth("Fresnel Width", Range(0, 1)) = 0.1
        _FresnelOutlinePower("Fresnel Power", Range(0.1, 20)) = 4.0
        _FresnelOutlineSharpness("Fresnel Sharpness", Range(0.1, 10)) = 2.0

        [Toggle(_OUTLINEGLINT_ON)] _OutlineGlint("Enable Glint", Float) = 0
        [HDR] _GlintColor("Glint Color", Color) = (1, 1, 0.5, 1)
        _GlintScale("Glint Scale", Range(0.1, 20)) = 5.0
        _GlintSpeed("Glint Speed", Range(0.1, 10)) = 2.0
        _GlintThreshold("Glint Threshold", Range(0, 1)) = 0.8

        [Header(Galaxy Settings)]
        _StarfieldMap("Starfield Map", 2D) = "black" {}
        [HDR] _StarfieldColor("Starfield Tint", Color) = (1,1,1,1)
        _StarfieldScale("Starfield Scale", Range(0.1, 10)) = 1.0
        _NoiseMap("Noise Map", 2D) = "grey" {} 
        [HDR] _DustColor1("Dust Color 1", Color) = (0.1, 0.2, 0.8, 1)
        [HDR] _DustColor2("Dust Color 2", Color) = (0.8, 0.2, 0.7, 1)
        [HDR] _DustColor3("Dust Color 3", Color) = (0.2, 0.8, 0.7, 1)
        _NoiseScale("Noise Scale", Range(1, 100)) = 20
        _NoiseSpeed1("Noise Speed1", Range(0, 5)) = 0.5
        _NoiseSpeed2("Noise Speed2", Range(0, 5)) = 0.5
        [Toggle(_GALAXY_RIM_RAMP_ON)] _GalaxyRimRampToggle("Use Rim Ramp", Float) = 0
        _GalaxyRimRampTexture("Rim Ramp", 2D) = "white" {}
        [HDR] _GalaxyRimColor("Rim Color", Color) = (1, 1, 1, 1)
        _GalaxyRimPower("Rim Power", Range(0.1, 20)) = 3.0
        _ParallaxStrength("Parallax Depth", Range(0, 0.1)) = 0.03 
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "IgnoreProjector"="True" }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            Cull Back
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local _VAT_ON
            #pragma multi_compile_local _ _VAT_INSTANCING_ON
            
            #pragma multi_compile_local _SURFACETYPE_OPAQUE _SURFACETYPE_METALLIC _SURFACETYPE_FOLIAGE _SURFACETYPE_GLASS _SURFACETYPE_GALAXY
            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local_fragment _EMISSION_ON
            #pragma shader_feature_local_fragment _FAKELIGHT_ON
            #pragma shader_feature_local_fragment _OUTLINEMODE_FRESNEL
            #pragma shader_feature_local_fragment _OUTLINEGLINT_ON
            #pragma shader_feature_local_fragment _FRESNEL_RAMP_ON 
            #pragma shader_feature_local_fragment _GALAXY_RIM_RAMP_ON 

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            
            #include "Includes/ToonVatUberCore.hlsl"

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                float3 positionOS = GetPositionOS(v);
                
                #if defined(_SURFACETYPE_FOLIAGE) && !defined(_VAT_ON)
                    ApplyWind(positionOS, v.color);
                #endif
                
                o.positionWS = TransformObjectToWorld(positionOS);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.color = v.color;
                o.screenPos = ComputeScreenPos(o.positionCS);
                o.positionOS = positionOS;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                ApplyAlphaClip(i.uv);
                
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                float3 viewDir = SafeNormalize(_WorldSpaceCameraPos.xyz - i.positionWS);
                Light mainLight = GetEffectiveMainLight(i.positionWS);
                half3 ambient = lerp(SampleSH(i.normalWS), _AmbientColor.rgb, _AmbientColor.a);
                
                half3 surfaceColor;

                #if defined(_SURFACETYPE_GLASS)
                    surfaceColor = CalculateGlassLighting(i, mainLight, viewDir, ambient);
                #else
                    half3 lighting = 0;
                    #if defined(_SURFACETYPE_OPAQUE)
                        lighting = CalculateToonLighting(i.normalWS, i.positionWS, mainLight);
                    #elif defined(_SURFACETYPE_METALLIC)
                        lighting = CalculateMetallicLighting(i.normalWS, viewDir, mainLight);
                    #elif defined(_SURFACETYPE_FOLIAGE)
                        lighting = CalculateFoliageLighting(i.normalWS, i.positionWS, mainLight);
                    #elif defined(_SURFACETYPE_GALAXY)
                        lighting = CalculateGalaxyLighting(i.normalWS, viewDir, i.positionWS, i.positionOS);
                    #endif

                    surfaceColor = albedo.rgb * (lighting + ambient);
                    surfaceColor = ApplyEmission(surfaceColor, i.uv);
                #endif
                
                surfaceColor = ApplyFresnelOutline(surfaceColor, i.normalWS, viewDir, i.positionWS);

                #if defined(_SURFACETYPE_GLASS)
                    return half4(surfaceColor, _GlassColor.a);
                #else
                    return half4(surfaceColor, albedo.a);
                #endif
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
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #pragma shader_feature_local _VAT_ON
            #pragma multi_compile_local _ _VAT_INSTANCING_ON
            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local _SURFACETYPE_FOLIAGE _SURFACETYPE_GLASS
            
            #include "Includes/ToonVatUberCore.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            
            struct ShadowVaryings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            ShadowVaryings ShadowVert(Attributes input)
            {
                ShadowVaryings o;
                float3 positionOS = GetPositionOS(input);
                
                #if defined(_SURFACETYPE_FOLIAGE) && !defined(_VAT_ON)
                    ApplyWind(positionOS, input.color);
                #endif
                
                VertexPositionInputs positionInputs = GetVertexPositionInputs(positionOS);
                o.positionCS = GetShadowCoord(positionInputs);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }

            half4 ShadowFrag(ShadowVaryings i) : SV_Target
            {
                #if defined(_SURFACETYPE_GLASS)
                     clip(_GlassColor.a - _Cutoff);
                #endif
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

            #pragma shader_feature_local _VAT_ON
            #pragma multi_compile_local _ _VAT_INSTANCING_ON
            #pragma shader_feature_local _ALPHACLIP_ON
            #pragma shader_feature_local _SURFACETYPE_FOLIAGE
            
            #include "Includes/ToonVatUberCore.hlsl"

            struct VaryingsDepthNormals
            {
                float4 positionCS   : SV_POSITION;
                float3 normalWS     : TEXCOORD0;
                float2 uv           : TEXCOORD1;
            };
            
            VaryingsDepthNormals DepthNormalsVert(Attributes v)
            {
                VaryingsDepthNormals o;
                float3 positionOS = GetPositionOS(v);

                #if defined(_SURFACETYPE_FOLIAGE) && !defined(_VAT_ON)
                    ApplyWind(positionOS, v.color);
                #endif
                
                o.positionCS = TransformObjectToHClip(positionOS);
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
    CustomEditor "ToonVatUberShaderGUI"
}