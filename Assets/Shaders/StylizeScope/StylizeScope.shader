Shader "Unlit/ScopeURP_Complete"
{
    Properties
    {
        [Header(Base Properties)]
        _BaseMap("Base Map (UV0)", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)
        
        [Header(Scope Properties)]
        _ReticleTexture("Reticle (UV1)", 2D) = "white" {}
        _ReticleColor("Reticle Color", Color) = (1,0,0,1)
        _LensMask("Lens Mask (R Channel)", 2D) = "white" {}

        [Header(Scope Effects)]
        _Zoom("Zoom", Range(1.0, 15.0)) = 2.0
        _Fisheye("Fisheye Distortion", Range(0.0, 1.0)) = 0.2
        _ChromaticAberration("Chromatic Aberration", Range(0.0, 0.05)) = 0.01
        
        [Header(Image Quality)]
        _Sharpness("Sharpness", Range(0.0, 1.0)) = 0.5

        [Header(Render Settings)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }

    SubShader
    {
        // Chuyển sang hàng đợi Transparent để đảm bảo shader chạy sau khi Opaque Texture được tạo.
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; float4 _BaseColor; float4 _ReticleColor;
                float _Zoom; float _Fisheye; float _ChromaticAberration; float _Sharpness;
            CBUFFER_END

            TEXTURE2D(_CameraOpaqueTexture); SAMPLER(sampler_CameraOpaqueTexture);
            float4 _CameraOpaqueTexture_TexelSize;
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_ReticleTexture); SAMPLER(sampler_ReticleTexture);
            TEXTURE2D(_LensMask); SAMPLER(sampler_LensMask);

            struct Attributes {
                float4 positionOS   : POSITION;
                float2 uv0          : TEXCOORD0;
                float2 uv1          : TEXCOORD1;
                float3 normalOS     : NORMAL;
            };

            struct Varyings {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float2 uv0          : TEXCOORD1;
                float2 uv1          : TEXCOORD2;
                float4 screenPos    : TEXCOORD3;
                float3 normalWS     : TEXCOORD4;
                float4 shadowCoord  : TEXCOORD5;
            };

            Varyings vert(Attributes i) {
                Varyings o = (Varyings)0;
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.uv0 = TRANSFORM_TEX(i.uv0, _BaseMap);
                o.uv1 = i.uv1;
                o.screenPos = ComputeScreenPos(o.positionCS);
                o.shadowCoord = TransformWorldToShadowCoord(o.positionWS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target {
                half mask = SAMPLE_TEXTURE2D(_LensMask, sampler_LensMask, i.uv1).r;
                clip(mask - 0.5);

                float2 scopeUV_centered = i.uv1 * 2.0 - 1.0;
                float scopeRadius = length(scopeUV_centered);
                float fisheyeFactor = 1.0 - scopeRadius * _Fisheye;
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float2 zoomedUV = (screenUV - 0.5) / (_Zoom * fisheyeFactor) + 0.5;

                float2 caOffset = scopeUV_centered * _ChromaticAberration;
                half sceneR = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, zoomedUV + caOffset).r;
                half sceneG = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, zoomedUV).g;
                half sceneB = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, zoomedUV - caOffset).b;
                half4 scopeColor = half4(sceneR, sceneG, sceneB, 1.0);

                if (_Sharpness > 0.0) {
                    float2 texelSize = _CameraOpaqueTexture_TexelSize.xy;
                    half4 centerPixel = scopeColor;
                    half4 topPixel = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, zoomedUV + float2(0, texelSize.y));
                    half4 bottomPixel = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, zoomedUV - float2(0, texelSize.y));
                    half4 leftPixel = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, zoomedUV - float2(texelSize.x, 0));
                    half4 rightPixel = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, zoomedUV + float2(texelSize.x, 0));
                    half4 sharpenedColor = centerPixel * 5.0 - (topPixel + bottomPixel + leftPixel + rightPixel);
                    scopeColor = lerp(scopeColor, sharpenedColor, _Sharpness);
                }

                half4 reticleSample = SAMPLE_TEXTURE2D(_ReticleTexture, sampler_ReticleTexture, i.uv1);
                half4 reticleColor = reticleSample * _ReticleColor;
                scopeColor.rgb = lerp(scopeColor.rgb, reticleColor.rgb, reticleColor.a);

                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv0) * _BaseColor;
                float3 normalWS = normalize(i.normalWS);
                Light mainLight = GetMainLight(i.shadowCoord);
                half3 ambient = SampleSH(normalWS);
                half3 lighting = mainLight.color * mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                lighting *= max(0.0, dot(normalWS, mainLight.direction));
                lighting += ambient;
                baseColor.rgb *= lighting;

                return lerp(baseColor, scopeColor, mask);
            }
            ENDHLSL
        }
        
        Pass {
            Name "ShadowCaster" Tags { "LightMode" = "ShadowCaster" }
            Cull [_Cull] ZWrite On ZTest LEqual
            HLSLPROGRAM
            #pragma vertex vert #pragma fragment frag #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Attributes { float4 p:POSITION; float3 n:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 p:SV_POSITION; };
            Varyings vert(Attributes i) {
                Varyings o; UNITY_SETUP_INSTANCE_ID(i);
                float3 posWS = TransformObjectToWorld(i.p.xyz); float3 nrmWS = TransformObjectToWorldNormal(i.n);
                o.p = TransformWorldToHClip(ApplyShadowBias(posWS, nrmWS, _MainLightPosition.xyz));
                #if UNITY_REVERSED_Z
                o.p.z=min(o.p.z,o.p.w*UNITY_NEAR_CLIP_VALUE);
                #else
                o.p.z=max(o.p.z,o.p.w*UNITY_NEAR_CLIP_VALUE);
                #endif
                return o;
            }
            half4 frag(Varyings i) : SV_TARGET { return 0; }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}