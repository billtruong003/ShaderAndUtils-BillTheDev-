Shader "Custom/URP/ToonEyeAdvanced_Pro"
{
    Properties
    {
        [Header(Base Eye)]
        _BaseMap ("Base Eye Texture (RGBA)", 2D) = "white" {} // Texture tổng thể của mắt (iris, pupil, alpha cho hình dạng)
        _BaseColor ("Base Eye Color Tint", Color) = (1,1,1,1) // Dùng để thay đổi màu mắt tổng thể

        [Header(Toon Shading)]
        _ToonThreshold ("Toon Threshold", Range(0,1)) = 0.5 // Ngưỡng chuyển giữa sáng/tối
        _ToonSmoothness ("Toon Smoothness", Range(0.001,0.5)) = 0.05 // Độ mềm mại của vùng chuyển
        [HDR] _ToonLitColor ("Toon Lit Color", Color) = (1,1,1,1) // Màu vùng sáng
        [HDR] _ToonShadowColor ("Toon Shadow Color", Color) = (0.5,0.5,0.5,1) // Màu vùng tối
        [HDR] _AmbientColor ("Ambient Color (Overall)", Color) = (0.2, 0.2, 0.2, 1) // Ánh sáng môi trường
        _ToonOverallStrength ("Toon Overall Strength", Range(0, 2)) = 1.0 // Cường độ tổng thể của Toon shading

        [Header(1. Outer Iris Rim)]
        _OuterIrisMask ("Outer Iris Mask (Grayscale)", 2D) = "white" {} // Mask cho vùng viền tròng mắt
        _OuterIrisColor ("Outer Iris Color", Color) = (0.0,0.0,0.0,1.0) // Màu của viền
        _OuterIrisThreshold ("Outer Iris Threshold", Range(0,1)) = 0.5 // Ngưỡng tạo viền
        _OuterIrisSmoothness ("Outer Iris Smoothness", Range(0.001,0.5)) = 0.05 // Độ mượt của viền

        [Header(2. Pupil)]
        _PupilMask ("Pupil Mask (Grayscale)", 2D) = "white" {} // Mask cho vùng đồng tử

        [Header(3. Specular Highlight)]
        _SpecularMask ("Specular Mask (Grayscale)", 2D) = "white" {}
        [HDR] _SpecularColor ("Specular Color", Color) = (1,1,1,1) // Màu điểm sáng (có thể HDR)
        _Shininess ("Shininess (Power)", Range(1, 10000)) = 30.0 // Độ bóng của điểm sáng
        _SpecularStrength ("Specular Strength", Range(0, 5)) = 1.0 // Cường độ tổng thể của điểm sáng
        _SpecularFresnelPower ("Specular Fresnel Power", Range(0, 5)) = 0.0 // Fresnel cho điểm sáng (0 = tắt)

        [Header(4. Special Pattern)]
        [Toggle(_SPECIAL_PATTERN_ENABLED)] _SpecialPatternEnabled ("Enable Special Pattern", Float) = 0 // Bật/tắt
        _SpecialPatternTexture ("Special Pattern Texture (RGBA)", 2D) = "white" {} // Texture cho pattern
        _SpecialPatternInfluenceMask ("Pattern Influence Mask (Grayscale)", 2D) = "white" {} // MỚI: Mask giới hạn vùng ảnh hưởng của pattern
        _SpecialPatternColor ("Special Pattern Color Tint", Color) = (1,1,1,1) // Màu tint cho pattern
        _SpecialPatternStrength ("Special Pattern Strength", Range(0,1)) = 1.0 // Cường độ pha trộn
        _SpecialPatternSpeed ("Special Pattern Scroll Speed (XY)", Vector) = (0.0, 0.0, 0, 0) // Tốc độ di chuyển
        _SpecialPatternRotationSpeed ("Special Pattern Rotation Speed", Range(0, 10)) = 0.0 // Tốc độ xoay

        [Header(5. Noise Depth Effect)]
        [Toggle(_NOISE_DEPTH_ENABLED)] _NoiseDepthEnabled ("Enable Noise Depth Effect", Float) = 0 // Bật/tắt
        _NoiseDepthTexture ("Noise Depth Texture (Grayscale)", 2D) = "gray" {} // Texture noise
        [HDR] _NoiseDepthColor ("Noise Depth Color", Color) = (0.1,0.1,0.1,1) // Màu của hiệu ứng noise
        _NoiseDepthStrength ("Noise Depth Strength", Range(0,1)) = 0.5 // Cường độ noise
        _NoiseDepthSpeed ("Noise Depth Scroll Speed (XY)", Vector) = (0.0, 0.0, 0, 0) // Tốc độ chạy của noise

        [Header(6. Bling Bling Glitter)]
        [Toggle(_BLING_ENABLED)] _BlingEnabled ("Enable Bling Bling", Float) = 0 // Bật/tắt
        _BlingTexture ("Bling Bling Texture (Alpha)", 2D) = "white" {} // Texture cho Bling Bling
        [HDR] _BlingColor ("Bling Bling Color", Color) = (1,1,1,1) // Màu của bling bling (có thể HDR)
        _BlingStrength ("Bling Bling Strength", Range(0,1)) = 1.0 // Cường độ pha trộn
        _BlingRotationSpeed ("Bling Bling Rotation Speed", Range(0, 10)) = 0.0 // Tốc độ xoay của bling bling
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fw_and_shadows
            #pragma shader_feature_local _SPECIAL_PATTERN_ENABLED
            #pragma shader_feature_local _NOISE_DEPTH_ENABLED
            #pragma shader_feature_local _BLING_ENABLED

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
                float4 _BaseMap_ST;
                float4 _BaseColor;

                float _ToonThreshold;
                float _ToonSmoothness;
                float4 _ToonLitColor;
                float4 _ToonShadowColor;
                float4 _AmbientColor;
                float _ToonOverallStrength;

                TEXTURE2D(_OuterIrisMask); SAMPLER(sampler_OuterIrisMask);
                float4 _OuterIrisMask_ST;
                float4 _OuterIrisColor;
                float _OuterIrisThreshold;
                float _OuterIrisSmoothness;

                TEXTURE2D(_PupilMask); SAMPLER(sampler_PupilMask);
                float4 _PupilMask_ST;

                TEXTURE2D(_SpecularMask); SAMPLER(sampler_SpecularMask);
                float4 _SpecularMask_ST;
                float4 _SpecularColor;
                float _Shininess;
                float _SpecularStrength;
                float _SpecularFresnelPower;

                TEXTURE2D(_SpecialPatternTexture); SAMPLER(sampler_SpecialPatternTexture);
                float4 _SpecialPatternTexture_ST;
                TEXTURE2D(_SpecialPatternInfluenceMask); SAMPLER(sampler_SpecialPatternInfluenceMask); // MỚI
                float4 _SpecialPatternInfluenceMask_ST; // MỚI
                float4 _SpecialPatternColor;
                float _SpecialPatternStrength;
                float2 _SpecialPatternSpeed;
                float _SpecialPatternRotationSpeed;

                TEXTURE2D(_NoiseDepthTexture); SAMPLER(sampler_NoiseDepthTexture);
                float4 _NoiseDepthTexture_ST;
                float4 _NoiseDepthColor;
                float _NoiseDepthStrength;
                float2 _NoiseDepthSpeed;

                TEXTURE2D(_BlingTexture); SAMPLER(sampler_BlingTexture);
                float4 _BlingTexture_ST;
                float4 _BlingColor;
                float _BlingStrength;
                float _BlingRotationSpeed;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = normalize(_WorldSpaceCameraPos.xyz - output.positionWS);
                return output;
            }

            // Hàm xoay UV xung quanh một điểm pivot (thường là 0.5, 0.5)
            float2 RotateAroundPivot(float2 uv, float2 pivot, float angle)
            {
                float2 rotatedUV = uv - pivot;
                float s = sin(angle);
                float c = cos(angle);
                rotatedUV = float2(rotatedUV.x * c - rotatedUV.y * s, rotatedUV.x * s + rotatedUV.y * c);
                return rotatedUV + pivot;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // Bước 1: Lấy màu mắt cơ bản từ _BaseMap và tint
                float4 baseEyeTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                float3 eyeColorRGB = baseEyeTex.rgb * _BaseColor.rgb;

                // Lấy mask cho đồng tử và viền tròng mắt
                float pupilMaskValue = SAMPLE_TEXTURE2D(_PupilMask, sampler_PupilMask, TRANSFORM_TEX(input.uv, _PupilMask)).r;
                float outerIrisMaskValue = SAMPLE_TEXTURE2D(_OuterIrisMask, sampler_OuterIrisMask, TRANSFORM_TEX(input.uv, _OuterIrisMask)).r;

                // Bước 2: Áp dụng Noise Depth Effect (nằm dưới đồng tử, chỉ ảnh hưởng iris)
                #ifdef _NOISE_DEPTH_ENABLED
                    float2 noiseDepthUV = TRANSFORM_TEX(input.uv, _NoiseDepthTexture);
                    noiseDepthUV += _Time.y * _NoiseDepthSpeed;
                    float noiseValue = SAMPLE_TEXTURE2D(_NoiseDepthTexture, sampler_NoiseDepthTexture, noiseDepthUV).r;
                    
                    float irisAreaFactor = 1.0 - pupilMaskValue; 
                    eyeColorRGB = lerp(eyeColorRGB, _NoiseDepthColor.rgb, noiseValue * _NoiseDepthStrength * irisAreaFactor);
                #endif

                // Bước 3: Áp dụng Toon Shading (cho màu mắt đã có noise)
                float3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float toonFactor = smoothstep(_ToonThreshold, _ToonThreshold + _ToonSmoothness, NdotL);
                toonFactor *= mainLight.shadowAttenuation;
                float3 toonShadedColor = lerp(_ToonShadowColor.rgb, _ToonLitColor.rgb, toonFactor);

                float3 finalColorRGB = eyeColorRGB * (toonShadedColor * _ToonOverallStrength);
                finalColorRGB += _AmbientColor.rgb;

                // Bước 4: Áp dụng Outer Iris Rim (viền tròng mắt)
                float rimFactor = smoothstep(_OuterIrisThreshold, _OuterIrisThreshold + _OuterIrisSmoothness, outerIrisMaskValue);
                finalColorRGB = lerp(finalColorRGB, _OuterIrisColor.rgb, rimFactor);

                // Bước 5: Áp dụng Special Pattern (nằm trên noise, dưới specular)
                #ifdef _SPECIAL_PATTERN_ENABLED
                    float2 patternUV = TRANSFORM_TEX(input.uv, _SpecialPatternTexture);
                    float patternAngle = _Time.y * _SpecialPatternRotationSpeed;
                    patternUV = RotateAroundPivot(patternUV, float2(0.5, 0.5), patternAngle);
                    patternUV += _Time.y * _SpecialPatternSpeed;

                    float4 specialPatternTex = SAMPLE_TEXTURE2D(_SpecialPatternTexture, sampler_SpecialPatternTexture, patternUV);

                    // MỚI: Lấy mẫu mask ảnh hưởng của pattern
                    float patternInfluenceMask = SAMPLE_TEXTURE2D(_SpecialPatternInfluenceMask, sampler_SpecialPatternInfluenceMask, TRANSFORM_TEX(input.uv, _SpecialPatternInfluenceMask)).r;
                    
                    // Pha trộn với màu cuối cùng, nhân với mask ảnh hưởng
                    // (specialPatternTex.a * _SpecialPatternStrength) là cường độ pha trộn ban đầu
                    // Nhân thêm patternInfluenceMask để giới hạn phạm vi hiển thị của pattern
                    finalColorRGB = lerp(finalColorRGB, specialPatternTex.rgb * _SpecialPatternColor.rgb, specialPatternTex.a * _SpecialPatternStrength * patternInfluenceMask);
                #endif

                // Bước 6: Áp dụng Specular Highlight
                float specMask = SAMPLE_TEXTURE2D(_SpecularMask, sampler_SpecularMask, TRANSFORM_TEX(input.uv, _SpecularMask)).r;
                float3 halfDir = normalize(mainLight.direction + input.viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                
                float specular = pow(NdotH, _Shininess);
                if (_SpecularFresnelPower > 0.001)
                {
                    float fresnel = pow(1.0 - saturate(dot(normalWS, input.viewDirWS)), _SpecularFresnelPower);
                    specular *= fresnel;
                }
                specular *= specMask;
                finalColorRGB += specular * _SpecularColor.rgb * mainLight.color * _SpecularStrength;

                // Bước 7: Áp dụng Bling Bling Glitter (cùng lớp với Specular Highlight)
                #ifdef _BLING_ENABLED
                    float2 blingUV = TRANSFORM_TEX(input.uv, _BlingTexture);
                    float blingAngle = _Time.y * _BlingRotationSpeed;
                    blingUV = RotateAroundPivot(blingUV, float2(0.5, 0.5), blingAngle);

                    float4 blingTex = SAMPLE_TEXTURE2D(_BlingTexture, sampler_BlingTexture, blingUV);
                    finalColorRGB = lerp(finalColorRGB, _BlingColor.rgb, blingTex.a * _BlingStrength);
                #endif
                
                return float4(finalColorRGB, baseEyeTex.a);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}