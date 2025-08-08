Shader "Toon/Advanced Dissolve & Swap"
{
    Properties
    {
        [Header(Dissolve Effect)]
        [Toggle(_DISSOLVE_ON)] _EnableDissolve("Enable Dissolve", Float) = 0
        _DissolveProgress("Dissolve Progress", Range(-0.1, 1.1)) = 0.0
        [Enum(Noise, 0, Directional, 1, Spherical, 2, Mask, 3, Vertex Color, 4)] _DissolveType("Dissolve Type", Float) = 0
        _DissolveMap("Dissolve Map (Noise/Mask)", 2D) = "gray" {}
        _DissolveMapTiling("Map Tiling", Float) = 1
        _DissolveVector("Direction / Center (W)", Vector) = (0, 1, 0, 1)
        [HDR] _DissolveEdgeColor("Edge Color", Color) = (1, 0, 0, 1)
        _DissolveEdgeWidth("Edge Width", Range(0.001, 0.5)) = 0.1
        _DissolveEdgeHardness("Edge Hardness", Range(0.001, 1)) = 0.1

        [Header(Texture Swap Effect)]
        [Toggle(_SWAP_ON)] _EnableSwap("Enable Texture Swap", Float) = 0
        _SwapProgress("Swap Progress", Range(0.0, 1.0)) = 0.0
        _SwapAlbedo("New Albedo", 2D) = "white" {}
        [Toggle(_SWAP_NORMAL_ON)] _EnableSwapNormal("Enable New Normal Map", Float) = 0
        [Normal] _SwapNormalMap("New Normal Map", 2D) = "bump" {}

        [Space(20)]
        [Header(Base Surface Properties)]
        _BaseMap("Base Map (Albedo)", 2D) = "white" {}
        [Toggle(_NORMALMAP_ON)] _EnableNormalMap("Enable Normal Map", Float) = 0
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Intensity", Range(0, 2)) = 1.0
        [Toggle(_ALPHATEST_ON)] _EnableAlphaClip("Enable Alpha Clipping", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [Header(Main Shading Ramp)]
        _HighlightColor("Highlight Color", Color) = (1,1,1,1)
        _MidtoneColor("Midtone Color", Color) = (0.8, 0.8, 0.8, 1)
        _ShadowColor("Shadow Color", Color) = (0.4, 0.4, 0.4, 1)
        _HighlightThreshold("Highlight Threshold", Range(0, 1)) = 0.8
        _ShadowThreshold("Shadow Threshold", Range(0, 1)) = 0.4
        _RampSmoothness("Ramp Smoothness", Range(0.001, 1)) = 0.05

        [Header(Lighting and Effects)]
        [Toggle(_SPECULAR_ON)] _EnableSpecular("Enable Specular", Float) = 1
        _SpecularColor("Specular Color", Color) = (1,1,1,1)
        _SpecularThreshold("Specular Threshold", Range(0, 1)) = 0.95
        _SpecularSmoothness("Specular Smoothness", Range(0.001, 1)) = 0.02
        
        [Toggle(_RIM_LIGHT_ON)] _EnableRimLight("Enable Rim Light", Float) = 1
        _RimColor("Rim Color", Color) = (1,1,1,1)
        _RimPower("Rim Power", Range(1, 10)) = 3.0
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" }

        HLSLINCLUDE
            #include "Assets/Shaders/Dissolve/Includes/AdvancedToonDissolveCore.hlsl"
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex MainVert
            #pragma fragment MainFrag

            #pragma shader_feature_local_fragment _NORMALMAP_ON
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SPECULAR_ON
            #pragma shader_feature_local_fragment _RIM_LIGHT_ON
            
            #pragma shader_feature_local_fragment _DISSOLVE_ON
            #pragma shader_feature_local_fragment _SWAP_ON
            #pragma shader_feature_local_fragment _SWAP_NORMAL_ON

            #pragma multi_compile_local _ _DISSOLVE_TYPE_NOISE _DISSOLVE_TYPE_DIRECTIONAL _DISSOLVE_TYPE_SPHERICAL _DISSOLVE_TYPE_MASK _DISSOLVE_TYPE_VERTEX_COLOR
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Front

            HLSLPROGRAM
            #pragma vertex MainVert
            #pragma fragment ShadowFrag

            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _DISSOLVE_ON
            #pragma multi_compile_local _ _DISSOLVE_TYPE_NOISE _DISSOLVE_TYPE_DIRECTIONAL _DISSOLVE_TYPE_SPHERICAL _DISSOLVE_TYPE_MASK _DISSOLVE_TYPE_VERTEX_COLOR
            ENDHLSL
        }
    }
    CustomEditor "ToonDissolveShaderGUI"
}