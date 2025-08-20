Shader "CleanCode/MasterTerrainShaderVR"
{
    Properties
    {
        [Header(Surface Textures and Colors)]
        _RockAlbedo("Rock Albedo", 2D) = "white" {}
        _RockBaseColor("Rock Base Color", Color) = (1,1,1,1)
        _RockNormal("Rock Normal", 2D) = "bump" {}
        _GrassAlbedo("Grass Albedo", 2D) = "white" {}
        _GrassBaseColor("Grass Base Color", Color) = (1,1,1,1)
        _GrassNormal("Grass Normal", 2D) = "bump" {}
        _TextureTiling("Global Tiling", Float) = 1.0
        _NormalIntensity("Normal Intensity", Range(0, 2)) = 1.0

        [Header(Stylized Terrain Blending)]
        [Toggle(_POSTERIZE_BLENDING)] _EnablePosterizeBlend("Enable Posterized Blending", Float) = 0
        _AxisBlendSharpness("Axis Blend Sharpness", Range(1.0, 50.0)) = 15.0
        _GrassBlendStart("Grass Blend Start (Upwards Normal)", Range(0.0, 1.0)) = 0.5
        _GrassBlendFalloff("Grass Blend Falloff", Range(0.01, 1.0)) = 0.2
        _BlendLevels("Posterize Blend Levels", Range(2, 10)) = 3

        [Header(WorldSpace Gradient Ramp)]
        [Toggle(_WORLD_GRADIENT)] _EnableGradient("Enable World-Space Gradient", Float) = 0
        _GradientBottomColor("Gradient Bottom Color", Color) = (1,1,1,1)
        _GradientTopColor("Gradient Top Color", Color) = (1,1,1,1)
        _GradientStartHeight("Gradient Start Height", Float) = 0
        _GradientEndHeight("Gradient End Height", Float) = 100

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
            #pragma shader_feature_local _WORLD_GRADIENT
            #pragma shader_feature_local _TOON_LIGHTING
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 p: POSITION; float3 n: NORMAL; float4 t: TANGENT; };
            struct Varyings { float4 pCS: SV_POSITION; float3 wP: TEXCOORD0; float3x3 tbn: TEXCOORD1; };

            TEXTURE2D(_RockAlbedo); SAMPLER(sampler_linear_repeat); TEXTURE2D(_RockNormal); TEXTURE2D(_GrassAlbedo); TEXTURE2D(_GrassNormal);
            CBUFFER_START(UnityPerMaterial)
                float _TextureTiling, _NormalIntensity, _AxisBlendSharpness, _GrassBlendStart, _GrassBlendFalloff;
                int _BlendLevels;
                half4 _GradientBottomColor, _GradientTopColor; float _GradientStartHeight, _GradientEndHeight;
                half4 _ShadowColor, _MidtoneColor, _HighlightColor; int _CelSteps; float _CelSharpness;
                half4 _RockBaseColor, _GrassBaseColor;
            CBUFFER_END
            
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

            half4 SampleTextureTriplanar(TEXTURE2D_PARAM(tex, smp), float3 scaledPos, float3 weights)
            {
                half4 sX = SAMPLE_TEXTURE2D(tex, smp, scaledPos.zy);
                half4 sY = SAMPLE_TEXTURE2D(tex, smp, scaledPos.xz);
                half4 sZ = SAMPLE_TEXTURE2D(tex, smp, scaledPos.xy);
                return sX * weights.x + sY * weights.y + sZ * weights.z;
            }
            
            half3 GetToonLighting(half NdotL)
            {
                float celValue = floor(NdotL * _CelSteps) / (_CelSteps - 1);
                half shadowMix = smoothstep(0.33 - (1.0/_CelSharpness), 0.33 + (1.0/_CelSharpness), celValue);
                half highlightMix = smoothstep(0.66 - (1.0/_CelSharpness), 0.66 + (1.0/_CelSharpness), celValue);
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
                half4 rockAlbedo = SampleTextureTriplanar(TEXTURE2D_ARGS(_RockAlbedo, sampler_linear_repeat), scaledPos, blendWeights) * _RockBaseColor;
                half4 grassAlbedo = SampleTextureTriplanar(TEXTURE2D_ARGS(_GrassAlbedo, sampler_linear_repeat), scaledPos, blendWeights) * _GrassBaseColor;
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

                // -- World Gradient Overlay --
                #if defined(_WORLD_GRADIENT)
                    half gradFactor = saturate((IN.wP.y - _GradientStartHeight) / (_GradientEndHeight - _GradientStartHeight));
                    half3 gradColor = lerp(_GradientBottomColor.rgb, _GradientTopColor.rgb, gradFactor);
                    finalColor *= gradColor;
                #endif

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        // PASS 2: Toon Outline rendering
        Pass
        {
            Name "OutlinePass"
            Cull Front
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vertOutline
            #pragma fragment fragOutline
            #pragma shader_feature_local_fragment _TOON_OUTLINE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 pOS : POSITION; float3 nOS : NORMAL; };
            struct Varyings { float4 pCS : SV_POSITION; };

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor; float _OutlineThickness; float _ViewAngleCompensation;
            CBUFFER_END

            Varyings vertOutline(Attributes IN)
            {
                Varyings OUT;
                float3 nWS = TransformObjectToWorldNormal(IN.nOS);
                float3 vDirWS = GetWorldSpaceViewDir(TransformObjectToWorld(IN.pOS.xyz));
                float viewFactor = 1.0 - saturate(dot(normalize(vDirWS), nWS) * _ViewAngleCompensation);
                float compThick = _OutlineThickness * viewFactor;
                float4 pCS = TransformObjectToHClip(IN.pOS.xyz);
                float3 nCS = TransformWorldToHClipDir(nWS);
                float2 offset = normalize(nCS.xy) * compThick * pCS.w * 0.05;
                pCS.xy += offset;
                OUT.pCS = pCS;
                return OUT;
            }

            half4 fragOutline(Varyings IN) : SV_TARGET
            {
                #if !defined(_TOON_OUTLINE)
                    discard;
                #endif
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}