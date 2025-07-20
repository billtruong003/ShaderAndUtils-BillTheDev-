Shader "Bill's Toon/Toon Uber Shader"
{
    Properties
    {
        // ================== CONTROLLER ==================
        [Header(Surface Pipeline)]
        [Enum(Opaque, 0, Transparent, 1, Metallic, 2)] _SurfaceType("Surface Type", Float) = 0
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

                    float distanceToCamera = length(positionWS - _WorldSpaceCameraPos.xyz);
                    float distanceFade = 1.0 - saturate((distanceToCamera - _DistanceFadeStart) / (_DistanceFadeEnd - _DistanceFadeStart + 0.0001));
                    float scaledWidth = _OutlineWidth * distanceFade;

                    #if defined(_OUTLINE_SCALE_WITH_DISTANCE)
                        scaledWidth *= positionCS.w * 0.01;
                    #endif

                    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                    float3 normalVS = TransformWorldToViewDir(normalWS);
                    float3 projectedNormal = mul((float3x3)UNITY_MATRIX_P, normalVS);
                    positionCS.xy += normalize(projectedNormal.xy) * scaledWidth;
                    
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

            #pragma shader_feature_local_fragment _SURFACETYPE_OPAQUE _SURFACETYPE_TRANSPARENT _SURFACETYPE_METALLIC
            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local_fragment _EMISSION_ON
            #pragma shader_feature_local_fragment _OUTLINEMODE_FRESNEL
            #pragma shader_feature_local_fragment _FAKELIGHT_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/_Shmackle/Models/CharacterTestShader/Shader/ToonShading.hlsl" 

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Cutoff;
                float4 _EmissionColor;
                
                float4 _FakeLightColor;
                float4 _FakeLightDirection;
                
                float _ToonRampOffset;
                float _ToonRampSmoothness;
                float4 _ShadowTint;
                
                float _Brightness, _Offset, _HighlightOffset, _RimPower;
                float4 _SpecuColor, _HiColor, _RimColor;
                
                float4 _GlassColor, _FresnelColor;
                float _FresnelPower, _RefractionStrength;
                float _GlassSpecularPower, _GlassSpecularIntensity;

                float4 _FresnelOutlineColor;
                float _FresnelOutlineWidth;
                float _FresnelOutlinePower;
            CBUFFER_END

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);
            TEXTURE2D(_Ramp); SAMPLER(sampler_Ramp);
            TEXTURE2D_X_FLOAT(_CameraOpaqueTexture); SAMPLER(sampler_CameraOpaqueTexture);

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float2 uv : TEXCOORD2; };
            Varyings vert(Attributes v) { Varyings o; o.positionWS = TransformObjectToWorld(v.positionOS.xyz); o.positionCS = TransformWorldToHClip(o.positionWS); o.normalWS = TransformObjectToWorldNormal(v.normalOS); o.uv = TRANSFORM_TEX(v.uv, _BaseMap); return o; }

            half4 frag(Varyings i) : SV_Target
            {
                half3 finalColor = 0;
                half finalAlpha = 1;
                float3 viewDir = SafeNormalize(_WorldSpaceCameraPos.xyz - i.positionWS);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                #if defined(_FAKELIGHT_ON)
                    float mainLightIntensity = dot(mainLight.color, float3(1, 1, 1));
                    if (mainLightIntensity < 0.001)
                    {
                        mainLight.direction = normalize(_FakeLightDirection.xyz);
                        mainLight.color = _FakeLightColor.rgb;
                        mainLight.shadowAttenuation = 1.0;
                    }
                #endif

                #if defined(_SURFACETYPE_OPAQUE)
                    half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                    
                    #if defined(_ALPHACLIP_ON)
                        clip(albedo.a - _Cutoff);
                    #endif
                    
                    float3 directLighting = CalculateToonLighting(
                        i.normalWS, _ToonRampSmoothness, i.positionCS, i.positionWS, _ShadowTint, _ToonRampOffset, mainLight
                    );
                    float3 indirectLighting = SampleSH(i.normalWS);
                    finalColor = albedo.rgb * (directLighting + indirectLighting);
                    finalAlpha = albedo.a;
                #endif

                #if defined(_SURFACETYPE_METALLIC)
                    half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                    half d = dot(i.normalWS, mainLight.direction) * 0.5 + 0.5;
                    half3 ramp = SAMPLE_TEXTURE2D(_Ramp, sampler_Ramp, float2(d,d)).rgb;
                    
                    finalColor = albedo.rgb * mainLight.color * ramp * (mainLight.shadowAttenuation * 2);
                    
                    float3 halfVec = normalize(viewDir + mainLight.direction);
                    float specDot = saturate(dot(halfVec, i.normalWS));
                    finalColor += step(_Offset, specDot) * _SpecuColor.rgb * _Brightness * mainLight.color * mainLight.shadowAttenuation;
                    
                    float highlightDot = saturate(dot(i.normalWS, mainLight.direction));
                    finalColor += step(_HighlightOffset, highlightDot) * _HiColor.rgb * mainLight.color * mainLight.shadowAttenuation;
                    
                    half rim = 1.0 - saturate(dot(viewDir, i.normalWS));
                    finalColor += _RimColor.rgb * pow(rim, _RimPower);
                    
                    finalAlpha = albedo.a;
                #endif
                
                #if defined(_SURFACETYPE_TRANSPARENT)
                    float fresnelDot = 1.0 - saturate(dot(i.normalWS, viewDir));
                    float fresnel = pow(fresnelDot, _FresnelPower);
                    
                    float2 screenUV = i.positionCS.xy / i.positionCS.w;
                    float2 distortion = i.normalWS.xy * _RefractionStrength;
                    float3 sceneColor = SAMPLE_TEXTURE2D_X_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + distortion, 0).rgb;
                    
                    finalColor = lerp(sceneColor, _GlassColor.rgb, _GlassColor.a);
                    finalColor = lerp(finalColor, _FresnelColor.rgb, fresnel);

                    float3 reflectDir = reflect(-mainLight.direction, i.normalWS);
                    float spec = pow(saturate(dot(viewDir, reflectDir)), _GlassSpecularPower);
                    finalColor += mainLight.color * spec * _GlassSpecularIntensity * mainLight.shadowAttenuation;

                    finalAlpha = _GlassColor.a;
                #endif

                #if defined(_EMISSION_ON)
                    finalColor += SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, i.uv).rgb * _EmissionColor.rgb;
                #endif

                #if defined(_OUTLINEMODE_FRESNEL)
                    float fresnelDot = 1.0 - saturate(dot(i.normalWS, viewDir));
                    float fresnelOutline = pow(fresnelDot, _FresnelOutlinePower);
                    float outlineFactor = smoothstep(1.0 - _FresnelOutlineWidth, 1.0 - _FresnelOutlineWidth + 0.05, fresnelOutline);
                    finalColor = lerp(finalColor, _FresnelOutlineColor.rgb, outlineFactor);
                #endif

                return half4(finalColor, finalAlpha);
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

            // Bật tính năng alpha clipping cho pass đổ bóng
            #pragma shader_feature_local_fragment _ALPHACLIP_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // THÊM INCLUDE NÀY:
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _Cutoff;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            // Cập nhật struct Attributes, thêm normalOS
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 lightDirectionWS = _MainLightPosition.xyz;
                float3 biasedPositionWS = ApplyShadowBias(positionWS, normalWS, lightDirectionWS);
                output.positionCS = TransformWorldToHClip(biasedPositionWS);
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
    }
    CustomEditor "ToonUberShaderGUI"
}