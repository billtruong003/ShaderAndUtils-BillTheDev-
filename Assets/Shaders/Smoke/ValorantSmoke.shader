Shader "Custom/Stylized/ValorantSmoke"
{
    Properties
    {
        [Header(Shape and Density)]
        _ShellThickness("Shell Thickness", Range(0.01, 10.0)) = 0.3
        _DensityMultiplier("Density Multiplier", Range(0, 1000)) = 20
        _RaymarchSteps("Raymarch Steps", Range(16, 256)) = 64

        [Header(Noise and Animation)]
        _NoiseTexture("Noise Texture (3D)", 3D) = "white" {}
        _NoiseScale("Noise Scale", Float) = 2.0
        _NoiseScrollSpeed("Noise Scroll Speed", Vector) = (0.1, 0.2, 0.1, 0)
        
        [Header(Warp Effect)]
        [Toggle(_ENABLE_WARP)] _EnableWarp("Enable Warp", Float) = 1
        _WarpTexture("Warp Texture (3D)", 3D) = "black" {}
        _WarpScale("Warp Scale", Range(0.1, 20)) = 5
        _WarpStrength("Warp Strength", Range(0, 1)) = 0.2

        [Header(Color and Lighting)]
        _LitColorRamp("Lit Color Ramp", 2D) = "white" {}
        _ShadowColorRamp("Shadow Color Ramp", 2D) = "black" {}
        _LightAbsorption("Light Absorption", Range(0, 20)) = 5
        _RimColor("Rim Color", Color) = (1,1,1,0.5)
        _RimPower("Rim Power", Range(0.1, 20.0)) = 3.0

        [Header(Edge and Intersection)]
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
            #pragma shader_feature_local _ENABLE_WARP
            
            // Core logic is included first, which brings in all definitions.
            #include "Includes/ValorantSmokeCore.hlsl"

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.worldPosition = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.worldPosition);
                output.worldNormal = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirection = normalize(_WorldSpaceCameraPos.xyz - output.worldPosition);
                output.screenPosition = ComputeScreenPos(output.positionCS);
                output.objectPositionOS = input.positionOS.xyz;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // Call the main function directly without passing any CBuffer.
                return ValorantSmokeFragment(input);
            }
            ENDHLSL
        }
    }
    CustomEditor "ValorantSmokeShaderGUI"
}