Shader "CleanCode/Uber Master Shader"
{
    Properties
    {
        [Header(Workflow and Main Features)]
        [Enum(Unlit,0,Standard Lit,1,Basic Toon,2,Stylized Metal,3,Toon Bling,4)] _LightingModel("Lighting Model", Float) = 0
        [Toggle(_TEXTURE_SWAP_ON)] _EnableTextureSwap ("Enable Texture Swap", Float) = 0
        [Toggle(_DISSOLVE_ON)] _EnableDissolve ("Enable Dissolve", Float) = 0

        [Header(Base Properties)]
        _BaseMap ("Primary Albedo (A=Opacity)", 2D) = "white" {}
        [HDR] _BaseColor ("Base Color", Color) = (1,1,1,1)
        [HDR] _AmbientColor ("Ambient Color", Color) = (0.1, 0.1, 0.1, 1)
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Culling Mode", Float) = 2
        [Toggle(_ALPHACLIP_ON)] _AlphaClipMode("Enable Alpha Clip", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [Header(Texture Swap Effect)]
        _SecondaryAlbedoMap("Secondary Albedo", 2D) = "black" {}
        _SwapProgress("Transition Progress", Range(-2, 2)) = 0.0
        _SwapTransitionHardness("Transition Hardness", Range(1, 100)) = 20.0
        [Toggle(_SWAP_LOCALSPACE_ON)] _UseSwapLocalSpace("Use Local Space", Float) = 0
        [Enum(Sphere,0,Box,1,Mask Texture,2)] _SwapShape("Transition Shape", Float) = 0
        _SwapShapeMask("Shape Mask (R)", 2D) = "white" {}
        _SwapEffectCenter("Effect Center (Local)", Vector) = (0,0,0,0)
        _SwapEffectExtents("Effect Extents (Box)", Vector) = (1,1,1,0)
        _SwapEffectRadius("Effect Radius (Sphere)", Range(0, 10)) = 1.0
        [Enum(Noise,0,Linear,1,Radial,2,Pattern,3)] _SwapType("Transition Type", Float) = 0
        _SwapNoiseMap("Swap Noise (Triplanar)", 2D) = "gray" {}
        _SwapNoiseScale("Noise Scale", Range(0.0, 10.0)) = 1.0
        _SwapNoiseStrength("Noise Influence", Range(0.0, 1.0)) = 0.2
        _SwapNoiseScrollSpeed("Noise Scroll Speed", Vector) = (0.1, 0.1, 0, 0)
        _SwapDirection("Direction (Linear)", Vector) = (0, 1, 0, 0)
        [Enum(SinCos,0,Checker,1,Grid,2)]_SwapPatternType("Pattern Type", Float) = 0
        _SwapPatternFrequency("Pattern Frequency", Float) = 10
        _SwapLineWidth("Transition Line Width", Range(0.0, 0.2)) = 0.02
        [HDR] _SwapLineColor("Transition Line Color", Color) = (1, 1, 1, 1)

        [Header(Dissolve Control)]
        [Enum(Noise,0,Linear,1,Radial,2,Pattern,3,Alpha Blend,4,Shatter,5)] _DissolveType("Dissolve Type", Float) = 0
        _DissolveThreshold("Threshold", Range(-2, 2)) = 0.5
        [Toggle(_USE_TIME_ANIMATION)] _UseTimeAnimation("Use Time Animation", Float) = 0
        _TimeScale("Time Scale", Float) = 1
        [Toggle(_DISSOLVE_LOCALSPACE_ON)] _UseLocalSpace("Use Local Space", Float) = 0
        _DissolveDirection("Direction (Linear/Shatter)", Vector) = (0, 1, 0, 0)
        _RadialDirection("Radial Direction", Range(-1, 1)) = 1

        [Header(Dissolve Edge)]
        _DissolveNoiseTex("Dissolve Noise Texture (R)", 2D) = "white" {}
        _DissolveNoiseScale("Noise Scale", Float) = 1.0
        _DissolveNoiseStrength("Noise Strength", Range(0, 2)) = 0.1
        _DissolveEdgeWidth("Edge Width", Range(0, 2)) = 0.05
        [HDR] _DissolveEdgeColor("Edge Color (HDR)", Color) = (1, 0.5, 0, 1)

        [Header(Dissolve Pattern Settings)]
        [Enum(SinCos,0,Checker,1,Grid,2)]_PatternType("Pattern Type", Float) = 0
        _PatternFrequency("Pattern Frequency", Float) = 10

        [Header(Dissolve Alpha Blend Settings)]
        _AlphaFadeRange("Fade Range", Range(0.01, 1)) = 0.5

        [Header(Dissolve Vertex Effects)]
        [Toggle(_VERTEX_DISPLACEMENT_ON)] _EnableVertexDisplacement("Enable Standard Displacement", Float) = 0
        [Toggle(_DISPLACEMENT_SATURATE_ON)] _UseSaturateDisplacement("Saturate Displacement (Sustained)", Float) = 0
        [Toggle(_SHATTER_EFFECT_ON)] _EnableShatterEffect("Enable Shatter Effect", Float) = 0
        _VertexDisplacement("Intensity / Outward Push", Range(-5, 5)) = 0.1
        _BounceWaveWidth("Effect Width", Range(0.01, 5)) = 0.5
        
        [Header(Dissolve Shatter Effect)]
        _ShatterStrength("Overall Strength", Range(0, 5)) = 1
        _ShatterLiftSpeed("Lift Speed", Float) = 1
        _ShatterOffsetStrength("Offset Strength", Float) = 0.5
        _ShatterTriggerRange("Trigger Range", Range(0, 1)) = 0.1

        [Header(Basic Toon Lighting)]
        _ToonRampOffset("Diffuse Ramp Offset", Range(0.0, 1.0)) = 0.5
        _ToonRampSmoothness("Diffuse Ramp Smoothness", Range(0.001, 1.0)) = 0.05
        [HDR] _ShadowTint("Shadow Tint", Color) = (0.1, 0.1, 0.2, 1.0)
        [HDR] _Toon_SpecColor("Specular Color", Color) = (1,1,1,1)
        _Toon_SpecSmoothness("Specular Smoothness", Range(0.001, 1.0)) = 0.05
        _Toon_SpecOffset("Specular Offset", Range(0, 1)) = 0.95
        [HDR] _Toon_RimColor("Rim Color", Color) = (1,1,1,1)
        _Toon_RimPower("Rim Power", Range(0.1, 10.0)) = 3.0
        _Toon_RimMin("Rim Min", Range(0, 1)) = 0.0
        _Toon_RimMax("Rim Max", Range(0, 1)) = 1.0
        
        [Header(Toon Bling Lighting)]
        [HDR] _Bling_SpecColor("Specular Color", Color) = (1,1,1,1)
        _Bling_SpecSmoothness("Specular Smoothness", Range(0.001, 1.0)) = 0.05
        _Bling_SpecOffset("Specular Offset", Range(0, 1)) = 0.95
        [HDR] _Bling_RimColor("Rim Color", Color) = (1,1,1,1)
        _Bling_RimPower("Rim Power", Range(0.1, 10.0)) = 3.0
        _Bling_RimMin("Rim Min", Range(0, 1)) = 0.0
        _Bling_RimMax("Rim Max", Range(0, 1)) = 1.0
        [Toggle(_BLING_EFFECT_ON)] _EnableBlingEffect ("Enable Bling Effect", Float) = 1
        [Toggle(_BLING_WORLDSPACE_ON)] _BlingWorldSpace("Use World Space Bling", Float) = 0
        [HDR] _BlingColor("Bling Color", Color) = (1,1,1,1)
        _BlingIntensity("Bling Intensity", Range(0, 10)) = 2.0
        _BlingScale("Bling Scale", Range(1, 10000)) = 50.0
        _BlingSpeed("Bling Speed", Range(0, 5)) = 1.0
        _BlingFresnelPower("Bling Fresnel Power", Range(0.1, 10)) = 2.0
        _BlingThreshold("Bling Threshold", Range(0.5, 1.0)) = 0.95
        
        [Header(Stylized Metal Lighting)]
        _Metal_Ramp("Toon Ramp (RGB)", 2D) = "white" {} 
        _Metal_Brightness("Specular Brightness", Range(0, 2)) = 1.3  
        _Metal_Offset("Specular Size", Range(0, 1)) = 0.8
        [HDR] _Metal_SpecuColor("Specular Color", Color) = (0.8,0.45,0.2,1)
        _Metal_HighlightOffset("Highlight Size", Range(0, 1)) = 0.9  
        [HDR] _Metal_HiColor("Highlight Color", Color) = (1,1,1,1)
        [HDR] _Metal_RimColor("Rim Color", Color) = (1,0.3,0.3,1)
        _Metal_RimPower("Rim Power", Range(0, 20)) = 6

        [Header(Advanced Rendering)]
        [Toggle(_ZWRITE_ON)] _ZWrite ("ZWrite", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

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
            #pragma shader_feature_local _TEXTURE_SWAP_ON
            #pragma shader_feature_local _SWAP_LOCALSPACE_ON
            #pragma shader_feature_local _DISSOLVE_ON
            #pragma shader_feature_local _DISSOLVE_LOCALSPACE_ON
            #pragma shader_feature_local _VERTEX_DISPLACEMENT_ON
            #pragma shader_feature_local _SHATTER_EFFECT_ON
            #pragma shader_feature_local _DISPLACEMENT_SATURATE_ON
            #pragma shader_feature_local _ZWRITE_ON
            #pragma shader_feature_local_fragment _BLING_EFFECT_ON
            #pragma shader_feature_local_fragment _BLING_WORLDSPACE_ON
            
            #pragma multi_compile_local _LIGHTINGMODEL_UNLIT _LIGHTINGMODEL_STANDARD_LIT _LIGHTINGMODEL_BASIC_TOON _LIGHTINGMODEL_STYLIZED_METAL _LIGHTINGMODEL_TOON_BLING
            #pragma multi_compile_local_fragment _DISSOLVETYPE_NOISE _DISSOLVETYPE_LINEAR _DISSOLVETYPE_RADIAL _DISSOLVETYPE_PATTERN _DISSOLVETYPE_ALPHA_BLEND _DISSOLVETYPE_SHATTER
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            #include "Assets/Shaders/Dissolve/Includes/UberMaster_Core.hlsl"

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                float3 positionOS = input.positionOS.xyz;
                output.positionOS = positionOS;
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
                    half vertexPerturbation = (output.perVertexNoise - 0.5h) * _DissolveNoiseStrength;
                    half perturbedDissolveValueForVertex = output.dissolveValue + vertexPerturbation;

                    #if defined(_DISSOLVETYPE_SHATTER)
                        displacedPositionOS = ApplyShatterEffect(positionOS, threshold, perturbedDissolveValueForVertex, output.perVertexNoise);
                    #else
                        displacedPositionOS = ApplyStandardVertexDisplacement(positionOS, input.normalOS, threshold, perturbedDissolveValueForVertex);
                    #endif
                #endif

                output.positionWS = TransformObjectToWorld(displacedPositionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = SafeNormalize(_WorldSpaceCameraPos.xyz - output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                
                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                #if defined(_ALPHACLIP_ON)
                    clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a - _Cutoff);
                #endif

                half3 baseAlbedo, swapEmission;
                ApplyTextureSwapAndGetEmission(input.positionWS, input.positionOS, normalize(input.normalWS), input.uv, baseAlbedo, swapEmission);
                
                half3 litColor;
                #if defined(_LIGHTINGMODEL_UNLIT)
                    litColor = CalculateLighting_Unlit(baseAlbedo);
                #elif defined(_LIGHTINGMODEL_STANDARD_LIT)
                    litColor = CalculateLighting_StandardLit(baseAlbedo, input.normalWS, input.positionWS);
                #elif defined(_LIGHTINGMODEL_BASIC_TOON)
                    litColor = CalculateLighting_BasicToon(baseAlbedo, input.normalWS, input.viewDirWS, input.positionWS);
                #elif defined(_LIGHTINGMODEL_STYLIZED_METAL)
                    litColor = CalculateLighting_StyledMetal(baseAlbedo, input.normalWS, input.viewDirWS, input.positionWS);
                #elif defined(_LIGHTINGMODEL_TOON_BLING)
                    litColor = CalculateLighting_ToonBling(baseAlbedo, input.normalWS, input.viewDirWS, input.positionWS, input.uv);
                #endif
                
                half3 finalColor = litColor * _BaseColor.rgb + _AmbientColor.rgb + swapEmission;
                half finalAlpha = _BaseColor.a;

                #if defined(_DISSOLVE_ON)
                    half dissolveAlpha;
                    finalColor = ApplyDissolveToFinalColor(finalColor, input.uv, input.dissolveValue, dissolveAlpha);
                    finalAlpha *= dissolveAlpha;
                #endif
                
                clip(finalAlpha - 0.01h);
                
                return half4(finalColor, finalAlpha);
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

            #include "Assets/Shaders/Dissolve/Includes/UberMaster_Core.hlsl"

            struct ShadowVaryings {
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
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a * _BaseColor.a;
                #if defined(_ALPHACLIP_ON)
                    clip(alpha - _Cutoff);
                #endif

                #if defined(_DISSOLVE_ON)
                    half noiseTexSample = SAMPLE_TEXTURE2D(_DissolveNoiseTex, sampler_DissolveNoiseTex, i.uv * _DissolveNoiseScale).r;
                    half pixelPerturbation = (noiseTexSample - 0.5h) * _DissolveNoiseStrength;
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
    }
    CustomEditor "UberMaster_GUI"
}