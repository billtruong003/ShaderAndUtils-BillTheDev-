Shader "Shmackle/Uber Dissolve FX"
{
    Properties
    {
        [Header(Workflow)]
        [Enum(Unlit,0,Standard Lit,1,Basic Toon,2,Studio Toon,3,Toon Bling,4)] _LightingModel ("Lighting Model", Float) = 0
        [Toggle(_DISSOLVE_ON)] _EnableDissolve ("Enable Dissolve", Float) = 0
        
        [Header(Base Properties)]
        _BaseMap ("Base Texture (RGB) Alpha (A)", 2D) = "white" {}
        [HDR] _BaseColor ("Base Color", Color) = (1,1,1,1)
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Culling Mode", Float) = 2
        [Toggle(_ALPHACLIP_ON)] _AlphaClipMode("Enable Alpha Clip", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [Toggle(_NORMALMAP_ON)] _EnableNormalMap("Enable Normal Map", Float) = 0
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Intensity", Range(0, 2)) = 1.0

        [Header(Emission)]
        [Toggle(_EMISSION_ON)] _EnableEmission("Enable Emission", Float) = 0
        _EmissionMap("Emission Map (RGB)", 2D) = "white" {}
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,1)

        [Header(Dissolve Control)]
        [Enum(Noise,0,Linear,1,Radial,2,Pattern,3,Alpha Blend,4,Shatter,5)] _DissolveType ("Dissolve Type", Float) = 0
        _DissolveThreshold ("Threshold", Range(-2, 2)) = 0.5
        [Toggle(_USE_TIME_ANIMATION)] _UseTimeAnimation ("Use Time Animation", Float) = 0
        _TimeScale ("Time Scale", Float) = 1
        [Toggle(_DISSOLVE_LOCALSPACE_ON)] _UseLocalSpace ("Use Local Space", Float) = 0
        _DissolveDirection ("Direction (Linear/Shatter)", Vector) = (0, 1, 0, 0)
        _RadialDirection ("Radial Direction", Range(-1, 1)) = 1

        [Header(Dissolve Edge)]
        _NoiseTex ("Noise Texture (R)", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Float) = 1.0
        _NoiseStrength ("Noise Strength", Range(0, 2)) = 0.1
        _DissolveEdgeWidth ("Edge Width", Range(0, 2)) = 0.05
        [HDR] _DissolveEdgeColor ("Edge Color (HDR)", Color) = (1, 0.5, 0, 1)

        [Header(Dissolve Pattern Settings)]
        [Enum(SinCos,0,Checker,1,Grid,2)]_PatternType ("Pattern Type", Float) = 0
        _PatternFrequency ("Pattern Frequency", Float) = 10

        [Header(Dissolve Alpha Blend Settings)]
        _AlphaFadeRange ("Fade Range", Range(0.01, 1)) = 0.5

        [Header(Dissolve Vertex Effects)]
        [Toggle(_VERTEX_DISPLACEMENT_ON)] _EnableVertexDisplacement ("Enable Standard Displacement", Float) = 0
        [Toggle(_SHATTER_EFFECT_ON)] _EnableShatterEffect ("Enable Shatter Effect", Float) = 0
        _VertexDisplacement ("Intensity / Outward Push", Range(-5, 5)) = 0.1
        _DisplacementWaveWidth ("Wave Width", Range(-5, 5)) = 0.5
        
        [Header(Dissolve Shatter Effect)]
        _ShatterStrength ("Overall Strength", Range(0, 5)) = 1
        _ShatterLiftSpeed ("Lift Speed", Float) = 1
        _ShatterOffsetStrength ("Offset Strength", Float) = 0.5
        _ShatterTriggerRange ("Trigger Range", Range(0, 1)) = 0.1

        [Header(Basic Toon Lighting)]
        _ToonRampOffset("Ramp Offset", Range(0.0, 1.0)) = 0.5
        _ToonRampSmoothness("Ramp Smoothness", Range(0.001, 1.0)) = 0.05
        [HDR] _ShadowTint("Shadow Tint", Color) = (0.1, 0.1, 0.2, 1.0)
        
        [Header(Studio Toon Lighting)]
        _StudioToon_HighlightColor("Highlight Color", Color) = (1,1,1,1)
        _StudioToon_MidtoneColor("Midtone Color", Color) = (0.8, 0.8, 0.8, 1)
        _StudioToon_ShadowColor("Shadow Color", Color) = (0.4, 0.4, 0.4, 1)
        _StudioToon_HighlightThreshold("Highlight Threshold", Range(0, 1)) = 0.8
        _StudioToon_ShadowThreshold("Shadow Threshold", Range(0, 1)) = 0.4
        _StudioToon_RampSmoothness("Ramp Smoothness", Range(0.001, 1)) = 0.05
        [Toggle(_USE_FAKE_LIGHT)] _UseFakeLight("Use Fake Light Direction", Float) = 0
        _FakeLightDirection("Fake Light Direction", Vector) = (0, 1, 0, 0)
        _CustomShadowColor("Main Light Custom Shadow Color", Color) = (0,0,0,1)
        _ShadowTintInfluence("Light Color On Shadow", Range(0, 1)) = 0.2
        
        [Header(Studio Toon Ambient)]
        [Toggle(_STUDIO_GRADIENT_AMBIENT_ON)] _EnableGradientAmbient("Enable Gradient Ambient", Float) = 0
        _StudioToon_SkyColor("Sky Color (Ambient)", Color) = (0.2, 0.3, 0.4, 1)
        _StudioToon_GroundColor("Ground Color (Ambient)", Color) = (0.1, 0.1, 0.1, 1)
        _StudioToon_AmbientGradientPower("Ambient Gradient Power", Range(0.1, 5)) = 1.0
        
        [Header(Studio Toon Effects)]
        [Toggle(_STUDIO_SPECULAR_ON)] _EnableSpecular("Enable Specular", Float) = 0
        _StudioToon_SpecularColor("Specular Color", Color) = (1,1,1,1)
        _StudioToon_SpecularThreshold("Specular Threshold", Range(0, 1)) = 0.95
        _StudioToon_SpecularSmoothness("Specular Smoothness", Range(0.001, 1)) = 0.02
        [Toggle(_STUDIO_RIM_LIGHT_ON)] _EnableRimLight("Enable Rim Light", Float) = 0
        _StudioToon_RimColor("Rim Color", Color) = (1,1,1,1)
        _StudioToon_RimPower("Rim Power", Range(1, 10)) = 3.0
        _StudioToon_RimThreshold("Rim Threshold", Range(0, 1)) = 0.5
        
        [Header(Studio Toon Advanced Effects)]
        [Toggle(_HATCHING_ON)] _EnableHatching("Enable Shadow Hatching", Float) = 0
        _HatchingMap("Hatching Map", 2D) = "gray" {}
        _HatchingTiling("Hatching Tiling", Float) = 1.0
        _HatchingVisibility("Hatching Visibility", Range(0, 1)) = 1.0
        [Toggle(_MATCAP_ON)] _EnableMatcap("Enable MatCap", Float) = 0
        [Enum(Add,0,Multiply,1,Lerp,2)]_MatcapBlendMode("MatCap Blend Mode", Float) = 0.0
        _MatcapMap("MatCap Map", 2D) = "gray" {}
        _MatcapTint("MatCap Tint & Lerp Alpha", Color) = (1,1,1,1)
        _MatcapIntensity("MatCap Intensity", Range(0, 5)) = 1.0

        [Header(Studio Toon Additional Lights)]
        [Toggle(_ADDITIONAL_LIGHTS_ON)] _EnableAdditionalLights("Enable Additional Lights", Float) = 1
        _AdditionalLightInfluence("Additional Light Influence", Range(0, 1)) = 1.0
        
        [Header(Toon Bling Lighting)]
        [Toggle(_BLING_EFFECT_ON)] _EnableBlingEffect ("Enable Bling Effect", Float) = 1
        [HDR] _Bling_SpecColor("Specular Color", Color) = (1,1,1,1)
        _Bling_SpecSmoothness("Specular Smoothness", Range(0.001, 1.0)) = 0.05
        _Bling_SpecOffset("Specular Offset", Range(0, 1)) = 0.95
        [HDR] _Bling_RimColor("Rim Color", Color) = (1,1,1,1)
        _Bling_RimPower("Rim Power", Range(0.1, 10.0)) = 3.0
        _Bling_RimMin("Rim Min", Range(0, 1)) = 0.0
        _Bling_RimMax("Rim Max", Range(0, 1)) = 1.0
        [Toggle(_BLING_WORLDSPACE_ON)] _BlingWorldSpace("Use World Space Bling", Float) = 0
        [HDR] _BlingColor("Bling Color", Color) = (1,1,1,1)
        _BlingIntensity("Bling Intensity", Range(0, 10)) = 2.0
        _BlingScale("Bling Scale", Range(1, 10000)) = 50.0
        _BlingSpeed("Bling Speed", Range(0, 5)) = 1.0
        _BlingFresnelPower("Bling Fresnel Power", Range(0.1, 10)) = 2.0
        _BlingThreshold("Bling Threshold", Range(0.5, 1.0)) = 0.95

        [Header(Advanced Rendering)]
        [Toggle(_ZWRITE_ON)] _ZWrite ("ZWrite", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            Cull [_CullMode]
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local_fragment _NORMALMAP_ON
            #pragma shader_feature_local_fragment _EMISSION_ON
            #pragma shader_feature_local _DISSOLVE_ON
            #pragma shader_feature_local _DISSOLVE_LOCALSPACE_ON
            #pragma shader_feature_local _VERTEX_DISPLACEMENT_ON
            #pragma shader_feature_local _SHATTER_EFFECT_ON
            
            #pragma shader_feature_local_fragment _USE_FAKE_LIGHT
            #pragma shader_feature_local_fragment _STUDIO_GRADIENT_AMBIENT_ON
            #pragma shader_feature_local_fragment _STUDIO_SPECULAR_ON
            #pragma shader_feature_local_fragment _STUDIO_RIM_LIGHT_ON
            #pragma shader_feature_local_fragment _ADDITIONAL_LIGHTS_ON
            #pragma shader_feature_local_fragment _HATCHING_ON
            #pragma shader_feature_local_fragment _MATCAP_ON

            #pragma shader_feature_local_fragment _BLING_EFFECT_ON
            #pragma shader_feature_local_fragment _BLING_WORLDSPACE_ON
            
            #pragma multi_compile_local _LIGHTINGMODEL_UNLIT _LIGHTINGMODEL_STANDARD_LIT _LIGHTINGMODEL_BASIC_TOON _LIGHTINGMODEL_STUDIO_TOON _LIGHTINGMODEL_TOON_BLING
            #pragma multi_compile_local_fragment _DISSOLVETYPE_NOISE _DISSOLVETYPE_LINEAR _DISSOLVETYPE_RADIAL _DISSOLVETYPE_PATTERN _DISSOLVETYPE_ALPHA_BLEND _DISSOLVETYPE_SHATTER

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS
            
            #include "Includes/UberDissolve_Core.hlsl"

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                float3 positionOS = input.positionOS.xyz;
                output.perVertexNoise = Hash11(positionOS.x + positionOS.y * 10.0h + positionOS.z * 100.0h);
                
                #if defined(_DISSOLVE_ON)
                    float3 positionForDissolve = positionOS;
                    #ifndef _DISSOLVE_LOCALSPACE_ON
                        positionForDissolve = TransformObjectToWorld(positionOS);
                    #endif
                    output.dissolveValue = CalculateDissolveValue(positionForDissolve, input.uv, output.perVertexNoise, _DissolveType);
                #else
                    output.dissolveValue = 1.0h;
                #endif

                float3 displacedPositionOS = positionOS;
                #if defined(_DISSOLVE_ON) && (defined(_VERTEX_DISPLACEMENT_ON) || defined(_SHATTER_EFFECT_ON))
                    half timeAnimOffset = _UseTimeAnimation > 0.5h ? sin(_Time.y * _TimeScale) * 0.05h : 0.0h;
                    half threshold = _DissolveThreshold + timeAnimOffset;
                    half vertexPerturbation = (output.perVertexNoise - 0.5h) * _NoiseStrength;
                    half perturbedDissolveValueForVertex = output.dissolveValue + vertexPerturbation;

                    #if defined(_DISSOLVETYPE_SHATTER)
                        displacedPositionOS = ApplyShatterEffect(positionOS, threshold, perturbedDissolveValueForVertex, output.perVertexNoise);
                    #else
                        displacedPositionOS = ApplyStandardVertexDisplacement(positionOS, input.normalOS, threshold, perturbedDissolveValueForVertex);
                    #endif
                #endif

                VertexPositionInputs positionInputs = GetVertexPositionInputs(displacedPositionOS);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionWS = positionInputs.positionWS;
                output.positionCS = positionInputs.positionCS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = normalInputs.tangentWS;
                output.bitangentWS = normalInputs.bitangentWS;
                output.shadowCoord = GetShadowCoord(positionInputs);

                output.viewDirWS = SafeNormalize(_WorldSpaceCameraPos.xyz - output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                
                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                #if defined(_ALPHACLIP_ON)
                    clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a - _Cutoff);
                #endif

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half3 surfaceColor;
                
                #if defined(_LIGHTINGMODEL_UNLIT)
                    surfaceColor = CalculateLighting_Unlit(albedo.rgb);
                #elif defined(_LIGHTINGMODEL_STANDARD_LIT)
                    surfaceColor = CalculateLighting_StandardLit(albedo.rgb, input.normalWS, input.positionWS);
                #elif defined(_LIGHTINGMODEL_BASIC_TOON)
                    surfaceColor = CalculateLighting_BasicToon(albedo.rgb, input.normalWS, input.positionWS);
                #elif defined(_LIGHTINGMODEL_STUDIO_TOON)
                    surfaceColor = CalculateLighting_StudioToon(albedo.rgb, input);
                #elif defined(_LIGHTINGMODEL_TOON_BLING)
                    surfaceColor = CalculateLighting_ToonBling(albedo.rgb, input.normalWS, input.viewDirWS, input.positionWS, input.uv);
                #endif
                
                #if defined(_EMISSION_ON)
                    half3 emissionColor = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb * _EmissionColor.rgb;
                    surfaceColor += emissionColor;
                #endif

                #if defined(_DISSOLVE_ON)
                    half dissolveAlpha;
                    surfaceColor = ApplyDissolveToColor(surfaceColor, input.uv, input.dissolveValue, dissolveAlpha);
                    albedo.a *= dissolveAlpha;
                #endif
                
                clip(albedo.a - 0.01h);
                
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
            Cull [_CullMode]

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            
            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local _DISSOLVE_ON
            #pragma shader_feature_local _DISSOLVE_LOCALSPACE_ON
            #pragma multi_compile_local_fragment _DISSOLVETYPE_NOISE _DISSOLVETYPE_LINEAR _DISSOLVETYPE_RADIAL _DISSOLVETYPE_PATTERN _DISSOLVETYPE_ALPHA_BLEND _DISSOLVETYPE_SHATTER

            #include "Includes/UberDissolve_Core.hlsl"

            struct ShadowVaryings
            {
                float4 positionCS    : SV_POSITION;
                float2 uv            : TEXCOORD0;
                float  dissolveValue : TEXCOORD1;
            };

            ShadowVaryings ShadowVert(Attributes input)
            {
                ShadowVaryings o;
                float3 positionOS = input.positionOS.xyz;
                o.positionCS = GetShadowCoord(GetVertexPositionInputs(positionOS));
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                
                #if defined(_DISSOLVE_ON)
                    float perVertexNoise = Hash11(positionOS.x + positionOS.y * 10.0h + positionOS.z * 100.0h);
                    float3 positionForDissolve = positionOS;
                    #ifndef _DISSOLVE_LOCALSPACE_ON
                        positionForDissolve = TransformObjectToWorld(positionOS);
                    #endif
                    o.dissolveValue = CalculateDissolveValue(positionForDissolve, input.uv, perVertexNoise, _DissolveType);
                #else
                    o.dissolveValue = 1.0h;
                #endif
                
                return o;
            }

            half4 ShadowFrag(ShadowVaryings i) : SV_TARGET
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                half alpha = albedo.a;

                #if defined(_ALPHACLIP_ON)
                    clip(alpha - _Cutoff);
                #endif

                #if defined(_DISSOLVE_on)
                    half noiseTexSample = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, i.uv * _NoiseScale).r;
                    half pixelPerturbation = (noiseTexSample - 0.5h) * _NoiseStrength;
                    half perturbedDissolveValue = i.dissolveValue + pixelPerturbation;

                    half timeAnimOffset = _UseTimeAnimation > 0.5h ? sin(_Time.y * _TimeScale) * 0.05h : 0.0h;
                    half threshold = _DissolveThreshold + timeAnimOffset;
                    
                    half dissolveAlpha = step(threshold, perturbedDissolveValue);
                    clip(alpha * dissolveAlpha - 0.5h);
                #endif

                return 0;
            }
            ENDHLSL
        }
        
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
    CustomEditor "Shmackle_UberDissolve_GUI"
}