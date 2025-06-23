
// Shader "Custom/URP/ToonShaderInteractive_URP_Modern"
Shader "Custom/URP/ToonShaderInteractive_URP_Modern"
{
    Properties
    {
        [Header(Toon Shading)]
        _MainTex("Base Texture", 2D) = "white" {}
        _Color("Base Color", Color) = (1,1,1,1)
        _ToonRampSmoothness("Shadow Smoothness", Range(0.001, 1)) = 0.05
        _ShadowTint("Shadow Tint", Color) = (0.6, 0.6, 0.8, 1)

        [Header(Selection Outline)]
        _OutlineColor("Outline Color", Color) = (1, 0.8, 0, 1)
        _OutlineWidth("Outline Width", Range(0, 0.1)) = 0.02
        _EnableOutline("Enable Outline", Float) = 0.0

        [Header(Selection Rim Effect)]
        [HDR] _RimColor("Rim Color", Color) = (1, 0.8, 0, 1)
        _RimPower("Rim Power", Range(0.1, 10.0)) = 3.0
        _RimEnable("Enable Rim", Float) = 0.0

        [Header(Flash Effect)]
        [HDR] _FlashColor("Flash Color", Color) = (1,1,1,1)
        _FlashAmount("Flash Amount", Range(0,1)) = 0.0

        [Header(Dissolve Effect)]
        _DissolveNoiseTex("Dissolve Noise Texture", 2D) = "white" {}
        _DissolveAmount("Dissolve Amount", Range(0,1.01)) = 0.0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }

        // Pass 1: Vẽ Outline
        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            CBUFFER_START(UnityPerMaterial)
                float _OutlineWidth;
                float4 _OutlineColor;
                float _EnableOutline;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                if (_EnableOutline > 0.5)
                {
                    float4 positionOS = input.positionOS;
                    positionOS.xyz += normalize(input.normalOS) * _OutlineWidth;
                    output.positionCS = TransformObjectToHClip(positionOS.xyz);
                }
                else { output.positionCS = float4(2, 2, 2, 1); }
                return output;
            }
            half4 frag(Varyings input) : SV_TARGET { return _OutlineColor; }
            ENDHLSL
        }

        Pass
        {
            Name "ToonLit_Opaque_Modern"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
                float2 lightmapUV   : TEXCOORD1; // Thêm để hỗ trợ lightmap
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float2 uv           : TEXCOORD2;
                float2 lightmapUV   : TEXCOORD3; // Thêm để truyền lightmap UV
                float3 vertexSH     : TEXCOORD4; // Thêm để truyền spherical harmonics
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _ToonRampSmoothness;
                half4 _ShadowTint;
                half4 _RimColor;
                half _RimPower;
                half _RimEnable;
                half4 _FlashColor;
                half _FlashAmount;
                float _DissolveAmount;
            CBUFFER_END

            TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);
            TEXTURE2D(_DissolveNoiseTex); SAMPLER(sampler_DissolveNoiseTex);

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.lightmapUV = input.lightmapUV * unity_LightmapST.xy + unity_LightmapST.zw; // Xử lý lightmap UV
                OUTPUT_SH(output.normalWS, output.vertexSH); // Tính spherical harmonics
                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                // --- 1. DISSOLVE (Tùy chọn, giữ nguyên nếu cần) ---
                half noise = SAMPLE_TEXTURE2D(_DissolveNoiseTex, sampler_DissolveNoiseTex, input.uv).r;
                clip(noise - _DissolveAmount);

                // --- 2. TÍNH TOÁN ÁNH SÁNG ---
                half3 normalWS = normalize(input.normalWS);
                half3 viewDirectionWS = SafeNormalize(_WorldSpaceCameraPos.xyz - input.positionWS);

                // Khởi tạo InputData
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirectionWS;
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS); // Tính shadowCoord
                inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, normalWS);

                // Sửa lỗi: Gọi GetMainLight không tham số
                Light mainLight = GetMainLight();
                half shadow = mainLight.shadowAttenuation; // Lấy bóng từ ánh sáng chính

                // Tính toán ánh sáng toon
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half shadowFactor = smoothstep(0.5, 0.5 - _ToonRampSmoothness, NdotL) * shadow;
                
                half3 ambient = inputData.bakedGI;
                half3 lighting = ambient + (mainLight.color * shadowFactor);

                // --- 3. MÀU SẮC CƠ BẢN ---
                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;
                half3 finalColor = baseColor.rgb * lighting;

                // Thêm màu cho vùng tối (tùy chọn)
                finalColor = lerp(finalColor, finalColor * _ShadowTint.rgb, 1.0 - shadowFactor);

                // --- 4. HIỆU ỨNG (Tùy chọn) ---
                half rimDot = 1.0 - saturate(dot(viewDirectionWS, normalWS));
                half rim = pow(rimDot, _RimPower);
                finalColor += _RimColor.rgb * rim * _RimEnable;

                finalColor = lerp(finalColor, _FlashColor.rgb, _FlashAmount);

                return half4(finalColor, baseColor.a);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}