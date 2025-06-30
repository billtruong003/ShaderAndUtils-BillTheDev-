Shader "Custom/Toon Uber Shader"
{
    Properties
    {
        // ================== CONTROLLER ==================
        [Header(Surface Pipeline)]
        [Enum(Opaque, 0, Transparent, 1, Metallic, 2)] _SurfaceType ("Surface Type", Float) = 0

        // ================== OPAQUE & METALLIC BASE PROPERTIES ==================
        [Header(Base Properties)]
        _BaseMap("Albedo Texture", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        // ================== OPAQUE PROPERTIES ==================
        [Header(Toon Shading)]
        _ToonRampOffset("Toon Ramp Offset", Range(0.0, 1.0)) = 0.5
        _ToonRampSmoothness("Toon Ramp Smoothness", Range(0.001, 1.0)) = 0.05
        _ShadowTint("Shadow Tint", Color) = (0.1, 0.1, 0.2, 1.0)
        
        // ================== STYLIZED METALLIC PROPERTIES (TỪ SHADER CỦA BẠN) ==================
        [Header(Stylized Metal)]
        _Ramp ("Toon Ramp (RGB)", 2D) = "white" {} 
        _Brightness("Specular Brightness", Range(0, 2)) = 1.3  
        _Offset("Specular Size", Range(0, 1)) = 0.8
        _SpecuColor("Specular Color", Color) = (0.8,0.45,0.2,1)
        [Header(Highlight)]
        _HighlightOffset("Highlight Size", Range(0, 1)) = 0.9  
        _HiColor("Highlight Color", Color) = (1,1,1,1)
        [Header(Rim)]
        _RimColor("Rim Color", Color) = (1,0.3,0.3,1)
        _RimPower("Rim Power", Range(0, 20)) = 6

        // ================== TRANSPARENT (GLASS) PROPERTIES ==================
        [Header(Stylized Glass)]
        _GlassColor("Glass Color & Opacity", Color) = (0.8, 0.9, 1.0, 0.5)
        _FresnelColor("Fresnel (Edge) Color", Color) = (1,1,1,1)
        _FresnelPower("Fresnel Power", Range(1, 10)) = 5.0
        _RefractionStrength("Refraction Strength", Range(0, 0.1)) = 0.01

        // ================== SHARED PROPERTIES ==================
        [Header(Emission)]
        [Toggle(_EMISSION_ON)] _EnableEmission("Enable Emission", Float) = 0
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,1)
        _EmissionMap("Emission Map", 2D) = "black" {}

        // Properties for Custom Editor to control render state
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0
    }

    SubShader
    {
        Pass
        {
            Name "ForwardLit"
            Tags { "RenderType"="Opaque" "Queue"="Geometry" "LightMode"="UniversalForward" }
            
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _SURFACETYPE_OPAQUE _SURFACETYPE_TRANSPARENT _SURFACETYPE_METALLIC
            #pragma shader_feature_local_fragment _EMISSION_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/Includes/ToonShading.hlsl" 

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _ToonRampOffset, _ToonRampSmoothness;
                float4 _ShadowTint;
                float4 _GlassColor, _FresnelColor;
                float _FresnelPower, _RefractionStrength;
                float4 _EmissionColor;
                // Metal Properties
                float _Brightness, _Offset, _HighlightOffset, _RimPower;
                float4 _SpecuColor, _HiColor, _RimColor;
            CBUFFER_END

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);
            TEXTURE2D(_Ramp); SAMPLER(sampler_Ramp);
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
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half3 finalColor = 0;
                half finalAlpha = 1;

                // ================== LOGIC CHẾ ĐỘ OPAQUE ==================
                #if defined(_SURFACETYPE_OPAQUE)
                    half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                    
                    float3 directLighting;
                    float3 mainLightDirection;
                    float mainLightAttenuation;
                    ToonShading_float(i.normalWS, _ToonRampSmoothness, i.positionCS, i.positionWS, _ShadowTint, _ToonRampOffset, directLighting, mainLightDirection, mainLightAttenuation);

                    float3 indirectLighting = SampleSH(i.normalWS);
                    finalColor = albedo.rgb * (directLighting + indirectLighting);
                    finalAlpha = albedo.a;
                #endif

                // ================== LOGIC CHẾ ĐỘ METALLIC MỚI ==================
                #if defined(_SURFACETYPE_METALLIC)
                    Light mainLight = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                    float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.positionWS);

                    // 1. Ánh sáng khuếch tán (Diffuse) dựa trên Ramp Texture
                    half d = dot(i.normalWS, mainLight.direction) * 0.5 + 0.5;
                    half3 ramp = SAMPLE_TEXTURE2D(_Ramp, sampler_Ramp, float2(d,d)).rgb;
                    half3 diffuseColor = _BaseColor.rgb * mainLight.color * ramp * (mainLight.shadowAttenuation * 2);
                    finalColor = diffuseColor;

                    // 2. Lớp Specular
                    float3 halfVec = normalize(viewDir + mainLight.direction);
                    float specDot = saturate(dot(halfVec, i.normalWS));
                    float specStep = step(_Offset, specDot); // Tạo ra cạnh cứng
                    finalColor += specStep * _SpecuColor.rgb * _Brightness * mainLight.color * mainLight.shadowAttenuation;
                    
                    // 3. Lớp Highlight (sáng hơn và nhỏ hơn Specular)
                    float highlightDot = saturate(dot(i.normalWS, mainLight.direction));
                    float highlightStep = step(_HighlightOffset, highlightDot);
                    finalColor += highlightStep * _HiColor.rgb * mainLight.color * mainLight.shadowAttenuation;

                    // 4. Rim Light (thêm vào như Emission)
                    half rim = 1.0 - saturate(dot(viewDir, i.normalWS));
                    finalColor += _RimColor.rgb * pow(rim, _RimPower);
                    
                    finalAlpha = _BaseColor.a;
                #endif
                
                // ================== LOGIC CHẾ ĐỘ TRANSPARENT (GLASS) ==================
                #if defined(_SURFACETYPE_TRANSPARENT)
                    float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.positionWS);
                    float fresnelDot = 1.0 - saturate(dot(i.normalWS, viewDir));
                    float fresnel = pow(fresnelDot, _FresnelPower);
                    
                    float2 screenUV = i.positionCS.xy / i.positionCS.w;
                    float2 distortion = i.normalWS.xy * _RefractionStrength;
                    float3 sceneColor = SAMPLE_TEXTURE2D_X_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + distortion, 0).rgb;

                    finalColor = lerp(sceneColor, _GlassColor.rgb, _GlassColor.a);
                    finalColor = lerp(finalColor, _FresnelColor.rgb, fresnel);
                    finalAlpha = _GlassColor.a;
                #endif

                // ================== LOGIC EMISSION (Dùng chung) ==================
                #if defined(_EMISSION_ON)
                    half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, i.uv).rgb * _EmissionColor.rgb;
                    finalColor += emission;
                #endif

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }

        // ===================================================================
        // PASS 2: DEPTH NORMALS (Cung cấp dữ liệu cho Screen-space Outline)
        // ===================================================================
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 normalWS     : TEXCOORD1;
            };

            Varyings DepthNormalsVert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 DepthNormalsFrag(Varyings input) : SV_TARGET
            {
                return half4(normalize(input.normalWS) * 0.5 + 0.5, 0);
            }
            ENDHLSL
        }
    }
    CustomEditor "ToonUberShaderGUI"
}