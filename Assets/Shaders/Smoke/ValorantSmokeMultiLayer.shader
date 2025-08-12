Shader "CleanCode/Stylized/ValorantSmokeMultiLayer_Advanced"
{
    Properties
    {
        [Header(Formation and Quality)]
        _Progress("Formation Progress", Range(0.001, 1)) = 1.0
        _SphereRadius("Sphere Radius", Range(0.1, 10)) = 2.5
        _DensityMultiplier("Global Density Multiplier", Range(0, 100)) = 20
        _RaymarchSteps("Max Raymarch Steps", Range(16, 256)) = 64

        [Header(Shell Layer)]
        [Toggle(_ENABLE_SHELL)] _EnableShell("Enable Energy Shell", Float) = 1
        _ShellThickness("Shell Thickness", Range(0.01, 2.0)) = 0.2
        _ShellEdgeSoftness("Shell Edge Softness", Range(0.01, 1.0)) = 0.1
        _ShellDensity("Shell Density", Range(0, 10)) = 1.0
        [HDR] _ShellColor("Shell Color", Color) = (0.5, 0.1, 1, 1)
        _ShellNoiseScale("Shell Noise Scale", Float) = 4.0
        _ShellScrollSpeed("Shell Scroll Speed", Vector) = (0.3, -0.1, 0.2, 0)

        [Header(Core Layer)]
        _CoreFalloff("Core Falloff", Range(0.01, 5.0)) = 1.0
        _CoreNoiseScale("Core Noise Scale", Float) = 2.0
        _CoreScrollSpeed("Core Scroll Speed", Vector) = (0.1, 0.2, 0.1, 0)
        
        [Header(Camera Proximity Effect)]
        _ProximityDetailBoost("Proximity Detail Boost", Range(0, 5)) = 2.0
        _ProximityDensityMultiplier("Proximity Density Multiplier", Range(0, 5)) = 1.5

        [Header(Noise Source and Distortion)]
        _NoiseTexture("Noise Texture (3D)", 3D) = "white" {}
        [Toggle(_ENABLE_WARP)] _EnableWarp("Enable Noise Warp", Float) = 1
        _WarpTexture("Warp Texture (3D)", 3D) = "black" {}
        _WarpScale("Warp Scale", Range(0.1, 20)) = 5
        _WarpStrength("Warp Strength", Range(0, 1)) = 0.2

        [Header(Core Lighting and Color)]
        _LitColorRamp("Lit Color Ramp", 2D) = "white" {}
        _ShadowColorRamp("Shadow Color Ramp", 2D) = "black" {}
        _LightAbsorption("Light Absorption", Range(0, 20)) = 5
        _RimColor("Rim Color", Color) = (1,1,1,0.5)
        _RimPower("Rim Power", Range(0.1, 20.0)) = 3.0

        [Header(Final Edge and Intersection)]
        _EdgeColor("Edge Color (RGB) & Intensity (A)", Color) = (0,0,0,0.5)
        _EdgeHardness("Edge Hardness", Range(0.01, 1.0)) = 0.4
        _EdgeSoftness("Edge Softness", Range(0.01, 1.0)) = 0.1
        _DepthFadeDistance("Depth Fade Distance", Float) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma shader_feature_local _ENABLE_SHELL
            #pragma shader_feature_local _ENABLE_WARP
            
            #include "Includes/ValorantSmokeCore_Advanced.hlsl"

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.objectPositionOS = input.positionOS.xyz;
                output.worldPosition = TransformObjectToWorld(output.objectPositionOS);
                output.positionCS = TransformWorldToHClip(output.worldPosition);
                output.worldNormal = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirection = normalize(_WorldSpaceCameraPos.xyz - output.worldPosition);
                output.screenPosition = ComputeScreenPos(output.positionCS);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                return ValorantSmokeFragment(input);
            }
            ENDHLSL
        }
    }
    CustomEditor "ValorantSmokeMultiLayerShaderGUI"
}