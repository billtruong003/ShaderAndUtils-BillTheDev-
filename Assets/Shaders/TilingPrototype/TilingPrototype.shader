Shader "CleanCodeShaders/AdvancedToonGridTriplanar"
{
    Properties
    {
        [Header(Grid Appearance)]
        [HDR] _GridLineColor("Line Color", Color) = (0, 0, 0, 1)
        [HDR] _GridDotColor("Dot Color", Color) = (0.2, 0.6, 1, 1)
        _GridTiling("Tiling", Float) = 10
        _GridLineThickness("Line Thickness", Range(0.001, 1)) = 0.05
        _GridLineSoftness("Line Softness", Range(0.001, 2)) = 1.0
        _GridDotSize("Dot Size", Range(0.001, 1)) = 0.2
        _GridDotSoftness("Dot Softness", Range(0.001, 2)) = 1.0

        [Header(Toon Shading)]
        [HDR] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _ShadowColor("Shadow Color", Color) = (0.7, 0.7, 0.7, 1)
        _ShadowThreshold("Shadow Threshold", Range(0, 1)) = 0.5
        _ShadowSmoothness("Shadow Smoothness", Range(0.001, 1)) = 0.05
        
        [Header(Toon Specular)]
        _SpecularColor("Specular Color", Color) = (1, 1, 1, 1)
        _Glossiness("Glossiness", Range(1, 200)) = 100
        _SpecularThreshold("Specular Threshold", Range(0, 1)) = 0.95

        [Header(Rim Lighting)]
        _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower("Rim Power", Range(0.1, 10)) = 3.0
        _RimThreshold("Rim Threshold", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        struct Attributes
        {
            float4 positionOS   : POSITION;
            float3 normalOS     : NORMAL;
        };

        struct Varyings
        {
            float4 positionCS   : SV_POSITION;
            float3 worldPosition: TEXCOORD0;
            float3 worldNormal  : NORMAL;
            float4 shadowCoord  : TEXCOORD1;
        };
        
        CBUFFER_START(UnityPerMaterial)
            float4 _GridLineColor;
            float4 _GridDotColor;
            float _GridTiling;
            float _GridLineThickness;
            float _GridLineSoftness;
            float _GridDotSize;
            float _GridDotSoftness;

            float4 _BaseColor;
            float4 _ShadowColor;
            float _ShadowThreshold;
            float _ShadowSmoothness;

            float4 _SpecularColor;
            float _Glossiness;
            float _SpecularThreshold;

            float4 _RimColor;
            float _RimPower;
            float _RimThreshold;
        CBUFFER_END

        Varyings Vertex(Attributes IN)
        {
            Varyings OUT;
            OUT.worldPosition = TransformObjectToWorld(IN.positionOS.xyz);
            OUT.worldNormal = TransformObjectToWorldNormal(IN.normalOS);
            OUT.positionCS = TransformWorldToHClip(OUT.worldPosition);
            OUT.shadowCoord = TransformWorldToShadowCoord(OUT.worldPosition);
            return OUT;
        }

        void CalculateAdvancedGridPattern(float2 uv, out float lineAlpha, out float dotAlpha)
        {
            float2 tiledUV = frac(uv);
            float lineSoftness = fwidth(tiledUV.x) * _GridLineSoftness;

            float2 distanceToCenter = abs(tiledUV - 0.5);
            float maxDistance = max(distanceToCenter.x, distanceToCenter.y);
            float lineFactor = smoothstep(0.5 - _GridLineThickness * 0.5 - lineSoftness, 0.5 - _GridLineThickness * 0.5, maxDistance);
            lineAlpha = 1.0 - lineFactor;
            
            float2 distanceToCorner = tiledUV;
            float nearestCornerDistance = length(distanceToCorner - round(distanceToCorner));
            float dotSoftness = fwidth(nearestCornerDistance) * _GridDotSoftness;
            float dotFactor = smoothstep(_GridDotSize * 0.5 - dotSoftness, _GridDotSize * 0.5, nearestCornerDistance);
            dotAlpha = 1.0 - dotFactor;
        }

        void GetTriplanarGrid(float3 worldPos, float3 worldNormal, out float totalLineAlpha, out float totalDotAlpha)
        {
            float3 blendWeights = pow(abs(worldNormal), 3);
            blendWeights /= (blendWeights.x + blendWeights.y + blendWeights.z + 1e-6);

            float lineX, dotX, lineY, dotY, lineZ, dotZ;
            CalculateAdvancedGridPattern(worldPos.yz * _GridTiling, lineX, dotX);
            CalculateAdvancedGridPattern(worldPos.xz * _GridTiling, lineY, dotY);
            CalculateAdvancedGridPattern(worldPos.xy * _GridTiling, lineZ, dotZ);
            
            totalLineAlpha = lineX * blendWeights.x + lineY * blendWeights.y + lineZ * blendWeights.z;
            totalDotAlpha = dotX * blendWeights.x + dotY * blendWeights.y + dotZ * blendWeights.z;
        }
        
        float3 CalculateToonLighting(float3 worldNormal, float3 worldPos, float4 shadowCoord)
        {
            float3 viewDirection = normalize(_WorldSpaceCameraPos - worldPos);
            Light mainLight = GetMainLight(shadowCoord);
            float3 lightDirection = mainLight.direction;
            float shadowAttenuation = mainLight.shadowAttenuation;
            
            float NdotL = saturate(dot(worldNormal, lightDirection));
            float lightIntensity = NdotL * shadowAttenuation;
            float lightStep = smoothstep(_ShadowThreshold - _ShadowSmoothness, _ShadowThreshold + _ShadowSmoothness, lightIntensity);
            float3 diffuseColor = lerp(_ShadowColor.rgb, _BaseColor.rgb, lightStep);

            float3 halfwayVector = normalize(lightDirection + viewDirection);
            float NdotH = saturate(dot(worldNormal, halfwayVector));
            float specularIntensity = pow(NdotH, _Glossiness);
            float specularStep = smoothstep(_SpecularThreshold, _SpecularThreshold + 0.01, specularIntensity);
            float3 specularColor = specularStep * _SpecularColor.rgb * mainLight.color;

            float NdotV = 1.0 - saturate(dot(worldNormal, viewDirection));
            float rimIntensity = pow(NdotV, _RimPower);
            float rimStep = smoothstep(_RimThreshold, _RimThreshold + 0.1, rimIntensity);
            float3 rimColor = rimStep * _RimColor.rgb;

            return diffuseColor + specularColor + rimColor;
        }

        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            ZWrite On
            ZTest LEqual
            
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            float4 Fragment(Varyings IN) : SV_Target
            {
                float3 worldNormal = normalize(IN.worldNormal);
                float3 toonShadedColor = CalculateToonLighting(worldNormal, IN.worldPosition, IN.shadowCoord);

                float lineAlpha, dotAlpha;
                GetTriplanarGrid(IN.worldPosition, worldNormal, lineAlpha, dotAlpha);
                
                float3 finalColor = toonShadedColor;
                finalColor = lerp(finalColor, _GridLineColor.rgb, lineAlpha);
                finalColor = lerp(finalColor, _GridDotColor.rgb, dotAlpha);

                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex VertShadow
            #pragma fragment FragShadow
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            ShadowVaryings VertShadow(ShadowAttributes IN)
            {
                ShadowVaryings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection.xyz));
                
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                
                OUT.positionCS = positionCS;
                return OUT;
            }

            half4 FragShadow(ShadowVaryings IN) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}