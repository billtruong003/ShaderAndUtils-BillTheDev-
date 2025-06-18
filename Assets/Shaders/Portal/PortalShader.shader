Shader "Custom/SwirlPortalShader"
{
    Properties
    {
        // Portal Shape Properties
        _PortalSize ("Portal Size", Range(0.1, 1.0)) = 0.4
        _PortalSoftness ("Portal Softness", Float) = 3.0

        // Swirl Properties
        _SwirlSpeed ("Swirl Speed", Float) = 1.0
        _SwirlTightness ("Swirl Tightness", Float) = 5.0
        _SwirlStrength ("Swirl Strength", Range(0.0, 1.0)) = 1.0

        // Texture Noise Properties
        [NoScaleOffset] _NoiseTexture ("Noise Texture (Grayscale)", 2D) = "white" {}
        _TextureNoiseScale ("Texture Noise Scale", Float) = 1.0
        _TextureNoiseSpeed ("Texture Noise Speed (XY)", Vector) = (0.1, 0.0, 0,0)
        _TextureNoiseStrength ("Texture Noise Strength (Inner)", Float) = 1.0

        // Edge Noise Properties
        _EdgeNoiseAmplitude ("Edge Noise Amplitude", Range(0.0, 1.0)) = 0.2

        // Gradient Color Properties
        [HDR] _GradientColor1 ("Gradient Color 1 (Center)", Color) = (0,0,1,0.7)
        [HDR] _GradientColor2 ("Gradient Color 2", Color) = (0,1,1,0.7)
        [HDR] _GradientColor3 ("Gradient Color 3 (Edge)", Color) = (1,0,1,0.7)
        [HDR] _GlowColor ("Glow Color", Color) = (1,1,1,1)
        
        // Edge Glow Properties
        _EdgeThickness ("Edge Thickness", Float) = 0.1
        _GlowIntensity ("Glow Intensity", Float) = 5.0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _PortalSize;
                float _PortalSoftness;
                
                float _SwirlSpeed;
                float _SwirlTightness;
                float _SwirlStrength;

                TEXTURE2D(_NoiseTexture); SAMPLER(sampler_NoiseTexture); 
                float _TextureNoiseScale;
                float2 _TextureNoiseSpeed;
                float _TextureNoiseStrength;
                float _EdgeNoiseAmplitude;

                float4 _GradientColor1;
                float4 _GradientColor2;
                float4 _GradientColor3;
                float4 _GlowColor;
                float _EdgeThickness;
                float _GlowIntensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float2 ComputeNoiseUV(float2 uv, float time)
            {
                float2 centeredUV = uv - float2(0.5, 0.5);
                float distFromCenter = length(centeredUV);
                float angle = atan2(centeredUV.y, centeredUV.x);
                angle += time * _SwirlSpeed + distFromCenter * _SwirlTightness;
                float2 swirledCenteredUV = float2(cos(angle), sin(angle)) * distFromCenter;
                return lerp(centeredUV, swirledCenteredUV, _SwirlStrength) + float2(0.5, 0.5);
            }

            float ComputeEdgeNoise(float sampledNoise)
            {
                return (sampledNoise - 0.5) * _EdgeNoiseAmplitude;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _Time.y;

                // Calculate noise UV for Swirl
                float2 noiseUV = ComputeNoiseUV(uv, time);

                // Sample noise texture
                float2 textureSampleUV = noiseUV * _TextureNoiseScale + _TextureNoiseSpeed * time;
                float sampledNoise = SAMPLE_TEXTURE2D(_NoiseTexture, sampler_NoiseTexture, textureSampleUV).r;

                // Calculate edge noise
                float edgeNoise = ComputeEdgeNoise(sampledNoise);

                // Calculate portal mask
                float2 centeredUV = uv - float2(0.5, 0.5);
                float distFromCenter = length(centeredUV);
                float rawMask = distFromCenter / _PortalSize;
                float oneMinusRawMask = 1.0 - rawMask + edgeNoise;
                float portalAlphaMask = pow(saturate(oneMinusRawMask), _PortalSoftness);
                portalAlphaMask = saturate(portalAlphaMask);

                // Combine noise for inner effect
                float finalCombinedNoise = saturate(sampledNoise * _TextureNoiseStrength);

                // Calculate three-color gradient
                float gradientFactor = saturate(distFromCenter / _PortalSize);
                float4 gradientColor;
                if (gradientFactor < 0.5)
                {
                    gradientColor = lerp(_GradientColor1, _GradientColor2, gradientFactor / 0.5);
                }
                else
                {
                    gradientColor = lerp(_GradientColor2, _GradientColor3, (gradientFactor - 0.5) / 0.5);
                }
                gradientColor = lerp(gradientColor, gradientColor + finalCombinedNoise, 0.2); // Subtle noise influence

                // Edge glow effect
                float edgeRaw = pow(1.0 - portalAlphaMask, _EdgeThickness);
                float edgeMask = saturate(edgeRaw);
                float4 edgeGlowColor = edgeMask * _GlowColor;

                // Calculate final color
                float4 finalColorPreMask = gradientColor + edgeGlowColor;
                float4 finalColor = finalColorPreMask * portalAlphaMask;
                finalColor.rgb *= _GlowIntensity;
                finalColor.a = portalAlphaMask;

                return finalColor;
            }
            ENDHLSL
        }
    }
}