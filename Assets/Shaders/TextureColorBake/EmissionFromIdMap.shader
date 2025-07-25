Shader "CleanCode/EmissionFromIdMap"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _IdMap ("ID Map (RGB)", 2D) = "black" {} // New texture slot for the ID Map

        [Header(Emission Properties)]
        _TargetIdColor ("Target ID Color", Color) = (1, 0, 0, 1) // The flat color to find in the ID Map
        [HDR] _EmissionColor ("Emission Color", Color) = (1, 1, 0, 1)
        _EmissionIntensity ("Emission Intensity", Range(0, 20)) = 5.0
        
        // A small tolerance to account for texture compression artifacts
        _IdTolerance ("ID Match Tolerance", Range(0, 0.1)) = 0.01 
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline", "RenderType"="Opaque" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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
            };
            
            CBUFFER_START(UnityPerMaterial)
                sampler2D _MainTex;
                float4 _MainTex_ST;
                sampler2D _IdMap;
                float4 _IdMap_ST;
                float4 _TargetIdColor;
                float4 _EmissionColor;
                float _EmissionIntensity;
                float _IdTolerance;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return o;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 albedoColor = tex2D(_MainTex, input.uv);
                float3 idColor = tex2D(_IdMap, input.uv).rgb;

                // Direct, precise comparison of the ID color
                // distance() is a clean way to check if two colors are nearly identical
                float colorDistance = distance(idColor, _TargetIdColor.rgb);
                
                // Use smoothstep for a slightly softer edge than a hard if/else
                float emissionMask = 1.0 - smoothstep(0, _IdTolerance, colorDistance);
                
                float3 finalEmission = _EmissionColor.rgb * _EmissionIntensity * emissionMask;
                
                float3 finalColor = albedoColor.rgb + finalEmission;

                return float4(finalColor, albedoColor.a);
            }
            ENDHLSL
        }
    }
}