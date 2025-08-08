Shader "BillTheDev/Stylized Overlay"
{
    Properties
    {
        [Header(General Overlay Settings)]
        [KeywordEnum(Snow, Ice, Crystal)] _OverlayType("Overlay Type", Float) = 0
        _OverlayDirection("Overlay Direction (World Space)", Vector) = (0, 1, 0, 0)
        _TransitionProgress("Transition Progress", Range(-1, 1)) = 0.5
        _DisplacementHeight("Displacement Height", Range(0, 0.2)) = 0.05
        _TransitionHardness("Transition Hardness", Range(1, 100)) = 20

        [Header(Breakup Noise UV Based)]
        _NoiseMap("Breakup Noise (R)", 2D) = "white" {}
        _NoiseTiling("Noise Tiling", Float) = 1.0

        [Header(Snow Material)]
        _SnowBaseColor("Base Color", Color) = (0.8, 0.8, 1, 1)
        _SnowTopColor("Top Color", Color) = (1, 1, 1, 1)
        _SnowToonRamp("Toon Ramp", 2D) = "gray" {}
        _SnowRimColor("Rim Color", Color) = (0.5, 0.8, 1, 1)
        _SnowRimPower("Rim Power", Range(0.1, 8)) = 3.0
        
        [Header(Ice and Crystal Shared Settings)]
        _IceBaseColor("Ice Base Color", Color) = (0.7, 0.8, 1, 0.5)
        [Toggle(_REFRACTION_ON)] _EnableRefraction("Enable Edge Refraction", Float) = 0
        _EdgeColor("Edge Color", Color) = (0.5, 0.9, 1, 1)
        _EdgeWidth("Edge Width", Range(0.01, 1.0)) = 0.15
        _EdgePulseSpeed("Edge Pulse Speed", Range(0, 20)) = 5.0
        _EdgePulseStrength("Edge Pulse Strength", Range(0, 1)) = 0.5
        _EdgeSpecularColor("Specular Color", Color) = (1,1,1,1)
        _EdgeSpecularPower("Specular Power", Range(1, 128)) = 32
        _EdgeRefractionStrength("Refraction Strength", Range(0, 0.1)) = 0.02
        
        [Header(Crystal Specific Voronoi)]
        _CrystalCellColor("Cell Color", Color) = (0.1, 0.2, 0.3, 1)
        _CrystalCellScale("Cell Scale", Float) = 5.0
        _CrystalCellHardness("Cell Hardness", Range(0.01, 1.0)) = 0.1
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
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalRenderPipeline" }
        
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #pragma shader_feature_local_fragment _REFRACTION_ON
            #pragma multi_compile_local _OVERLAY_TYPE_SNOW _OVERLAY_TYPE_ICE _OVERLAY_TYPE_CRYSTAL

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Includes/SimplexNoise.hlsl"
            #include "Includes/VoronoiNoise.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float  overlayMask  : TEXCOORD2;
                float4 screenPos    : TEXCOORD3;
                float3 positionOS   : TEXCOORD4;
            };
            
            TEXTURE2D(_NoiseMap); SAMPLER(sampler_NoiseMap);
            TEXTURE2D(_SnowToonRamp); SAMPLER(sampler_SnowToonRamp);
            
            #if _REFRACTION_ON
                TEXTURE2D(_CameraOpaqueTexture); SAMPLER(sampler_CameraOpaqueTexture);
            #endif

            CBUFFER_START(UnityPerMaterial)
            float4 _OverlayDirection; float _TransitionProgress; float _DisplacementHeight; float _TransitionHardness;
            float _NoiseTiling;
            float4 _SnowBaseColor; float4 _SnowTopColor; float4 _SnowRimColor; float _SnowRimPower;
            float4 _IceBaseColor; float _EnableRefraction; float4 _EdgeColor; float _EdgeWidth;
            float _EdgePulseSpeed; float _EdgePulseStrength; float4 _EdgeSpecularColor; float _EdgeSpecularPower; float _EdgeRefractionStrength;
            float4 _BlingColor; float _BlingScale; float _BlingDensity; float _BlingHardness; float _BlingSpeed;
            float4 _CrystalCellColor; float _CrystalCellScale; float _CrystalCellHardness; float _CrystalCellJitter;
            CBUFFER_END
            
            Varyings Vertex(Attributes IN)
            {
                Varyings OUT;
                OUT.positionOS = IN.positionOS.xyz;
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                
                float dotProduct = dot(normalWS, normalize(_OverlayDirection.xyz));
                float heightBias = lerp(1.0, -1.0, _TransitionProgress * 0.5 + 0.5);
                float baseOverlayFactor = saturate((dotProduct - heightBias) * _TransitionHardness);
                
                float2 noiseUV = IN.uv * _NoiseTiling;
                float noiseValue = SAMPLE_TEXTURE2D_LOD(_NoiseMap, sampler_NoiseMap, noiseUV, 0).r;
                OUT.overlayMask = saturate(baseOverlayFactor * noiseValue);

                float displacement = _DisplacementHeight * OUT.overlayMask;
                
                #if defined(_OVERLAY_TYPE_CRYSTAL)
                    float2 voronoi = WorleyNoise(OUT.positionOS * _CrystalCellScale, _CrystalCellJitter, false);
                    float crystalPattern = smoothstep(0.0, _CrystalCellHardness, voronoi.y - voronoi.x);
                    displacement = _DisplacementHeight * crystalPattern * OUT.overlayMask;
                #endif
                
                float3 displacedPositionOS = IN.positionOS.xyz + IN.normalOS * displacement;

                OUT.positionWS = TransformObjectToWorld(displacedPositionOS);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = normalWS;
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                return OUT;
            }

            half4 Fragment(Varyings IN) : SV_Target
            {
                float overlayMask = IN.overlayMask;
                clip(overlayMask - 0.001);

                float3 normalWS = normalize(IN.normalWS);
                float3 viewDirectionWS = SafeNormalize(GetCameraPositionWS() - IN.positionWS);
                Light mainLight = GetMainLight();
                float3 ambientLight = SampleSH(normalWS);

                float3 finalColor = 0;
                float3 finalEmission = 0;
                float finalAlpha = 0;
                
                #if defined(_OVERLAY_TYPE_SNOW)
                    float rimFactor = pow(1.0 - saturate(dot(viewDirectionWS, normalWS)), _SnowRimPower);
                    float lightDot = saturate(dot(normalWS, mainLight.direction)) * 0.5 + 0.5;
                    float3 ramp = SAMPLE_TEXTURE2D_LOD(_SnowToonRamp, sampler_SnowToonRamp, float2(lightDot, 0.5), 0).rgb;
                    float3 albedo = lerp(_SnowBaseColor.rgb, _SnowTopColor.rgb, overlayMask);
                    finalColor = albedo * (ambientLight + mainLight.color * ramp);
                    finalEmission = _SnowRimColor.rgb * rimFactor;
                    finalAlpha = _SnowBaseColor.a * overlayMask;
                #else // ICE & CRYSTAL
                    // --- 1. DEFINE BASE COLOR & ALPHA ---
                    float3 baseColor = _IceBaseColor.rgb;
                    float baseAlpha = _IceBaseColor.a;

                    #if defined(_OVERLAY_TYPE_CRYSTAL)
                        float2 voronoi = WorleyNoise(IN.positionOS * _CrystalCellScale, _CrystalCellJitter, false);
                        float crystalPattern = smoothstep(0.0, _CrystalCellHardness, voronoi.y - voronoi.x);
                        baseColor = lerp(_CrystalCellColor.rgb, _IceBaseColor.rgb, crystalPattern);
                        baseAlpha = lerp(_CrystalCellColor.a, _IceBaseColor.a, crystalPattern);
                    #endif

                    // --- 2. SET FINAL COLOR & APPLY REFRACTION ON TOP ---
                    finalColor = baseColor;
                    float edgeFactor = 1.0 - smoothstep(0.0, _EdgeWidth, overlayMask);

                    #if _REFRACTION_ON
                        float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                        float3 refrNoiseInput = float4(IN.positionOS * 2.0, _Time.y);
                        float2 refrNoise = float2(snoise(refrNoiseInput), snoise(refrNoiseInput + 34.56));
                        float2 refrOffset = refrNoise * _EdgeRefractionStrength * edgeFactor;
                        float3 sceneColor = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + refrOffset).rgb;
                        finalColor = lerp(finalColor, sceneColor, edgeFactor);
                    #endif

                    // --- 3. CALCULATE FINAL ALPHA ---
                    finalAlpha = baseAlpha * overlayMask;

                    // --- 4. CALCULATE ALL EMISSIONS ---
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
    CustomEditor "StylizedOverlayShaderGUI"
    FallBack "Universal Render Pipeline/Transparent"
}