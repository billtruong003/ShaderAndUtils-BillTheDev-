Shader "CleanCode/UltimateScanner"
{
    Properties
    {
        [Header(Scan Core)]
        [HDR] _ScanColor ("Scan Color (HDR)", Color) = (0.2, 0.8, 1, 1)
        _ScanRadius ("Scan Current Radius", Float) = 10.0
        _ScanWidth ("Scan Main Width", Range(0.1, 50.0)) = 5.0
        _ScanHardness ("Scan Hardness", Range(1, 50)) = 10.0
        
        [Header(World Space Settings)]
        _ScanCenter ("Scan Center (World Space)", Vector) = (0,0,0,0)

        [Header(Leading Glow)]
        _LeadingGlowIntensity ("Leading Glow Intensity", Range(0, 5)) = 1.5
        _LeadingGlowWidth ("Leading Glow Width Multiplier", Range(1, 10)) = 4.0

        [Header(Noise Properties)]
        _NoiseTexture ("Noise Texture (Grayscale)", 2D) = "white" {}
        _NoiseScale ("Noise Tiling Scale", Float) = 2.0
        _NoiseStrength ("Noise Intersection Strength", Range(0, 10)) = 0.5
        _NoiseScrollSpeed ("Noise Scroll Speed (XY)", Vector) = (0.1, 0.05, 0, 0)
        
        [Header(Rim Light)]
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0.5, 8.0)) = 3.0

        [Header(Render States)]
        [Toggle(_USE_LOCAL_SPACE)] _UseLocalSpace ("Enable Local Space Scan", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z-Test Mode", Float) = 4 // 4 is LEqual
        [Enum(Off, 0, On, 1)] _ZWrite ("Z-Write Mode", Float) = 0 // 0 is Off
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalRenderPipeline" }
        
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Front
            ZWrite [_ZWrite]
            ZTest [_ZTest]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _USE_LOCAL_SPACE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS     : SV_POSITION;
                float4 screenPos      : TEXCOORD0;
                float3 worldPos       : TEXCOORD1;
                float3 worldNormal    : TEXCOORD2;
                float3 viewDir        : TEXCOORD3;
                float3 positionOS     : TEXCOORD4;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _ScanColor, _ScanCenter, _RimColor;
                float _ScanRadius, _ScanWidth, _ScanHardness;
                float _LeadingGlowIntensity, _LeadingGlowWidth;
                float _NoiseScale, _NoiseStrength;
                float2 _NoiseScrollSpeed;
                float _RimPower;
            CBUFFER_END

            TEXTURE2D_X(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);
            TEXTURE2D(_NoiseTexture);
            SAMPLER(sampler_NoiseTexture);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = posInputs.positionCS;
                OUT.worldPos = posInputs.positionWS;
                OUT.worldNormal = GetVertexNormalInputs(IN.normalOS).normalWS;
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                OUT.viewDir = normalize(_WorldSpaceCameraPos.xyz - OUT.worldPos);
                OUT.positionOS = IN.positionOS.xyz;
                return OUT;
            }

            float calculate_intersection_value(float meshDepth, float sceneDepth, float width, float hardness)
            {
                float delta = abs(meshDepth - sceneDepth);
                float intersection = 1.0 - smoothstep(0.0, width, delta);
                return pow(intersection, hardness);
            }
            
            float sample_triplanar_noise(float3 worldPos, float3 normal, float2 scrollSpeed, float scale)
            {
                float2 uvX = worldPos.yz * scale + (_Time.y * scrollSpeed);
                float2 uvY = worldPos.xz * scale + (_Time.y * scrollSpeed);
                float2 uvZ = worldPos.xy * scale + (_Time.y * scrollSpeed);

                float noiseX = SAMPLE_TEXTURE2D(_NoiseTexture, sampler_NoiseTexture, uvX).r;
                float noiseY = SAMPLE_TEXTURE2D(_NoiseTexture, sampler_NoiseTexture, uvY).r;
                float noiseZ = SAMPLE_TEXTURE2D(_NoiseTexture, sampler_NoiseTexture, uvZ).r;

                float3 blendWeights = abs(normal);
                blendWeights = blendWeights / (blendWeights.x + blendWeights.y + blendWeights.z);

                return noiseX * blendWeights.x + noiseY * blendWeights.y + noiseZ * blendWeights.z;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float sceneDepthRaw = SAMPLE_TEXTURE2D_X_LOD(_CameraDepthTexture, sampler_CameraDepthTexture, IN.screenPos.xy / IN.screenPos.w, 0).r;
                float sceneDepth = LinearEyeDepth(sceneDepthRaw, _ZBufferParams);
                float meshDepth = IN.screenPos.w;

                float distFromScanCenter;
                #if _USE_LOCAL_SPACE
                    distFromScanCenter = length(IN.positionOS);
                #else
                    distFromScanCenter = distance(IN.worldPos, _ScanCenter.xyz);
                #endif

                float scanDelta = abs(distFromScanCenter - _ScanRadius);
                float scanFalloff = 1.0 - smoothstep(0, _ScanWidth, scanDelta);

                float noiseValue = sample_triplanar_noise(IN.worldPos, IN.worldNormal, _NoiseScrollSpeed, _NoiseScale);
                float intersectionWidth = _ScanWidth * (1.0 - noiseValue * _NoiseStrength);
                float intersectionValue = calculate_intersection_value(meshDepth, sceneDepth, intersectionWidth, _ScanHardness);
                
                float leadingGlowWidth = _ScanWidth * _LeadingGlowWidth;
                float glowFalloff = 1.0 - smoothstep(_ScanWidth, _ScanWidth + leadingGlowWidth, scanDelta);
                float glowValue = intersectionValue * glowFalloff * _LeadingGlowIntensity;
                
                float finalIntensity = scanFalloff * intersectionValue;

                float rimDot = 1.0 - saturate(dot(IN.viewDir, IN.worldNormal));
                float rimValue = pow(rimDot, _RimPower);
                float4 rimColor = _RimColor * rimValue * finalIntensity;

                float4 mainColor = _ScanColor * finalIntensity;
                float4 glowColor = _ScanColor * glowValue;

                float4 finalColor = mainColor + glowColor + rimColor;
                finalColor.a = saturate(finalIntensity + glowValue + (rimValue * finalIntensity));

                return finalColor;
            }
            ENDHLSL
        }
    }
}