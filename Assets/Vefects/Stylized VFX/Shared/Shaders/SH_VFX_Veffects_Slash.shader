// Made with Amplify Shader Editor - Converted for URP
// Available at the Unity Asset Store - http://u3d.as/y3X
Shader "Vefects/URP/SH_VFX_Veffects_Slash_URP"
{
    Properties
    {
        [Space(13)][Header(Slash)][Space(13)]
        _Slash_Texture("Slash_Texture", 2D) = "white" {}
        _Slash_Scale("Slash_Scale", Float) = 1
        _Slash_Speed("Slash_Speed", Float) = 1
        [Space(13)][Header(Slash Noise)][Space(13)]
        _Slash_Noise_Texture("Slash_Noise_Texture", 2D) = "white" {}
        _Slash_Noise_Scale("Slash_Noise_Scale", Vector) = (1,1,0,0)
        _Slash_Noise_Speed("Slash_Noise_Speed", Vector) = (-1,0.5,0,0)
        _Slash_Noise_Intensity("Slash_Noise_Intensity", Float) = 1
        [Space(13)][Header(Emissive)][Space(13)]
        _Emissive_Slash_Texture("Emissive_Slash_Texture", 2D) = "white" {}
        _Emissive_Slash_Scale("Emissive_Slash_Scale", Float) = 1
        _Emissive_Slash_Speed("Emissive_Slash_Speed", Float) = 1
        _Emissive_Intensity("Emissive_Intensity", Float) = 3
        [Space(13)][Header(Emissive Dissolve)][Space(13)]
        _Emissive_Dissolve_Texture("Emissive_Dissolve_Texture", 2D) = "white" {}
        _Emissive_Dissolve_Scale("Emissive_Dissolve_Scale", Vector) = (1,1,0,0)
        _Emissive_Dissolve_Speed("Emissive_Dissolve_Speed", Vector) = (1,1,0,0)
        [Space(13)][Header(Distortion)][Space(13)]
        _Distortion_Noise_Texture("Distortion_Noise_Texture", 2D) = "white" {}
        _Distortion_Noise_Scale("Distortion_Noise_Scale", Vector) = (1,1,0,0)
        _Distortion_Noise_Speed("Distortion_Noise_Speed", Vector) = (1,1,0,0)
        _Distortion_Intensity("Distortion_Intensity", Float) = 1
        [Space(13)][Header(Color Noise)][Space(13)]
        _Color_Noise_Texture("Color_Noise_Texture", 2D) = "white" {}
        _ColorNoise_Scale("ColorNoise_Scale", Vector) = (1,1,0,0)
        _ColorNoise_Speed("ColorNoise_Speed", Vector) = (1,1,0,0)
        _Color_Boost("Color_Boost", Float) = 1
        [Space(13)][Header(Opacity)][Space(13)]
        _Mask("Mask", 2D) = "white" {}
        _Opacity_Boost("Opacity_Boost", Float) = 1
        [Space(13)][Header(Colors)][Space(13)]
        _Color_1("Color_1", Color) = (1,0,0.6261435,0)
        _Color_2("Color_2", Color) = (0.06587124,0,1,0)
        _Emissive_Color("Emissive_Color", Color) = (1,0,0.6261435,0)
        
        [Space(33)][Header(Render Settings)][Space(13)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 0 // Off
        [Enum(UnityEngine.Rendering.BlendMode)] _Src("Src", Float) = 5 // SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _Dst("Dst", Float) = 10 // OneMinusSrcAlpha
        [Enum(Off, 0, On, 1)] _ZWrite("ZWrite", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4 // LEqual
    }

    SubShader
    {
        Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "RenderPipeline" = "UniversalPipeline" "IsEmissive" = "true" }
        
        Pass
        {
            Cull [_Cull]
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Blend [_Src] [_Dst]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float4 uv           : TEXCOORD0; // uv.xy, uv.zw (from uv2_texcoord2)
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 uv2          : TEXCOORD1; // Dùng để truyền uv2 từ shader gốc
                float4 color        : COLOR;
            };

            // Textures & Samplers
            TEXTURE2D(_Slash_Texture); SAMPLER(sampler_Slash_Texture);
            TEXTURE2D(_Slash_Noise_Texture); SAMPLER(sampler_Slash_Noise_Texture);
            TEXTURE2D(_Emissive_Slash_Texture); SAMPLER(sampler_Emissive_Slash_Texture);
            TEXTURE2D(_Emissive_Dissolve_Texture); SAMPLER(sampler_Emissive_Dissolve_Texture);
            TEXTURE2D(_Distortion_Noise_Texture); SAMPLER(sampler_Distortion_Noise_Texture);
            TEXTURE2D(_Color_Noise_Texture); SAMPLER(sampler_Color_Noise_Texture);
            TEXTURE2D(_Mask); SAMPLER(sampler_Mask);
            
            // Properties
            float4 _CutoutTexture_ST;
            float _Slash_Scale, _Slash_Speed;
            float2 _Slash_Noise_Scale, _Slash_Noise_Speed;
            float _Slash_Noise_Intensity;
            float _Emissive_Slash_Scale, _Emissive_Slash_Speed, _Emissive_Intensity;
            float2 _Emissive_Dissolve_Scale, _Emissive_Dissolve_Speed;
            float2 _Distortion_Noise_Scale, _Distortion_Noise_Speed;
            float _Distortion_Intensity;
            float2 _ColorNoise_Scale, _ColorNoise_Speed;
            float _Color_Boost;
            float4 _Mask_ST;
            float _Opacity_Boost;
            half4 _Color_1, _Color_2, _Emissive_Color;

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv.xy;
                // Shader gốc dùng uv_texcoord (TEXCOORD0) và uv2_texcoord2 (TEXCOORD1)
                // Để đơn giản, ta sẽ giả định uv2 nằm trong i.uv.zw
                // Nếu không đúng, bạn cần dùng một TEXCOORD riêng như TEXCOORD1 cho nó.
                o.uv2 = i.uv; 
                o.color = i.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // -- Distortion Calculation --
                float2 distUV = i.uv * _Distortion_Noise_Scale + (_Time.y * _Distortion_Noise_Speed);
                float distortionValue = SAMPLE_TEXTURE2D(_Distortion_Noise_Texture, sampler_Distortion_Noise_Texture, distUV).r;
                float distortion = (distortionValue * 0.1) * _Distortion_Intensity;

                // -- Albedo/Base Color Calculation --
                // Color Noise
                float2 colorNoiseUV = i.uv * _ColorNoise_Scale + (_Time.y * _ColorNoise_Speed);
                float colorNoise = SAMPLE_TEXTURE2D(_Color_Noise_Texture, sampler_Color_Noise_Texture, colorNoiseUV).r;
                half3 baseColor = lerp(_Color_1.rgb, _Color_2.rgb, colorNoise);

                // Main Slash Shape
                float2 slashUV = i.uv * float2(_Slash_Scale, 1.0) + (_Time.y * float2(_Slash_Speed, 0.0));
                float slashShape = SAMPLE_TEXTURE2D(_Slash_Texture, sampler_Slash_Texture, slashUV + distortion).r;

                // Slash Noise
                float2 slashNoiseUV = i.uv * _Slash_Noise_Scale + (_Time.y * _Slash_Noise_Speed);
                float slashNoise = SAMPLE_TEXTURE2D(_Slash_Noise_Texture, sampler_Slash_Noise_Texture, slashNoiseUV).g;
                
                // Combine Slash
                float combinedSlash = clamp((slashShape * _Slash_Noise_Intensity) + slashNoise, 0.0, 1.0);
                
                // Mask
                float2 maskUV = i.uv * _Mask_ST.xy + _Mask_ST.zw;
                float mask = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, maskUV).r;

                float finalSlashMask = mask * combinedSlash;
                
                half3 finalAlbedo = (baseColor * _Color_Boost) * finalSlashMask;

                // -- Emission Calculation --
                // Emissive Slash
                float2 emissiveSlashUV = i.uv * float2(_Emissive_Slash_Scale, 1.0) + (_Time.y * float2(_Emissive_Slash_Speed, 0.0));
                float emissiveSlashShape = SAMPLE_TEXTURE2D(_Emissive_Slash_Texture, sampler_Emissive_Slash_Texture, emissiveSlashUV + distortion).g;
                
                // Emissive Dissolve
                float2 emissiveDissolveUV = i.uv * _Emissive_Dissolve_Scale + (_Time.y * _Emissive_Dissolve_Speed);
                float emissiveDissolve = SAMPLE_TEXTURE2D(_Emissive_Dissolve_Texture, sampler_Emissive_Dissolve_Texture, emissiveDissolveUV).r;

                // Remap based on uv2.w (from i.uv2_texcoord2.w)
                float remap_in = 1.0 - i.uv2.w; // i.uv2.w tương ứng với i.uv2_texcoord2.w
                float remap = -1.0 + (remap_in - 0.0) * (0.0 - -1.0) / (1.0 - 0.0);
                
                float emissiveMask = saturate(remap + saturate(emissiveSlashShape * emissiveDissolve));
                emissiveMask *= mask;

                half3 finalEmission = (i.color.rgb * emissiveMask * _Emissive_Color.rgb * _Emissive_Intensity);

                // -- Alpha Calculation --
                // Remap based on uv2.z (from i.uv2_texcoord2.z)
                float alpha_remap_in = 1.0 - i.uv2.z; // i.uv2.z tương ứng với i.uv2_texcoord2.z
                float alpha_remap = 0.0 + (alpha_remap_in - 0.0) * (2.0 - 0.0) / (1.0 - 0.0);

                float alphaMask = saturate(alpha_remap + saturate(finalSlashMask * _Opacity_Boost));
                float finalAlpha = i.color.a * alphaMask;

                return half4(finalAlbedo + finalEmission, finalAlpha);
            }
            ENDHLSL
        }
    }
    // Fallback "Diffuse" // URP không dùng Fallback này
    Fallback "Universal Render Pipeline/Transparent"
}