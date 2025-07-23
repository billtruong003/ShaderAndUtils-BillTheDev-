Shader "PRO/AdvancedPortalURP"
{
    Properties
    {
        [Header(Render Pipeline Settings)]
        [Enum(Opaque, 0, Transparent, 1)] _SurfaceType("Surface Type", Float) = 1.0

        [Header(Portal Core)]
        [HDR] _PortalColor("Core Color", Color) = (0, 1, 1, 1)
        _PortalRadius("Radius", Range(0, 1)) = 0.4
        _NoiseTex("Spiral Noise (Seamless)", 2D) = "white" {}
        _NoiseTilingAndSpeed("Noise Tiling & Speed", Vector) = (1, 1, 0.2, 0.3)
        _PullSpeed("Inward Pull Speed", Float) = 0.5
        _SpiralStrength("Spiral Strength", Float) = 2.0

        [Header(Edge and Rim)]
        _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        _RimWidth("Rim Width", Range(0, 0.2)) = 0.05
        _EdgeSoftness("Edge Softness", Range(0.001, 0.1)) = 0.02

        [Header(Dynamic Effects)]
        [Enum(Off, 0, Procedural, 1, Texture Based, 2)] _WobbleMode("Wobble Mode", Float) = 2
        _WobbleNoise("Wobble Noise (Texture)", 2D) = "gray" {}
        _WobbleTilingAndSpeed("Wobble Tiling & Speed", Vector) = (5, 5, 0.5, 0.5)
        _WobbleAmplitude("Wobble Amplitude", Range(0, 0.1)) = 0.05
        _WobbleFrequency("Wobble Frequency (Procedural)", Float) = 10.0

        [Header(Transparent Mode Effects)]
        [Enum(Off, 0, Simple Distortion, 1, Chromatic Aberration, 2)] _DistortionMode("Distortion Mode", Float) = 2
        _DistortionAmount("Scene Distortion", Range(0, 0.1)) = 0.03
        _ChromaticAberration("Chromatic Aberration", Range(0, 0.1)) = 0.01
        _ParallaxDepth("View Parallax Depth", Range(0, 0.2)) = 0.05
        _SoftIntersectionDistance("Soft Intersection Distance", Range(0.01, 5.0)) = 0.5

        [Header(Animation)]
        _TimeScale("Time Scale", Float) = 1.0

        // Keyword Toggles
        [Toggle(_PARALLAX_EFFECT_ON)] _EnableParallax("Enable View Parallax", Float) = 1
        [Toggle(_SOFT_INTERSECTION_ON)] _EnableSoftIntersection("Enable Soft Intersection", Float) = 1
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
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_local _SURFACE_TYPE_OPAQUE _SURFACE_TYPE_TRANSPARENT
            #pragma multi_compile_local _WOBBLEMODE_OFF _WOBBLEMODE_PROCEDURAL _WOBBLEMODE_TEXTURE_BASED
            #pragma multi_compile_local _DISTORTIONMODE_OFF _DISTORTIONMODE_SIMPLE_DISTORTION _DISTORTIONMODE_CHROMATIC_ABERRATION
            
            #pragma shader_feature_local _PARALLAX_EFFECT_ON
            #pragma shader_feature_local _SOFT_INTERSECTION_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D_X(_CameraOpaqueTexture);
            TEXTURE2D_X(_CameraDepthTexture);
            SAMPLER(sampler_LinearClamp);

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 screenPos    : TEXCOORD1;
                float3 viewDirVS    : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _PortalColor, _RimColor;
                float _PortalRadius, _PullSpeed, _SpiralStrength, _RimWidth, _EdgeSoftness;
                float _WobbleAmplitude, _WobbleFrequency, _DistortionAmount, _ParallaxDepth, _TimeScale, _ChromaticAberration;
                float _SoftIntersectionDistance;
                float4 _NoiseTilingAndSpeed, _WobbleTilingAndSpeed;
            CBUFFER_END

            TEXTURE2D(_NoiseTex);       SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_WobbleNoise);    SAMPLER(sampler_WobbleNoise);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = posInputs.positionCS;
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                OUT.uv = IN.uv;
                OUT.viewDirVS = posInputs.positionVS;
                return OUT;
            }

            float calculatePortalMask(float distance, float radius, float softness)
            {
                return 1.0 - smoothstep(radius - softness, radius, distance);
            }

            float calculateWobble(float2 uv, float time)
            {
                #if defined(_WOBBLEMODE_PROCEDURAL)
                    float proceduralNoise = sin(uv.x * _WobbleFrequency + time) * cos(uv.y * _WobbleFrequency + time);
                    return proceduralNoise * _WobbleAmplitude;
                #elif defined(_WOBBLEMODE_TEXTURE_BASED)
                    float2 wobbleUV = uv * _WobbleTilingAndSpeed.xy + time * _WobbleTilingAndSpeed.zw;
                    float textureNoise = SAMPLE_TEXTURE2D(_WobbleNoise, sampler_WobbleNoise, wobbleUV).r * 2.0 - 1.0;
                    return textureNoise * _WobbleAmplitude;
                #else
                    return 0.0;
                #endif
            }

            float calculateSpiralNoise(float2 uv, float distance, float time)
            {
                float2 centeredUV = uv - 0.5;
                float angle = atan2(centeredUV.y, centeredUV.x) + distance * _SpiralStrength;
                float inwardRadius = distance - time * _PullSpeed;

                float2 noiseCoords = float2(cos(angle), sin(angle)) * inwardRadius;
                float2 finalNoiseUV = noiseCoords * _NoiseTilingAndSpeed.xy + time * _NoiseTilingAndSpeed.zw;
                return saturate(pow(SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, finalNoiseUV).r, 2.0));
            }

            float calculateSoftIntersection(float4 screenPos, float surfaceDepthVS)
            {
                #if defined(_SURFACE_TYPE_TRANSPARENT) && defined(_SOFT_INTERSECTION_ON)
                    float sceneDepthRaw = SampleSceneDepth(screenPos.xy / screenPos.w);
                    float sceneDepthVS = LinearEyeDepth(sceneDepthRaw, _ZBufferParams);
                    float intersectionFade = saturate((sceneDepthVS - surfaceDepthVS) / _SoftIntersectionDistance);
                    return intersectionFade;
                #else
                    return 1.0;
                #endif
            }

            float3 getSceneColor(float4 screenPos, float2 distortion)
            {
                float2 screenUV = screenPos.xy / screenPos.w;
                #if defined(_DISTORTIONMODE_SIMPLE_DISTORTION)
                    return SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_LinearClamp, screenUV + distortion).rgb;
                #elif defined(_DISTORTIONMODE_CHROMATIC_ABERRATION)
                    float r = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_LinearClamp, screenUV + distortion * (1.0 + _ChromaticAberration)).r;
                    float g = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_LinearClamp, screenUV + distortion).g;
                    float b = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_LinearClamp, screenUV - distortion * (1.0 - _ChromaticAberration)).b;
                    return float3(r, g, b);
                #else // Off
                    return SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_LinearClamp, screenUV).rgb;
                #endif
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float time = _Time.y * _TimeScale;
                float2 centeredUV = IN.uv - 0.5;

                #if _PARALLAX_EFFECT_ON && defined(_SURFACE_TYPE_TRANSPARENT)
                    float parallaxAmount = (1.0 - length(centeredUV) * 2.0) * _ParallaxDepth;
                    float2 parallaxOffset = normalize(IN.viewDirVS.xy) * parallaxAmount;
                    centeredUV -= parallaxOffset;
                #endif

                float distanceToCenter = length(centeredUV);
                float wobbleOffset = calculateWobble(IN.uv, time);
                float distortedRadius = distanceToCenter - wobbleOffset;

                float portalMask = calculatePortalMask(distortedRadius, _PortalRadius, _EdgeSoftness);
                float noise = calculateSpiralNoise(IN.uv, distortedRadius, time);

                float4 coreColor = _PortalColor * noise * portalMask;
                float rimBand = smoothstep(_PortalRadius - _RimWidth, _PortalRadius, distortedRadius) - smoothstep(_PortalRadius, _PortalRadius + _EdgeSoftness, distortedRadius);
                float4 rimColor = _RimColor * rimBand * portalMask * _RimColor.a;
                float4 portalVisuals = coreColor + rimColor;

                #if _SURFACE_TYPE_OPAQUE
                    clip(portalMask - 0.001);
                    return float4(portalVisuals.rgb, 1.0);
                #else // _SURFACE_TYPE_TRANSPARENT
                    float2 distortionVector = 0;
                    #if !defined(_DISTORTIONMODE_OFF)
                       distortionVector = normalize(centeredUV) * noise * portalMask * _DistortionAmount;
                    #endif
                    
                    float3 sceneColor = getSceneColor(IN.screenPos, distortionVector);
                    float3 blendedRgb = lerp(sceneColor, portalVisuals.rgb, portalVisuals.a);
                    
                    float softIntersectionFade = calculateSoftIntersection(IN.screenPos, -IN.viewDirVS.z);
                    float finalAlpha = portalMask * softIntersectionFade;

                    return float4(blendedRgb, finalAlpha);
                #endif
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _SURFACE_TYPE_OPAQUE _SURFACE_TYPE_TRANSPARENT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            
            CBUFFER_START(UnityPerMaterial)
                float _PortalRadius;
            CBUFFER_END

            Varyings vert(Attributes IN) 
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }
            
            void frag(Varyings IN) 
            {
                #if _SURFACE_TYPE_OPAQUE
                    float mask = 1.0 - smoothstep(_PortalRadius - 0.01, _PortalRadius, length(IN.uv - 0.5));
                    clip(mask - 0.5);
                #else
                    clip(-1);
                #endif
            }
            ENDHLSL
        }
    }
    CustomEditor "AdvancedPortalURPShaderGUI"
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}