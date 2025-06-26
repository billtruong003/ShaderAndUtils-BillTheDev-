Shader "Tutorial/SimpleHologram"
{
    Properties
    {
        [Header(Hologram Properties)]
        _BaseColor("Base Color (RGBA)", Color) = (0.2, 0.8, 1, 0.1) // Màu chính và độ trong suốt gốc
        [HDR] _FresnelColor("Fresnel (Edge) Color", Color) = (0.5, 0.9, 1, 1) // Màu của viền sáng
        _FresnelPower("Fresnel Power", Range(0.1, 10.0)) = 2.5 // Độ dày của viền sáng
        
        [Header(Scanline Properties)]
        _ScanlineTiling("Scanline Tiling", Float) = 10.0 // Mật độ của các đường sọc
        _ScanlineSpeed("Scanline Speed", Float) = -2.0 // Tốc độ và hướng di chuyển
        _ScanlineStrength("Scanline Strength", Range(0, 1)) = 0.5 // Độ đậm nhạt của đường sọc
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
        }

        Pass
        {
            // Thiết lập chế độ blending cho vật thể trong suốt
            Blend SrcAlpha OneMinusSrcAlpha 
            // Không cull mặt sau, để hologram hiển thị cả mặt trong
            Cull Off
            // Không ghi vào Z-buffer để các vật thể trong suốt khác có thể vẽ chồng lên
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0; // Giữ lại uvเผื่อ cần dùng sau
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
            };

            // Khai báo các thuộc tính từ Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _FresnelColor;
                float _FresnelPower;
                float _ScanlineTiling;
                float _ScanlineSpeed;
                float _ScanlineStrength;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                // --- 1. Tính toán hiệu ứng Fresnel (Viền sáng) ---
                // Lấy hướng nhìn từ camera đến điểm ảnh trên bề mặt
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.positionWS);
                // Chuẩn hóa vector pháp tuyến của bề mặt
                float3 normalDirection = normalize(i.normalWS);
                
                // Tích vô hướng để xác định góc nhìn
                // dot product gần 0 khi nhìn vào cạnh, gần 1 khi nhìn trực diện
                float fresnelDot = dot(viewDirection, normalDirection);
                // Dùng 1 trừ đi để đảo ngược, làm cho cạnh sáng lên
                float fresnelTerm = 1.0 - saturate(fresnelDot);
                // Dùng hàm pow để kiểm soát độ dày và sắc nét của viền
                float fresnel = pow(fresnelTerm, _FresnelPower);

                // --- 2. Tính toán hiệu ứng Scanline (Quét dòng) ---
                // Tạo tọa độ cho các dòng quét dựa trên vị trí thế giới (world position)
                // Nhân với _Time để làm chúng di chuyển
                float scanlineCoord = i.positionWS.y * _ScanlineTiling + _Time.y * _ScanlineSpeed;
                // Dùng sin để tạo ra các dải sóng mềm mại, sau đó dùng pow để làm chúng sắc nét hơn
                float scanlineWave = pow(abs(sin(scanlineCoord)), 40.0) * _ScanlineStrength;

                // --- 3. Kết hợp màu sắc và độ trong suốt ---
                // Màu phát sáng (emission) là sự kết hợp của màu viền và màu sọc
                float3 emissionColor = fresnel * _FresnelColor.rgb + scanlineWave * _BaseColor.rgb * 2.0;

                // Độ trong suốt (alpha) được quyết định bởi màu gốc, và tăng lên ở các viền và sọc
                float finalAlpha = saturate(_BaseColor.a + fresnel + scanlineWave);
                
                // Kết quả cuối cùng: Màu phát sáng và độ trong suốt
                return float4(emissionColor, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Transparent"
}