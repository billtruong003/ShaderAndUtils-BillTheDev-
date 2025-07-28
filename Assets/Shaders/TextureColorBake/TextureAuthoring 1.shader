Shader "Creative/TextureAuthoringTool_Optimized_V5_Production"
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
        [IntRange] _AOSamples("AO Samples (Optimized)", Range(4, 32)) = 16

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

        [Header(PostProcessing and Denoising)]
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

            #define PI 3.14159265359
            #define GOLDEN_RATIO_CONJUGATE 0.61803398875

            // --- DATA STRUCTURES ---
            struct CurvatureInfo { float convex; float concave; };
            struct SurfaceGeometry { float height; float3 normal; float ambientOcclusion; CurvatureInfo curvature; };
            struct MaterialProperties { float4 albedo; float metallic; float smoothness; };
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; float3 normalWS : NORMAL; };

            // --- UNIFORM VARIABLES ---
            TEXTURE2D(_BaseMap);    SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NoiseMap);   SAMPLER(sampler_NoiseMap);
            TEXTURE2D(_WarpMap);    SAMPLER(sampler_WarpMap);
            float4 _BaseMap_TexelSize;

            float _DebugCategory, _DebugView_Core, _DebugView_Masks, _DebugView_Advanced;
            float _HeightContrast, _HeightBrightness, _HeightCurve, _NormalStrength;
            float _AORadius, _AOIntensity, _AODistanceFalloff; int _AOSamples;
            float _CurvatureKernelScale, _CurvatureStrength, _CurvatureBias;
            float _EdgeThreshold, _EdgeSoftness; float4 _OutlineColor;
            float _WearLevel, _WearExposureInfluence, _WearNoiseTiling, _WearNoiseInfluence;
            float4 _WearColor; float _WearMetallic, _WearSmoothness;
            float _DirtLevel, _DirtGravityInfluence, _DirtNoiseTiling, _DirtNoiseInfluence;
            float4 _DirtColor; float _DirtMetallic, _DirtSmoothness;
            float _WarpStrength, _WarpTiling;
            float4 _WorldEffectColor; float _WorldEffectMetallic, _WorldEffectSmoothness;
            float _WorldEffectProjectionScale, _WorldEffectIntensity, _WorldEffectAngleThreshold, _WorldEffectHardness;
            int _DenoiseRadius; float _DenoiseDepthSensitivity, _DenoiseSpatialSigma;

            // --- CORE SAMPLING AND UTILITIES ---
            float2 GetWarpedUVs(float2 uv)
            {
                #if ENABLE_UV_WARP
                    float2 warpVector = (SAMPLE_TEXTURE2D(_WarpMap, sampler_WarpMap, uv * _WarpTiling).rg * 2.0 - 1.0) * _WarpStrength;
                    return uv + warpVector;
                #else
                    return uv;
                #endif
            }

            float SampleHeightValue(float2 uv)
            {
                float luminance = dot(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb, float3(0.299, 0.587, 0.114));
                float adjustedLuminance = pow(abs(luminance), _HeightCurve) * _HeightContrast + _HeightBrightness;
                return saturate(adjustedLuminance);
            }
            
            float3 SampleNoise(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, uv).rgb;
            }

            float Gaussian(float x, float sigma)
            {
                return exp(-(x * x) / (2.0 * sigma * sigma));
            }

            // --- GEOMETRIC CALCULATION ---
            float3 CalculateNormalFromHeight(float2 uv, float strength)
            {
                float2 texel = _BaseMap_TexelSize.xy;
                float h_l = SampleHeightValue(uv - float2(texel.x, 0));
                float h_r = SampleHeightValue(uv + float2(texel.x, 0));
                float h_b = SampleHeightValue(uv - float2(0, texel.y));
                float h_t = SampleHeightValue(uv + float2(0, texel.y));
                return normalize(float3(-(h_r - h_l) * strength, -(h_t - h_b) * strength, 1.0));
            }

            float CalculateOptimizedAmbientOcclusion(float2 uv)
            {
                float centerHeight = SampleHeightValue(uv);
                float totalOcclusion = 0.0;
                float angleStep = 2.0 * PI * GOLDEN_RATIO_CONJUGATE;

                [loop]
                for (int i = 0; i < _AOSamples; ++i)
                {
                    float sampleAngle = i * angleStep;
                    float sampleRadius = (float(i) + 0.5) / float(_AOSamples); // Uniform distribution
                    sampleRadius = sqrt(sampleRadius); // Skew towards center for better coverage
                    
                    float2 sampleDir = float2(cos(sampleAngle), sin(sampleAngle));
                    float2 sampleUV = uv + sampleDir * sampleRadius * _AORadius;

                    float sampleHeight = SampleHeightValue(sampleUV);
                    float heightDiff = (sampleHeight - centerHeight) * _AOIntensity * 100.0;
                    
                    float distFactor = length(sampleUV - uv) * 100.0;
                    float horizonAngle = atan2(heightDiff, distFactor);
                    
                    float falloff = pow(saturate(1.0 - length(uv - sampleUV) / _AORadius), _AODistanceFalloff);
                    totalOcclusion += max(0.0, horizonAngle * falloff);
                }
                return saturate(1.0 - (totalOcclusion * 2.0 / _AOSamples));
            }

            CurvatureInfo CalculateCurvature(float2 uv)
            {
                float centerHeight = SampleHeightValue(uv);
                float2 texel = _BaseMap_TexelSize.xy * _CurvatureKernelScale;
                float h_x1 = SampleHeightValue(uv + float2(texel.x, 0));
                float h_x2 = SampleHeightValue(uv - float2(texel.x, 0));
                float h_y1 = SampleHeightValue(uv + float2(0, texel.y));
                float h_y2 = SampleHeightValue(uv - float2(0, texel.y));

                float ddx = h_x2 - 2.0 * centerHeight + h_x1;
                float ddy = h_y2 - 2.0 * centerHeight + h_y1;
                float curvature = (ddx + ddy) * _CurvatureStrength * 0.5;

                CurvatureInfo info;
                info.convex = saturate(curvature + _CurvatureBias);
                info.concave = saturate(-curvature + _CurvatureBias);
                return info;
            }
            
            SurfaceGeometry CalculateRawSurfaceGeometry(float2 uv)
            {
                SurfaceGeometry geo;
                geo.height = SampleHeightValue(uv);
                geo.normal = CalculateNormalFromHeight(uv, _NormalStrength);
                geo.ambientOcclusion = CalculateOptimizedAmbientOcclusion(uv);
                geo.curvature = CalculateCurvature(uv);
                return geo;
            }

            // --- DENOISING ---
            SurfaceGeometry DenoiseSurfaceGeometry(float2 uv, SurfaceGeometry rawGeo)
            {
                #if ENABLE_DENOISING
                    float totalWeight = 1.0;
                    float filteredAO = rawGeo.ambientOcclusion;
                    float2 filteredCurvature = float2(rawGeo.curvature.convex, rawGeo.curvature.concave);

                    [loop]
                    for (int x = -_DenoiseRadius; x <= _DenoiseRadius; ++x)
                    {
                        [loop]
                        for (int y = -_DenoiseRadius; y <= _DenoiseRadius; ++y)
                        {
                            if (x == 0 && y == 0) continue;

                            float2 offset = float2(x, y) * _BaseMap_TexelSize.xy;
                            float2 sampleUV = uv + offset;

                            float sampleHeight = SampleHeightValue(sampleUV);
                            float heightDiff = abs(sampleHeight - rawGeo.height);
                            float depthWeight = Gaussian(heightDiff, _DenoiseDepthSensitivity);
                            float spatialWeight = Gaussian(length(offset), _DenoiseSpatialSigma * 0.01);
                            float weight = depthWeight * spatialWeight;
                            
                            // NOTE: This on-the-fly recalculation is extremely slow.
                            // A multi-pass approach is highly recommended for production.
                            filteredAO += CalculateOptimizedAmbientOcclusion(sampleUV) * weight;
                            CurvatureInfo sampleCurvature = CalculateCurvature(sampleUV);
                            filteredCurvature.x += sampleCurvature.convex * weight;
                            filteredCurvature.y += sampleCurvature.concave * weight;
                            
                            totalWeight += weight;
                        }
                    }
                    SurfaceGeometry denoisedGeo = rawGeo;
                    denoisedGeo.ambientOcclusion = filteredAO / totalWeight;
                    denoisedGeo.curvature.convex = filteredCurvature.x / totalWeight;
                    denoisedGeo.curvature.concave = filteredCurvature.y / totalWeight;
                    return denoisedGeo;
                #else
                    return rawGeo;
                #endif
            }

            // --- MASK GENERATION ---
            float CalculateWearMask(SurfaceGeometry geo, float2 uv)
            {
                float exposure = saturate(geo.ambientOcclusion + _WearExposureInfluence);
                float noise = SampleNoise(uv * _WearNoiseTiling).r * _WearNoiseInfluence;
                return saturate(geo.curvature.convex * exposure * _WearLevel) * noise;
            }

            float CalculateDirtMask(SurfaceGeometry geo, float2 uv)
            {
                float upwardFacing = saturate(dot(geo.normal, float3(0, 1, 0)));
                float noise = SampleNoise(uv * _DirtNoiseTiling).g * _DirtNoiseInfluence;
                return saturate(geo.curvature.concave * (1.0 + upwardFacing * _DirtGravityInfluence) * _DirtLevel) * noise;
            }
            
            float CalculateWorldEffectMask(Varyings IN)
            {
                #if ENABLE_WORLD_EFFECTS
                    float worldUp = saturate(dot(normalize(IN.normalWS), float3(0, 1, 0)));
                    float worldNoise = SampleNoise(IN.positionWS.xz * _WorldEffectProjectionScale).b;
                    return pow(saturate(worldUp - (1.0 - _WorldEffectAngleThreshold)), _WorldEffectHardness) * _WorldEffectIntensity * worldNoise;
                #else
                    return 0.0;
                #endif
            }

            float CalculateEdgeMask(float2 uv)
            {
                float2 texel = _BaseMap_TexelSize.xy;
                float h_l = SampleHeightValue(uv + texel * float2(-1, 1)); float h_c = SampleHeightValue(uv + texel * float2(0, 1)); float h_r = SampleHeightValue(uv + texel * float2(1, 1));
                float v_l = SampleHeightValue(uv + texel * float2(-1, 0));                                                          float v_r = SampleHeightValue(uv + texel * float2(1, 0));
                float b_l = SampleHeightValue(uv + texel * float2(-1, -1)); float b_c = SampleHeightValue(uv + texel * float2(0, -1)); float b_r = SampleHeightValue(uv + texel * float2(1, -1));

                float Gx = (h_r + 2.0 * v_r + b_r) - (h_l + 2.0 * v_l + b_l);
                float Gy = (h_l + 2.0 * h_c + h_r) - (b_l + 2.0 * b_c + b_r);
                float edgeValue = length(float2(Gx, Gy));
                return smoothstep(_EdgeThreshold - _EdgeSoftness, _EdgeThreshold + _EdgeSoftness, edgeValue);
            }

            // --- COMPOSITION & DEBUG ---
            MaterialProperties CreateBaseMaterial(float4 baseColor)
            {
                MaterialProperties mat;
                mat.albedo = baseColor;
                mat.metallic = 0.0;
                mat.smoothness = 0.5; // A sensible default
                return mat;
            }
            
            MaterialProperties LayerMaterial(MaterialProperties base, MaterialProperties layer, float mask)
            {
                MaterialProperties result;
                result.albedo.rgb = lerp(base.albedo.rgb, layer.albedo.rgb, mask);
                result.metallic = lerp(base.metallic, layer.metallic, mask);
                result.smoothness = lerp(base.smoothness, layer.smoothness, mask);
                result.albedo.a = base.albedo.a;
                return result;
            }

            float4 HandleDebugOutput(float4 baseColor, SurfaceGeometry rawGeo, SurfaceGeometry denoisedGeo, float wearMask, float dirtMask, float worldMask, float2 uv)
            {
                float debugMode = _DebugView_Core;
                if (_DebugCategory == 1) debugMode = _DebugView_Masks;
                if (_DebugCategory == 2) debugMode = _DebugView_Advanced;
                
                if (debugMode == 1) return baseColor;
                if (debugMode == 2) return float4(rawGeo.height.xxx, 1.0);
                if (debugMode == 3) return float4(rawGeo.normal * 0.5 + 0.5, 1.0);
                if (debugMode == 4) return float4(rawGeo.ambientOcclusion.xxx, 1.0);
                if (debugMode == 5) return float4(rawGeo.curvature.concave, rawGeo.curvature.convex, 0, 1.0);
                if (debugMode == 6) return float4(saturate(CalculateEdgeMask(uv)).xxx, 1.0);
                if (debugMode == 7) return float4(wearMask.xxx, 1.0);
                if (debugMode == 8) return float4(dirtMask.xxx, 1.0);
                if (debugMode == 9) return float4(worldMask.xxx, 1.0);
                if (debugMode == 10) return float4(denoisedGeo.ambientOcclusion.xxx, 1.0);
                if (debugMode == 11) return float4(denoisedGeo.curvature.concave, denoisedGeo.curvature.convex, 0, 1.0);
                
                return float4(1,0,1,1); // Magenta for error
            }

            // --- MAIN SHADER STAGES ---
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
                
                SurfaceGeometry rawGeo = CalculateRawSurfaceGeometry(warpedUV);
                SurfaceGeometry finalGeo = DenoiseSurfaceGeometry(warpedUV, rawGeo);

                float wearMask = CalculateWearMask(finalGeo, warpedUV);
                float dirtMask = CalculateDirtMask(finalGeo, warpedUV);
                float worldEffectMask = CalculateWorldEffectMask(IN);

                #if _OUTPUT_MODE_SELECT_DEBUG
                    float4 baseColorForDebug = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, warpedUV);
                    return HandleDebugOutput(baseColorForDebug, rawGeo, finalGeo, wearMask, dirtMask, worldEffectMask, warpedUV);
                #endif

                MaterialProperties finalMaterial = CreateBaseMaterial(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, warpedUV));
                
                MaterialProperties wearLayer = {_WearColor, _WearMetallic, _WearSmoothness};
                finalMaterial = LayerMaterial(finalMaterial, wearLayer, wearMask);

                MaterialProperties dirtLayer = {_DirtColor, _DirtMetallic, _DirtSmoothness};
                finalMaterial = LayerMaterial(finalMaterial, dirtLayer, dirtMask);
                
                #if ENABLE_WORLD_EFFECTS
                    MaterialProperties worldLayer = {_WorldEffectColor, _WorldEffectMetallic, _WorldEffectSmoothness};
                    finalMaterial = LayerMaterial(finalMaterial, worldLayer, worldEffectMask);
                #endif

                finalMaterial.albedo.rgb *= finalGeo.ambientOcclusion;

                float edgeMask = CalculateEdgeMask(warpedUV);
                finalMaterial.albedo.rgb = lerp(finalMaterial.albedo.rgb, _OutlineColor.rgb, edgeMask * _OutlineColor.a);

                return finalMaterial.albedo;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}