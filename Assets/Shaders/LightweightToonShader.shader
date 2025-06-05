Shader "Custom/URP/LightweightToon"
{
    Properties
    {
        [Header(Base Properties)]
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)

        [Header(Toon Shading)]
        _ToonThreshold ("Toon Threshold", Range(0,1)) = 0.5 // Ngưỡng chuyển đổi từ bóng sang sáng
        _ToonSmoothness ("Toon Smoothness", Range(0.001,0.5)) = 0.05 // Độ mượt của chuyển đổi
        [HDR] _ToonLitColor ("Toon Lit Color", Color) = (1,1,1,1) // Màu phần sáng
        [HDR] _ToonShadowColor ("Toon Shadow Color", Color) = (0.5,0.5,0.5,1) // Màu phần bóng
        [HDR] _AmbientColor ("Ambient Color (Overall)", Color) = (0.2, 0.2, 0.2, 1) // Màu ánh sáng môi trường chung
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back // Render mặt trước của đối tượng

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fw_and_shadows // Bật hỗ trợ ánh sáng chính và đổ bóng

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
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                float3 normalWS = normalize(input.normalWS);

                // Lấy thông tin ánh sáng chính (Main Light)
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));

                // Tính toán dot product giữa pháp tuyến và hướng ánh sáng (saturate để giới hạn từ 0 đến 1)
                float NdotL = saturate(dot(normalWS, mainLight.direction));

                // Áp dụng Toon Shading bằng smoothstep
                // toonFactor = 0 (bóng) khi NdotL <= _ToonThreshold
                // toonFactor = 1 (sáng) khi NdotL >= _ToonThreshold + _ToonSmoothness
                // Có một khoảng chuyển đổi mượt giữa 0 và 1
                float toonFactor = smoothstep(_ToonThreshold, _ToonThreshold + _ToonSmoothness, NdotL);

                // Áp dụng đổ bóng (shadows) từ ánh sáng chính
                toonFactor *= mainLight.shadowAttenuation;

                // Nội suy giữa màu bóng và màu sáng dựa trên toonFactor
                float3 toonShadedColor = lerp(_ToonShadowColor.rgb, _ToonLitColor.rgb, toonFactor);

                // Màu cuối cùng: màu gốc * màu toon + màu môi trường
                float3 finalColor = baseColor.rgb * toonShadedColor + _AmbientColor.rgb;

                return float4(finalColor, baseColor.a);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError" // Fallback cho URP
}