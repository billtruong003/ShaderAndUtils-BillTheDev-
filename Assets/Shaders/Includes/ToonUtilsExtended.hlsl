Shader "Custom/Toon Uber Shader"
{
    Properties
    {
        [Header(Render Mode)]
        [Toggle(_DITHER_FADE_ON)] _EnableDitherFade("Enable Dither Fade (X-Ray)", Float) = 0
        _FadeAmount("Fade Amount", Range(0.0, 1.0)) = 1.0

        [Header(Base Properties)]
        _BaseMap("Albedo Texture", 2D) = "white" {}
        _BaseColor("Albedo Color & Alpha", Color) = (1, 1, 1, 1)

        [Header(Toon Properties)]
        _ToonRampOffset("Toon Ramp Offset", Range(0.0, 1.0)) = 0.5
        _ToonRampSmoothness("Toon Ramp Smoothness", Range(0.001, 1.0)) = 0.05
        _ShadowTint("Shadow Tint", Color) = (0.1, 0.1, 0.2, 1.0)
        
        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0, 5)) = 1

        [Header(Metallic Properties)]
        [Toggle(_METALLIC_ON)] _EnableMetallic("Enable Metallic", Float) = 0
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _SpecularTint("Specular Tint", Color) = (1,1,1,1)

        [Header(Emission Properties)]
        [Toggle(_EMISSION_ON)] _EnableEmission("Enable Emission", Float) = 0
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,1)

        // Thuộc tính ẩn cho chế độ Transparent
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0
    }

    SubShader
    {
        // ======================================================
        // PASS 1: OUTLINE (Vẽ viền)
        // ======================================================
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front // Chỉ render mặt sau của model

            HLSLPROGRAM
            #pragma vertex vert_outline
            #pragma fragment frag_outline
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };
            
            Varyings vert_outline(Attributes v)
            {
                Varyings o;
                // Đẩy đỉnh ra xa theo normal vector
                float4 pos = v.positionOS;
                pos.xyz += normalize(v.normalOS) * _OutlineWidth * 0.01;
                o.positionCS = TransformObjectToWorld(pos.xyz);
                o.positionCS = TransformWorldToHClip(o.positionCS);
                return o;
            }

            half4 frag_outline(Varyings i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // ======================================================
        // PASS 2: MAIN FORWARD LIT (Vẽ chính)
        // ======================================================
        Pass
        {
            Name "ForwardLit"
            Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalRenderPipeline" "Queue"="Geometry" "LightMode"="UniversalForward" }
            
            // Lấy các giá trị blend từ properties
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Bật các keyword để điều khiển tính năng
            #pragma shader_feature_local _METALLIC_ON
            #pragma shader_feature_local _EMISSION_ON
            #pragma shader_feature_local _DITHER_FADE_ON

            // Các multi_compile cần thiết của URP
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            // File include của bạn, giả sử nó chứa hàm ToonLighting_float và Dither
            #include "Assets/Shaders/Includes/ToonShading.hlslinc"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _ToonRampOffset;
                float _ToonRampSmoothness;
                float4 _ShadowTint;
                float _Metallic;
                float _Smoothness;
                float4 _SpecularTint;
                float4 _EmissionColor;
                float _FadeAmount;
            CBUFFER_END

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap); // Giả sử có emission map

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
                o.normalWS = normalize(TransformObjectToWorldNormal(v.normalOS));
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // --- Dither Fade (X-Ray) ---
                #if _DITHER_FADE_ON
                    // Áp dụng dither để "đục lỗ" pixel, tạo hiệu ứng trong suốt
                    Dither(i.positionCS.xy, _FadeAmount);
                #endif

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                
                // --- Tính toán ánh sáng ---
                float3 directLighting;
                ToonLighting_float(i.normalWS, i.positionWS, _ToonRampOffset, _ToonRampSmoothness, _ShadowTint.rgb, directLighting);
                
                float3 indirectLighting = SampleSH(i.normalWS);
                half3 finalColor = albedo.rgb * (directLighting + indirectLighting);

                // --- Tính toán Metallic ---
                #if _METALLIC_ON
                    float3 viewDir = normalize(_WorldSpaceCameraPos - i.positionWS);
                    float3 reflectDir = reflect(-viewDir, i.normalWS);
                    half4 reflection = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectDir, PerceptualRoughnessToMipmapLevel(1.0 - _Smoothness));
                    
                    Light mainLight = GetMainLight();
                    float3 halfVec = normalize(mainLight.direction + viewDir);
                    float NdotH = saturate(dot(i.normalWS, halfVec));
                    float specularStep = smoothstep(0.95 - _Smoothness * 0.9, 0.99 - _Smoothness * 0.9, NdotH);
                    
                    float3 specularContribution = specularStep * mainLight.color * _SpecularTint.rgb * mainLight.shadowAttenuation;
                    
                    finalColor += reflection.rgb * _Metallic * indirectLighting; // Phản chiếu bị ảnh hưởng bởi ánh sáng môi trường
                    finalColor += specularContribution * _Metallic; // Blik sáng chỉ từ đèn chính
                #endif

                // --- Tính toán Emission ---
                #if _EMISSION_ON
                    finalColor += _EmissionColor.rgb; // Đơn giản là cộng màu vào
                #endif

                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }
    }
    // Custom Editor để tự động set Blend Mode
    CustomEditor "Custom.ToonUberShaderGUI"
}