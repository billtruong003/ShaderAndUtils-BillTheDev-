Shader "CleanCode/OptimizedHybridTriplanarVR"
{
    Properties
    {
        [Header(Surface Textures)]
        _RockAlbedo("Rock Albedo", 2D) = "white" {}
        _RockNormal("Rock Normal", 2D) = "bump" {}
        _GrassAlbedo("Grass Albedo", 2D) = "white" {}
        _GrassNormal("Grass Normal", 2D) = "bump" {}
        _TextureTiling("Global Tiling", Float) = 1.0
        _NormalIntensity("Normal Intensity", Range(0, 2)) = 1.0

        [Header(Terrain Blending Logic)]
        _AxisBlendSharpness("Axis Blend Sharpness", Range(1.0, 50.0)) = 15.0
        _GrassBlendStart("Grass Blend Start (Upwards Normal)", Range(0.0, 1.0)) = 0.5
        _GrassBlendFalloff("Grass Blend Falloff", Range(0.01, 1.0)) = 0.2

        [Header(Blend Noise Control)]
        [Toggle(_TRIPLANAR_NOISE)] _EnableNoise("Enable Blend Noise", Float) = 0
        _NoiseTexture("Blend Noise Texture (R channel)", 2D) = "grey" {}
        _NoiseScale("Noise Scale", Float) = 5.0
        _NoiseInfluence("Noise Influence", Range(0.0, 1.0)) = 0.1

        [Header(ArtistDriven Lighting)]
        [Toggle(_ARTIST_LIGHTING_RAMP)] _EnableRampLighting("Enable Ramp Lighting", Float) = 0
        _HighlightColor("Highlight Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _MidtoneColor("Midtone Color", Color) = (0.7, 0.7, 0.7, 1.0)
        _ShadowColor("Shadow Color", Color) = (0.3, 0.3, 0.3, 1.0)
        _HighlightThreshold("Highlight Threshold", Range(0.0, 1.0)) = 0.8
        _ShadowThreshold("Shadow Threshold", Range(0.0, 1.0)) = 0.4
        _RampSmoothness("Ramp Smoothness", Range(0.001, 0.5)) = 0.05

        [Header(Local Hatching Shadow)]
        [Toggle(_LOCAL_HATCHING)] _EnableHatching("Enable Hatching", Float) = 0
        _HatchingTexture("Hatching Texture (R channel)", 2D) = "white" {}
        _HatchingTiling("Hatching Tiling", Float) = 5.0
        _HatchingLightLevel("Hatching Appears Below Light Level", Range(0.0, 1.0)) = 0.25
        _HatchingColor("Hatching Color", Color) = (0,0,0,1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local _TRIPLANAR_NOISE
            #pragma shader_feature_local _ARTIST_LIGHTING_RAMP
            #pragma shader_feature_local _LOCAL_HATCHING

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 worldPos     : TEXCOORD0;
                float3x3 tbn        : TEXCOORD1;
            };

            struct TriplanarData
            {
                float3 blendWeights;
                float2 uvX, uvY, uvZ;
            };

            struct Surface
            {
                half4 albedo;
                half3 normal;
            };

            TEXTURE2D(_RockAlbedo);     SAMPLER(sampler_linear_repeat);
            TEXTURE2D(_RockNormal);
            TEXTURE2D(_GrassAlbedo);
            TEXTURE2D(_GrassNormal);
            TEXTURE2D(_NoiseTexture);
            TEXTURE2D(_HatchingTexture);

            CBUFFER_START(UnityPerMaterial)
                float _TextureTiling, _NormalIntensity, _AxisBlendSharpness, _GrassBlendStart, _GrassBlendFalloff;
                float _NoiseScale, _NoiseInfluence;
                half4 _HighlightColor, _MidtoneColor, _ShadowColor;
                half _HighlightThreshold, _ShadowThreshold, _RampSmoothness;
                float _HatchingTiling;
                half _HatchingLightLevel;
                half4 _HatchingColor;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float3 tangentWS = TransformObjectToWorldDir(IN.tangentOS.xyz);
                float3 bitangentWS = cross(normalWS, tangentWS) * IN.tangentOS.w;
                OUT.tbn = float3x3(tangentWS, bitangentWS, normalWS);
                
                return OUT;
            }
            
            TriplanarData CalculateTriplanarBlendData(float3 worldPos, float3x3 tbn)
            {
                TriplanarData data;
                
                float3 pos_dx = ddx(worldPos);
                float3 pos_dy = ddy(worldPos);
                float3 proceduralNormal = normalize(cross(pos_dy, pos_dx));
                
                data.blendWeights = pow(abs(proceduralNormal), _AxisBlendSharpness);
                data.blendWeights /= dot(data.blendWeights, 1.0);

                float3 scaledPos = worldPos / _TextureTiling;
                data.uvX = scaledPos.zy;
                data.uvY = scaledPos.xz;
                data.uvZ = scaledPos.xy;
                
                return data;
            }

            half4 SampleTextureTriplanar(TEXTURE2D_PARAM(tex, smp), TriplanarData data)
            {
                half4 sampleX = SAMPLE_TEXTURE2D(tex, smp, data.uvX);
                half4 sampleY = SAMPLE_TEXTURE2D(tex, smp, data.uvY);
                half4 sampleZ = SAMPLE_TEXTURE2D(tex, smp, data.uvZ);
                return sampleX * data.blendWeights.x + sampleY * data.blendWeights.y + sampleZ * data.blendWeights.z;
            }

            Surface GetBlendedSurface(float3 worldPos, float3 vertexNormal, TriplanarData triplanarData)
            {
                half4 rockAlbedo = SampleTextureTriplanar(TEXTURE2D_ARGS(_RockAlbedo, sampler_linear_repeat), triplanarData);
                half4 grassAlbedo = SampleTextureTriplanar(TEXTURE2D_ARGS(_GrassAlbedo, sampler_linear_repeat), triplanarData);
                half3 rockTangentNormal = UnpackNormal(SampleTextureTriplanar(TEXTURE2D_ARGS(_RockNormal, sampler_linear_repeat), triplanarData));
                half3 grassTangentNormal = UnpackNormal(SampleTextureTriplanar(TEXTURE2D_ARGS(_GrassNormal, sampler_linear_repeat), triplanarData));

                half noiseValue = 0;
                #if defined(_TRIPLANAR_NOISE)
                    noiseValue = (SAMPLE_TEXTURE2D(_NoiseTexture, sampler_linear_repeat, worldPos.xz / _NoiseScale).r - 0.5) * _NoiseInfluence;
                #endif

                float upwardNormalDot = dot(vertexNormal, float3(0, 1, 0));
                float grassMask = smoothstep(_GrassBlendStart - _GrassBlendFalloff * 0.5, _GrassBlendStart + _GrassBlendFalloff * 0.5, upwardNormalDot + noiseValue);
                
                Surface surface;
                surface.albedo = lerp(rockAlbedo, grassAlbedo, grassMask);
                half3 blendedTangentNormal = lerp(rockTangentNormal, grassTangentNormal, grassMask);
                surface.normal = lerp(half3(0,0,1), blendedTangentNormal, _NormalIntensity);
                
                return surface;
            }

            half3 GetStylizedLighting(half NdotL)
            {
                half shadowMix = smoothstep(_ShadowThreshold - _RampSmoothness, _ShadowThreshold + _RampSmoothness, NdotL);
                half highlightMix = smoothstep(_HighlightThreshold - _RampSmoothness, _HighlightThreshold + _RampSmoothness, NdotL);
                half3 rampMidtones = lerp(_ShadowColor.rgb, _MidtoneColor.rgb, shadowMix);
                return lerp(rampMidtones, _HighlightColor.rgb, highlightMix);
            }

            half3 GetHatchingShadow(half NdotL, float3 worldPos, TriplanarData triplanarData)
            {
                float3 hatchingUVs = worldPos / (_TextureTiling / _HatchingTiling);
                TriplanarData hatchingTriplanarData = triplanarData;
                hatchingTriplanarData.uvX = hatchingUVs.zy;
                hatchingTriplanarData.uvY = hatchingUVs.xz;
                hatchingTriplanarData.uvZ = hatchingUVs.xy;

                half hatchingSample = SampleTextureTriplanar(TEXTURE2D_ARGS(_HatchingTexture, sampler_linear_repeat), hatchingTriplanarData).r;
                half hatchingMask = smoothstep(_HatchingLightLevel + 0.05, _HatchingLightLevel - 0.05, NdotL);
                return (1.0 - hatchingSample) * hatchingMask * _HatchingColor.rgb;
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                TriplanarData triplanarData = CalculateTriplanarBlendData(IN.worldPos, IN.tbn);
                float3 vertexNormal = normalize(IN.tbn[2]);

                Surface surface = GetBlendedSurface(IN.worldPos, vertexNormal, triplanarData);
                half3 finalNormalWS = normalize(TransformTangentToWorld(surface.normal, IN.tbn));
                
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(finalNormalWS, mainLight.direction));
                half3 ambient = SampleSH(finalNormalWS);
                
                half3 lightContribution = NdotL * mainLight.color;
                #if defined(_ARTIST_LIGHTING_RAMP)
                    lightContribution = GetStylizedLighting(NdotL) * mainLight.color;
                #endif

                half3 finalColor = (ambient + lightContribution) * surface.albedo.rgb;

                #if defined(_LOCAL_HATCHING)
                    finalColor -= GetHatchingShadow(NdotL, IN.worldPos, triplanarData);
                #endif
                
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}