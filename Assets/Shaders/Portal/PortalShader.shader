// Shader by VNTALKINGTECH - The Ultimate, Art-Directed Magic Portal for URP
Shader "VNTALKINGTECH/Magic Portal Ultimate"
{
    Properties
    {
        [Header(Core Textures and Gradient)]
        _GradientTex ("Color Gradient", 2D) = "white" {}
        _NoiseTex ("Nebula Swirl Noise (Soft, Perlin)", 2D) = "white" {}
        _VeinsTex ("Energy Veins Noise (Hard, Voronoi)", 2D) = "black" {}

        [Header(Nebula Swirl Background)]
        _SwirlScale ("Nebula Scale", Range(0.1, 10)) = 2.0
        _SwirlStrength ("Nebula Swirl Strength", Range(0, 10)) = 3.0
        _SwirlSpeed ("Nebula Swirl Speed", Range(-5, 5)) = 0.5

        [Header(Energy Veins Foreground)]
        _VeinsScale ("Veins Scale", Range(1, 20)) = 8.0
        _VeinsSpeed ("Veins Flow Speed", Range(-10, 10)) = -2.0
        _VeinsThreshold ("Veins Visibility Threshold", Range(0, 1)) = 0.7
        _VeinsSoftness("Veins Edge Softness", Range(0.001, 0.2)) = 0.02
        [HDR] _VeinsColor ("Veins Color (HDR)", Color) = (1, 1, 1, 1)

        [Header(Portal Shape and Core)]
        _EdgeSoftness ("Outer Edge Softness", Range(0.01, 1.0)) = 0.4
        [HDR] _RimColor("Inner Rim Color (HDR)", Color) = (0.5, 2, 2, 1)
        _RimSize("Inner Rim Size", Range(0.01, 0.2)) = 0.02
        [HDR] _CoreColor ("Pulsating Core Color (HDR)", Color) = (2, 2, 2, 1)
        _CoreSize ("Core Size", Range(0, 0.5)) = 0.05
        _CorePulseSpeed("Core Pulse Speed", Range(0, 10)) = 3.0
        _CorePulseMagnitude("Core Pulse Magnitude", Range(0, 1)) = 0.5
        
        [Header(Overall Effects)]
        _UVDisplacementStrength("UV Distortion", Range(0, 0.2)) = 0.03
        _Glow ("Overall Glow", Float) = 1.5
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            TEXTURE2D(_NoiseTex);    SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_VeinsTex);    SAMPLER(sampler_VeinsTex);
            TEXTURE2D(_GradientTex); SAMPLER(sampler_GradientTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _NoiseTex_ST;
                float4 _VeinsColor, _RimColor, _CoreColor;
                float _SwirlScale, _SwirlStrength, _SwirlSpeed;
                float _VeinsScale, _VeinsSpeed, _VeinsThreshold, _VeinsSoftness;
                float _EdgeSoftness, _RimSize, _CoreSize, _CorePulseSpeed, _CorePulseMagnitude;
                float _UVDisplacementStrength, _Glow;
            CBUFFER_END

            Varyings vert(Attributes IN) {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _NoiseTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 1. Tọa độ & Biến cơ bản
                float2 uvFromCenter = IN.uv - 0.5;
                float dist = length(uvFromCenter);
                float angle = atan2(uvFromCenter.y, uvFromCenter.x);

                // 2. UV Distortion (Làm méo toàn bộ portal để trông "bất ổn")
                float2 distortionUV = IN.uv + _Time.y * 0.1;
                float displacement = (SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, distortionUV).r * 2.0 - 1.0) * _UVDisplacementStrength;
                float2 distortedUV = IN.uv + displacement;
                // Cập nhật lại các biến dựa trên UV đã méo
                uvFromCenter = distortedUV - 0.5;
                dist = length(uvFromCenter);
                angle = atan2(uvFromCenter.y, uvFromCenter.x);

                // 3. LỚP 1: NỀN NEBULA XOÁY MỀM MẠI
                float swirlAngle = angle + dist * _SwirlStrength + _Time.y * _SwirlSpeed;
                float2 nebulaUV = float2(cos(swirlAngle), sin(swirlAngle)) * dist * _SwirlScale;
                float nebulaNoise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, nebulaUV).r;
                half4 nebulaColor = SAMPLE_TEXTURE2D(_GradientTex, sampler_GradientTex, float2(nebulaNoise, 0.5));

                // 4. LỚP 2: TIA NĂNG LƯỢNG SẮC NÉT
                // Cho các tia chảy từ tâm ra hoặc từ ngoài vào
                float2 veinsUV = distortedUV * _VeinsScale;
                veinsUV.x += _Time.y * _VeinsSpeed; // Panning
                float veinsNoise = SAMPLE_TEXTURE2D(_VeinsTex, sampler_VeinsTex, veinsUV).r;
                // Dùng smoothstep để tạo các đường sắc nét từ noise
                float veinsMask = smoothstep(_VeinsThreshold - _VeinsSoftness, _VeinsThreshold + _VeinsSoftness, veinsNoise);
                half4 veinsColor = _VeinsColor * veinsMask;

                // 5. LỚP 3: LÕI ĐẬP THEO NHỊP
                float pulse = 1.0 + sin(_Time.y * _CorePulseSpeed) * _CorePulseMagnitude;
                float coreMask = 1.0 - smoothstep(_CoreSize * pulse, _CoreSize * pulse + 0.15, dist);
                half4 coreColor = _CoreColor * coreMask;

                // 6. LỚP 4: VIỀN SÁNG & MASK ALPHA
                // Mask alpha tổng thể để làm mờ cạnh ngoài
                float alphaMask = 1.0 - smoothstep(0.5 - _EdgeSoftness, 0.5, dist);
                // Viền sáng bên trong
                float rimMask = smoothstep(0.5 - _RimSize, 0.5, dist) - smoothstep(0.5, 0.5 + 0.01, dist);
                half4 rimColor = _RimColor * rimMask;
                
                // 7. KẾT HỢP TẤT CẢ CÁC LỚP
                // Bắt đầu với màu nebula, sau đó CỘNG thêm các lớp hiệu ứng phát sáng lên trên
                half4 finalColor = nebulaColor;
                finalColor.rgb += veinsColor.rgb * _VeinsColor.a; // Dùng alpha của màu để điều khiển cường độ
                finalColor.rgb += coreColor.rgb * _CoreColor.a;
                finalColor.rgb += rimColor.rgb * _RimColor.a;

                // 8. GLOW & ALPHA CUỐI CÙNG
                finalColor.rgb *= _Glow;
                finalColor.a = nebulaColor.a * alphaMask; // Alpha cuối cùng dựa trên nền và mask rìa
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}