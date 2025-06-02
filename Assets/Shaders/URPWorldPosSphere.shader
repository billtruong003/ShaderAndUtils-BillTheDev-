Shader "Custom/URPWorldPosSphere"
{
    Properties
    {
        _MainTex ("Primary (RGB)", 2D) = "white" {}
        _SecondTex ("Secondary (RGB)", 2D) = "white" {}
        [Header(Noise)]
        _NoiseTex ("Noise", 2D) = "white" {}
        _NScale ("Noise Scale", Range(0, 10)) = 1
        _NoiseSpeed ("Noise Speed", Range(0, 5)) = 1  // Thêm tham số tốc độ noise
        _NoiseCutoff ("Noise Radius Cutoff", Range(-1, 1)) = 0.5
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.5
        [Header(Line)]
        _LineWidth ("Line Width", Range(0, 2)) = 0.1
        [HDR]_LineColor ("Line Color", Color) = (1,1,1,1)
        [KeywordEnum(SwapTextures, Appear, Disappear)] _STYLE ("Style", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        LOD 200

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _STYLE_SWAPTEXTURES _STYLE_APPEAR _STYLE_DISAPPEAR
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_SecondTex);
            SAMPLER(sampler_SecondTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            // Biến toàn cục để nhận từ PivotInput
            uniform float3 _Position;
            uniform float _Radius;

            CBUFFER_START(UnityPerMaterial)
                float _NoiseCutoff;
                float _NScale;
                float _NoiseSpeed;  // Thêm vào CBUFFER
                float _LineWidth;
                float4 _LineColor;
                float _NoiseStrength;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.vertex = TransformObjectToHClip(IN.vertex.xyz);
                OUT.uv = IN.uv;
                OUT.worldPos = TransformObjectToWorld(IN.vertex.xyz);
                OUT.worldNormal = TransformObjectToWorldNormal(IN.normal);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Tính toán hình cầu
                float dis = distance(_Position, IN.worldPos);
                float sphereR = 1 - saturate(dis / _Radius);

                // Noise triplanar
                float3 blendNormal = saturate(pow(IN.worldNormal * 1.4, 4));
                half4 nSide1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, (IN.worldPos.xy + _Time.x * _NoiseSpeed) * _NScale);
                half4 nSide2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, (IN.worldPos.xz + _Time.x * _NoiseSpeed) * _NScale);
                half4 nTop = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, (IN.worldPos.yz + _Time.x * _NoiseSpeed) * _NScale);

                // Pha trộn noise, fallback nếu không có texture
                float3 noisetexture = nSide1.rgb;
                noisetexture = lerp(noisetexture, nTop.rgb, blendNormal.x);
                noisetexture = lerp(noisetexture, nSide2.rgb, blendNormal.y);
                noisetexture = any(noisetexture.rgb) ? noisetexture : float3(0.5, 0.5, 0.5);

                // Kết hợp noise với hình cầu
                float sphereNoise = lerp(noisetexture.r * sphereR, sphereR, _NoiseStrength);
                float radiusCutoff = step(_NoiseCutoff, sphereNoise);

                // Viền phát sáng (sử dụng emission logic)
                float edgeLine = step(sphereNoise - _LineWidth, _NoiseCutoff) * radiusCutoff;
                float3 colouredLine = edgeLine * _LineColor.rgb * 2;  // Tăng cường độ để mô phỏng emission glow

                // Chuyển đổi texture
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 c2 = SAMPLE_TEXTURE2D(_SecondTex, sampler_SecondTex, IN.uv);
                c = c.a == 0 ? half4(1,1,1,1) : c;
                c2 = c2.a == 0 ? half4(1,1,1,1) : c2;

                float3 combinedTex;
                #if defined(_STYLE_APPEAR) || defined(_STYLE_DISAPPEAR)
                    combinedTex = c.rgb;  // Chỉ dùng _MainTex trong Appear và Disappear
                #else
                    combinedTex = lerp(c.rgb, c2.rgb, radiusCutoff);  // SwapTextures dùng cả hai texture
                #endif

                // Ánh sáng toon
                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;
                #ifndef USING_DIRECTIONAL_LIGHT
                    lightDir = normalize(lightDir);
                #endif
                float d = dot(normalize(IN.worldNormal), lightDir);
                float3 lightIntensity = smoothstep(0, 0.1, d);
                float3 lightColor = mainLight.color.rgb == float3(0,0,0) ? float3(1,1,1) : mainLight.color.rgb;
                float atten = mainLight.distanceAttenuation == 0 ? 1 : mainLight.distanceAttenuation;
                float3 albedo = combinedTex * lightIntensity * lightColor * atten * 2;

                // Kết hợp kết quả với emission cho viền sáng
                float3 resultTex = albedo;
                resultTex += colouredLine;  // Thêm emission cho viền sáng, độc lập với ánh sáng

                // Cắt alpha cho Appear/Disappear
                #if defined(_STYLE_APPEAR)
                    clip(radiusCutoff - 0.01);
                #elif defined(_STYLE_DISAPPEAR)
                    clip(1 - (radiusCutoff - edgeLine) - 0.01);  // Sửa logic clip cho Disappear
                #endif

                return half4(resultTex, c.a);
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Lit"
}