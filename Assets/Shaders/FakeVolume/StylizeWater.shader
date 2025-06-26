Shader "Custom/StylizedWater"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.0, 0.5, 0.5, 1.0)
        _DeepColor ("Deep Color", Color) = (0.0, 0.2, 0.2, 1.0)
        _HorizonColor ("Horizon Color", Color) = (0.5, 0.5, 0.5, 1.0)
        _HorizonDistance ("Horizon Distance", Float) = 1.0
        _DepthRange ("Depth Range", Float) = 1.0
        _RefractionStrength ("Refraction Strength", Float) = 0.1
        _FoamTexture ("Foam Texture", 2D) = "white" {}
        _FoamSpeed ("Foam Speed", Float) = 0.1
        _FoamTiling ("Foam Tiling", Float) = 1.0
        _FoamDirection ("Foam Direction", Float) = 0.0
        _FoamCutoff ("Foam Cutoff", Float) = 0.5
        _FoamColor ("Foam Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _IntersectionFoamTexture ("Intersection Foam Texture", 2D) = "white" {}
        _IntersectionFoamCutoff ("Intersection Foam Cutoff", Float) = 0.2
        _IntersectionFoamFade ("Intersection Foam Fade", Float) = 0.5
        _NormalsTexture ("Normals Texture", 2D) = "bump" {}
        _NormalsScale ("Normals Scale", Float) = 1.0
        _NormalsSpeed ("Normals Speed", Float) = 0.1
        _NormalStrength ("Normal Strength", Float) = 1.0
        _Smoothness ("Smoothness", Float) = 0.5
        _Hardness ("Hardness", Float) = 0.5
        _SpecularColor ("Specular Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _WaveNoiseTexture ("Wave Noise Texture", 2D) = "white" {}
        _WaveHeight ("Wave Height", Float) = 0.5
        _WaveSpeed ("Wave Speed", Float) = 0.1
        _WaveDirection ("Wave Direction", Float) = 0.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float4 screenPos : TEXCOORD4;
                half noise : TEXCOORD5;
            };

            half4 _ShallowColor;
            half4 _DeepColor;
            half4 _HorizonColor;
            float _HorizonDistance;
            float _DepthRange;
            float _RefractionStrength;
            TEXTURE2D(_FoamTexture); SAMPLER(sampler_FoamTexture);
            float _FoamSpeed;
            float _FoamTiling;
            float _FoamDirection;
            float _FoamCutoff;
            half4 _FoamColor;
            TEXTURE2D(_IntersectionFoamTexture); SAMPLER(sampler_IntersectionFoamTexture);
            float _IntersectionFoamCutoff;
            float _IntersectionFoamFade;
            TEXTURE2D(_NormalsTexture); SAMPLER(sampler_NormalsTexture);
            float _NormalsScale;
            float _NormalsSpeed;
            float _NormalStrength;
            float _Smoothness;
            float _Hardness;
            half4 _SpecularColor;
            TEXTURE2D(_WaveNoiseTexture); SAMPLER(sampler_WaveNoiseTexture);
            float _WaveHeight;
            float _WaveSpeed;
            float _WaveDirection;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float2 waveUV = frac(IN.uv + _Time.y * _WaveSpeed * float2(cos(_WaveDirection * 3.1415926535), sin(_WaveDirection * 3.1415926535)));
                half noise = SAMPLE_TEXTURE2D_LOD(_WaveNoiseTexture, sampler_WaveNoiseTexture, waveUV, 0).r;
                float waveOffset = (noise * 2 - 1) * _WaveHeight;
                positionWS.y += waveOffset;

                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.positionWS = positionWS;
                OUT.uv = IN.uv;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.tangentWS = float4(TransformObjectToWorldDir(IN.tangentOS.xyz), IN.tangentOS.w);
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                OUT.noise = noise;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float2 noiseUV = IN.uv * _NormalsScale + _Time.y * _NormalsSpeed;
                half noise = SAMPLE_TEXTURE2D(_NormalsTexture, sampler_NormalsTexture, noiseUV).r;
                float2 refractUV = screenUV + (noise * 2 - 1) * _RefractionStrength;

                float sceneDepth = LinearEyeDepth(SampleSceneDepth(refractUV), _ZBufferParams);
                float waterDepth = LinearEyeDepth(IN.positionCS.z / IN.positionCS.w, _ZBufferParams);
                float depthDifference = sceneDepth - waterDepth;
                float depthFade = saturate(depthDifference / _DepthRange);
                float depthFadeInv = 1 - depthFade;

                half3 shallowColor = _ShallowColor.rgb;
                half3 deepColor = _DeepColor.rgb;
                half3 depthColor = lerp(shallowColor, deepColor, depthFade);
                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.positionWS);
                float fresnel = pow(1 - saturate(dot(IN.normalWS, viewDir)), _HorizonDistance);
                half3 horizonColor = lerp(depthColor, _HorizonColor.rgb, fresnel);

                half3 sceneColor = SampleSceneColor(refractUV);
                half3 waterColor = lerp(sceneColor, horizonColor, depthFadeInv);

                // Tính foam
                float2 foamUV = IN.uv * _FoamTiling + _Time.y * _FoamSpeed * float2(cos(_FoamDirection * 3.1415926535), sin(_FoamDirection * 3.1415926535));
                half foamTex = SAMPLE_TEXTURE2D(_FoamTexture, sampler_FoamTexture, foamUV).r;
                half foam = step(_FoamCutoff, foamTex);

                // Tính bọt giao nhau với vật thể
                float intersectionFade = saturate(depthDifference / _IntersectionFoamFade);
                float2 intersectionFoamUV = IN.uv * _FoamTiling + _Time.y * _FoamSpeed * float2(cos(_FoamDirection * 3.1415926535), sin(_FoamDirection * 3.1415926535));
                half intersectionFoamTex = SAMPLE_TEXTURE2D(_IntersectionFoamTexture, sampler_IntersectionFoamTexture, intersectionFoamUV).r;
                half intersectionFoam = step(_IntersectionFoamCutoff, intersectionFoamTex) * intersectionFade;

                // Blend foam với baseColor
                half3 baseColor = waterColor;
                baseColor = lerp(baseColor, _FoamColor.rgb, foam * _FoamColor.a);
                baseColor = lerp(baseColor, _FoamColor.rgb, intersectionFoam * _FoamColor.a);

                float2 normalUV1 = IN.uv * _NormalsScale + _Time.y * _NormalsSpeed * 0.9;
                float2 normalUV2 = IN.uv * _NormalsScale + _Time.y * _NormalsSpeed * 1.1;
                half3 normal1 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalsTexture, sampler_NormalsTexture, normalUV1));
                half3 normal2 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalsTexture, sampler_NormalsTexture, normalUV2));
                half3 blendedNormal = normalize(lerp(normal1, normal2, 0.5));
                blendedNormal = normalize(lerp(float3(0, 0, 1), blendedNormal, _NormalStrength));
                half3 normalWS = TransformTangentToWorld(blendedNormal, half3x3(IN.tangentWS.xyz, cross(IN.normalWS, IN.tangentWS.xyz) * IN.tangentWS.w, IN.normalWS));

                float3 viewWS = SafeNormalize(_WorldSpaceCameraPos - IN.positionWS);
                float smoothness = exp2(10 * _Smoothness + 1);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                float3 L = mainLight.direction;
                float3 H = SafeNormalize(L + viewWS);
                float NdotH = saturate(dot(normalWS, H));
                float specularMain = pow(NdotH, smoothness);
                specularMain = step(0.5, specularMain);
                half3 specularColorMain = specularMain * _SpecularColor.rgb * mainLight.color;

                half3 specularAdditional = 0;
                int pixelLightCount = GetAdditionalLightsCount();
                for (int i = 0; i < pixelLightCount; ++i)
                {
                    Light light = GetAdditionalLight(i, IN.positionWS);
                    float3 attenuatedLight = light.color * light.distanceAttenuation * light.shadowAttenuation;
                    float3 L_add = light.direction;
                    float3 H_add = SafeNormalize(L_add + viewWS);
                    float NdotH_add = saturate(dot(normalWS, H_add));
                    float specular_soft = pow(NdotH_add, smoothness);
                    float specular_hard = smoothstep(0.005, 0.01, specular_soft);
                    float specular_term = lerp(specular_soft, specular_hard, _Hardness);
                    specularAdditional += specular_term * attenuatedLight;
                }

                half3 finalColor = baseColor + specularColorMain + specularAdditional;
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}