Shader "Bill's Toon/Enhanced/Toon Bling"
{
    Properties
    {
        [Header(Base Properties)]
        _BaseMap("Albedo (RGB) Alpha (A)", 2D) = "white" {}
        [HDR] _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [HideInInspector] _AlphaClipMode("Enable Alpha Clip", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        
        [HideInInspector] _EmissionMode("Enable Emission", Float) = 0
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,1)
        _EmissionMap("Emission Map", 2D) = "black" {}

        [Header(Toon Shading)]
        _ToonRampOffset("Ramp Offset", Range(0, 1)) = 0.5
        _ToonRampSmoothness("Ramp Smoothness", Range(0.001, 1)) = 0.05
        [HDR] _ShadowTint("Shadow Tint", Color) = (0.7, 0.7, 0.8, 1)

        [Header(Toon Specular)]
        [HDR] _SpecColor("Specular Color", Color) = (1,1,1,1)
        _SpecSmoothness("Specular Smoothness", Range(0.001, 1.0)) = 0.05
        _SpecOffset("Specular Offset", Range(0, 1)) = 0.95

        [Header(Rim Lighting)]
        [HDR] _RimColor("Rim Color", Color) = (1,1,1,1)
        _RimPower("Rim Power", Range(0.1, 10.0)) = 3.0
        _RimMin("Rim Min", Range(0, 1)) = 0.0
        _RimMax("Rim Max", Range(0, 1)) = 1.0

        [Header(Bling Effect)]
        [Toggle(_BLING_WORLDSPACE_ON)] _BlingWorldSpace("Use World Space Bling", Float) = 0
        [HDR] _BlingColor("Bling Color", Color) = (1,1,1,1)
        _BlingIntensity("Bling Intensity", Range(0, 10)) = 2.0
        _BlingScale("Bling Scale", Range(1, 10000)) = 50.0
        _BlingSpeed("Bling Speed", Range(0, 5)) = 1.0
        _BlingFresnelPower("Bling Fresnel Power", Range(0.1, 10)) = 2.0
        _BlingThreshold("Bling Threshold", Range(0.5, 1.0)) = 0.95
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex BlingVertex
            #pragma fragment BlingFragment
            
            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local_fragment _EMISSION_ON
            #pragma shader_feature_local_fragment _BLING_WORLDSPACE_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            
            #include "Assets/_Shmackle/Models/CharacterTestShader/Shader/ToonUberBreakDownShader/Includes/ToonBlingMetallic_Core.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            
            #pragma shader_feature_local_fragment _ALPHACLIP_ON

            #include "Assets/_Shmackle/Models/CharacterTestShader/Shader/ToonUberBreakDownShader/Includes/ToonBlingMetallic_Core.hlsl"

            Varyings_ShadowPass ShadowVertex(Attributes input)
            {
                Varyings_ShadowPass o;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = GetShadowCoord(positionInputs);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }

            half4 ShadowFragment(Varyings_ShadowPass input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half alpha = albedo.a * _BaseColor.a;

                #if defined(_ALPHACLIP_ON)
                    clip(alpha - _Cutoff);
                #endif
                
                return 0;
            }
            ENDHLSL
        }
    }
    CustomEditor "ToonBlingMetallicShaderGUI"
}