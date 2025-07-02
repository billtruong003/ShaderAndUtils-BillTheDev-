Shader "Custom/Stylized Glass"
{
    Properties
    {
        [Header(Stylized Glass)]
        _GlassColor("Glass Color & Opacity", Color) = (0.8, 0.9, 1.0, 0.5)
        _FresnelColor("Fresnel (Edge) Color", Color) = (1,1,1,1)
        _FresnelPower("Fresnel Power", Range(1, 10)) = 5.0
        _RefractionStrength("Refraction Strength", Range(0, 0.1)) = 0.01

        [Header(Emission)]
        [Toggle(_EMISSION_ON)] _EnableEmission("Enable Emission", Float) = 0
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,1)
        _EmissionMap("Emission Map", 2D) = "black" {}
    }

    SubShader
    {
        // Rất quan trọng: Tags, Blend, và ZWrite phải được cài đặt đúng cho hiệu ứng trong suốt
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" // Render sau các vật thể Opaque
            "LightMode"="UniversalForward" 
        }
        
        Blend SrcAlpha OneMinusSrcAlpha // Chế độ hoà trộn alpha tiêu chuẩn
        ZWrite Off                     // Không ghi vào depth buffer
        Cull Back

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma shader_feature_local_fragment _EMISSION_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _GlassColor, _FresnelColor;
                float _FresnelPower, _RefractionStrength;
                float4 _EmissionColor;
            CBUFFER_END

            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);
            // Texture chứa cảnh đã được render phía sau vật thể
            TEXTURE2D_X_FLOAT(_CameraOpaqueTexture); SAMPLER(sampler_CameraOpaqueTexture);

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float2 uv           : TEXCOORD2;
            };
            
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = v.uv; // UV cho emission map
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // ================== LOGIC CHẾ ĐỘ TRANSPARENT (GLASS) ==================
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.positionWS);
                
                // 1. Tính Fresnel
                float fresnelDot = 1.0 - saturate(dot(i.normalWS, viewDir));
                float fresnel = pow(fresnelDot, _FresnelPower);
                
                // 2. Tính "khúc xạ" giả bằng cách làm méo UV của scene texture
                float2 screenUV = i.positionCS.xy / i.positionCS.w;
                float2 distortion = i.normalWS.xy * _RefractionStrength;
                float3 sceneColor = SAMPLE_TEXTURE2D_X_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + distortion, 0).rgb;

                // 3. Trộn các màu lại
                // Trộn màu kính với màu cảnh nền
                half3 finalColor = lerp(sceneColor, _GlassColor.rgb, _GlassColor.a);
                // Trộn thêm hiệu ứng Fresnel ở cạnh
                finalColor = lerp(finalColor, _FresnelColor.rgb, fresnel);
                
                // ================== LOGIC EMISSION ==================
                #if defined(_EMISSION_ON)
                    half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, i.uv).rgb * _EmissionColor.rgb;
                    finalColor += emission;
                #endif

                // Alpha cuối cùng lấy từ màu kính
                return half4(finalColor, _GlassColor.a);
            }
            ENDHLSL
        }
        
        // Pass DepthNormals không thực sự cần thiết cho vật thể trong suốt
        // vì chúng thường không đóng góp vào các hiệu ứng screen-space như outline
        // theo cách giống vật thể đục. Bạn có thể xóa pass này đi.
    }
    // Không cần Custom Editor nữa vì các thuộc tính đã rất đơn giản
}