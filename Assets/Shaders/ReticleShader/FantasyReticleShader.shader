Shader "My Shaders/MultiStyleReticleShader"
{
    Properties
    {
        [Header(General Settings)]
        [MainTexture] _BaseMap ("Base Map (Reticle Shape)", 2D) = "white" {}
        [MainColor] _BaseColor ("Color (HDR)", Color) = (1, 0.5, 0, 1)
        _NoiseTex ("Noise Texture", 2D) = "gray" {}

        [Header(Style Controller)]
        // Dùng Float để dễ dàng truyền từ C# int, shader sẽ cast nó
        _Style ("Style (0=Fantasy, 1=Magic, 2=SciFi)", Float) = 0 

        [Header(Fantasy Style Settings)]
        _NoiseScale ("Fantasy - Noise Scale", Float) = 1.0
        _DistortionStrength ("Fantasy - Distortion", Range(0, 0.1)) = 0.02
        _ErosionThreshold ("Fantasy - Erosion", Range(0, 1)) = 0.4
        _AnimationSpeed("Fantasy - Anim Speed", Float) = 0.5

        [Header(Magic Circle Style Settings)]
        _RotationSpeed ("Magic - Rotation Speed", Float) = 2.0
        _PulseSpeed ("Magic - Pulse Speed", Float) = 5.0
        _RingCount ("Magic - Ring Count", Float) = 5.0
        _RingThickness("Magic - Ring Thickness", Range(0.001, 0.1)) = 0.02

        [Header(SciFi Style Settings)]
        _ScanlineDensity("SciFi - Scanline Density", Float) = 200.0
        _ScanlineSpeed("SciFi - Scanline Speed", Float) = 2.0
        _GlitchFrequency("SciFi - Glitch Frequency", Float) = 5.0
        _GlitchStrength("SciFi - Glitch Strength", Range(0, 0.5)) = 0.1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha 
            ZWrite Off
            ZTest Always 
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _NoiseScale;
                float _DistortionStrength;
                float _ErosionThreshold;
                float _AnimationSpeed;
                float _RotationSpeed;
                float _PulseSpeed;
                float _RingCount;
                float _RingThickness;
                float _ScanlineDensity;
                float _ScanlineSpeed;
                float _GlitchFrequency;
                float _GlitchStrength;
                int _Style;
            CBUFFER_END

            TEXTURE2D(_BaseMap);      SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NoiseTex);     SAMPLER(sampler_NoiseTex);

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 finalColor = half4(0,0,0,0);
                float2 uv = IN.uv;

                // --- Style 0: FANTASY NOISE ---
                if (_Style == 0)
                {
                    float2 noiseUV = uv * _NoiseScale + float2(_Time.y * _AnimationSpeed, 0);
                    half noiseValue = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
                    
                    float2 distortionOffset = (noiseValue - 0.5) * 2.0 * _DistortionStrength;
                    float2 distortedUV = uv + distortionOffset;
                    
                    half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, distortedUV);
                    
                    float2 erosionUV = uv * _NoiseScale + float2(0, -_Time.y * _AnimationSpeed * 0.7);
                    half erosionNoise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, erosionUV).r;
                    half erosionMask = step(_ErosionThreshold, erosionNoise);
                    
                    finalColor = baseColor * _BaseColor;
                    finalColor.a *= erosionMask;
                }
                // --- Style 1: MAGIC CIRCLE ---
                else if (_Style == 1)
                {
                    float2 p = uv - 0.5; // Center UVs
                    float angle = _Time.y * _RotationSpeed;
                    float2x2 rotMatrix = float2x2(cos(angle), -sin(angle), sin(angle), cos(angle));
                    p = mul(rotMatrix, p);

                    float r = length(p); // Distance from center
                    float a = atan2(p.y, p.x); // Angle

                    // Create pulsating rings
                    float ringValue = sin(r * _RingCount * PI * 2 - _Time.y * _PulseSpeed);
                    float rings = smoothstep(0.9, 1.0, ringValue);
                    
                    // Mask with a master circle and a soft inner fade
                    float circleMask = 1.0 - smoothstep(0.4, 0.5, r);
                    circleMask *= smoothstep(0.1, 0.15, r);
                    
                    finalColor = _BaseColor * rings * circleMask;
                }
                // --- Style 2: SCI-FI GLITCH ---
                else if (_Style == 2)
                {
                    // Base reticle shape
                    half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                    
                    // Scanlines
                    float scanline = sin((uv.y + _Time.y * _ScanlineSpeed) * _ScanlineDensity) * 0.5 + 0.5;
                    scanline = smoothstep(0.8, 1.0, scanline) * 0.5 + 0.7; // Make them sharp but not too dark
                    
                    // Glitch effect
                    float glitchTime = floor(_Time.y * _GlitchFrequency);
                    float2 glitchNoiseUV = float2(glitchTime, 0);
                    float glitchValue = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, glitchNoiseUV).r;
                    
                    float2 glitchOffset = 0;
                    if(glitchValue > 0.8) // Only glitch occasionally
                    {
                        glitchOffset.x = (SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, glitchNoiseUV * 0.5).r - 0.5) * _GlitchStrength;
                    }
                    half4 glitchedColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv + glitchOffset);

                    finalColor = glitchedColor * baseColor * _BaseColor * scanline;
                }
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}