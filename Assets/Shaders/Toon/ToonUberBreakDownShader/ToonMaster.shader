Shader "Bill's Toon/Toon Master"
{
    Properties
    {
        // WORKFLOW
        [Header(Workflow Selection)]
        [Enum(Opaque,0,Transparent,1)] _Workflow("Workflow", Float) = 0
        [Enum(None,0,Inverted Hull,1,Fresnel,2)] _OutlineMode("Outline Mode", Float) = 0

        // BASE PROPERTIES
        [Header(Base Properties)]
        _BaseMap("Albedo (RGB) Alpha (A)", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [Toggle(_ALPHACLIP_ON)] _AlphaClipToggle("Enable Alpha Clip", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        
        // LIGHTING MODULE
        [Header(Lighting Module)]
        _LightRamp("Lighting Ramp (1D)", 2D) = "white" {}
        [HDR] _ShadowTint("Shadow Tint", Color) = (0.8, 0.8, 1, 1)
        [HDR] _AmbientColor("Ambient Color", Color) = (0.2, 0.2, 0.3, 1)
        
        // SPECULAR MODULE
        [Header(Specular Module)]
        [Enum(None,0,Hard Toon,1,Soft,2,Anisotropic,3)] _SpecularMode("Specular Mode", Float) = 1
        [HDR] _SpecularColor("Specular Color", Color) = (1,1,1,1)
        _SpecularStrength("Strength", Range(0, 5)) = 1
        _SpecularToonSize("Toon Size", Range(0.001, 0.5)) = 0.1
        _SpecularToonThreshold("Toon Threshold", Range(0.5, 1)) = 0.98
        _SpecularSoftness("Softness", Range(1, 256)) = 32
        _AnisotropicOffset("Anisotropic Offset", Range(-1, 1)) = 0
        
        // RIM LIGHT MODULE
        [Header(Rim Light Module)]
        [Toggle(_RIMLIGHT_ON)] _RimLightToggle("Enable Rim Light", Float) = 1
        [HDR] _RimColor("Rim Color", Color) = (1,1,1,1)
        _RimPower("Power", Range(0.1, 20)) = 3
        _RimThreshold("Threshold", Range(0, 1)) = 0.5
        [Toggle] _RimMaskedByLight("Mask by Light", Float) = 1

        // MATCAP MODULE
        [Header(MatCap Module)]
        [Toggle(_MATCAP_ON)] _MatCapToggle("Enable MatCap", Float) = 0
        [NoScaleOffset] _MatCapTexture("MatCap Texture", 2D) = "gray" {}
        [Enum(Add,0,Multiply,1,Lerp,2)] _MatCapBlendMode("Blend Mode", Float) = 0
        _MatCapIntensity("Intensity / Lerp Factor", Range(0, 2)) = 1

        // PAINTERLY HATCHING MODULE
        [Header(Painterly Hatching Module)]
        [Toggle(_HATCHING_ON)] _HatchingToggle("Enable Painterly Hatching", Float) = 0
        [NoScaleOffset] _HatchingTexture("Hatching Texture (Screen Space)", 2D) = "gray" {}
        _HatchingTiling("Tiling", Float) = 10
        [HDR] _HatchingColor("Color", Color) = (0,0,0,1)
        _HatchingShadowThreshold("Shadow Threshold", Range(0, 1)) = 0.45

        // INVERTED HULL OUTLINE MODULE
        [Header(Inverted Hull Outline)]
        _OutlineColor("Color", Color) = (0, 0, 0, 1)
        _OutlineWidth("Width", Range(0.0, 10)) = 1
        _OutlineNoiseFrequency("Noise Frequency", Float) = 10
        _OutlineNoiseAmplitude("Noise Amplitude", Range(0, 0.1)) = 0.01

        // FRESNEL OUTLINE MODULE
        [Header(Fresnel Outline)]
        [HDR] _FresnelOutlineColor("Color", Color) = (0, 0, 0, 1)
        _FresnelOutlineWidth("Width", Range(0.001, 1.0)) = 0.1
        _FresnelOutlinePower("Power", Range(1.0, 100.0)) = 5.0
        
        // EFFECTS MODULE
        [Header(Effects Module)]
        [Toggle(_EMISSION_ON)] _EmissionToggle("Enable Emission", Float) = 0
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,1)
        _EmissionMap("Emission Map", 2D) = "black" {}
        [Toggle(_INTERIORGLOW_ON)] _InteriorGlowToggle("Enable Interior Glow", Float) = 0
        [HDR] _InteriorGlowColor("Glow Color", Color) = (1, 0.5, 0.5, 1)
        _InteriorGlowPower("Glow Power", Range(0.1, 20)) = 5
        
        // TRANSPARENT WORKFLOW
        [Header(Transparency Settings)]
        _Opacity("Opacity", Range(0, 1)) = 0.5

        // ADVANCED
        [HideInInspector] _SrcBlend ("Src Blend", Float) = 5
        [HideInInspector] _DstBlend ("Dst Blend", Float) = 10
        [HideInInspector] _ZWrite ("ZWrite", Float) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        // PASS 1: HULL OUTLINE
        Pass
        {
            Name "HULL_OUTLINE"
            Tags { "LightMode"="UniversalForward" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment
            
            #pragma multi_compile_local _OUTLINEMODE_NONE _OUTLINEMODE_INVERTED_HULL _OUTLINEMODE_FRESNEL

            #include "Includes/ToonMaster_Core.hlsl"

            VaryingsOutline OutlineVertex(Attributes input)
            {
                VaryingsOutline output = (VaryingsOutline)0;

                #if _OUTLINEMODE_INVERTED_HULL
                    float3 positionOS = input.positionOS.xyz;
                    float3 normalOS = input.normalOS;
                    
                    float noise = (sin(positionOS.y * _OutlineNoiseFrequency + _Time.y) + 1) * 0.5;
                    float width = _OutlineWidth * 0.005 * (1 + noise * _OutlineNoiseAmplitude);
                    
                    positionOS += normalOS * width;
                    output.positionCS = TransformObjectToHClip(positionOS);
                #else
                    output.positionCS = float4(0,0,0,-1); // Discard vertex
                #endif

                return output;
            }
            
            half4 OutlineFragment(VaryingsOutline input) : SV_Target 
            {
                 #if _OUTLINEMODE_INVERTED_HULL
                    return _OutlineColor;
                 #else
                    discard;
                    return 0;
                 #endif
            }
            ENDHLSL
        }
        
        // PASS 2: MAIN FORWARD LIT
        Pass
        {
            Name "FORWARD_LIT"
            Tags { "LightMode"="UniversalForward" }
            
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull Back

            HLSLPROGRAM
            #pragma vertex MasterVertex
            #pragma fragment MasterFragment
            
            // Keywords
            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local_fragment _EMISSION_ON
            #pragma shader_feature_local_fragment _RIMLIGHT_ON
            #pragma shader_feature_local_fragment _MATCAP_ON
            #pragma shader_feature_local_fragment _HATCHING_ON
            #pragma shader_feature_local_fragment _INTERIORGLOW_ON

            #pragma multi_compile_local_fragment _SPECULARMODE_NONE "_SPECULARMODE_HARD_TOON" "_SPECULARMODE_SOFT" "_SPECULARMODE_ANISOTROPIC"
            #pragma multi_compile_local_fragment _MATCAPBLENDMODE_ADD _MATCAPBLENDMODE_MULTIPLY _MATCAPBLENDMODE_LERP
            #pragma multi_compile_local_fragment _OUTLINEMODE_NONE _OUTLINEMODE_INVERTED_HULL _OUTLINEMODE_FRESNEL

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            
            #include "Includes/ToonMaster_Core.hlsl"
            #include "Includes/ToonMaster_Functions.hlsl"

            Varyings MasterVertex(Attributes v)
            {
                Varyings o = (Varyings)0;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.tangentWS = TransformObjectToWorldDir(v.tangentOS.xyz);
                o.bitangentWS = cross(o.normalWS, o.tangentWS) * v.tangentOS.w;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            half4 MasterFragment(Varyings i) : SV_Target
            {
                // PREPARE
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                #if defined(_ALPHACLIP_ON)
                    clip(albedo.a - _Cutoff);
                #endif
                
                float3 normalWS = normalize(i.normalWS);
                float3 viewDir = SafeNormalize(_WorldSpaceCameraPos.xyz - i.positionWS);

                // LIGHTING
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half3 lighting = CalculateMainLighting(normalWS, i.positionWS, mainLight);
                
                // SPECULAR
                half3 specular = CalculateSpecular(i, mainLight, normalWS, viewDir);
                
                // RIM LIGHT
                half3 rimLight = CalculateRimLight(normalWS, viewDir, mainLight.direction);

                // AMBIENT
                half3 ambient = SampleSH(normalWS) * _AmbientColor.rgb;

                // COMBINE
                half3 finalColor = albedo.rgb * (lighting + ambient) + specular + rimLight;
                
                // MATCAP
                finalColor = ApplyMatCap(finalColor, i.positionCS, normalWS, viewDir);
                
                // HATCHING
                finalColor = ApplyHatching(finalColor, i.positionCS, lighting);
                
                // FRESNEL OUTLINE
                finalColor = ApplyFresnelOutline(finalColor, normalWS, viewDir);
                
                // INTERIOR GLOW
                finalColor = ApplyInteriorGlow(finalColor, normalWS, viewDir);

                // EMISSION
                #if defined(_EMISSION_ON)
                    finalColor += SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, i.uv).rgb * _EmissionColor.rgb;
                #endif

                return half4(finalColor, albedo.a * _Opacity);
            }
            ENDHLSL
        }

        // PASS 3: SHADOW CASTER
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Includes/ToonMaster_Core.hlsl"

            VaryingsShadow ShadowVert(Attributes input)
            {
                VaryingsShadow o;
                o.positionCS = GetShadowCoord(GetVertexPositionInputs(input.positionOS.xyz));
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }
            half4 ShadowFrag(VaryingsShadow i) : SV_Target
            {
                #if defined(_ALPHACLIP_ON)
                    half albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a * _BaseColor.a;
                    clip(albedoAlpha - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }
    }
    CustomEditor "ToonMasterShaderGUI"
}