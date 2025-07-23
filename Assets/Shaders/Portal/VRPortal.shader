Shader "Master/VRPortal"
{
    Properties
    {
        [Header(Render Mode)]
        [Enum(Opaque, 0, Transparent, 1)] _SurfaceType ("Surface Type", Float) = 1.0

        [Header(Portal Core)]
        [HDR] _PortalColor("Core Color", Color) = (0, 1, 1, 1)
        _PortalRadius("Radius", Range(0, 1)) = 0.4
        _PrimaryNoise("Primary Noise (Seamless RGBA)", 2D) = "white" {}
        _PrimaryNoise_TilingSpeed("Noise Tiling & Speed (XY=Tiling, ZW=Speed)", Vector) = (1, 1, 0.2, 0.3)
        _SpiralStrength("Spiral Strength", Float) = 2.0
        _InwardPullSpeed("Inward Pull Speed", Float) = 0.5

        [Header(Edge and Glow)]
        _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        _RimWidth("Rim Width", Range(0, 0.2)) = 0.05
        _EdgeSoftness("Edge Softness", Range(0.001, 0.1)) = 0.02
        _InnerGlowColor("Inner Glow Color", Color) = (0.5, 1, 1, 0.5)
        _InnerGlowWidth("Inner Glow Width", Range(0, 0.5)) = 0.1

        [Header(Wobble Effect)]
        _WobbleFrequency("Wobble Frequency", Float) = 10.0
        _WobbleAmplitude("Wobble Amplitude", Range(0, 0.1)) = 0.05

        [Header(Distortion Effect)]
        _DistortionNoise("Distortion Noise (Seamless)", 2D) = "gray" {}
        _DistortionNoise_TilingSpeed("Noise Tiling & Speed (XY=Tiling, ZW=Speed)", Vector) = (2, 2, -0.1, -0.1)
        _DistortionStrength("Distortion Strength", Range(0, 0.2)) = 0.03

        [Header(Sparkle Effect)]
        _SparkleNoise("Sparkle Noise (High Contrast)", 2D) = "black" {}
        [HDR] _SparkleColor("Sparkle Color", Color) = (1, 1, 0.5, 1)
        _Sparkle_TilingSpeed("Noise Tiling & Speed (XY=Tiling, ZW=Speed)", Vector) = (3, 3, 0.5, 0.5)
        _SparkleThreshold("Sparkle Threshold", Range(0.5, 1)) = 0.95
        _SparkleSize("Sparkle Size", Range(0.001, 0.1)) = 0.01

        [Header(Animation)]
        _TimeScale("Time Scale", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "PortalForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_local _ _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local _WOBBLE_EFFECT_ON
            #pragma shader_feature_local _DISTORTION_EFFECT_ON
            #pragma shader_feature_local _SPARKLE_EFFECT_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct PortalCoordinates
            {
                float2 vectorToCenter;
                float distanceToCenter;
                float angle;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _PortalColor, _RimColor, _InnerGlowColor, _SparkleColor;
                float _PortalRadius, _RimWidth, _EdgeSoftness, _InnerGlowWidth;
                float _InwardPullSpeed, _SpiralStrength;
                float _WobbleFrequency, _WobbleAmplitude;
                float _DistortionStrength;
                float _SparkleThreshold, _SparkleSize;
                float _TimeScale;
                float4 _PrimaryNoise_TilingSpeed, _DistortionNoise_TilingSpeed, _Sparkle_TilingSpeed;
            CBUFFER_END

            TEXTURE2D(_PrimaryNoise);       SAMPLER(sampler_PrimaryNoise);
            TEXTURE2D(_DistortionNoise);    SAMPLER(sampler_DistortionNoise);
            TEXTURE2D(_SparkleNoise);       SAMPLER(sampler_SparkleNoise);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }
            
            float2 applyDistortion(float2 uv, float time)
            {
                #if _DISTORTION_EFFECT_ON
                    float2 distortionUV = uv * _DistortionNoise_TilingSpeed.xy + time * _DistortionNoise_TilingSpeed.zw;
                    float2 distortionOffset = (SAMPLE_TEXTURE2D(_DistortionNoise, sampler_DistortionNoise, distortionUV).xy - 0.5) * _DistortionStrength;
                    return uv + distortionOffset;
                #else
                    return uv;
                #endif
            }

            PortalCoordinates getPortalCoordinates(float2 uv)
            {
                PortalCoordinates p;
                p.vectorToCenter = 0.5 - uv;
                p.distanceToCenter = length(p.vectorToCenter);
                p.angle = atan2(p.vectorToCenter.y, p.vectorToCenter.x);
                return p;
            }

            float calculateWobble(float2 uv, float time)
            {
                #if _WOBBLE_EFFECT_ON
                    float sinTime = time * 0.2;
                    float2 waveVector = float2(sin(sinTime), cos(sinTime));
                    return sin(dot(uv * _WobbleFrequency, waveVector) + time) * _WobbleAmplitude;
                #else
                    return 0.0;
                #endif
            }

            float getPrimaryNoise(PortalCoordinates p, float time)
            {
                float pulledRadius = p.distanceToCenter - time * _InwardPullSpeed;
                float spiralAngle = p.angle + p.distanceToCenter * _SpiralStrength;
                
                float2 spiralUV = float2(cos(spiralAngle), sin(spiralAngle)) * pulledRadius;
                float2 noiseUV = spiralUV * _PrimaryNoise_TilingSpeed.xy + time * _PrimaryNoise_TilingSpeed.zw;

                return saturate(SAMPLE_TEXTURE2D(_PrimaryNoise, sampler_PrimaryNoise, noiseUV).r);
            }

            float4 getCoreColor(float noise, float mask)
            {
                return _PortalColor * noise * mask;
            }

            float4 getRimColor(float radius, float mask)
            {
                float rim = smoothstep(_PortalRadius - _RimWidth, _PortalRadius, radius) - smoothstep(_PortalRadius, _PortalRadius + _EdgeSoftness, radius);
                return _RimColor * rim * mask * _RimColor.a;
            }

            float4 getInnerGlow(float radius, float mask)
            {
                float glow = smoothstep(_PortalRadius - _InnerGlowWidth, _PortalRadius - _RimWidth, radius) - smoothstep(_PortalRadius - _RimWidth, _PortalRadius, radius);
                return _InnerGlowColor * glow * mask * _InnerGlowColor.a;
            }
            
            float4 getSparkles(float2 uv, float time, float mask)
            {
                #if _SPARKLE_EFFECT_ON
                    float2 sparkleUV = uv * _Sparkle_TilingSpeed.xy + time * _Sparkle_TilingSpeed.zw;
                    float noise = SAMPLE_TEXTURE2D(_SparkleNoise, sampler_SparkleNoise, sparkleUV).r;
                    float sparkle = smoothstep(_SparkleThreshold, _SparkleThreshold + _SparkleSize, noise);
                    return _SparkleColor * sparkle * mask;
                #else
                    return 0;
                #endif
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float time = _Time.y * _TimeScale;
                float2 distortedUV = applyDistortion(IN.uv, time);
                
                PortalCoordinates pCoords = getPortalCoordinates(distortedUV);
                float wobble = calculateWobble(distortedUV, time);
                float distortedRadius = pCoords.distanceToCenter + wobble;

                float visibilityMask = 1.0 - smoothstep(_PortalRadius - _EdgeSoftness, _PortalRadius, distortedRadius);

                #if !_SURFACE_TYPE_TRANSPARENT
                    clip(visibilityMask - 0.001);
                #endif

                float primaryNoise = getPrimaryNoise(pCoords, time);
                
                float4 core = getCoreColor(primaryNoise, visibilityMask);
                float4 rim = getRimColor(distortedRadius, visibilityMask);
                float4 glow = getInnerGlow(distortedRadius, visibilityMask);
                float4 sparkles = getSparkles(distortedUV, time, visibilityMask);

                float4 finalColor = core + rim + glow + sparkles;

                #if _SURFACE_TYPE_TRANSPARENT
                    finalColor.a = saturate(dot(finalColor, float4(0.299, 0.587, 0.114, 0)) + rim.a + glow.a) * visibilityMask;
                #else
                    finalColor.a = 1.0;
                #endif

                return finalColor;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_local _ _SURFACE_TYPE_TRANSPARENT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };
            
            float _PortalRadius;
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }
            
            void frag(Varyings IN)
            {
                #if !_SURFACE_TYPE_TRANSPARENT
                    float2 uv = IN.positionCS.xy / _ScreenParams.xy;
                    float distanceToCenter = length(uv - 0.5);
                    clip(_PortalRadius - distanceToCenter);
                #endif
            }
            ENDHLSL
        }
    }
    CustomEditor "Master.VRPortalShaderGUI"
    FallBack "Legacy Shaders/Transparent/Cutout/VertexLit"
}