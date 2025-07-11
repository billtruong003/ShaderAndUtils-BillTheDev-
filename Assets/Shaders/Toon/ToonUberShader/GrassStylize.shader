Shader "Stylized/URP/Grass Terrain Toon (No Outline)"
{
    Properties
    {
        // --- Texture & Alpha Clip ---
        [Header(Texture and Alpha Clipping)]
        _MainTex("Grass Texture (A = Opacity)", 2D) = "white" {}
        _AlphaCutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        // --- Color Properties ---
        [Header(Color Settings)]
        _BaseColor("Base Color", Color) = (0.2, 0.5, 0.1, 1.0)
        _TipColor("Tip Color", Color) = (0.5, 0.8, 0.2, 1.0)
        _ShadowColor("Shadow Color", Color) = (0.1, 0.3, 0.05, 1.0)
        _ColorHeight("Color Height", Range(0, 5)) = 1.0

        // --- Toon Shading Properties ---
        [Header(Toon Shading)]
        _CelShadingThreshold("Cel Shading Threshold", Range(0, 1)) = 0.5

        // --- Wind Properties ---
        [Header(Wind Effect)]
        _WindSpeed("Wind Speed", Range(0, 5)) = 1.0
        _WindStrength("Wind Strength", Range(0, 1)) = 0.1
        _WindScale("Wind Scale", Range(0.1, 10)) = 2.0
    }

    SubShader
    {
        Tags { 
            "RenderPipeline" = "UniversalRenderPipeline" 
            "RenderType"="TransparentCutout"
            "Queue"="AlphaTest"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off // Tốt cho cỏ 2D

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                sampler2D _MainTex;
                float4 _MainTex_ST;
                float _AlphaCutoff;
                float4 _BaseColor;
                float4 _TipColor;
                float4 _ShadowColor;
                float _ColorHeight;
                float _CelShadingThreshold;
                float _WindSpeed;
                float _WindStrength;
                float _WindScale;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float2 uv           : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                
                // --- Wind Effect ---
                float windOffsetX = sin(_Time.y * _WindSpeed + OUT.positionWS.x * _WindScale) * _WindStrength;
                float windOffsetZ = cos(_Time.y * _WindSpeed + OUT.positionWS.z * _WindScale) * _WindStrength;
                OUT.positionWS.x += windOffsetX;
                OUT.positionWS.z += windOffsetZ;

                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Lấy màu từ texture
                float4 texColor = tex2D(_MainTex, IN.uv);

                // Thực hiện Alpha Clipping
                clip(texColor.a - _AlphaCutoff);

                // --- Color Variation by Height ---
                float heightFactor = saturate(IN.positionWS.y / _ColorHeight);
                float4 baseColor = lerp(_BaseColor, _TipColor, heightFactor);

                // Kết hợp màu của shader với màu của texture
                baseColor.rgb *= texColor.rgb;

                // --- Toon Lighting (Cel-Shading) ---
                Light mainLight = GetMainLight();
                float3 normalDir = normalize(IN.normalWS);
                float NdotL = dot(normalDir, mainLight.direction);
                float lightIntensity = step(_CelShadingThreshold, NdotL);

                // Kết hợp màu bóng đổ và màu cơ bản, nhân với màu của ánh sáng
                float3 finalColor = lerp(_ShadowColor.rgb, baseColor.rgb, lightIntensity) * mainLight.color;
                
                // Thêm một chút ambient light để vùng tối không bị đen hoàn toàn
                // Bạn có thể nhân _ShadowColor với một giá trị nhỏ hoặc dùng SampleSH
                finalColor += _ShadowColor.rgb * 0.1;

                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}