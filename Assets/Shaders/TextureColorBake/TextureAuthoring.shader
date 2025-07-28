Shader "Creative/TextureAuthoringTool_Expert_Final_V4_Production"
{
    Properties
    {
        [Header(Core Input Maps)]
        [MainTexture] _BaseMap("Base Color Map (Source)", 2D) = "white" {}
        _NoiseMap("Primary Noise Map (RG)", 2D) = "gray" {}
        _WarpMap("Distortion/Warp Map", 2D) = "gray" {}

        [Header(Master Output Control)]
        [KeywordEnum(Composite, Debug)] _OUTPUT_MODE_SELECT("Output Mode", Float) = 0
        [Enum(Core Maps,0,Effect Masks,1,Advanced Views,2)] _DebugCategory("Debug Category", Float) = 0

        [Header(Debug Views)]
        [Enum(Base Color,1,Height Map,2,Normal Map,3)] _DebugView_Core("   [Core Maps]", Float) = 1
        [Enum(Wear Mask,7,Dirt Mask,8,World Space Mask,9)] _DebugView_Masks("   [Effect Masks]", Float) = 7
        [Enum(Raw AO,4,Raw Curvature,5,Denoised AO,10,Denoised Curvature,11,Edges,6)] _DebugView_Advanced("   [Advanced Views]", Float) = 4

        [Header(Height and Normal Generation)]
        _HeightContrast("Height Contrast", Range(0, 5)) = 1.0
        _HeightBrightness("Height Brightness", Range(-1, 1)) = 0.0
        _HeightCurve("Height Power Curve", Range(0.1, 5.0)) = 1.0
        _NormalStrength("Normal Map Strength", Range(0, 15)) = 2.0

        [Header(Advanced Ambient Occlusion)]
        _AORadius("AO Ray Radius", Range(0, 0.1)) = 0.02
        _AOIntensity("AO Intensity", Range(0, 5)) = 1.8
        _AODistanceFalloff("AO Distance Falloff", Range(0, 5)) = 1.5
        [IntRange] _AOSamples("AO Samples Per Direction", Range(4, 64)) = 16
        [IntRange] _AODirections("AO Directions", Range(4, 32)) = 8

        [Header(Advanced Curvature)]
        _CurvatureKernelScale("Curvature Sample Scale", Range(1.0, 5.0)) = 1.0
        _CurvatureStrength("Curvature Strength", Range(0.1, 100.0)) = 25.0
        _CurvatureBias("Curvature Bias (Cavity/Edge)", Range(-1.0, 1.0)) = 0.0

        [Header(Edge Detection)]
        _EdgeThreshold("Edge Detection Threshold", Range(0, 2)) = 0.4
        _EdgeSoftness("Edge Softness", Range(0.001, 1.0)) = 0.05
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)

        [Header(Material Driven Wear)]
        _WearColor("Wear Albedo", Color) = (0.6, 0.6, 0.65, 1)
        _WearMetallic("Wear Metallic", Range(0, 1)) = 0.9
        _WearSmoothness("Wear Smoothness", Range(0, 1)) = 0.8
        _WearLevel("Wear Level (From Edges)", Range(0, 5)) = 1.5
        _WearExposureInfluence("Wear Exposure Influence (via AO)", Range(0, 1)) = 0.5
        _WearNoiseTiling("Wear Noise Tiling", Float) = 1.0
        _WearNoiseInfluence("Wear Noise Influence", Range(0, 2)) = 1.0

        [Header(Material Driven Dirt)]
        _DirtColor("Dirt Albedo", Color) = (0.2, 0.15, 0.1, 1)
        _DirtMetallic("Dirt Metallic", Range(0, 1)) = 0.0
        _DirtSmoothness("Dirt Smoothness", Range(0, 1)) = 0.1
        _DirtLevel("Dirt Level (In Cavities)", Range(0, 5)) = 1.8
        _DirtGravityInfluence("Dirt Gravity Influence (Upward Normal)", Range(0, 2)) = 0.75
        _DirtNoiseTiling("Dirt Noise Tiling", Float) = 2.5
        _DirtNoiseInfluence("Dirt Noise Influence", Range(0, 2)) = 1.0

        [Header(UV Warping)]
        [Toggle(ENABLE_UV_WARP)] _EnableUVWarp("Enable UV Warping", Float) = 0
        _WarpStrength("Warp Strength", Range(0, 0.1)) = 0.005
        _WarpTiling("Warp Noise Tiling", Float) = 0.5

        [Header(World Space Effects)]
        [Toggle(ENABLE_WORLD_EFFECTS)] _EnableWorldEffects("Enable World-Space Moss/Dust", Float) = 0
        _WorldEffectColor("World Effect Color", Color) = (0.3, 0.4, 0.2, 1)
        _WorldEffectMetallic("World Effect Metallic", Range(0, 1)) = 0.0
        _WorldEffectSmoothness("World Effect Smoothness", Range(0, 1)) = 0.2
        _WorldEffectProjectionScale("World Effect Projection Scale", Float) = 5.0
        _WorldEffectIntensity("World Effect Intensity", Range(0, 5)) = 1.5
        _WorldEffectAngleThreshold("World Effect Angle Threshold", Range(0, 1)) = 0.6
        _WorldEffectHardness("World Effect Hardness", Range(0, 10)) = 3.0

        [Header(PostProcessing amd Denoising)]
        [Toggle(ENABLE_DENOISING)] _EnableDenoising("Enable Bilateral Denoising", Float) = 1
        [IntRange] _DenoiseRadius("Denoise Filter Radius (Pixels)", Range(1, 8)) = 3
        _DenoiseDepthSensitivity("Denoise Depth Sensitivity", Range(0.01, 1.0)) = 0.1
        _DenoiseSpatialSigma("Denoise Spatial Sigma", Range(1.0, 10.0)) = 2.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local _OUTPUT_MODE_SELECT_DEBUG
            #pragma shader_feature_local ENABLE_UV_WARP
            #pragma shader_feature_local ENABLE_WORLD_EFFECTS
            #pragma shader_feature_local ENABLE_DENOISING

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
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
                float3 normalWS     : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);    SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NoiseMap);   SAMPLER(sampler_NoiseMap);
            TEXTURE2D(_WarpMap);    SAMPLER(sampler_WarpMap);
            float4 _BaseMap_TexelSize;

            float _DebugCategory, _DebugView_Core, _DebugView_Masks, _DebugView_Advanced;
            float _HeightContrast, _HeightBrightness, _HeightCurve, _NormalStrength;
            float _AORadius, _AOIntensity, _AODistanceFalloff;
            int _AOSamples, _AODirections;
            float _CurvatureKernelScale, _CurvatureStrength, _CurvatureBias;
            float _EdgeThreshold, _EdgeSoftness;
            float4 _OutlineColor;

            float _WearLevel, _WearExposureInfluence, _WearNoiseTiling, _WearNoiseInfluence;
            float4 _WearColor; float _WearMetallic, _WearSmoothness;

            float _DirtLevel, _DirtGravityInfluence, _DirtNoiseTiling, _DirtNoiseInfluence;
            float4 _DirtColor; float _DirtMetallic, _DirtSmoothness;

            float _WarpStrength, _WarpTiling;

            float4 _WorldEffectColor; float _WorldEffectMetallic, _WorldEffectSmoothness;
            float _WorldEffectProjectionScale, _WorldEffectIntensity, _WorldEffectAngleThreshold, _WorldEffectHardness;

            int _DenoiseRadius; float _DenoiseDepthSensitivity, _DenoiseSpatialSigma;

            // --- SECTION 1: UTILITY & SAMPLING FUNCTIONS ---

            float2 GetWarpedUVs(float2 uv)
            {
                #if ENABLE_UV_WARP
                    float2 warpVector = (SAMPLE_TEXTURE2D(_WarpMap, sampler_WarpMap, uv * _WarpTiling).rg * 2.0 - 1.0) * _WarpStrength;
                    return uv + warpVector;
                #else
                    return uv;
                #endif
            }

            float GetHeightFromUV(float2 uv)
            {
                float luminance = dot(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb, float3(0.299, 0.587, 0.114));
                float adjustedLuminance = pow(abs(luminance), _HeightCurve) * _HeightContrast + _HeightBrightness;
                return saturate(adjustedLuminance);
            }

            float3 GetNoise(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, uv).rgb;
            }

            float Gaussian(float x, float sigma)
            {
                return exp(-(x * x) / (2.0 * sigma * sigma));
            }

            // --- SECTION 2: GEOMETRIC ANALYSIS FUNCTIONS ---

            float3 GenerateNormalMap(float2 uv, float strength)
            {
                float2 texel = _BaseMap_TexelSize.xy;
                float h_l = GetHeightFromUV(uv - float2(texel.x, 0));
                float h_r = GetHeightFromUV(uv + float2(texel.x, 0));
                float h_b = GetHeightFromUV(uv - float2(0, texel.y));
                float h_t = GetHeightFromUV(uv + float2(0, texel.y));

                float dx = (h_r - h_l) * strength;
                float dy = (h_t - h_b) * strength;

                return normalize(float3(-dx, -dy, 1.0));
            }

            float GenerateAmbientOcclusion(float2 uv)
            {
                float centerHeight = GetHeightFromUV(uv);
                float totalOcclusion = 0.0;
                float stepSize = _AORadius / _AOSamples;

                [loop]
                for (int d = 0; d < _AODirections; ++d)
                {
                    float angle = d * (TWO_PI / _AODirections);
                    float2 direction = float2(cos(angle), sin(angle));
                    float maxOcclusionAngle = 0.0;
                    
                    [loop]
                    for (int s = 1; s <= _AOSamples; ++s)
                    {
                        float currentDist = s * stepSize;
                        float sampleHeight = GetHeightFromUV(uv + direction * currentDist);
                        float heightDiff = (sampleHeight - centerHeight) * _AOIntensity * 100.0;
                        float occlusionAngle = atan2(heightDiff, currentDist * 100.0);
                        float falloff = pow(saturate(1.0 - currentDist / _AORadius), _AODistanceFalloff);
                        maxOcclusionAngle = max(maxOcclusionAngle, occlusionAngle * falloff);
                    }
                    totalOcclusion += maxOcclusionAngle;
                }
                return saturate(1.0 - (totalOcclusion / _AODirections));
            }

            float2 GenerateCurvatureMap(float2 uv)
            {
                float centerHeight = GetHeightFromUV(uv);
                float2 texel = _BaseMap_TexelSize.xy * _CurvatureKernelScale;
                float h_x1 = GetHeightFromUV(uv + float2(texel.x, 0));
                float h_x2 = GetHeightFromUV(uv - float2(texel.x, 0));
                float h_y1 = GetHeightFromUV(uv + float2(0, texel.y));
                float h_y2 = GetHeightFromUV(uv - float2(0, texel.y));

                float ddx = h_x2 - 2.0 * centerHeight + h_x1;
                float ddy = h_y2 - 2.0 * centerHeight + h_y1;
                float curvature = (ddx + ddy) * _CurvatureStrength * 0.5;

                float convex = saturate(curvature + _CurvatureBias);
                float concave = saturate(-curvature + _CurvatureBias);
                return float2(convex, concave);
            }

            float GenerateEdgeValue(float2 uv)
            {
                float2 texel = _BaseMap_TexelSize.xy;
                float h_l = GetHeightFromUV(uv + texel * float2(-1, 1));
                float h_c = GetHeightFromUV(uv + texel * float2(0, 1));
                float h_r = GetHeightFromUV(uv + texel * float2(1, 1));
                float v_l = GetHeightFromUV(uv + texel * float2(-1, 0));
                float v_r = GetHeightFromUV(uv + texel * float2(1, 0));
                float b_l = GetHeightFromUV(uv + texel * float2(-1, -1));
                float b_c = GetHeightFromUV(uv + texel * float2(0, -1));
                float b_r = GetHeightFromUV(uv + texel * float2(1, -1));

                float Gx = (h_r + 2.0 * v_r + b_r) - (h_l + 2.0 * v_l + b_l);
                float Gy = (h_l + 2.0 * h_c + h_r) - (b_l + 2.0 * b_c + b_r);
                return length(float2(Gx, Gy));
            }

            // --- SECTION 3: POST-PROCESSING & DENOISING ---

            float DenoiseAmbientOcclusion(float2 uv, float rawAO)
            {
                #if ENABLE_DENOISING
                    float totalWeight = 1.0;
                    float filteredValue = rawAO;
                    float centerHeight = GetHeightFromUV(uv);

                    [loop]
                    for (int x = -_DenoiseRadius; x <= _DenoiseRadius; ++x)
                    {
                        [loop]
                        for (int y = -_DenoiseRadius; y <= _DenoiseRadius; ++y)
                        {
                            if (x == 0 && y == 0) continue;

                            float2 offset = float2(x, y) * _BaseMap_TexelSize.xy;
                            float2 sampleUV = uv + offset;

                            float sampleHeight = GetHeightFromUV(sampleUV);
                            float heightDiff = abs(sampleHeight - centerHeight);
                            float depthWeight = Gaussian(heightDiff, _DenoiseDepthSensitivity);
                            float spatialWeight = Gaussian(length(offset), _DenoiseSpatialSigma * 0.01);
                            float weight = depthWeight * spatialWeight;

                            filteredValue += GenerateAmbientOcclusion(sampleUV) * weight;
                            totalWeight += weight;
                        }
                    }
                    return filteredValue / totalWeight;
                #else
                    return rawAO;
                #endif
            }

            float2 DenoiseCurvatureMap(float2 uv, float2 rawCurvature)
            {
                #if ENABLE_DENOISING
                    float totalWeight = 1.0;
                    float2 filteredValue = rawCurvature;
                    float centerHeight = GetHeightFromUV(uv);

                    [loop]
                    for (int x = -_DenoiseRadius; x <= _DenoiseRadius; ++x)
                    {
                        [loop]
                        for (int y = -_DenoiseRadius; y <= _DenoiseRadius; ++y)
                        {
                            if (x == 0 && y == 0) continue;

                            float2 offset = float2(x, y) * _BaseMap_TexelSize.xy;
                            float2 sampleUV = uv + offset;

                            float sampleHeight = GetHeightFromUV(sampleUV);
                            float heightDiff = abs(sampleHeight - centerHeight);
                            float depthWeight = Gaussian(heightDiff, _DenoiseDepthSensitivity);
                            float spatialWeight = Gaussian(length(offset), _DenoiseSpatialSigma * 0.01);
                            float weight = depthWeight * spatialWeight;

                            filteredValue += GenerateCurvatureMap(sampleUV) * weight;
                            totalWeight += weight;
                        }
                    }
                    return filteredValue / totalWeight;
                #else
                    return rawCurvature;
                #endif
            }

            // --- SECTION 4: VERTEX & FRAGMENT SHADERS ---

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float2 warpedUV = GetWarpedUVs(IN.uv);
                float4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, warpedUV);
                float centerHeight = GetHeightFromUV(warpedUV);

                float3 normalMap = GenerateNormalMap(warpedUV, _NormalStrength);
                float rawAO = GenerateAmbientOcclusion(warpedUV);
                float2 rawCurvature = GenerateCurvatureMap(warpedUV);

                float denoisedAO = DenoiseAmbientOcclusion(warpedUV, rawAO);
                float2 denoisedCurvature = DenoiseCurvatureMap(warpedUV, rawCurvature);

                #if _OUTPUT_MODE_SELECT_DEBUG
                    float debugMode = _DebugView_Core;
                    if (_DebugCategory == 1) debugMode = _DebugView_Masks;
                    if (_DebugCategory == 2) debugMode = _DebugView_Advanced;

                    if (debugMode == 1) return baseColor;
                    if (debugMode == 2) return float4(centerHeight.xxx, 1.0);
                    if (debugMode == 3) return float4(normalMap * 0.5 + 0.5, 1.0);
                    if (debugMode == 4) return float4(rawAO.xxx, 1.0);
                    if (debugMode == 5) return float4(rawCurvature.y, rawCurvature.x, 0, 1.0);
                    if (debugMode == 6) return float4(saturate(GenerateEdgeValue(warpedUV)).xxx, 1.0);
                    if (debugMode == 10) return float4(denoisedAO.xxx, 1.0);
                    if (debugMode == 11) return float4(denoisedCurvature.y, denoisedCurvature.x, 0, 1.0);
                #endif

                float exposure = saturate(denoisedAO + _WearExposureInfluence);
                float wearNoise = GetNoise(warpedUV * _WearNoiseTiling).r * _WearNoiseInfluence;
                float wearMask = saturate(denoisedCurvature.x * exposure * _WearLevel) * wearNoise;

                float upwardFacing = saturate(dot(normalMap, float3(0, 1, 0)));
                float dirtNoise = GetNoise(warpedUV * _DirtNoiseTiling).g * _DirtNoiseInfluence;
                float dirtMask = saturate(denoisedCurvature.y * (1.0 + upwardFacing * _DirtGravityInfluence) * _DirtLevel) * dirtNoise;

                float worldEffectMask = 0.0;
                #if ENABLE_WORLD_EFFECTS
                    float worldUp = saturate(dot(normalize(IN.normalWS), float3(0, 1, 0)));
                    float worldNoise = GetNoise(IN.positionWS.xz * _WorldEffectProjectionScale).b;
                    worldEffectMask = pow(saturate(worldUp - (1.0 - _WorldEffectAngleThreshold)), _WorldEffectHardness) * _WorldEffectIntensity * worldNoise;
                #endif

                #if _OUTPUT_MODE_SELECT_DEBUG
                    if (_DebugCategory == 1)
                    {
                        if (debugMode == 7) return float4(wearMask.xxx, 1.0);
                        if (debugMode == 8) return float4(dirtMask.xxx, 1.0);
                        if (debugMode == 9) return float4(worldEffectMask.xxx, 1.0);
                    }
                #endif

                float4 finalColor = baseColor;
                float finalMetallic = 0.0;
                float finalSmoothness = 0.0;

                finalColor.rgb = lerp(finalColor.rgb, _WearColor.rgb, wearMask);
                finalMetallic = lerp(finalMetallic, _WearMetallic, wearMask);
                finalSmoothness = lerp(finalSmoothness, _WearSmoothness, wearMask);

                finalColor.rgb = lerp(finalColor.rgb, _DirtColor.rgb, dirtMask);
                finalMetallic = lerp(finalMetallic, _DirtMetallic, dirtMask);
                finalSmoothness = lerp(finalSmoothness, _DirtSmoothness, dirtMask);
                
                #if ENABLE_WORLD_EFFECTS
                    finalColor.rgb = lerp(finalColor.rgb, _WorldEffectColor.rgb, worldEffectMask);
                    finalMetallic = lerp(finalMetallic, _WorldEffectMetallic, worldEffectMask);
                    finalSmoothness = lerp(finalSmoothness, _WorldEffectSmoothness, worldEffectMask);
                #endif

                finalColor.rgb *= denoisedAO;

                float edgeValue = GenerateEdgeValue(warpedUV);
                float edgeMask = smoothstep(_EdgeThreshold - _EdgeSoftness, _EdgeThreshold + _EdgeSoftness, edgeValue);
                finalColor.rgb = lerp(finalColor.rgb, _OutlineColor.rgb, edgeMask * _OutlineColor.a);

                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}