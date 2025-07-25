Shader "CleanCode/AdvancedColorReplacementURP"
{
    Properties
    {
        [Header(Main Texture)]
        _MainTex ("Albedo (RGB)", 2D) = "white" {}

        [Header(Color Selection and Replacement)]
        _TargetColor ("Target Color (sRGB)", Color) = (1, 1, 1, 1)
        _ReplacementColor ("Replacement Color (sRGB)", Color) = (0, 1, 1, 1)

        [Header(Matching Logic CIEDE2000)]
        _ColorDifferenceTolerance ("Color Difference Tolerance (Delta E)", Range(0.0, 100)) = 10
        _TransitionSoftness("Transition Softness", Range(0.01, 20)) = 2
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                sampler2D _MainTex;
                float4 _MainTex_ST;
                float4 _TargetColor;
                float4 _ReplacementColor;
                float _ColorDifferenceTolerance;
                float _TransitionSoftness;
            CBUFFER_END

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

            // Section: Color Space Conversion Functions
            // =========================================

            float3 convertSrgbToLinear(float3 srgb)
            {
                bool3 isLinear = srgb <= 0.04045;
                float3 linearPart = srgb / 12.92;
                float3 gammaPart = pow((srgb + 0.055) / 1.055, 2.4);
                return lerp(gammaPart, linearPart, isLinear);
            }

            float3 convertLinearToXyz(float3 linearRgb)
            {
                const float3x3 sRGB_TO_XYZ_MATRIX = {
                    0.4124, 0.3576, 0.1805,
                    0.2126, 0.7152, 0.0722,
                    0.0193, 0.1192, 0.9505
                };
                return mul(sRGB_TO_XYZ_MATRIX, linearRgb);
            }

            float3 convertXyzToLab(float3 xyz)
            {
                const float3 D65_WHITE_POINT = float3(0.95047, 1.00000, 1.08883);
                xyz /= D65_WHITE_POINT;

                const float epsilon = 216.0 / 24389.0; // (6/29)^3
                const float kappa = 24389.0 / 27.0;   // (29/3)^3

                float3 f;
                bool3 isGtEpsilon = xyz > epsilon;
                // FIX: Reverted cbrt(x) back to the compatible pow(x, 1.0/3.0) function
                f.x = isGtEpsilon.x ? pow(xyz.x, 1.0 / 3.0) : (kappa * xyz.x + 16.0) / 116.0;
                f.y = isGtEpsilon.y ? pow(xyz.y, 1.0 / 3.0) : (kappa * xyz.y + 16.0) / 116.0;
                f.z = isGtEpsilon.z ? pow(xyz.z, 1.0 / 3.0) : (kappa * xyz.z + 16.0) / 116.0;

                float L = (116.0 * f.y) - 16.0;
                float a = 500.0 * (f.x - f.y);
                float b = 200.0 * (f.y - f.z);

                return float3(L, a, b);
            }
            
            float3 convertSrgbToLab(float3 srgb)
            {
                float3 linearRgb = convertSrgbToLinear(srgb);
                float3 xyz = convertLinearToXyz(linearRgb);
                return convertXyzToLab(xyz);
            }

            // Section: CIEDE2000 Delta E Calculation
            // =======================================

            float calculateDeltaE2000(float3 lab1, float3 lab2)
            {
                const float kL = 1.0;
                const float kC = 1.0;
                const float kH = 1.0;

                float L1 = lab1.x; float a1 = lab1.y; float b1 = lab1.z;
                float L2 = lab2.x; float a2 = lab2.y; float b2 = lab2.z;

                float C1 = sqrt(a1*a1 + b1*b1);
                float C2 = sqrt(a2*a2 + b2*b2);

                float avgC = (C1 + C2) / 2.0;
                float G = 0.5 * (1.0 - sqrt(pow(avgC, 7.0) / (pow(avgC, 7.0) + pow(25.0, 7.0))));

                float a1_prime = (1.0 + G) * a1;
                float a2_prime = (1.0 + G) * a2;

                float C1_prime = sqrt(a1_prime*a1_prime + b1*b1);
                float C2_prime = sqrt(a2_prime*a2_prime + b2*b2);

                float h1_prime_rad = atan2(b1, a1_prime);
                float h2_prime_rad = atan2(b2, a2_prime);
                
                if (h1_prime_rad < 0) h1_prime_rad += 2 * PI;
                if (h2_prime_rad < 0) h2_prime_rad += 2 * PI;

                float deltaL_prime = L2 - L1;
                float deltaC_prime = C2_prime - C1_prime;
                
                float deltah_prime;
                if (C1_prime * C2_prime == 0) {
                    deltah_prime = 0;
                } else {
                    deltah_prime = h2_prime_rad - h1_prime_rad;
                    if (abs(deltah_prime) > PI) {
                        deltah_prime -= sign(deltah_prime) * 2 * PI;
                    }
                }
                
                float deltaH_prime = 2.0 * sqrt(C1_prime * C2_prime) * sin(deltah_prime / 2.0);

                float avgL_prime = (L1 + L2) / 2.0;
                float avgC_prime = (C1_prime + C2_prime) / 2.0;
                
                float avgh_prime;
                if (C1_prime * C2_prime == 0) {
                    avgh_prime = h1_prime_rad + h2_prime_rad;
                } else {
                    avgh_prime = (h1_prime_rad + h2_prime_rad) / 2.0;
                    if(abs(h1_prime_rad - h2_prime_rad) > PI) {
                        avgh_prime -= PI;
                    }
                }

                float T = 1 - 0.17 * cos(avgh_prime - radians(30)) + 0.24 * cos(2 * avgh_prime) + 0.32 * cos(3 * avgh_prime + radians(6)) - 0.20 * cos(4 * avgh_prime - radians(63));
                float deltaTheta_rad = radians(30) * exp(-pow((degrees(avgh_prime) - 275) / 25.0, 2.0));
                
                float Rc = 2.0 * sqrt(pow(avgC_prime, 7.0) / (pow(avgC_prime, 7.0) + pow(25.0, 7.0)));
                float SL = 1.0 + (0.015 * pow(avgL_prime - 50, 2.0)) / sqrt(20 + pow(avgL_prime - 50, 2.0));
                float SC = 1.0 + 0.045 * avgC_prime;
                float SH = 1.0 + 0.015 * avgC_prime * T;
                float RT = -sin(2 * deltaTheta_rad) * Rc;

                float termL = deltaL_prime / (kL * SL);
                float termC = deltaC_prime / (kC * SC);
                float termH = deltaH_prime / (kH * SH);
                
                return sqrt(pow(termL, 2.0) + pow(termC, 2.0) + pow(termH, 2.0) + RT * termC * termH);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 originalColorSrgb = tex2D(_MainTex, input.uv);
                
                float3 originalLab = convertSrgbToLab(originalColorSrgb.rgb);
                float3 targetLab = convertSrgbToLab(_TargetColor.rgb);

                float deltaE = calculateDeltaE2000(originalLab, targetLab);
                
                float edge0 = _ColorDifferenceTolerance - _TransitionSoftness;
                float edge1 = _ColorDifferenceTolerance + _TransitionSoftness;
                float mask = 1.0 - smoothstep(edge0, edge1, deltaE);

                float3 finalRgb = lerp(originalColorSrgb.rgb, _ReplacementColor.rgb, mask);
                
                return float4(finalRgb, originalColorSrgb.a);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}