// Gần như giống hệt ToonUber_Opaque.shader, nhưng thêm Pass "Outline" ở đầu
// và có các thuộc tính Outline được hiển thị
Shader "Custom/Toon Uber/Opaque (Hull Outline)"
{
    Properties
    {
        // ... (Sao chép toàn bộ khối Properties từ ToonUber_Opaque.shader)
        [HideInInspector] _SurfaceType("Surface Type", Float) = 0
        [HideInInspector] _OutlineMode("Outline Mode", Float) = 1 // Giá trị mặc định là Inverted Hull
        
        [Header(Base Properties)]
        _BaseMap("Albedo (RGB) Alpha (A)", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [Header(Alpha Clipping)]
        [Enum(Off, 0, On, 1)] _AlphaClipMode("Enable Alpha Clip", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        
        [Header(Emission)]
        [Enum(Off, 0, On, 1)] _EmissionMode("Enable Emission", Float) = 0
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,1)
        _EmissionMap("Emission Map", 2D) = "black" {}

        [Header(Lighting)]
        [Enum(Off, 0, On, 1)] _FakeLightMode("Enable Fake Light", Float) = 1
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
        _OutlineWidth("Width", Range(0.0, 10)) = 0.01
        [Enum(Off, 0, On, 1)] _OutlineScaleWithDistance("Scale With Distance", Float) = 1
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

            #include "Assets\Shaders\Toon\ToonUberBreakDownShader\Includes\ToonUberCore.hlsl"
            
            struct OutlineVaryings { float4 positionCS : SV_POSITION; };
            
            OutlineVaryings OutlineVert(Attributes input)
            {
                OutlineVaryings o = (OutlineVaryings)0;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float4 positionCS = TransformWorldToHClip(positionWS);

                float cameraDist = length(positionWS - _WorldSpaceCameraPos.xyz);
                float distFade = 1.0 - saturate((cameraDist - _DistanceFadeStart) / (_DistanceFadeEnd - _DistanceFadeStart + 1e-5));
                float scaledWidth = _OutlineWidth * distFade;

                #if defined(_OUTLINE_SCALE_WITH_DISTANCE)
                    scaledWidth *= positionCS.w * 0.01;
                #endif

                float3 normalVS = TransformWorldToView(TransformObjectToWorldNormal(input.normalOS));
                float2 projectedNormal = normalize(TransformWViewToHClip(float4(normalVS, 0)).xy);
                positionCS.xy += projectedNormal * scaledWidth;
                
                o.positionCS = positionCS;
                return o;
            }
            half4 OutlineFrag(OutlineVaryings input) : SV_Target { return _OutlineColor; }
            ENDHLSL
        }
        
        // ... (Sao chép toàn bộ khối Pass "ForwardLit" và "ShadowCaster" từ ToonUber_Opaque.shader vào đây)
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
            
            #pragma shader_feature_local _SURFACETYPE_OPAQUE _SURFACETYPE_METALLIC _SURFACETYPE_FOLIAGE
            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local_fragment _EMISSION_ON
            #pragma shader_feature_local_fragment _FAKELIGHT_ON
            // Fresnel Outline không có trong shader này, có thể bỏ keyword đi
            // #pragma shader_feature_local_fragment _OUTLINEMODE_FRESNEL

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            
            #include "Assets\Shaders\Toon\ToonUberBreakDownShader\Includes\ToonUberCore.hlsl"
            #include "Assets\Shaders\Toon\ToonUberBreakDownShader\Includes\ToonUber_Lighting.hlsl"
            #include "Assets\Shaders\Toon\ToonUberBreakDownShader\Includes\ToonUber_Foliage.hlsl"

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
                
                half3 surfaceColor = 0;
                #if defined(_SURFACETYPE_OPAQUE)
                    float3 lighting = CalculateToonLighting(i.normalWS, i.positionWS, mainLight);
                    surfaceColor = albedo.rgb * (lighting + SampleSH(i.normalWS));
                #elif defined(_SURFACETYPE_METALLIC)
                    float3 lighting = CalculateMetallicLighting(i.normalWS, viewDir, mainLight);
                    surfaceColor = albedo.rgb * lighting;
                #elif defined(_SURFACETYPE_FOLIAGE)
                    float3 lighting = CalculateFoliageLighting(i.normalWS, i.positionWS, mainLight);
                    surfaceColor = albedo.rgb * (lighting + SampleSH(i.normalWS));
                #endif

                surfaceColor = ApplyEmission(surfaceColor, i.uv);
                // Fresnel outline không áp dụng ở đây
                // surfaceColor = ApplyFresnelOutline(surfaceColor, i.normalWS, viewDir);

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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Assets\Shaders\Toon\ToonUberBreakDownShader\Includes\ToonUberCore.hlsl"
            #include "Assets\Shaders\Toon\ToonUberBreakDownShader\Includes\ToonUber_Foliage.hlsl"

            struct ShadowVaryings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            ShadowVaryings ShadowVert(Attributes input)
            {
                ShadowVaryings o;
                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(input.positionOS.xyz, input.color);
                #endif
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.positionCS = GetShadowPositionHClip(input.positionOS, posWS, normalWS);
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
    CustomEditor "ToonUberShaderSeparateGUI"
}