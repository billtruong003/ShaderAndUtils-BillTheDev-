Shader "CleanCode/AdvancedEmissionDetection"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}

        [Header(Color Detection)]
        _TargetColor ("Target Color to Detect", Color) = (1, 0, 0, 1)

        [Header(Emission Properties)]
        [HDR] _EmissionColor ("Emission Color", Color) = (1, 0, 0, 1)
        _EmissionIntensity ("Emission Intensity", Range(0, 10)) = 2.0

        [Header(Detection Thresholds)]
        _HueThreshold ("Hue Threshold", Range(0.0, 0.5)) = 0.05
        _SaturationThreshold ("Saturation Threshold", Range(0.0, 1.0)) = 0.2
    }
    SubShader
    {
        Tags 
        { 
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"="Opaque"
        }

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
                float4 _TargetColor;
                float4 _EmissionColor;
                float _EmissionIntensity;
                float _HueThreshold;
                float _SaturationThreshold;
            CBUFFER_END

            // Function converts RGB to HSV color space
            float3 convertRgbToHsv(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                float d = q.x - min(q.w, q.y);
                float e = 1e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

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

                float3 pixelHSV = convertRgbToHsv(albedoColor.rgb);
                float3 targetHSV = convertRgbToHsv(_TargetColor.rgb);

                // Compare only Hue and Saturation, ignoring Brightness (V)
                float hueDiff = abs(pixelHSV.x - targetHSV.x);
                hueDiff = min(hueDiff, 1.0 - hueDiff); // Handle hue wrap-around

                bool isHueMatch = hueDiff <= _HueThreshold;
                bool isSaturationMatch = abs(pixelHSV.y - targetHSV.y) <= _SaturationThreshold;

                // The mask is now independent of the original pixel's brightness
                float emissionMask = (isHueMatch && isSaturationMatch) ? 1.0 : 0.0;
                
                // Modulate emission by the original pixel's brightness to preserve detail
                float3 brightnessModulation = pixelHSV.z * _EmissionIntensity;
                float3 finalEmission = _EmissionColor.rgb * brightnessModulation * emissionMask;

                // The final color is the albedo plus the calculated emission
                float3 finalColor = albedoColor.rgb + finalEmission;
                
                return float4(finalColor, albedoColor.a);
            }
            ENDHLSL
        }
    }
}