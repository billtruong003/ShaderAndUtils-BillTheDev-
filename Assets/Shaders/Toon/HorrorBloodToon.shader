Shader "Stylized/HorrorBloodToon_Stable_Final"
{
    Properties
    {
        [Header(Base Properties)]
        _BaseMap("Base Map (Albedo)", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)
        [Space]

        [Header(Toon Shading)]
        _ToonRamp("Toon Ramp", 2D) = "white" {}
        _ShadowThreshold("Shadow Threshold", Range(0.0, 1.0)) = 0.5
        _SpecularSize("Specular Size", Range(0.0, 1.0)) = 0.1
        _SpecularColor("Specular Color", Color) = (1,1,1,1)
        [Space]

        [Header(Blood Effect)]
        _BloodMap("Blood Mask", 2D) = "white" {}
        _BloodColor("Blood Color", Color) = (0.5, 0, 0, 1)
        _BloodThreshold("Blood Coverage", Range(0, 1)) = 0.5
        _BloodNoiseScale("Blood Noise Scale", Float) = 10.0
        _BloodNoiseSpeed("Blood Noise Speed", Float) = 1.0
        [Space]

        [Header(Alpha Clipping)]
        _CutoutTex("Alpha Cutout Texture", 2D) = "white" {}
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers gles
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float3 viewDirWS    : TEXCOORD2;
                float3 worldPos     : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_ToonRamp);           SAMPLER(sampler_ToonRamp);
            TEXTURE2D(_BloodMap);           SAMPLER(sampler_BloodMap);
            TEXTURE2D(_CutoutTex);          SAMPLER(sampler_CutoutTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _ShadowThreshold;
                half _SpecularSize;
                half4 _SpecularColor;
                half4 _BloodColor;
                half _BloodThreshold;
                float _BloodNoiseScale;
                float _BloodNoiseSpeed;
                half _Cutoff;
            CBUFFER_END
            
            float random (float2 st) { return frac(sin(dot(st.xy, float2(12.9898,78.233))) * 43758.5453123); }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.worldPos = positionWS;
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half cutoutAlpha = SAMPLE_TEXTURE2D(_CutoutTex, sampler_CutoutTex, input.uv).r;
                clip(cutoutAlpha - _Cutoff);
                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 finalColor = baseTex * _BaseColor;
                Light mainLight = GetMainLight();
                float3 normalWS = normalize(input.normalWS);
                float3 lightDirWS = normalize(mainLight.direction);
                float3 viewDirWS = normalize(input.viewDirWS);
                float NdotL = dot(normalWS, lightDirWS);
                float lightIntensity = NdotL * 0.5 + 0.5;
                float shadow = lightIntensity > _ShadowThreshold ? 1.0 : 0.5;
                half3 ramp = SAMPLE_TEXTURE2D(_ToonRamp, sampler_ToonRamp, float2(shadow, 0.5)).rgb;
                half3 diffuse = ramp * mainLight.color;
                float3 halfwayDir = normalize(lightDirWS + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfwayDir));
                float specularIntensity = pow(NdotH, 128);
                float specular = specularIntensity > (1.0 - _SpecularSize) ? 1.0 : 0.0;
                half3 specularColor = specular * _SpecularColor.rgb * mainLight.color;
                finalColor.rgb *= diffuse;
                finalColor.rgb += specularColor;
                float bloodMask = SAMPLE_TEXTURE2D(_BloodMap, sampler_BloodMap, input.uv).r;
                float2 noiseUV = input.worldPos.xy * _BloodNoiseScale + _Time.y * _BloodNoiseSpeed;
                float bloodNoise = random(noiseUV) * 0.5 + 0.5;
                float bloodAmount = saturate((bloodMask * bloodNoise) - (1.0 - _BloodThreshold));
                finalColor.rgb = lerp(finalColor.rgb, _BloodColor.rgb, bloodAmount);
                half3 ambient = SampleSH(normalWS);
                finalColor.rgb += ambient;
                return finalColor;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            
            #pragma multi_compile_instancing
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers gles
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            TEXTURE2D(_CutoutTex);      SAMPLER(sampler_CutoutTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half _Cutoff;
            CBUFFER_END

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                // This is the universal, robust, and stable method for shadow casting.
                // It does not rely on any high-level helper functions that might change.
                // It gets the light information directly from the pipeline state for the shadow pass.
                float3 lightDirection = GetLightDirection(positionWS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirection));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE * positionCS.w);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE * positionCS.w);
                #endif
                
                output.positionCS = positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                half alpha = SAMPLE_TEXTURE2D(_CutoutTex, sampler_CutoutTex, input.uv).r;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}