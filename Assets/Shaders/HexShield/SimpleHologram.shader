Shader "Tutorial/SimpleHologram"
{
    Properties
    {
        [Header(Hologram Properties)]
        _BaseColor("Base Color (RGBA)", Color) = (0.2, 0.8, 1, 0.1) // Màu chính và độ trong suốt gốc
        [HDR] _FresnelColor("Fresnel (Edge) Color", Color) = (0.5, 0.9, 1, 1) // Màu của viền sáng
        _FresnelPower("Fresnel Power", Range(0.1, 10.0)) = 2.5 // Độ dày của viền sáng
        [MainTexture] _BaseTexture("Base Texture", 2D) = "white" {} // Texture nền
        _TextureStrength("Texture Strength", Range(0, 1)) = 0.5 // Cường độ texture
        
        [Header(Scanline Properties)]
        _ScanlineTiling("Scanline Tiling", Float) = 10.0 // Mật độ của các đường sọc
        _ScanlineSpeed("Scanline Speed", Float) = -2.0 // Tốc độ và hướng di chuyển
        _ScanlineStrength("Scanline Strength", Range(0, 1)) = 0.5 // Độ đậm nhạt của đường sọc
        
        [Header(Glitch and Flicker)]
        _FlickerSpeed("Flicker Speed", Range(0, 20)) = 5.0 // Tốc độ nhấp nháy
        _FlickerIntensity("Flicker Intensity", Range(0, 1)) = 0.3 // Cường độ nhấp nháy
        _GlitchIntensity("Glitch Intensity", Range(0, 1)) = 0.05 // Cường độ nhiễu
        _GlitchSpeed("Glitch Speed", Range(0, 50)) = 10.0 // Tốc độ nhiễu
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
            Blend SrcAlpha OneMinusSrcAlpha 
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _FresnelColor;
                float _FresnelPower;
                float _ScanlineTiling;
                float _ScanlineSpeed;
                float _ScanlineStrength;
                float _FlickerSpeed;
                float _FlickerIntensity;
                float _GlitchIntensity;
                float _GlitchSpeed;
                float _TextureStrength;
            CBUFFER_END

            // Khai báo texture
            TEXTURE2D(_BaseTexture);
            SAMPLER(sampler_BaseTexture);

            // Hàm tạo nhiễu đơn giản
            float random(float2 input)
            {
                return frac(sin(dot(input, float2(12.9898, 78.233))) * 43758.5453);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                // Thêm hiệu ứng nhiễu vào vị trí đỉnh
                float glitch = _GlitchIntensity * sin(_Time.y * _GlitchSpeed + v.positionOS.y);
                v.positionOS.x += glitch * step(0.5, random(float2(v.positionOS.y, _Time.y)));
                
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = v.uv;
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                // --- 1. Lấy mẫu texture ---
                float4 texColor = SAMPLE_TEXTURE2D(_BaseTexture, sampler_BaseTexture, i.uv);
                float3 baseTextureColor = texColor.rgb * _TextureStrength;

                // --- 2. Tính toán hiệu ứng Fresnel ---
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.positionWS);
                float3 normalDirection = normalize(i.normalWS);
                float fresnelDot = dot(viewDirection, normalDirection);
                float fresnelTerm = 1.0 - saturate(fresnelDot);
                float fresnel = pow(fresnelTerm, _FresnelPower);

                // --- 3. Tính toán hiệu ứng Scanline ---
                float scanlineCoord = i.positionWS.y * _ScanlineTiling + _Time.y * _ScanlineSpeed;
                float scanlineWave = pow(abs(sin(scanlineCoord)), 20.0) * _ScanlineStrength;

                // --- 4. Tính toán hiệu ứng nhấp nháy ---
                float flicker = 1.0 - _FlickerIntensity * (0.5 + 0.5 * sin(_Time.y * _FlickerSpeed + random(i.uv)));

                // --- 5. Kết hợp màu sắc và độ trong suốt ---
                // Kết hợp texture với màu cơ bản
                float3 baseColor = baseTextureColor * _BaseColor.rgb;
                float3 emissionColor = fresnel * _FresnelColor.rgb + scanlineWave * baseColor * 2.0;
                emissionColor *= flicker; // Áp dụng nhấp nháy

                float finalAlpha = saturate(_BaseColor.a + fresnel + scanlineWave) * flicker * texColor.a;

                return float4(emissionColor, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Transparent"
}