Shader "Advanced/HologramEmissive"
{
    Properties
    {
        [Header(Base Properties)]
        _BaseColor("Base Color Tint", Color) = (0, 0.75, 1, 1)
        [NoScaleOffset] _MainTex("Base Texture (A as Master Mask)", 2D) = "white" {}
        _AlphaMultiplier("Overall Alpha", Range(0.0, 1.0)) = 0.8

        [Header(Emission Layer)]
        _EmissionColor("Emission Color", Color) = (1, 1, 1, 1)
        _EmissionIntensity("Emission Intensity", Range(0, 10)) = 2.0
        [NoScaleOffset] _EmissionMap("Emission Map (Grayscale)", 2D) = "white" {}
        _EmissionScrollSpeed("Emission Scroll Speed (X, Y)", Vector) = (0.1, 0, 0, 0)

        [Header(Fresnel Effect)]
        _FresnelColor("Fresnel Color", Color) = (0.5, 1, 1, 1)
        _FresnelPower("Fresnel Power", Range(0.1, 10.0)) = 2.5
        _FresnelIntensity("Fresnel Intensity", Range(0.0, 5.0)) = 1.5

        [Header(Scanlines Effect)]
        _ScanlineColor("Scanline Color", Color) = (0.1, 0.1, 0.1, 0.1)
        _ScanlineSpeed("Scanline Speed", Float) = 15.0
        _ScanlineScale("Scanline Scale", Float) = 300.0

        [Header(Distortion Effects)]
        _FlickerFrequency("Flicker Frequency", Range(0.0, 100.0)) = 25.0
        _GlitchAmount("Glitch Amount", Range(0.0, 1.0)) = 0.05
        _GlitchFrequency("Glitch Frequency", Range(0.0, 100.0)) = 5.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float2 uv           : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _FresnelColor;
                float _FresnelPower;
                float _FresnelIntensity;
                float4 _ScanlineColor;
                float _ScanlineSpeed;
                float _ScanlineScale;
                float _FlickerFrequency;
                float _GlitchAmount;
                float _GlitchFrequency;
                float _AlphaMultiplier;
                float4 _EmissionColor;
                float _EmissionIntensity;
                float4 _EmissionScrollSpeed;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 viewDirectionWS = normalize(_WorldSpaceCameraPos.xyz - input.positionWS);

                float glitchNoise = (frac(sin(dot(input.positionWS.xy, float2(12.9898, 78.233))) * 43758.5453) - 0.5) * 2.0;
                float glitchDisplacement = frac(_Time.y * _GlitchFrequency) * glitchNoise * _GlitchAmount;
                float2 glitchUV = input.uv + float2(glitchDisplacement, 0);

                half4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, glitchUV);
                half textureAlphaMask = baseTex.a;

                float2 emissionUV = input.uv + _Time.y * _EmissionScrollSpeed.xy;
                half emissionSample = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, emissionUV).r;
                
                float fresnelDot = dot(input.normalWS, viewDirectionWS);
                float fresnelTerm = pow(1.0 - saturate(fresnelDot), _FresnelPower);
                half3 fresnelColor = _FresnelColor.rgb * fresnelTerm * _FresnelIntensity;

                half3 baseColor = _BaseColor.rgb * baseTex.rgb;
                half3 emissionColor = _EmissionColor.rgb * emissionSample * _EmissionIntensity;
                
                half3 internalColor = baseColor + emissionColor;
                half3 maskedInternalColor = internalColor * textureAlphaMask;

                half3 combinedColor = maskedInternalColor + fresnelColor;
                
                float scanlineValue = sin((input.positionWS.y + _Time.y * _ScanlineSpeed) * _ScanlineScale);
                half3 scanlineModifier = lerp(half3(1, 1, 1), _ScanlineColor.rgb, saturate(scanlineValue));
                
                float flickerValue = step(0.5, sin(_Time.y * _FlickerFrequency));
                float flickerModifier = lerp(1.0, flickerValue, saturate(fresnelTerm));

                half3 finalColor = combinedColor * scanlineModifier * flickerModifier;

                half finalAlpha = saturate(fresnelTerm + textureAlphaMask) * _AlphaMultiplier;

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Transparent/Unlit"
}