Shader "ShaderAndUtils/Internal/PbrLitBaker"
{
    Properties
    {
        _AlbedoMap ("Albedo", 2D) = "white" {}
        _NormalMap ("Normal", 2D) = "bump" {}
        _MetallicMap ("Metallic", 2D) = "black" {}
        _RoughnessMap("Roughness", 2D) = "white" {}
        _AoMap ("Occlusion", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Cull Off ZWrite Off ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma editor_sync_compilation

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_AlbedoMap);      SAMPLER(sampler_AlbedoMap);
            TEXTURE2D(_NormalMap);      SAMPLER(sampler_NormalMap);
            TEXTURE2D(_MetallicMap);    SAMPLER(sampler_MetallicMap);
            TEXTURE2D(_RoughnessMap);   SAMPLER(sampler_RoughnessMap);
            TEXTURE2D(_AoMap);          SAMPLER(sampler_AoMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _LightDirection;
                float4 _LightColor;
                float4 _AmbientColor;
            CBUFFER_END

            Varyings vert(Attributes input) {
                Varyings output;
                output.positionCS = float4(input.vertex.xy * 2.0 - 1.0, 0.0, 1.0);
                output.uv = input.uv;
                return output;
            }

            float4 frag(Varyings input) : SV_Target {
                float3 albedo = SAMPLE_TEXTURE2D(_AlbedoMap, sampler_AlbedoMap, input.uv).rgb;
                float3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));
                float metallic = SAMPLE_TEXTURE2D(_MetallicMap, sampler_MetallicMap, input.uv).r;
                float roughness = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, input.uv).r;
                float ao = SAMPLE_TEXTURE2D(_AoMap, sampler_AoMap, input.uv).r;

                SurfaceData surfaceData;
                surfaceData.albedo = albedo;
                surfaceData.metallic = metallic;
                surfaceData.specular = float3(0.5, 0.5, 0.5); // Not used in this PBR model
                surfaceData.smoothness = 1.0 - roughness;
                surfaceData.normalTS = normalTS;
                surfaceData.occlusion = ao;
                surfaceData.emission = float3(0,0,0);
                
                InputData inputData;
                inputData.positionWS = float3(input.uv.x, input.uv.y, 0); // Dummy value
                inputData.normalWS = normalize(float3(normalTS.x, normalTS.y, normalTS.z));
                inputData.viewDirectionWS = float3(0, 0, 1); // Orthographic view for baking
                inputData.shadowCoord = float4(0,0,0,0);
                inputData.fogCoord = 0;

                Light mainLight;
                mainLight.direction = normalize(_LightDirection.xyz);
                mainLight.color = _LightColor.rgb;
                mainLight.distanceAttenuation = 1.0;
                mainLight.shadowAttenuation = 1.0;

                float3 finalColor = GlobalIllumination(BRDFData(surfaceData), inputData, surfaceData.occlusion, mainLight);
                finalColor += _AmbientColor.rgb * albedo * ao;

                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}