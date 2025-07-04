// Shader "Uber" cho nhiều loại vòng phép
Shader "DanTheCreative/MagicCircleUber"
{
    Properties
    {
        [Header(Base Settings)]
        _Ring1Tex("Vân đồ Vòng 1 (RGBA)", 2D) = "white" {}
        _Ring1Color("Màu Vòng 1", Color) = (1, 1, 1, 1)
        _Ring1Speed("Tốc độ xoay Vòng 1", Range(-5, 5)) = 1.0
        _Ring2Tex("Vân đồ Vòng 2 (RGBA)", 2D) = "white" {}
        _Ring2Color("Màu Vòng 2", Color) = (1, 1, 1, 1)
        _Ring2Speed("Tốc độ xoay Vòng 2", Range(-5, 5)) = -0.5
        [HDR]_EmissionColor("Màu Phát Sáng và Cường Độ", Color) = (0, 1, 1, 1)
        
        [Header(Effects)]
        _Activation("Kích hoạt (0-1)", Range(0, 1.01)) = 1.0
        _PulseSpeed("Tốc độ Rung Động", Float) = 2.0
        _PulseStrength("Sức mạnh Rung Động", Range(0, 2)) = 0.5
        
        // --- SCI-FI ---
        [Header(SciFi Effects)]
        [Toggle(ENABLE_SCANLINES)] _EnableScanlines ("Bật Dòng Quét (Scanlines)", Float) = 0
        _ScanlineDensity("Mật độ Dòng Quét", Range(10, 200)) = 100
        _ScanlineSpeed("Tốc độ Dòng Quét", Float) = 10
        [Toggle(ENABLE_GLITCH)] _EnableGlitch ("Bật Nhiễu (Glitch)", Float) = 0
        _GlitchStrength("Cường độ Nhiễu", Range(0, 0.1)) = 0.01
        _GlitchFrequency("Tần suất Nhiễu", Range(1, 100)) = 50

        // --- CORRUPTED / DARK ---
        [Header(Corrupted Dark Effects)]
        [Toggle(ENABLE_DISTORTION)] _EnableDistortion ("Bật Biến Dạng (Distortion)", Float) = 0
        _DistortionTex("Vân đồ Biến Dạng (Noise)", 2D) = "gray" {}
        _DistortionStrength("Sức mạnh Biến Dạng", Range(0, 0.2)) = 0.05
        _DistortionSpeed("Tốc độ Biến Dạng", Float) = 1

        // --- NATURE / DIVINE ---
        [Header(Nature Divine Effects)]
        [Toggle(ENABLE_BREATHING)] _EnableBreathing ("Bật Hiệu ứng 'Thở'", Float) = 0
        _BreatheSpeed("Tốc độ 'Thở'", Range(0, 5)) = 1
        _BreatheAmount("Biên độ 'Thở'", Range(0, 0.2)) = 0.05

        [Header(Rendering Options)]
        _Cull("Culling Mode", Float) = 2.0 // 0=Off, 1=Front, 2=Back
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "DisableBatching" = "True" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Blend SrcAlpha OneMinusSrcAlpha 
            ZWrite Off 
            Cull [_Cull] 

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Các #pragma shader_feature này sẽ tạo ra các biến thể shader
            // Dựa trên các Toggle trong Properties
            #pragma shader_feature _ ENABLE_SCANLINES
            #pragma shader_feature _ ENABLE_GLITCH
            #pragma shader_feature _ ENABLE_DISTORTION
            #pragma shader_feature _ ENABLE_BREATHING

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets\Shaders\MagicCircle\MagicCircleURP_Pass.hlsl"

            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Transparent"
}