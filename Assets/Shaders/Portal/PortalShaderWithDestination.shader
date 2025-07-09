Shader "CleanCode/AdvancedPortal"
{
    Properties
    {
        [Header(Portal View and Color)]
        _MainTex ("Portal View (Render Texture)", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (1,1,1,1)

        [Header(Portal Shape)]
        [Toggle(USE_CUSTOM_SHAPE_MASK)] _UseCustomShape ("Use Shape Mask", Float) = 0
        _ShapeMask ("Shape Mask (R)", 2D) = "white" {}
        _PortalSize ("Portal Size / Reveal", Range(0, 1.5)) = 0.5
        _Softness ("Edge Softness", Range(0.001, 0.5)) = 0.05

        [Header(Edge Effects)]
        [Toggle(USE_POLAR_NOISE_EDGE)] _UsePolarNoiseEdge ("Use Polar Coords for Edge Noise", Float) = 1
        _EdgeNoiseTex ("Edge Noise", 2D) = "white" {}
        _EdgeNoiseStrength ("Edge Noise Strength", Range(0, 0.2)) = 0.02
        _EdgeNoiseScale ("Edge Noise Scale", Float) = 2.0
        _GlowColor ("Inner Glow Color", Color) = (0.5, 0.8, 1, 1)
        _EdgeGradientColor ("Outer Edge Color", Color) = (0.1, 0.2, 0.8, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 1.5

        [Header(View Distortion)]
        _DistortionNoiseTex ("Distortion Noise", 2D) = "white" {}
        _DistortionStrength ("Distortion Strength", Range(0, 0.1)) = 0.02
        _DistortionScale ("Distortion Scale", Float) = 1.0

        [Header(3D Parallax Effect)]
        [Toggle(ENABLE_PARALLAX)] _EnableParallax ("Enable 3D Parallax", Float) = 1
        _ParallaxStrength ("Parallax Strength", Range(0, 0.2)) = 0.05

        [Header(Chromatic Aberration)]
        [Toggle(ENABLE_CHROMATIC_ABERRATION)] _EnableChromaticAberration ("Enable Chromatic Aberration", Float) = 1
        _ChromaticAberrationAmount ("Chromatic Aberration Amount", Range(0, 0.05)) = 0.01

        [Header(Depth Pattern)]
        [Toggle(USE_POLAR_PATTERN)] _UsePolarPattern ("Use Polar Coords for Pattern", Float) = 0
        _DepthPatternColor ("Pattern Color (RGB) & Intensity (A)", Color) = (0.7, 0.9, 1, 0.2)
        _DepthPatternDistance ("Pattern Tiling / Spokes", Range(1, 100)) = 25.0
        [Toggle(ANIMATE_DEPTH_PATTERN)] _AnimateDepthPattern ("Animate Depth Pattern", Float) = 1
        _DepthPatternSpeed ("Pattern Animation Speed", Float) = 1.0
        _DepthPatternNoiseStrength ("Pattern Noise Strength", Range(0, 1)) = 0.1

        [Header(Global Animation)]
        _AnimationSpeed ("Global Animation Speed", Float) = 0.5
        _AnimationDirection ("Animation (X:Cartesian/Rotate, Y:Cartesian/Flow)", Vector) = (0.2, 0.5, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "DisableBatching" = "True"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local USE_CUSTOM_SHAPE_MASK
            #pragma shader_feature_local USE_POLAR_NOISE_EDGE
            #pragma shader_feature_local ENABLE_PARALLAX
            #pragma shader_feature_local ENABLE_CHROMATIC_ABERRATION
            #pragma shader_feature_local ANIMATE_DEPTH_PATTERN
            #pragma shader_feature_local USE_POLAR_PATTERN

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 viewDirTS    : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);                SAMPLER(sampler_MainTex);
            TEXTURE2D(_ShapeMask);              SAMPLER(sampler_ShapeMask);
            TEXTURE2D(_EdgeNoiseTex);           SAMPLER(sampler_EdgeNoiseTex);
            TEXTURE2D(_DistortionNoiseTex);     SAMPLER(sampler_DistortionNoiseTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _TintColor;
                half _PortalSize;
                half _Softness;
                half _EdgeNoiseStrength;
                float _EdgeNoiseScale;
                half _DistortionStrength;
                float _DistortionScale;
                half _ParallaxStrength;
                half _ChromaticAberrationAmount;
                half4 _GlowColor;
                half4 _EdgeGradientColor;
                half _GlowIntensity;
                half4 _DepthPatternColor;
                float _DepthPatternDistance;
                half _DepthPatternSpeed;
                half _DepthPatternNoiseStrength;
                float _AnimationSpeed;
                float2 _AnimationDirection;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;

                #if defined(ENABLE_PARALLAX)
                    float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                    float3 viewDirWS = GetWorldSpaceViewDir(positionWS);
                    float3 normalWS = TransformObjectToWorldNormal(float3(0, 0, -1));
                    float3 tangentWS = TransformObjectToWorldDir(float3(1, 0, 0));
                    float3 bitangentWS = TransformObjectToWorldDir(float3(0, 1, 0));
                    float3x3 worldToTangent = float3x3(tangentWS, bitangentWS, normalWS);
                    OUT.viewDirTS = mul(worldToTangent, viewDirWS);
                #else
                    OUT.viewDirTS = float3(0, 0, 0);
                #endif
                
                return OUT;
            }

            half CalculatePortalMask(float2 uv, half radius, half angle)
            {
                float2 edgeNoiseUV;
                #if defined(USE_POLAR_NOISE_EDGE)
                    half u = (angle / TWO_PI) + 0.5h;
                    half v = radius * _EdgeNoiseScale;
                    u += _Time.y * _AnimationSpeed * _AnimationDirection.x;
                    v -= _Time.y * _AnimationSpeed * _AnimationDirection.y;
                    edgeNoiseUV = float2(u, v);
                #else
                    edgeNoiseUV = uv * _EdgeNoiseScale + normalize(_AnimationDirection.xy) * _Time.y * _AnimationSpeed;
                #endif

                half edgeNoise = SAMPLE_TEXTURE2D(_EdgeNoiseTex, sampler_EdgeNoiseTex, edgeNoiseUV).r - 0.5h;
                half noisyRadius = radius - edgeNoise * _EdgeNoiseStrength;

                half radialMask = 1.0h - saturate(noisyRadius / _PortalSize);

                half shapeMask = 1.0h;
                #if defined(USE_CUSTOM_SHAPE_MASK)
                    shapeMask = SAMPLE_TEXTURE2D(_ShapeMask, sampler_ShapeMask, uv).r;
                #endif

                half baseMask = radialMask * shapeMask;
                return smoothstep(0, _Softness, baseMask);
            }

            half2 GetParallaxOffset(float3 viewDirTS)
            {
                #if defined(ENABLE_PARALLAX)
                    return normalize(viewDirTS.xy) * (1 - viewDirTS.z) * _ParallaxStrength;
                #else
                    return 0;
                #endif
            }

            half2 GetDistortionOffset(float2 uv)
            {
                float2 distortNoiseUV = uv * _DistortionScale - normalize(_AnimationDirection.xy) * _Time.y * _AnimationSpeed;
                half noiseX = SAMPLE_TEXTURE2D(_DistortionNoiseTex, sampler_DistortionNoiseTex, distortNoiseUV).r - 0.5h;
                half noiseY = SAMPLE_TEXTURE2D(_DistortionNoiseTex, sampler_DistortionNoiseTex, distortNoiseUV + 0.5h).r - 0.5h;
                return half2(noiseX, noiseY) * _DistortionStrength;
            }

            half4 ApplyChromaticAberration(TEXTURE2D(tex), SAMPLER(samp), half2 uv, half2 center)
            {
                #if defined(ENABLE_CHROMATIC_ABERRATION)
                    half2 offsetDir = normalize(uv - center);
                    half r = SAMPLE_TEXTURE2D(tex, samp, uv - offsetDir * _ChromaticAberrationAmount).r;
                    half g = SAMPLE_TEXTURE2D(tex, samp, uv).g;
                    half b = SAMPLE_TEXTURE2D(tex, samp, uv + offsetDir * _ChromaticAberrationAmount).b;
                    return half4(r, g, b, 1.0h);
                #else
                    return SAMPLE_TEXTURE2D(tex, samp, uv);
                #endif
            }
            
            half3 ApplyDepthPattern(half3 currentColor, half radius, half angle)
            {
                half patternInput = radius;
                #if defined(USE_POLAR_PATTERN)
                    patternInput = angle / TWO_PI;
                #endif

                half pattern = 0.5h + 0.5h * sin(patternInput * _DepthPatternDistance);
                
                #if defined(ANIMATE_DEPTH_PATTERN)
                    half2 noiseUV = half2(patternInput * 2.0h, 0.0h) + _Time.y * _DepthPatternSpeed * 0.1h;
                    half depthNoise = SAMPLE_TEXTURE2D(_DistortionNoiseTex, sampler_DistortionNoiseTex, noiseUV).r;
                    float movement = _Time.y * _DepthPatternSpeed + depthNoise * _DepthPatternNoiseStrength;
                    pattern = 0.5h + 0.5h * sin(patternInput * _DepthPatternDistance + movement);
                #endif
                
                return lerp(currentColor, _DepthPatternColor.rgb, pattern * _DepthPatternColor.a);
            }

            half4 CalculateEdgeFX(half portalMask, half radius)
            {
                half edgeValue = saturate((1.0h - portalMask) * portalMask * 4.0h);
                half4 edgeGradient = lerp(_GlowColor, _EdgeGradientColor, saturate(radius / _PortalSize));
                return edgeGradient * edgeValue * _GlowIntensity;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half2 center = half2(0.5, 0.5);
                float2 vecFromCenter = IN.uv - center;
                half radius = length(vecFromCenter);
                half angle = atan2(vecFromCenter.y, vecFromCenter.x);
                
                half portalMask = CalculatePortalMask(IN.uv, radius, angle);

                half2 parallaxOffset = GetParallaxOffset(IN.viewDirTS);
                half2 distortionOffset = GetDistortionOffset(IN.uv);
                half2 finalUV = IN.uv + parallaxOffset + distortionOffset;

                half4 portalViewColor = ApplyChromaticAberration(_MainTex, sampler_MainTex, finalUV, center + parallaxOffset);
                portalViewColor.rgb *= _TintColor.rgb;
                
                half3 finalRGB = ApplyDepthPattern(portalViewColor.rgb, radius, angle);

                half4 edgeColor = CalculateEdgeFX(portalMask, radius);

                finalRGB = finalRGB * portalMask + edgeColor.rgb;
                half finalAlpha = saturate(portalMask + edgeColor.a);
                
                return half4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack "Transparent/VertexLit"
}