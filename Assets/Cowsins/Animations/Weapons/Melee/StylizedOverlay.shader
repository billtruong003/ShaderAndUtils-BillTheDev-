Shader "BillTheDev/StylizedOverlayV3_Hybrid"
{
    Properties
    {
        [Header(General Overlay Settings)]
        [KeywordEnum(Snow, Ice, Crystal)] _OverlayType("Overlay Type", Float) = 0
        _OverlayDirection("Overlay Direction (World Space)", Vector) = (0, 1, 0, 0)
        _TransitionProgress("Transition Progress", Range(-1, 1)) = 0.5
        _TransitionHardness("Transition Hardness", Range(1, 256)) = 50
        
        [Header(Transition Noise (Triplanar))]
        _TransitionNoiseMap("Transition Noise (R)", 2D) = "white" {}
        _TransitionNoiseScale("Noise Scale", Float) = 5.0
        _TransitionNoiseStrength("Noise Strength", Range(0, 1)) = 0.2
        _TriplanarFalloff("Triplanar Blend Sharpness", Range(1, 10)) = 4

        [Header(Displacement and Surface Detail)]
        [Toggle(_VERTEX_DISPLACEMENT_ON)] _EnableVertexDisplacement("Enable Vertex Displacement", Float) = 0
        _DisplacementStrength("Vertex Displacement Strength", Range(0, 0.5)) = 0.05
        
        [Toggle(_NORMAL_MAP_ON)] _EnableNormalMap("Enable Normal Map (UV)", Float) = 0
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Strength", Range(0, 2)) = 1.0

        [Toggle(_PARALLAX_OCCLUSION_ON)] _EnablePOM("Enable Parallax Occlusion (UV)", Float) = 0
        _POMHeightMap("POM Height Map (R)", 2D) = "gray" {}
        _POMDepth("POM Depth", Range(0.005, 0.1)) = 0.02
        _POMLayers("POM Layers", Range(8, 64)) = 16

        [Header(Snow Material)]
        _SnowBaseColor("Base Color", Color) = (0.8, 0.8, 1, 1)
        _SnowDeepColor("Subsurface Color", Color) = (0.4, 0.5, 0.9, 1)
        _SnowSubsurfaceFactor("Subsurface Power", Range(0, 8)) = 2.0
        _SnowToonRamp("Toon Ramp", 2D) = "gray" {}
        _SnowRimColor("Rim Color", Color) = (0.5, 0.8, 1, 1)
        _SnowRimPower("Rim Power", Range(0.1, 8)) = 3.0
        [HDR] _SnowGlitterColor("Glitter Color", Color) = (1.5, 1.5, 2.0, 1)
        _SnowGlitterScale("Glitter Scale", Float) = 35.0
        _SnowGlitterDensity("Glitter Density", Range(0.9, 0.999)) = 0.99
        _SnowGlitterHardness("Glitter Hardness", Range(1, 1024)) = 512

        [Header(Ice and Crystal Shared Settings)]
        _IceBaseColor("Base Color", Color) = (0.7, 0.8, 1, 0.5)
        _IceInternalFogColor("Internal Fog Color", Color) = (0.1, 0.2, 0.4, 1)
        _IceInternalFogDensity("Internal Fog Density", Range(0, 10)) = 3.0
        [Toggle(_REFRACTION_ON)] _EnableRefraction("Enable Edge Refraction", Float) = 0
        _EdgeColor("Edge Color", Color) = (0.5, 0.9, 1, 1)
        _EdgeWidth("Edge Width", Range(0.01, 1.0)) = 0.15
        _EdgePulseSpeed("Edge Pulse Speed", Range(0, 20)) = 5.0
        _EdgePulseStrength("Edge Pulse Strength", Range(0, 1)) = 0.5
        _EdgeSpecularColor("Specular Color", Color) = (1,1,1,1)
        _EdgeSpecularPower("Specular Power", Range(1, 256)) = 64
        _EdgeRefractionStrength("Refraction Strength", Range(0, 0.1)) = 0.02
        _ChromaticAberration("Chromatic Aberration", Range(0, 0.01)) = 0.005
        
        [Header(Crystal Specific Voronoi)]
        _CrystalCellColor("Cell Color", Color) = (0.1, 0.2, 0.3, 1)
        _CrystalBorderColor("Cell Border Color", Color) = (0.9, 0.9, 1.0, 1)
        _CrystalCellScale("Cell Scale", Float) = 5.0
        _CrystalCellBorderWidth("Cell Border Width", Range(0.01, 0.5)) = 0.1
        _CrystalCellJitter("Cell Jitter", Range(0, 2)) = 1.0
        
        [Header(Shared Bling Effect Simplex)]
        [HDR] _BlingColor("Bling Color", Color) = (1, 1.2, 1.5, 1)
        _BlingScale("Bling Scale", Float) = 15.0
        _BlingDensity("Bling Density Threshold", Range(-1, 1)) = 0.95
        _BlingHardness("Bling Hardness", Range(1, 512)) = 256
        _BlingSpeed("Bling Speed", Float) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalRenderPipeline" "DisableBatching" = "True" }
        
        Pass
        {
            Name "OverlayPass"
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #pragma shader_feature_local_fragment _OVERLAY_TYPE_SNOW _OVERLAY_TYPE_ICE _OVERLAY_TYPE_CRYSTAL
            #pragma shader_feature_local _VERTEX_DISPLACEMENT_ON
            #pragma shader_feature_local_fragment _NORMAL_MAP_ON
            #pragma shader_feature_local_fragment _PARALLAX_OCCLUSION_ON
            #pragma shader_feature_local_fragment _REFRACTION_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Includes/SimplexNoise.hlsl"
            #include "Includes/VoronoiNoise.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float4 screenPos    : TEXCOORD2;
                float3 positionOS   : TEXCOORD3;
                float2 uv           : TEXCOORD4;
                float3x3 TBN        : TEXCOORD5;
            };
            
            TEXTURE2D(_TransitionNoiseMap); SAMPLER(sampler_TransitionNoiseMap);
            TEXTURE2D(_SnowToonRamp);       SAMPLER(sampler_SnowToonRamp);
            TEXTURE2D(_BumpMap);            SAMPLER(sampler_BumpMap);
            TEXTURE2D(_POMHeightMap);       SAMPLER(sampler_POMHeightMap);
            
            #if _REFRACTION_ON
                TEXTURE2D(_CameraOpaqueTexture); SAMPLER(sampler_CameraOpaqueTexture);
            #endif

            CBUFFER_START(UnityPerMaterial)
            float4 _OverlayDirection; float _TransitionProgress; float _TransitionHardness;
            float _TransitionNoiseScale; float _TransitionNoiseStrength; float _TriplanarFalloff;
            float _DisplacementStrength; float _BumpScale; float _POMDepth; float _POMLayers;
            float4 _SnowBaseColor; float4 _SnowDeepColor; float _SnowSubsurfaceFactor; float4 _SnowRimColor; float _SnowRimPower;
            float4 _SnowGlitterColor; float _SnowGlitterScale; float _SnowGlitterDensity; float _SnowGlitterHardness;
            float4 _IceBaseColor; float4 _IceInternalFogColor; float _IceInternalFogDensity;
            float4 _EdgeColor; float _EdgeWidth; float _EdgePulseSpeed; float _EdgePulseStrength;
            float4 _EdgeSpecularColor; float _EdgeSpecularPower; float _EdgeRefractionStrength; float _ChromaticAberration;
            float4 _BlingColor; float _BlingScale; float _BlingDensity; float _BlingHardness; float _BlingSpeed;
            float4 _CrystalCellColor; float4 _CrystalBorderColor; float _CrystalCellScale; float _CrystalCellBorderWidth; float _CrystalCellJitter;
            CBUFFER_END

            float SampleTextureTriplanar(TEXTURE2D_PARAM(tex, smp), float3 position, float3 normal, float scale)
            {
                float3 blendWeights = pow(abs(normal), _TriplanarFalloff);
                blendWeights /= dot(blendWeights, 1.0);
                float2 uvX = position.zy * scale;
                float2 uvY = position.xz * scale;
                float2 uvZ = position.xy * scale;
                float colX = SAMPLE_TEXTURE2D_LOD(tex, smp, uvX, 0).r;
                float colY = SAMPLE_TEXTURE2D_LOD(tex, smp, uvY, 0).r;
                float colZ = SAMPLE_TEXTURE2D_LOD(tex, smp, uvZ, 0).r;
                return colX * blendWeights.x + colY * blendWeights.y + colZ * blendWeights.z;
            }

            float2 CalculateParallaxUV(float3 viewDirTS, float2 uv)
            {
                float numLayers = _POMLayers;
                float layerDepth = 1.0 / numLayers;
                float currentLayerDepth = 0.0;
                float2 P = viewDirTS.xy * _POMDepth;
                float2 deltaUV = P / numLayers;
                float2 currentUV = uv;
                for (int i = 0; i < numLayers; i++)
                {
                    float heightAtCurrentUV = SAMPLE_TEXTURE2D_LOD(_POMHeightMap, sampler_POMHeightMap, currentUV, 0).r;
                    if (heightAtCurrentUV > currentLayerDepth)
                    {
                        currentUV -= deltaUV;
                        currentLayerDepth -= layerDepth;
                    }
                    currentUV += deltaUV;
                    currentLayerDepth += layerDepth;
                }
                float2 prevUV = currentUV - deltaUV;
                float nextDepth = SAMPLE_TEXTURE2D_LOD(_POMHeightMap, sampler_POMHeightMap, currentUV, 0).r - currentLayerDepth + layerDepth;
                float prevDepth = SAMPLE_TEXTURE2D_LOD(_POMHeightMap, sampler_POMHeightMap, prevUV, 0).r - currentLayerDepth;
                float weight = nextDepth / (nextDepth - prevDepth);
                return lerp(currentUV, prevUV, weight);
            }
            
            float CalculateOverlayMask(float3 positionWS, float3 normalWS)
            {
                float dotProduct = dot(normalWS, normalize(_OverlayDirection.xyz));
                float heightBias = lerp(1.0, -1.0, _TransitionProgress * 0.5 + 0.5);
                float noiseValue = SampleTextureTriplanar(TEXTURE2D_ARGS(_TransitionNoiseMap, sampler_TransitionNoiseMap), positionWS, normalWS, _TransitionNoiseScale);
                float noisyThreshold = heightBias - (noiseValue - 0.5) * 2.0 * _TransitionNoiseStrength;
                return saturate((dotProduct - noisyThreshold) * _TransitionHardness);
            }

            Varyings Vertex(Attributes IN)
            {
                Varyings OUT;
                OUT.positionOS = IN.positionOS.xyz;
                OUT.uv = IN.uv;
                
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

                #if defined(_VERTEX_DISPLACEMENT_ON)
                    float overlayMaskForDisplacement = CalculateOverlayMask(positionWS, normalWS);
                    float displacementHeight = SampleTextureTriplanar(TEXTURE2D_ARGS(_TransitionNoiseMap, sampler_TransitionNoiseMap), positionWS, normalWS, _TransitionNoiseScale);
                    float displacementAmount = displacementHeight * overlayMaskForDisplacement * _DisplacementStrength;
                    positionWS += normalWS * displacementAmount;
                #endif
                
                OUT.positionWS = positionWS;
                OUT.normalWS = normalWS;
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);

                float3 tangentWS = TransformObjectToWorldDir(IN.tangentOS.xyz);
                float3 bitangentWS = cross(OUT.normalWS, tangentWS) * IN.tangentOS.w;
                OUT.TBN = float3x3(tangentWS, bitangentWS, OUT.normalWS);
                
                return OUT;
            }

            half4 Fragment(Varyings IN) : SV_Target
            {
                float overlayMask = CalculateOverlayMask(IN.positionWS, IN.normalWS);
                clip(overlayMask - 0.001);

                float3 viewDirectionWS = SafeNormalize(GetCameraPositionWS() - IN.positionWS);
                float3 viewDirTS = mul(transpose(IN.TBN), viewDirectionWS);
                float2 uv = IN.uv;

                #if defined(_PARALLAX_OCCLUSION_ON)
                    uv = CalculateParallaxUV(viewDirTS, IN.uv);
                #endif
                
                float3 normalWS = IN.normalWS;
                #if defined(_NORMAL_MAP_ON)
                    float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);
                    normalWS = mul(IN.TBN, normalTS);
                #endif
                normalWS = normalize(normalWS);
                
                Light mainLight = GetMainLight();
                float3 ambientLight = SampleSH(normalWS);
                float3 finalColor = 0, finalEmission = 0;
                float finalAlpha = 0;
                
                #if defined(_OVERLAY_TYPE_SNOW)
                    float rimFactor = pow(1.0 - saturate(dot(viewDirectionWS, normalWS)), _SnowRimPower);
                    float lightDot = saturate(dot(normalWS, mainLight.direction));
                    float ndotlWrapped = lightDot * 0.5 + 0.5;
                    float3 ramp = SAMPLE_TEXTURE2D_LOD(_SnowToonRamp, sampler_SnowToonRamp, float2(ndotlWrapped, 0.5), 0).rgb;
                    float SSS = pow(saturate(dot(viewDirectionWS, -mainLight.direction)), _SnowSubsurfaceFactor);
                    float3 SSSColor = _SnowDeepColor.rgb * SSS * mainLight.color;
                    finalColor = _SnowBaseColor.rgb * (ambientLight + mainLight.color * ramp) + SSSColor;

                    float3 glitterNoisePos = IN.positionWS * _SnowGlitterScale;
                    float glitterNoise = snoise(glitterNoisePos);
                    float glitter = pow(saturate(glitterNoise), _SnowGlitterHardness) * step(_SnowGlitterDensity, glitterNoise);
                    float3 rimEmission = _SnowRimColor.rgb * rimFactor;
                    float3 glitterEmission = glitter * _SnowGlitterColor.rgb * _SnowGlitterColor.a;
                    finalEmission = rimEmission + glitterEmission;
                    finalAlpha = _SnowBaseColor.a * overlayMask;

                #else // ICE & CRYSTAL
                    float3 baseColor = _IceBaseColor.rgb;
                    float baseAlpha = _IceBaseColor.a;
                    #if defined(_OVERLAY_TYPE_CRYSTAL)
                        float2 voronoi = WorleyNoise(IN.positionOS * _CrystalCellScale, _CrystalCellJitter, false);
                        float crystalCells = smoothstep(0.0, _CrystalCellBorderWidth, voronoi.y - voronoi.x);
                        baseColor = lerp(_CrystalBorderColor.rgb, _CrystalCellColor.rgb, crystalCells);
                        baseAlpha = lerp(_CrystalBorderColor.a, _CrystalCellColor.a, crystalCells);
                    #endif
                    
                    float edgeFactor = 1.0 - smoothstep(0.0, _EdgeWidth, overlayMask);
                    float internalFog = exp(-_IceInternalFogDensity * edgeFactor);
                    finalColor = lerp(_IceInternalFogColor.rgb, baseColor, internalFog);
                    
                    #if _REFRACTION_ON
                        float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                        float3 refrNoiseInput = float3(IN.positionWS * 2.0 + _Time.y);
                        float2 refrNoise = float2(snoise(refrNoiseInput), snoise(refrNoiseInput + 34.56));
                        float2 refrOffset = refrNoise * _EdgeRefractionStrength * edgeFactor;
                        float3 sceneColorR = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + refrOffset + float2(_ChromaticAberration, 0)).rgb;
                        float3 sceneColorG = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + refrOffset).rgb;
                        float3 sceneColorB = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + refrOffset - float2(_ChromaticAberration, 0)).rgb;
                        float3 sceneColor = float3(sceneColorR.r, sceneColorG.g, sceneColorB.b);
                        finalColor = lerp(finalColor, sceneColor, edgeFactor);
                    #endif
                    
                    finalAlpha = baseAlpha * overlayMask;

                    float pulseFactor = 1.0 - (_EdgePulseStrength * (0.5 * sin(_Time.y * _EdgePulseSpeed) + 0.5));
                    float3 edgeEmission = edgeFactor * pulseFactor * _EdgeColor.rgb;
                    float3 halfVec = SafeNormalize(mainLight.direction + viewDirectionWS);
                    float specDot = saturate(dot(normalWS, halfVec));
                    float specToon = pow(specDot, _EdgeSpecularPower);
                    float3 specularEmission = _EdgeSpecularColor.rgb * specToon * mainLight.color * (1.0 - edgeFactor);

                    float3 noisePos = IN.positionOS * _BlingScale + float3(0,0,_Time.y * _BlingSpeed);
                    float blingNoise = snoise(noisePos);
                    float bling = pow(saturate(blingNoise), _BlingHardness) * step(_BlingDensity, blingNoise);
                    float3 blingEmission = bling * _BlingColor.rgb * _BlingColor.a * (1.0 - edgeFactor);
                    finalEmission = edgeEmission + specularEmission + blingEmission;
                #endif

                return float4(finalColor + finalEmission, finalAlpha);
            }
            ENDHLSL
        }
    }
    CustomEditor "StylizedOverlayV3ShaderGUI"
    FallBack "Universal Render Pipeline/Transparent"
}