Shader "CleanCode/AdvancedToonTerrainVR"
{
    Properties
    {
        [Header(Surface Textures)]
        _RockAlbedo("Rock Albedo", 2D) = "white" {}
        _RockNormal("Rock Normal", 2D) = "bump" {}
        _GrassAlbedo("Grass Albedo", 2D) = "white" {}
        _GrassNormal("Grass Normal", 2D) = "bump" {}
        _TextureTiling("Global Tiling", Float) = 1.0
        _NormalIntensity("Normal Intensity", Range(0, 2)) = 1.0

        [Header(Stylized Terrain Blending)]
        [Toggle(_POSTERIZE_BLENDING)] _EnablePosterizeBlend("Enable Posterized Blending", Float) = 0
        _AxisBlendSharpness("Axis Blend Sharpness", Range(1.0, 50.0)) = 15.0
        _GrassBlendStart("Grass Blend Start (Upwards Normal)", Range(0.0, 1.0)) = 0.5
        _GrassBlendFalloff("Grass Blend Falloff", Range(0.01, 1.0)) = 0.2
        _BlendLevels("Posterize Blend Levels", Range(2, 10)) = 3

        [Header(Toon Lighting Control)]
        [Toggle(_TOON_LIGHTING)] _EnableToonLighting("Enable Toon Lighting", Float) = 0
        _ShadowColor("Toon Shadow Color", Color) = (0.2, 0.2, 0.5, 1.0)
        _MidtoneColor("Toon Midtone Color", Color) = (0.5, 0.5, 0.8, 1.0)
        _HighlightColor("Toon Highlight Color", Color) = (0.9, 0.9, 1.0, 1.0)
        _CelSteps("Cel Shading Steps", Range(2, 8)) = 3
        _CelSharpness("Cel Step Sharpness", Range(1, 50)) = 20.0

        [Header(Dynamic Toon Outline)]
        [Toggle(_TOON_OUTLINE)] _EnableOutline("Enable Toon Outline", Float) = 0
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness("Outline Thickness", Range(0.001, 0.1)) = 0.01
        _ViewAngleCompensation("Outline View Angle Compensation", Range(0.0, 1.0)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        // PASS 1: Main surface rendering
        Pass
        {
            Name "MainPass"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local _POSTERIZE_BLENDING
            #pragma shader_feature_local _TOON_LIGHTING
            
            // Re-using the previous optimized includes and structures
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 p: POSITION; float3 n: NORMAL; float4 t: TANGENT; };
            struct Varyings { float4 pCS: SV_POSITION; float3 wP: TEXCOORD0; float3x3 tbn: TEXCOORD1; };

            // Structures and CBuffer from previous shader version are re-used
            TEXTURE2D(_RockAlbedo); SAMPLER(sampler_linear_repeat); TEXTURE2D(_RockNormal); TEXTURE2D(_GrassAlbedo); TEXTURE2D(_GrassNormal);
            CBUFFER_START(UnityPerMaterial)
                float _TextureTiling, _NormalIntensity, _AxisBlendSharpness, _GrassBlendStart, _GrassBlendFalloff;
                int _BlendLevels;
                half4 _ShadowColor, _MidtoneColor, _HighlightColor;
                int _CelSteps; float _CelSharpness;
            CBUFFER_END
            
            // Function prototypes from previous shader are re-used here
            // We only need to show the new/modified ones for clarity
            half4 SampleTextureTriplanar(TEXTURE2D_PARAM(tex, smp), float3 scaledPos, float3 weights)
            {
                half4 sX = SAMPLE_TEXTURE2D(tex, smp, scaledPos.zy);
                half4 sY = SAMPLE_TEXTURE2D(tex, smp, scaledPos.xz);
                half4 sZ = SAMPLE_TEXTURE2D(tex, smp, scaledPos.xy);
                return sX * weights.x + sY * weights.y + sZ * weights.z;
            }
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.pCS = TransformObjectToHClip(IN.p.xyz);
                OUT.wP = TransformObjectToWorld(IN.p.xyz);
                float3 nWS = TransformObjectToWorldNormal(IN.n);
                float3 tWS = TransformObjectToWorldDir(IN.t.xyz);
                float3 bWS = cross(nWS, tWS) * IN.t.w;
                OUT.tbn = float3x3(tWS, bWS, nWS);
                return OUT;
            }

            half3 GetToonLighting(half NdotL)
            {
                float celValue = floor(NdotL * _CelSteps) / (_CelSteps - 1);
                
                half shadowMix = smoothstep(0.33 - (1.0 / _CelSharpness), 0.33 + (1.0 / _CelSharpness), celValue);
                half highlightMix = smoothstep(0.66 - (1.0 / _CelSharpness), 0.66 + (1.0 / _CelSharpness), celValue);

                half3 rampedLight = lerp(_ShadowColor.rgb, _MidtoneColor.rgb, shadowMix);
                return lerp(rampedLight, _HighlightColor.rgb, highlightMix);
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                // -- Triplanar Blending --
                float3 proceduralNormal = normalize(cross(ddy(IN.wP), ddx(IN.wP)));
                float3 blendWeights = pow(abs(proceduralNormal), _AxisBlendSharpness);
                blendWeights /= dot(blendWeights, 1.0);
                float3 scaledPos = IN.wP / _TextureTiling;

                // -- Surface Sampling --
                half4 rockAlbedo = SampleTextureTriplanar(TEXTURE2D_ARGS(_RockAlbedo, sampler_linear_repeat), scaledPos, blendWeights);
                half4 grassAlbedo = SampleTextureTriplanar(TEXTURE2D_ARGS(_GrassAlbedo, sampler_linear_repeat), scaledPos, blendWeights);
                half3 rockN = UnpackNormal(SampleTextureTriplanar(TEXTURE2D_ARGS(_RockNormal, sampler_linear_repeat), scaledPos, blendWeights));
                half3 grassN = UnpackNormal(SampleTextureTriplanar(TEXTURE2D_ARGS(_GrassNormal, sampler_linear_repeat), scaledPos, blendWeights));

                // -- Posterized Blending Logic --
                float3 vertexNormal = normalize(IN.tbn[2]);
                float upwardDot = dot(vertexNormal, float3(0, 1, 0));
                float grassMask = smoothstep(_GrassBlendStart - _GrassBlendFalloff, _GrassBlendStart + _GrassBlendFalloff, upwardDot);
                #if defined(_POSTERIZE_BLENDING)
                    grassMask = floor(grassMask * _BlendLevels) / _BlendLevels;
                #endif

                // -- Final Surface Properties --
                half4 albedo = lerp(rockAlbedo, grassAlbedo, grassMask);
                half3 tangentNormal = lerp(rockN, grassN, grassMask);
                tangentNormal = lerp(half3(0,0,1), tangentNormal, _NormalIntensity);
                half3 finalNormalWS = normalize(TransformTangentToWorld(tangentNormal, IN.tbn));

                // -- Lighting Calculation --
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(finalNormalWS, mainLight.direction));
                half3 ambient = SampleSH(finalNormalWS);
                
                half3 lightContribution = NdotL * mainLight.color;
                #if defined(_TOON_LIGHTING)
                    lightContribution = GetToonLighting(NdotL) * mainLight.color;
                #endif

                half3 finalColor = (ambient + lightContribution) * albedo.rgb;
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        // PASS 2: Toon Outline rendering
        Pass
        {
            Name "OutlinePass"
            Cull Front

            HLSLPROGRAM
            #pragma vertex vertOutline
            #pragma fragment fragOutline
            #pragma shader_feature_local_fragment _TOON_OUTLINE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineThickness;
                float _ViewAngleCompensation;
            CBUFFER_END

            Varyings vertOutline(Attributes IN)
            {
                Varyings OUT;
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float3 viewDirWS = GetWorldSpaceViewDir(TransformObjectToWorld(IN.positionOS.xyz));
                
                // Compensate thickness based on view angle to prevent blobby outlines when viewed head-on
                float viewAngleFactor = 1.0 - saturate(dot(normalize(viewDirWS), normalWS) * _ViewAngleCompensation);
                float compensatedThickness = _OutlineThickness * viewAngleFactor;

                float4 positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                float3 normalCS = TransformWorldToHClipDir(normalWS);
                
                // Push vertex in screen space for consistent thickness
                float2 offset = normalize(normalCS.xy) * compensatedThickness * positionCS.w * 0.05;
                positionCS.xy += offset;
                
                OUT.positionCS = positionCS;
                return OUT;
            }

            // --- FIX START ---
            half4 fragOutline(Varyings IN) : SV_TARGET
            {
                #if defined(_TOON_OUTLINE)
                    return _OutlineColor;
                #else
                    discard;
                    // Add a dummy return to satisfy the Metal compiler.
                    // This code is unreachable but required for compilation.
                    return half4(0, 0, 0, 0); 
                #endif
            }
            // --- FIX END ---
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}