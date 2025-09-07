// Tên file: URP_VAT_Toon_Instanced_Advanced.shader
Shader "BillTheDev/VAT/URP_VAT_Toon_Instanced_Advanced"
{
    Properties
    {
        [Header(Render States)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2 // Back
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest Mode", Float) = 4 // LEqual
        [Enum(Opaque, 0, Transparent, 1)] _Surface ("Surface Type", Float) = 0

        [Header(VAT Properties)]
        [NoScaleOffset] _PositionTexture ("Position Texture (VAT)", 2D) = "white" {}
        _PositionMin ("Position Min (Local Space)", Vector) = (0,0,0,0)
        _PositionMax ("Position Max (Local Space)", Vector) = (0,0,0,0)

        [Header(Surface Properties)]
        [NoScaleOffset] _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        [NoScaleOffset] _BumpMap ("Normal Map", 2D) = "bump" {}

        [Header(Toon Shading)]
        _HighlightColor ("Highlight Color", Color) = (1,1,1,1)
        _MidtoneColor("Midtone Color", Color) = (0.7, 0.7, 0.7, 1)
        _ShadowColor ("Shadow Color", Color) = (0.3, 0.3, 0.3, 1)
        
        [Space(10)]
        [Range(0.01, 1)] _HighlightThreshold ("Highlight Threshold", Float) = 0.9
        [Range(0.01, 1)] _MidtoneThreshold ("Midtone Threshold", Float) = 0.6
        [Range(0.01, 1)] _Smoothness ("Transition Smoothness", Float) = 0.05

        [Header(Fake Light Properties)]
        _FakeLightDirection ("Fake Light Direction", Vector) = (0.5, 0.5, 0, 0)
        [Range(0, 5)] _LightIntensity ("Light Intensity", Float) = 1.0

        [Header(Rim Light)]
        [Toggle(_RIM_LIGHT_ON)] _EnableRimLight("Enable Rim Light", Float) = 0
        _RimColor("Rim Color", Color) = (1,1,1,1)
        [Range(0.1, 5.0)] _RimPower("Rim Power", Float) = 2.0
        [Range(0, 1)] _RimThreshold("Rim Threshold", Float) = 0.5
    }
    SubShader
    {
        Tags 
        { 
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 200

        Pass
        {
            Name "Opaque"
            Tags { "LightMode"="UniversalForward" }

            Cull [_Cull]
            ZTest [_ZTest]
            ZWrite On
            Blend Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _RIM_LIGHT_ON
            #pragma target 4.5

            #define IS_OPAQUE_PASS
            #include "Includes/URP_VAT_Toon_Instanced_Core.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Transparent"
            Tags { "LightMode"="UniversalForward" "Queue"="Transparent" "RenderType"="Transparent" }

            Cull [_Cull]
            ZTest [_ZTest]
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _RIM_LIGHT_ON
            #pragma target 4.5

            #define IS_TRANSPARENT_PASS
            #include "Includes/URP_VAT_Toon_Instanced_Core.hlsl"
            ENDHLSL
        }
    }
    CustomEditor "BillTheDev.Editor.ToonVATInstancedShaderGUI"
}