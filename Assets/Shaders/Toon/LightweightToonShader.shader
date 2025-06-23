Shader "Custom/ToonShaderFromGraph"
{
    Properties
    {
        [Header(Base Properties)]
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Color ("Color Tint", Color) = (1,1,1,1)
        _MaskTex("Mask (R Channel)", 2D) = "white" {}

        [Header(Toon Lighting)]
        [HDR] _LightColor("Light Color", Color) = (1,1,1,1)
        [HDR] _ShadowColor("Shadow Color", Color) = (0.5, 0.5, 0.5, 1)
        _ToonThreshold("Toon Threshold", Range(0, 1)) = 0.5
        _ToonSmoothness("Toon Smoothness", Range(0, 0.5)) = 0.05

        [Header(Rim Light)]
        [HDR] _RimColor("Rim Color", Color) = (1,1,1,1)
        _RimPower("Rim Power", Range(0.1, 10)) = 3.0
        _RimThreshold("Rim Threshold", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        // Pass 1: Pass chính để render màu sắc
        Pass
        {
            // ... (Giữ nguyên Pass "UniversalForward") ...
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _LightColor;
                float4 _ShadowColor;
                float _ToonThreshold;
                float _ToonSmoothness;
                float4 _RimColor;
                float _RimPower;
                float _RimThreshold;
            CBUFFER_END

            TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);
            TEXTURE2D(_MaskTex);        SAMPLER(sampler_MaskTex);

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float3 positionWS   : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                half3 lightDir = normalize(mainLight.direction);
                half3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));

                half NdotL = dot(normalWS, lightDir);
                half toonFactor = smoothstep(_ToonThreshold - _ToonSmoothness, _ToonThreshold + _ToonSmoothness, NdotL);
                half3 toonLight = lerp(_ShadowColor.rgb, _LightColor.rgb, toonFactor);

                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;
                half mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv).r;
                half3 maskedAlbedo = albedo.rgb * mask;

                half NdotV = dot(normalWS, viewDir);
                half fresnel = 1.0 - saturate(NdotV);
                half rimFactor = pow(fresnel, _RimPower);
                rimFactor = step(_RimThreshold, rimFactor);
                half3 rimLight = rimFactor * _RimColor.rgb * _RimColor.a;

                half3 finalColor = (maskedAlbedo * toonLight) + rimLight;
                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }
        
        // Pass 2: Pass đặc biệt để ghi thông tin Depth và Normals
        Pass
        {
            Name "DepthNormals"
            Tags{"LightMode" = "DepthNormals"}

            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            // Dọn dẹp include, chỉ giữ lại file cần thiết nhất
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // <<< XÓA DÒNG INCLUDE SHADOWS.HLSL >>>
            // #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0; // Giữ lại để tránh lỗi biên dịch nếu struct được dùng ở nơi khác
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 normalWS     : TEXCOORD0;
            };

            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output;
                // Sử dụng hàm TransformObjectToHClip thay cho TransformObjectToWorldClipPos để đơn giản hóa
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 DepthNormalsFragment(Varyings input) : SV_TARGET
            {
                float3 normalWS = normalize(input.normalWS);
                // Mã hóa normal vào dải [0,1]
                return half4(normalWS * 0.5 + 0.5, 1);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}