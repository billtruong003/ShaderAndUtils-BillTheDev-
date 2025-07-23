Shader "Optimized/OptimizeVRPortal"
{
    Properties
    {
        [Header(Portal Core)]
        [HDR] _PortalColor("Core Color", Color) = (0, 1, 1, 1)
        _PortalRadius("Radius", Range(0, 1)) = 0.4
        _PullSpeed("Inward Pull Speed", Float) = 0.5
        _SpiralStrength("Spiral Strength", Float) = 2.0
        _TimeScale("Time Scale", Float) = 1.0

        [Header(Edge and Rim)]
        _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        _RimWidth("Rim Width", Range(0, 0.2)) = 0.05
        _EdgeSoftness("Edge Softness", Range(0.001, 0.1)) = 0.02

        [Header(Wobble Effect)]
        _WobbleFrequency("Wobble Frequency", Float) = 10.0
        _WobbleAmplitude("Wobble Amplitude", Range(0, 0.1)) = 0.05

        [Header(Textures)]
        _NoiseTex("Spiral Noise (Seamless)", 2D) = "white" {}
        _NoiseTilingAndSpeed("Noise Tiling & Speed (XY=Tiling, ZW=Speed)", Vector) = (1, 1, 0.2, 0.3)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local _WOBBLE_EFFECT_ON

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

            struct PortalInfo
            {
                float2 toCenter;
                float radius;
                float angle;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _PortalColor;
                float4 _RimColor;
                float _PortalRadius;
                float _PullSpeed;
                float _SpiralStrength;
                float _RimWidth;
                float _EdgeSoftness;
                float _WobbleFrequency;
                float _WobbleAmplitude;
                float _TimeScale;
                float4 _NoiseTilingAndSpeed;
            CBUFFER_END

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            PortalInfo getPortalCoordinates(float2 uv)
            {
                PortalInfo p;
                p.toCenter = uv - 0.5;
                p.radius = length(p.toCenter);
                p.angle = atan2(p.toCenter.y, p.toCenter.x);
                return p;
            }

            float calculateWobble(float2 uv, float time, float frequency, float amplitude)
            {
                float sinTime = time * 0.2;
                float2 waveVec = float2(sin(sinTime), cos(sinTime));
                return sin(dot(uv * frequency, waveVec) + time) * amplitude;
            }

            float calculateVisibilityMask(float radius, float portalRadius, float edgeSoftness)
            {
                return 1.0 - smoothstep(portalRadius - edgeSoftness, portalRadius, radius);
            }

            float getAnimatedNoise(float angle, float radius, float time)
            {
                float inwardRadius = radius - time * _PullSpeed;
                angle += radius * _SpiralStrength;

                float2 spiralUV = float2(cos(angle), sin(angle)) * inwardRadius * _NoiseTilingAndSpeed.xy;
                float2 timeOffset = _NoiseTilingAndSpeed.zw * time;
                
                return saturate(SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, spiralUV + timeOffset).r);
            }

            float4 getCoreColor(float noise, float mask)
            {
                return _PortalColor * noise * mask;
            }

            float4 getRimColor(float radius, float portalRadius, float rimWidth, float edgeSoftness, float mask)
            {
                float rimFalloff = smoothstep(portalRadius, portalRadius + edgeSoftness, radius);
                float rim = smoothstep(portalRadius - rimWidth, portalRadius, radius) - rimFalloff;
                return _RimColor * rim * mask * _RimColor.a;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float time = _Time.y * _TimeScale;
                PortalInfo pInfo = getPortalCoordinates(IN.uv);
                
                float wobble = 0;
                #if _WOBBLE_EFFECT_ON
                    wobble = calculateWobble(IN.uv, time, _WobbleFrequency, _WobbleAmplitude);
                #endif

                float distortedRadius = pInfo.radius - wobble;
                
                float portalMask = calculateVisibilityMask(distortedRadius, _PortalRadius, _EdgeSoftness);
                clip(portalMask - 0.5); 

                float noiseSample = getAnimatedNoise(pInfo.angle, distortedRadius, time);

                float4 coreColor = getCoreColor(noiseSample, portalMask);
                float4 rimColor = getRimColor(distortedRadius, _PortalRadius, _RimWidth, _EdgeSoftness, portalMask);
            
                float4 finalColor = coreColor + rimColor;
                finalColor.a = 1.0; 

                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack "Legacy Shaders/Transparent/Cutout/VertexLit"
}