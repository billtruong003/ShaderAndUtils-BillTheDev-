Shader "Bill's Toon/Optimized/Opaque (Hull Outline)"
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

        [Header(Lighting)]
        [Toggle(_FAKELIGHT_ON)] _FakeLightMode("Enable Fake Light", Float) = 1
        _FakeLightColor("Fake Light Color", Color) = (0.8, 0.8, 0.8, 1)
        _FakeLightDirection("Fake Light Direction", Vector) = (0.5, 0.5, -0.5, 0)

        [Header(Toon Shading)]
        _ToonRampOffset("Ramp Offset", Range(0.0, 1.0)) = 0.5
        _ToonRampSmoothness("Ramp Smoothness", Range(0.001, 1.0)) = 0.05
        _ShadowTint("Shadow Tint", Color) = (0.1, 0.1, 0.2, 1.0)

        [Header(Stylized Metal)]
        _Ramp("Toon Ramp (RGB)", 2D) = "white" {} 
        _Brightness("Specular Brightness", Range(0, 2)) = 1.3  
        _Offset("Specular Size", Range(0, 1)) = 0.8
        _SpecuColor("Specular Color", Color) = (0.8,0.45,0.2,1)
        _HighlightOffset("Highlight Size", Range(0, 1)) = 0.9  
        _HiColor("Highlight Color", Color) = (1,1,1,1)
        _RimColor("Rim Color", Color) = (1,0.3,0.3,1)
        _RimPower("Rim Power", Range(0, 20)) = 6
        
        [Header(Foliage)]
        _WindFrequency("Wind Frequency", Range(0.1, 10)) = 2.0
        _WindAmplitude("Wind Amplitude", Range(0, 1)) = 0.1
        _WindDirection("Wind Direction", Vector) = (1, 0, 0.5, 0)
        [HDR] _TranslucencyColor("Translucency Color", Color) = (0.7, 0.9, 0.3, 1)
        _TranslucencyStrength("Translucency Strength", Range(0, 5)) = 1.0
        
        [Header(Outline Properties (Inverted Hull))]
        _OutlineColor("Color", Color) = (0, 0, 0, 1)
        _OutlineWidth("Width", Range(0.0, 10)) = 1.0
        [Toggle(_OUTLINE_SCALE_WITH_DISTANCE)] _OutlineScaleWithDistance("Screen-Space Scaling", Float) = 1
        _DistanceFadeStart("Distance Fade Start", Float) = 20
        _DistanceFadeEnd("Distance Fade End", Float) = 30
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }

        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            
            #pragma shader_feature_local _OUTLINE_SCALE_WITH_DISTANCE
            #pragma shader_feature_local _SURFACETYPE_FOLIAGE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/Toon/ToonUberBreakDownShader/Includes/ToonUberCore.hlsl"
            
            struct OutlineVaryings
            {
                float4 positionCS : SV_POSITION;
            };
            
            OutlineVaryings OutlineVert(Attributes input)
            {
                OutlineVaryings o = (OutlineVaryings)0;

                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(input.positionOS.xyz, input.color);
                #endif

                float cameraDist = length(TransformObjectToWorld(input.positionOS.xyz) - _WorldSpaceCameraPos.xyz);
                float distFade = 1.0 - saturate((cameraDist - _DistanceFadeStart) / (_DistanceFadeEnd - _DistanceFadeStart + 1e-5));
                float scaledWidth = _OutlineWidth * 0.01 * distFade;

                float4 positionCS = TransformObjectToHClip(input.positionOS.xyz);
                #if defined(_OUTLINE_SCALE_WITH_DISTANCE)
                    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                    float3 normalVS = TransformWorldToView(normalWS);
                    float4 projectedNormal = mul(UNITY_MATRIX_P, float4(normalVS, 0.0));
                    positionCS.xy += normalize(projectedNormal.xy) * scaledWidth * positionCS.w;
                #else
                    float3 positionOS_final = input.positionOS.xyz + input.normalOS * scaledWidth;
                    positionCS = TransformObjectToHClip(positionOS_final);
                #endif
                
                o.positionCS = positionCS;
                return o;
            }
            
            half4 OutlineFrag(OutlineVaryings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            Cull Back
            ZWrite On
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile_local _SURFACETYPE_OPAQUE _SURFACETYPE_METALLIC _SURFACETYPE_FOLIAGE
            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local_fragment _EMISSION_ON
            #pragma shader_feature_local_fragment _FAKELIGHT_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            
            #include "Assets/Shaders/Toon/ToonUberBreakDownShader/Includes/ToonUberCore.hlsl"

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

            half4 frag(Varyings i) : SV_Target
            {
                ApplyAlphaClip(i.uv);
                
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                float3 viewDir = SafeNormalize(_WorldSpaceCameraPos.xyz - i.positionWS);
                Light mainLight = GetEffectiveMainLight(i.positionWS);
                half3 ambient = SampleSH(i.normalWS);
                
                half3 lighting = 0;
                #if defined(_SURFACETYPE_OPAQUE)
                    lighting = CalculateToonLighting(i.normalWS, i.positionWS, mainLight);
                #elif defined(_SURFACETYPE_METALLIC)
                    lighting = CalculateMetallicLighting(i.normalWS, viewDir, mainLight);
                #elif defined(_SURFACETYPE_FOLIAGE)
                    lighting = CalculateFoliageLighting(i.normalWS, i.positionWS, mainLight);
                #endif

                half3 surfaceColor = albedo.rgb * (lighting + ambient);
                surfaceColor = ApplyEmission(surfaceColor, i.uv);

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
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local _SURFACETYPE_FOLIAGE
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Assets/Shaders/Toon/ToonUberBreakDownShader/Includes/ToonUberCore.hlsl"
            
            struct ShadowVaryings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            ShadowVaryings ShadowVert(Attributes input)
            {
                ShadowVaryings o;
                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(input.positionOS.xyz, input.color);
                #endif
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = GetShadowCoord(posInputs);
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
    }
    CustomEditor "ToonOpaqueHullOutlineShaderGUI"
}