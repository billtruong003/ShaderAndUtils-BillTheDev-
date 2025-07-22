Shader "PRO/AdvancedPortalURP"
{
    Properties
    {
        [Header(Portal Core)]
        [HDR] _PortalColor ("Core Color", Color) = (0, 1, 1, 1)
        _PortalRadius("Radius", Range(0, 1)) = 0.4
        _PullSpeed("Inward Pull Speed", Float) = 0.5
        _SpiralStrength("Spiral Strength", Float) = 2.0
        _TimeScale("Time Scale", Float) = 1.0

        [Header(Edge and Rim)]
        _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        _RimWidth("Rim Width", Range(0, 0.2)) = 0.05
        _EdgeSoftness("Edge Softness", Range(0.001, 0.1)) = 0.02

        [Header(Distortion and Effects)]
        _WobbleFrequency("Wobble Frequency", Float) = 10.0
        _WobbleAmplitude("Wobble Amplitude", Range(0, 0.1)) = 0.05
        _DistortionAmount("Scene Distortion", Range(0, 0.2)) = 0.03
        _ParallaxDepth("Parallax Depth", Range(0, 0.2)) = 0.05
        _ChromaticAberration("Chromatic Aberration", Range(0, 0.1)) = 0.01

        [Header(Textures)]
        _NoiseTex("Spiral Noise (Seamless)", 2D) = "white" {}
        _NoiseTilingAndSpeed("Noise Tiling & Speed (XY=Tiling, ZW=Speed)", Vector) = (1, 1, 0.2, 0.3)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
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

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

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
            };

            struct PortalInfo
            {
                float2 baseUV;
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
                float _DistortionAmount;
                float _ParallaxDepth;
                float _TimeScale;
                float _ChromaticAberration;
                float4 _NoiseTilingAndSpeed;
            CBUFFER_END

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            float snoise(float2 v)
            {
                const float4 C = float4(0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439);
                float2 i = floor(v + dot(v, C.yy));
                float2 x0 = v - i + dot(i, C.xx);
                float2 i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
                float4 x12 = x0.xyxy + C.xxzz;
                x12.xy -= i1;
                i = i - floor(i * (1.0 / 289.0)) * 289.0;
                float3 p = ( ( (i.y + float3(0.0, i1.y, 1.0)) * 34.0) + 1.0) * ( (i.y + float3(0.0, i1.y, 1.0))) + i.x + float3(0.0, i1.x, 1.0);
                p = p - floor(p * (1.0 / 289.0)) * 289.0;
                p = ( ( (p) * 34.0) + 1.0) * (p);
                p = p - floor(p * (1.0 / 289.0)) * 289.0;
                float3 m = max(0.5 - float3(dot(x0, x0), dot(x12.xy, x12.xy), dot(x12.zw, x12.zw)), 0.0);
                m = m * m;
                m = m * m;
                float3 x = 2.0 * frac(p * C.www) - 1.0;
                float3 h = abs(x) - 0.5;
                float3 ox = floor(x + 0.5);
                float3 a0 = x - ox;
                m *= 1.79284291400159 - 0.85373472095314 * (a0 * a0 + h * h);
                float3 g;
                g.x = a0.x * x0.x + h.x * x0.y;
                g.yz = a0.yz * x12.xz + h.yz * x12.yw;
                return 130.0 * dot(m, g);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                return OUT;
            }

            PortalInfo getPortalCoordinates(float2 uv)
            {
                PortalInfo p;
                p.baseUV = uv;
                p.toCenter = uv - 0.5;
                p.radius = length(p.toCenter);
                p.angle = atan2(p.toCenter.y, p.toCenter.x);
                return p;
            }

            float calculateWobble(float2 uv, float time, float frequency, float amplitude)
            {
                return snoise(uv * frequency + time) * amplitude;
            }

            float calculateVisibilityMask(float radius, float portalRadius, float edgeSoftness)
            {
                return 1.0 - smoothstep(portalRadius - edgeSoftness, portalRadius, radius);
            }

            float2 getNoiseVector(float2 tiling, float2 speed, float time)
            {
                return (tiling * speed * time);
            }

            float getLayeredNoise(float2 parallaxUV, float angle, float distortedRadius, float time)
            {
                float inwardRadius = distortedRadius - time * _PullSpeed;
                angle += distortedRadius * _SpiralStrength;
                
                float2 spiralUV = float2(cos(angle), sin(angle)) * inwardRadius * _NoiseTilingAndSpeed.xy;

                float2 noiseVec1 = getNoiseVector(float2(1.0, 1.0), _NoiseTilingAndSpeed.zw, time);
                float2 noiseVec2 = getNoiseVector(float2(2.1, 2.1), _NoiseTilingAndSpeed.zw * 1.5, time);

                float noise1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, spiralUV + parallaxUV + noiseVec1).r;
                float noise2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, spiralUV * 0.5 + parallaxUV + noiseVec2).r;

                return pow(noise1 * noise2, 2.5);
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

            float3 getDistortedSceneColor(float4 screenPos, float2 distortionVector)
            {
                float2 screenUV = screenPos.xy / screenPos.w;
                float3 sceneColor;

                if(_ChromaticAberration > 0)
                {
                    float3 R = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + distortionVector * (1.0 + _ChromaticAberration)).r;
                    float3 G = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + distortionVector).g;
                    float3 B = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + distortionVector * (1.0 - _ChromaticAberration)).b;
                    sceneColor = float3(R.r, G.g, B.b);
                }
                else
                {
                    sceneColor = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + distortionVector).rgb;
                }
                
                return sceneColor;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float time = _Time.y * _TimeScale;
                PortalInfo pInfo = getPortalCoordinates(IN.uv);
                
                float2 parallaxUV = pInfo.toCenter * _ParallaxDepth;
                float wobble = calculateWobble(pInfo.baseUV + parallaxUV, time, _WobbleFrequency, _WobbleAmplitude);
                float distortedRadius = pInfo.radius - wobble;

                float portalMask = calculateVisibilityMask(distortedRadius, _PortalRadius, _EdgeSoftness);
                clip(portalMask - 0.001);

                float noiseSample = getLayeredNoise(parallaxUV, pInfo.angle, distortedRadius, time);

                float4 coreColor = getCoreColor(noiseSample, portalMask);
                float4 rimColor = getRimColor(distortedRadius, _PortalRadius, _RimWidth, _EdgeSoftness, portalMask);
                
                float2 distortionVector = normalize(pInfo.toCenter) * noiseSample * portalMask * _DistortionAmount;
                float3 sceneColor = getDistortedSceneColor(IN.screenPos, distortionVector);

                float4 finalColor = lerp(float4(sceneColor, 1.0), coreColor, coreColor.a);
                finalColor += rimColor;
                finalColor.a = portalMask;

                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack "Transparent/VertexLit"
}