Shader "ShaderAndUtils/TextureProcessor"
{
    Properties
    {
        [Header(Input Textures)]
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        [Toggle(_USE_ID_MAP)] _UseIdMap("Enable ID Map Emission", Float) = 0
        _IdMap ("ID Map (when enabled)", 2D) = "black" {}

        [Header(Color Replacement CIEDE2000)]
        [Toggle(_ENABLE_REPLACEMENT)] _EnableReplacement("Enable Color Replacement", Float) = 0
        _TargetColor ("Target Color (sRGB)", Color) = (1,0,0,1)
        _ReplacementColor ("Replacement Color (sRGB)", Color) = (0,1,1,1)
        _ColorDifferenceTolerance ("Tolerance (Delta E)", Range(0, 100)) = 10
        _TransitionSoftness("Softness", Range(0.01, 20)) = 2

        [Header(Emission from Hue)]
        [Toggle(_ENABLE_HUE_EMISSION)] _EnableHueEmission("Enable Hue Emission", Float) = 0
        _HueTargetColor ("Hue Target Color", Color) = (1,0,0,1)
        _HueThreshold ("Hue Threshold", Range(0, 0.5)) = 0.05
        _SaturationThreshold ("Saturation Threshold", Range(0, 1)) = 0.2
        
        [Header(Emission from ID Map)]
        _TargetIdColor ("Target ID Color", Color) = (1, 0, 0, 1)
        _IdTolerance ("ID Match Tolerance", Range(0, 0.1)) = 0.01

        [Header(General Emission Properties)]
        [HDR] _EmissionColor ("Emission Color", Color) = (1,1,0,1)
        _EmissionIntensity ("Emission Intensity", Range(0, 20)) = 5.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma shader_feature_local _ENABLE_REPLACEMENT
            #pragma shader_feature_local _ENABLE_HUE_EMISSION
            #pragma shader_feature_local _USE_ID_MAP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Includes/ColorLibrary.hlsl"

            struct Attributes {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };
            
            CBUFFER_START(UnityPerMaterial)
                sampler2D _MainTex, _IdMap;
                float4 _MainTex_ST, _IdMap_ST;
                float4 _TargetColor, _ReplacementColor, _HueTargetColor, _TargetIdColor, _EmissionColor;
                float _ColorDifferenceTolerance, _TransitionSoftness;
                float _HueThreshold, _SaturationThreshold;
                float _IdTolerance, _EmissionIntensity;
            CBUFFER_END

            Varyings vert(Attributes input) {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return o;
            }

            float4 frag(Varyings input) : SV_Target {
                float4 albedo = tex2D(_MainTex, input.uv);
                float3 finalColor = albedo.rgb;
                float3 totalEmission = float3(0,0,0);

                #if _ENABLE_REPLACEMENT
                    float3 originalLab = ConvertSrgbToLab(albedo.rgb);
                    float3 targetLab = ConvertSrgbToLab(_TargetColor.rgb);
                    float deltaE = CalculateDeltaE2000(originalLab, targetLab);
                    float edge0 = _ColorDifferenceTolerance - _TransitionSoftness;
                    float edge1 = _ColorDifferenceTolerance + _TransitionSoftness;
                    float mask = 1.0 - smoothstep(edge0, edge1, deltaE);
                    float3 replacementLinear = ConvertSrgbToLinear(_ReplacementColor.rgb);
                    finalColor = lerp(ConvertSrgbToLinear(finalColor), replacementLinear, mask);
                    finalColor = ConvertLinearToSrgb(finalColor);
                #endif

                #if _ENABLE_HUE_EMISSION
                    float3 pixelHSV = ConvertRgbToHsv(albedo.rgb);
                    float3 targetHSV = ConvertRgbToHsv(_HueTargetColor.rgb);
                    float hueDiff = abs(pixelHSV.x - targetHSV.x);
                    hueDiff = min(hueDiff, 1.0 - hueDiff);
                    bool isHueMatch = hueDiff <= _HueThreshold;
                    bool isSaturationMatch = abs(pixelHSV.y - targetHSV.y) <= _SaturationThreshold;
                    float emissionMask = (isHueMatch && isSaturationMatch) ? 1.0 : 0.0;
                    totalEmission += _EmissionColor.rgb * _EmissionIntensity * emissionMask * pixelHSV.z;
                #endif
                
                #if _USE_ID_MAP
                    float3 idColor = tex2D(_IdMap, input.uv).rgb;
                    float colorDistance = distance(idColor, _TargetIdColor.rgb);
                    float idMask = 1.0 - smoothstep(0, _IdTolerance, colorDistance);
                    totalEmission += _EmissionColor.rgb * _EmissionIntensity * idMask;
                #endif

                finalColor += totalEmission;
                return float4(finalColor, albedo.a);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}