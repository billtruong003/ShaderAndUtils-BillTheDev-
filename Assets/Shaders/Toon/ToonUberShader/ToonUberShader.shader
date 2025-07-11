Shader "Custom/Toon Uber Shader"
{
    Properties
    {
        // ================== CONTROLLER ==================
        [Header(Surface Pipeline)]
        [Enum(Opaque, 0, Transparent, 1, Metallic, 2, Foliage, 3)] _SurfaceType("Surface Type", Float) = 0
        [Enum(None, 0, Inverted Hull, 1, Fresnel, 2)] _OutlineMode("Outline Mode", Float) = 0

        // ================== BASE & SHARED PROPERTIES ==================
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

        // ================== LIGHTING ==================
        [Header(Lighting)]
        [Enum(Off, 0, On, 1)] _FakeLightMode("Enable Fake Light", Float) = 1
        _FakeLightColor("Fake Light Color", Color) = (0.8, 0.8, 0.8, 1)
        _FakeLightDirection("Fake Light Direction", Vector) = (0.5, 0.5, -0.5, 0)

        // ================== OPAQUE (TOON) PROPERTIES ==================
        [Header(Toon Shading)]
        _ToonRampOffset("Ramp Offset", Range(0.0, 1.0)) = 0.5
        _ToonRampSmoothness("Ramp Smoothness", Range(0.001, 1.0)) = 0.05
        _ShadowTint("Shadow Tint", Color) = (0.1, 0.1, 0.2, 1.0)

        // ================== METALLIC (STYLIZED) PROPERTIES ==================
        [Header(Stylized Metal)]
        _Ramp("Toon Ramp (RGB)", 2D) = "white" {} 
        _Brightness("Specular Brightness", Range(0, 2)) = 1.3  
        _Offset("Specular Size", Range(0, 1)) = 0.8
        _SpecuColor("Specular Color", Color) = (0.8,0.45,0.2,1)
        [Header(Highlight)]
        _HighlightOffset("Highlight Size", Range(0, 1)) = 0.9  
        _HiColor("Highlight Color", Color) = (1,1,1,1)
        [Header(Rim)]
        _RimColor("Rim Color", Color) = (1,0.3,0.3,1)
        _RimPower("Rim Power", Range(0, 20)) = 6
        
        // ================== FOLIAGE PROPERTIES ==================
        [Header(Foliage)]
        _WindFrequency("Wind Frequency", Range(0.1, 10)) = 2.0
        _WindAmplitude("Wind Amplitude", Range(0, 1)) = 0.1
        _WindDirection("Wind Direction", Vector) = (1, 0, 0.5, 0)
        [HDR] _TranslucencyColor("Translucency Color", Color) = (0.7, 0.9, 0.3, 1)
        _TranslucencyStrength("Translucency Strength", Range(0, 5)) = 1.0

        // ================== TRANSPARENT (GLASS) PROPERTIES ==================
        [Header(Stylized Glass)]
        _GlassColor("Glass Color & Opacity", Color) = (0.8, 0.9, 1.0, 0.5)
        _FresnelColor("Fresnel (Edge) Color", Color) = (1,1,1,1)
        _FresnelPower("Fresnel Power", Range(1, 10)) = 5.0
        _RefractionStrength("Refraction Strength", Range(0, 0.1)) = 0.01
        _GlassSpecularPower("Specular Power", Range(1, 50)) = 20.0
        _GlassSpecularIntensity("Specular Intensity", Range(0, 5)) = 1.0

        // ================== OUTLINE (INVERTED HULL) ==================
        [Header(Outline Properties (Inverted Hull))]
        _OutlineColor("Color", Color) = (0, 0, 0, 1)
        _OutlineWidth("Width", Range(0.0, 10)) = 0.01
        [Enum(Off, 0, On, 1)] _OutlineScaleWithDistance("Scale With Distance", Float) = 1
        _DistanceFadeStart("Distance Fade Start", Float) = 20
        _DistanceFadeEnd("Distance Fade End", Float) = 30

        // ================== OUTLINE (FRESNEL) ==================
        [Header(Outline Properties (Fresnel))]
        _FresnelOutlineColor("Color", Color) = (0, 0, 0, 1)
        _FresnelOutlineWidth("Width", Range(0.001, 1.0)) = 0.1
        _FresnelOutlinePower("Power", Range(1.0, 20.0)) = 5.0

        // ================== RENDER STATE ==================
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0
        [HideInInspector] _Cull ("__cull", Float) = 2.0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "Outline"
            Tags { "RenderType"="Opaque" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            
            #pragma shader_feature_local _OUTLINEMODE_INVERTEDHULL
            #pragma shader_feature_local _OUTLINE_SCALE_WITH_DISTANCE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            #if defined(_OUTLINEMODE_INVERTEDHULL)
                CBUFFER_START(UnityPerMaterial)
                    float4 _OutlineColor;
                    float _OutlineWidth;
                    float _DistanceFadeStart;
                    float _DistanceFadeEnd;
                CBUFFER_END
            
                Varyings OutlineVert(Attributes input)
                {
                    Varyings output = (Varyings)0;
                    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                    float4 positionCS = TransformWorldToHClip(positionWS);

                    float cameraDist = length(positionWS - _WorldSpaceCameraPos.xyz);
                    float distFade = 1.0 - saturate((cameraDist - _DistanceFadeStart) / (_DistanceFadeEnd - _DistanceFadeStart + 1e-5));
                    float scaledWidth = _OutlineWidth * distFade;

                    #if defined(_OUTLINE_SCALE_WITH_DISTANCE)
                        scaledWidth *= positionCS.w * 0.01;
                    #endif

                    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                    float3 normalVS = TransformWorldToView(normalWS);
                    float2 projectedNormal = normalize(TransformWViewToHClip(float4(normalVS, 0)).xy);
                    positionCS.xy += projectedNormal * scaledWidth;
                    
                    output.positionCS = positionCS;
                    return output;
                }
                half4 OutlineFrag(Varyings input) : SV_Target { return _OutlineColor; }
            #else
                Varyings OutlineVert(Attributes input) { Varyings o = (Varyings)0; return o; }
                half4 OutlineFrag(Varyings input) : SV_Target { clip(-1); return 0; }
            #endif
            ENDHLSL
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "RenderType"="Opaque" "Queue"="Geometry" "LightMode"="UniversalForward" }
            
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local _SURFACETYPE_OPAQUE _SURFACETYPE_TRANSPARENT _SURFACETYPE_METALLIC _SURFACETYPE_FOLIAGE
            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local_fragment _EMISSION_ON
            #pragma shader_feature_local_fragment _OUTLINEMODE_FRESNEL
            #pragma shader_feature_local_fragment _FAKELIGHT_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/Toon/ToonUberShader/ToonShading.hlsl"
            #include "Assets/Shaders/Toon/ToonUberShader/Foliage.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor, _EmissionColor, _FakeLightColor, _FakeLightDirection;
                float _Cutoff;
                
                float _ToonRampOffset, _ToonRampSmoothness;
                float4 _ShadowTint;
                
                float _Brightness, _Offset, _HighlightOffset, _RimPower;
                float4 _SpecuColor, _HiColor, _RimColor;
                
                float4 _GlassColor, _FresnelColor;
                float _FresnelPower, _RefractionStrength, _GlassSpecularPower, _GlassSpecularIntensity;

                float _WindFrequency, _WindAmplitude;
                float3 _WindDirection;
                float3 _TranslucencyColor;
                float _TranslucencyStrength;

                float4 _FresnelOutlineColor;
                float _FresnelOutlineWidth, _FresnelOutlinePower;
            CBUFFER_END

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);
            TEXTURE2D(_Ramp); SAMPLER(sampler_Ramp);
            TEXTURE2D_X_FLOAT(_CameraOpaqueTexture); SAMPLER(sampler_CameraOpaqueTexture);

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float2 uv : TEXCOORD2; float4 color : COLOR; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(v.positionOS.xyz, v.normalOS, v.color, _WindFrequency, _WindAmplitude, _WindDirection);
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
                half3 surfaceColor = 0;
                half surfaceAlpha = 1;

                float3 viewDir = SafeNormalize(_WorldSpaceCameraPos.xyz - i.positionWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(i.positionWS));

                #if defined(_FAKELIGHT_ON)
                    if (all(mainLight.color < 0.001))
                    {
                        mainLight.direction = normalize(_FakeLightDirection.xyz);
                        mainLight.color = _FakeLightColor.rgb;
                        mainLight.shadowAttenuation = 1.0;
                    }
                #endif
                
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                #if defined(_ALPHACLIP_ON)
                    clip(albedo.a - _Cutoff);
                #endif

                #if defined(_SURFACETYPE_OPAQUE)
                    float3 lighting = CalculateToonLighting(i.normalWS, _ToonRampSmoothness, i.positionWS, _ShadowTint, _ToonRampOffset, mainLight);
                    surfaceColor = albedo.rgb * (lighting + SampleSH(i.normalWS));
                    surfaceAlpha = albedo.a;
                #elif defined(_SURFACETYPE_METALLIC)
                    half d = dot(i.normalWS, mainLight.direction) * 0.5 + 0.5;
                    half3 ramp = SAMPLE_TEXTURE2D(_Ramp, sampler_Ramp, float2(d,d)).rgb;
                    surfaceColor = albedo.rgb * mainLight.color * ramp * (mainLight.shadowAttenuation * 2);
                    
                    float3 halfVec = normalize(viewDir + mainLight.direction);
                    float specDot = saturate(dot(halfVec, i.normalWS));
                    surfaceColor += step(_Offset, specDot) * _SpecuColor.rgb * _Brightness * mainLight.color * mainLight.shadowAttenuation;
                    
                    float highlightDot = saturate(dot(i.normalWS, mainLight.direction));
                    surfaceColor += step(_HighlightOffset, highlightDot) * _HiColor.rgb * mainLight.color * mainLight.shadowAttenuation;
                    
                    half rim = 1.0 - saturate(dot(viewDir, i.normalWS));
                    surfaceColor += _RimColor.rgb * pow(rim, _RimPower);
                    surfaceAlpha = albedo.a;
                #elif defined(_SURFACETYPE_FOLIAGE)
                    float3 lighting = CalculateFoliageLighting(i.normalWS, i.positionWS, mainLight, _TranslucencyStrength, _TranslucencyColor);
                    surfaceColor = albedo.rgb * (lighting + SampleSH(i.normalWS));
                    surfaceAlpha = albedo.a;
                #elif defined(_SURFACETYPE_TRANSPARENT)
                    float fresnelDot = 1.0 - saturate(dot(i.normalWS, viewDir));
                    float fresnel = pow(fresnelDot, _FresnelPower);
                    
                    float2 screenUV = i.positionCS.xy / i.positionCS.w;
                    float2 distortion = i.normalWS.xy * _RefractionStrength;
                    float3 sceneColor = SAMPLE_TEXTURE2D_X_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + distortion, 0).rgb;
                    
                    surfaceColor = lerp(sceneColor, _GlassColor.rgb, _GlassColor.a);
                    surfaceColor = lerp(surfaceColor, _FresnelColor.rgb, fresnel);

                    float3 reflectDir = reflect(-mainLight.direction, i.normalWS);
                    float spec = pow(saturate(dot(viewDir, reflectDir)), _GlassSpecularPower);
                    surfaceColor += mainLight.color * spec * _GlassSpecularIntensity * mainLight.shadowAttenuation;
                    surfaceAlpha = _GlassColor.a;
                #endif

                #if defined(_EMISSION_ON)
                    surfaceColor += SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, i.uv).rgb * _EmissionColor.rgb;
                #endif

                #if defined(_OUTLINEMODE_FRESNEL)
                    float fresnelDot = 1.0 - saturate(dot(i.normalWS, viewDir));
                    float fresnelOutline = pow(fresnelDot, _FresnelOutlinePower);
                    float outlineFactor = smoothstep(1.0 - _FresnelOutlineWidth, 1.0 - _FresnelOutlineWidth + 0.05, fresnelOutline);
                    surfaceColor = lerp(surfaceColor, _FresnelOutlineColor.rgb, outlineFactor);
                #endif

                return half4(surfaceColor, surfaceAlpha);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local _SURFACETYPE_FOLIAGE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/Toon/ToonUberShader/Foliage.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _Cutoff;
                float _WindFrequency, _WindAmplitude;
                float3 _WindDirection;
            CBUFFER_END

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(input.positionOS.xyz, input.normalOS, input.color, _WindFrequency, _WindAmplitude, _WindDirection);
                #endif
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = GetShadowPositionHClip(input.positionOS, positionWS, normalWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                #if defined(_ALPHACLIP_ON)
                    half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                    clip(albedo.a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }
    }
    CustomEditor "ToonUberShaderGUI"
}