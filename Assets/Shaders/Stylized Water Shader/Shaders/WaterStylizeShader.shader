Shader "CleanCode/StylizedWater_GraphLogic"
{
    // Properties được đổi tên để khớp với Shader Graph
    Properties
    {
        [Header(Water Colors)]
        _Shallow_Color("Shallow Color", Color) = (0.486, 1, 0.894, 0.784)
        _Deep_Color("Deep Color", Color) = (0.07, 0.12, 0.25, 1)
        _Depth_Fade_Distance("Depth Fade Distance", Range(0.01, 20)) = 2.0
        
        [Header(Horizon)]
        _Horizon_Color("Horizon Color", Color) = (0.6, 0.8, 1, 1)
        _Horizon_Distance("Horizon Power", Range(0, 40)) = 5.0 // Đổi tên để rõ nghĩa: đây là Power của Fresnel

        [Header(Waves)]
        _Wave_Steepness("Wave Steepness", Range(0, 1)) = 0.5
        _Wave_Length("Wave Length", Range(1, 20)) = 10
        _Wave_Speed("Wave Speed", Range(0, 5)) = 1
        _Wave_Directions("Wave Directions (XY ZW)", Vector) = (0.3, 0.4, 0.6, 0.7)
        
        [Header(Normal Maps)]
        [NoScaleOffset] _Normals_Texture("Normals Texture", 2D) = "bump" {}
        _Normals_Strength("Normals Strength", Range(0, 2)) = 1
        _Normals_Scale("Normals Scale", Range(0.01, 2)) = 0.2
        _Normals_Speed("Normals Speed", Range(0, 1)) = 0.2

        [Header(Refraction)]
        _Refraction_Strength("Refraction Strength", Range(0, 0.1)) = 0.02
        _Refraction_Scale("Refraction Scale", Range(0.01, 1.0)) = 0.1
        _Refraction_Speed("Refraction Speed", Range(0, 1)) = 0.1
        
        [Header(Surface Foam)]
        [NoScaleOffset] _Surface_Foam_Texture("Surface Foam Texture", 2D) = "white" {}
        _Surface_Foam_Color("Surface Foam Color", Color) = (1,1,1,1)
        _Surface_Foam_Tiling("Surface Foam Tiling", Range(0, 100)) = 10
        _Surface_Foam_Direction("Surface Foam Direction", Range(0, 1)) = 0.5
        _Surface_Foam_Speed("Surface Foam Speed", Range(0, 1)) = 0.1
        _Surface_Foam_Distortion("Surface Foam Distortion", Range(0, 10)) = 2
        _Surface_Foam_Cutoff("Surface Foam Cutoff", Range(0, 1)) = 0.7
        _Surface_Foam_Color_Blend("Surface Foam Color Blend", Range(0, 1)) = 0.5 // Thuộc tính mới

        [Header(Intersection Foam)]
        [NoScaleOffset] _Intersection_Foam_Texture("Intersection Foam Texture", 2D) = "white" {}
        _Intersection_Foam_Color("Intersection Foam Color", Color) = (1,1,1,1)
        _Intersection_Foam_Depth("Intersection Foam Depth", Range(0.01, 5)) = 1.5
        _Intersection_Foam_Fade("Intersection Foam Fade", Range(0.01, 1)) = 0.8
        _Intersection_Foam_Tiling("Intersection Foam Tiling", Range(0, 100)) = 20
        _Intersection_Foam_Direction("Intersection Foam Direction", Range(0, 1)) = 0.25
        _Intersection_Foam_Speed("Intersection Foam Speed", Range(0, 1)) = 0.2
        _Intersection_Foam_Cutoff("Intersection Foam Cutoff", Range(0, 1)) = 0.5
        _Intersection_Foam_Color_Blend("Intersection Foam Color Blend", Range(0, 1)) = 0.5 // Thuộc tính mới

        [Header(Lighting)]
        _Specular_Color("Specular Color", Color) = (1,1,1,1)
        _Lighting_Smoothness("Lighting Smoothness", Range(0, 1)) = 0.9
        _Lighting_Hardness("Lighting Hardness", Range(0, 1)) = 0.9

        [Toggle(WORLD_SPACE_UV)] _WorldSpaceUV("World Space UV", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fog

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            
            #pragma shader_feature_local WORLD_SPACE_UV

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Shaders/Stylized Water Shader/Shaders/WaterStylizeCommon.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
                float3 normalWS     : TEXCOORD2;
                float4 tangentWS    : TEXCOORD3; 
                float4 screenPos    : TEXCOORD4;
                float3 viewDirWS    : TEXCOORD5;
                float fogCoord      : TEXCOORD6;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                float3 offset, waveNormalOS;
                GerstnerWaves_float(IN.positionOS.xyz, _Wave_Steepness, _Wave_Length, _Wave_Speed, _Wave_Directions, offset, waveNormalOS);

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz + offset);
                
                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.positionWS = positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(waveNormalOS); // Truyền normal của sóng
                
                #if defined(WORLD_SPACE_UV)
                    OUT.uv = positionWS.xz;
                #else
                    OUT.uv = IN.uv;
                #endif

                OUT.tangentWS = float4(normalize(TransformObjectToWorldDir(IN.tangentOS.xyz)), IN.tangentOS.w);
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                OUT.viewDirWS = GetWorldSpaceViewDir(positionWS);
                OUT.fogCoord = ComputeFogFactor(OUT.positionCS.z);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // --- 1. REFRACTION (Procedural Noise) & DEPTH (World Space) ---
                float2 panningNoiseUV = GetPanningUV(IN.uv, 1.0 / _Refraction_Scale, 0.5, _Refraction_Speed);
                float noise = (Unity_GradientNoise_LegacyMod_float(panningNoiseUV, 10) * 2.0 - 1.0) * _Refraction_Strength;
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float2 refractedUV = screenUV + noise;

                // World Space Depth Logic from Shader Graph
                float3 scenePosWS = GetScenePositionWS(refractedUV);
                float depthDifference = IN.positionWS.y - scenePosWS.y;
                
                // --- 2. WATER COLOR (Depth & Horizon) ---
                half depthFade = saturate(depthDifference / _Depth_Fade_Distance);
                half4 waterColor = lerp(_Deep_Color, _Shallow_Color, 1.0 - depthFade);
                
                float3 viewDir = normalize(IN.viewDirWS);
                float3 baseNormal = normalize(IN.normalWS); // Dùng normal của sóng
                float3 detailNormalTS = GetBlendedNormals(IN.uv);
                detailNormalTS.xy *= _Normals_Strength;
                float3 bitangentWS = cross(baseNormal, IN.tangentWS.xyz) * IN.tangentWS.w;
                float3x3 TBN = float3x3(IN.tangentWS.xyz, bitangentWS, baseNormal);
                float3 finalNormalWS = normalize(mul(detailNormalTS, TBN));

                // Fresnel logic from Shader Graph
                half fresnel = pow(1.0 - saturate(dot(finalNormalWS, viewDir)), _Horizon_Distance);
                waterColor = lerp(waterColor, _Horizon_Color, fresnel);
                
                // --- 3. UNDERWATER COLOR BLEND ---
                half3 sceneColor = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, refractedUV).rgb;
                half3 finalColor = lerp(sceneColor, waterColor.rgb, waterColor.a);
                half finalAlpha = waterColor.a;

                // --- 4. FOAM ---
                // Surface Foam
                float2 distortedUV_simple = IN.uv + (detailNormalTS.xy * _Surface_Foam_Distortion * 0.01);
                half surfaceFoamMask = SAMPLE_TEXTURE2D(_Surface_Foam_Texture, sampler_linear_repeat, GetPanningUV(distortedUV_simple, _Surface_Foam_Tiling, _Surface_Foam_Direction, _Surface_Foam_Speed)).r;
                surfaceFoamMask = step(_Surface_Foam_Cutoff, surfaceFoamMask);
                half4 surfaceFoam = _Surface_Foam_Color * surfaceFoamMask;
                
                // Foam blend logic from Shader Graph
                finalColor = BlendFoam_GraphLogic(finalColor, surfaceFoam.rgb, surfaceFoam.a, _Surface_Foam_Color_Blend);
                finalAlpha = saturate(finalAlpha + surfaceFoam.a);

                // Intersection Foam
                half intersectionDepthMask = saturate(depthDifference / _Intersection_Foam_Depth);
                half intersectionFoamValue = SAMPLE_TEXTURE2D(_Intersection_Foam_Texture, sampler_linear_repeat, GetPanningUV(IN.uv, _Intersection_Foam_Tiling, _Intersection_Foam_Direction, _Intersection_Foam_Speed)).r;
                // Cutoff logic from Shader Graph
                half intersectionFoamCutoff = (1.0 - intersectionDepthMask) * _Intersection_Foam_Cutoff;
                half intersectionFoamMask = step(intersectionFoamCutoff, intersectionFoamValue);
                half intersectionFade = smoothstep(0, _Intersection_Foam_Fade, intersectionDepthMask);
                half4 intersectionFoam = _Intersection_Foam_Color * intersectionFoamMask * intersectionFade;

                // Foam blend logic from Shader Graph
                finalColor = BlendFoam_GraphLogic(finalColor, intersectionFoam.rgb, intersectionFoam.a, _Intersection_Foam_Color_Blend);
                finalAlpha = saturate(finalAlpha + intersectionFoam.a);

                // --- 5. LIGHTING ---
                float mainLightSpecular;
                MainLighting_float(finalNormalWS, IN.positionWS, viewDir, _Lighting_Smoothness, mainLightSpecular);
                mainLightSpecular = step(1.0 - _Lighting_Hardness, mainLightSpecular);
                
                float3 additionalLightSpecular;
                AdditionalLighting_float(finalNormalWS, IN.positionWS, viewDir, _Lighting_Smoothness, _Lighting_Hardness, additionalLightSpecular);
                
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                float3 specularLighting = (mainLightSpecular * mainLight.color * mainLight.shadowAttenuation * _Specular_Color.rgb) + additionalLightSpecular;
                
                finalColor += specularLighting;

                // --- 6. FOG ---
                finalColor = MixFog(finalColor, IN.fogCoord);

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On ZTest LEqual Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Assets/Shaders/Stylized Water Shader/Shaders/WaterStylizeCommon.hlsl"
            struct Attributes { 
                float4 positionOS : POSITION; 
                UNITY_VERTEX_INPUT_INSTANCE_ID 
            };
            
            struct Varyings { 
                float4 positionCS : SV_POSITION; 
                UNITY_VERTEX_OUTPUT_STEREO 
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                float3 offset, waveNormalOS;
                
                // DÒNG CẦN SỬA: Thêm dấu gạch dưới vào các biến sóng
                GerstnerWaves_float(IN.positionOS.xyz, _Wave_Steepness, _Wave_Length, _Wave_Speed, _Wave_Directions, offset, waveNormalOS);

                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz + offset);
                float3 worldNormal = TransformObjectToWorldNormal(waveNormalOS);
                float3 lightDirection = GetMainLight().direction;
                worldPos = ApplyShadowBias(worldPos, worldNormal, lightDirection);
                OUT.positionCS = TransformWorldToHClip(worldPos);
                return OUT;
            }
            half4 frag(Varyings IN) : SV_TARGET { return 0; }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}