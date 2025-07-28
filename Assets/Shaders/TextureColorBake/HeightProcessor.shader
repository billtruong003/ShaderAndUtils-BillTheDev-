Shader "ShaderAndUtils/Internal/HeightProcessor"
{
    Properties
    {
        _MainTex ("Height Map (R)", 2D) = "gray" {}
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
            
            #pragma shader_feature_local _BAKE_NORMAL
            #pragma shader_feature_local _BAKE_CURVATURE
            #pragma shader_feature_local _BAKE_AO
            #pragma shader_feature_local _BAKE_METALLIC

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            struct Attributes {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            
            CBUFFER_START(UnityPerMaterial)
                float _NormalStrength;
                float _CurvatureRadius, _CurvatureStrength;
                int _AoSamples;
                float _AoRadius, _AoStrength;
                float _MetallicLow, _MetallicHigh, _MetallicContrast;
            CBUFFER_END
            
            float SampleHeight(float2 uv) {
                return SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, 0).r;
            }
            
            float random(float2 st) {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }

            Varyings vert(Attributes input) {
                Varyings output;
                output.positionCS = float4(input.vertex.xy * 2.0 - 1.0, 0.0, 1.0);
                output.uv = input.uv;
                return output;
            }

            float4 FragNormal(Varyings input) {
                float2 texel = _MainTex_TexelSize.xy;
                float tl = SampleHeight(input.uv + float2(-texel.x,  texel.y));
                float t  = SampleHeight(input.uv + float2( 0,       texel.y));
                float tr = SampleHeight(input.uv + float2( texel.x,  texel.y));
                float l  = SampleHeight(input.uv + float2(-texel.x,  0));
                float r  = SampleHeight(input.uv + float2( texel.x,  0));
                float bl = SampleHeight(input.uv + float2(-texel.x, -texel.y));
                float b  = SampleHeight(input.uv + float2( 0,      -texel.y));
                float br = SampleHeight(input.uv + float2( texel.x, -texel.y));

                float dX = (tr + 2.0 * r + br) - (tl + 2.0 * l + bl);
                float dY = (bl + 2.0 * b + br) - (tl + 2.0 * t + tr);
                
                float3 normalVector = normalize(float3(dX * _NormalStrength, dY * _NormalStrength, 1.0));
                return float4(normalVector * 0.5 + 0.5, 1.0);
            }

            float4 FragCurvature(Varyings input) {
                float2 texelSize = _MainTex_TexelSize.xy * _CurvatureRadius;
                float center = SampleHeight(input.uv);
                float top = SampleHeight(input.uv + float2(0, texelSize.y));
                float bottom = SampleHeight(input.uv - float2(0, texelSize.y));
                float left = SampleHeight(input.uv - float2(texelSize.x, 0));
                float right = SampleHeight(input.uv + float2(texelSize.x, 0));
                
                float ddx = right + left - 2 * center;
                float ddy = top + bottom - 2 * center;

                float curvature = (ddx + ddy) * _CurvatureStrength * 0.5 + 0.5;
                return float4(curvature.xxx, 1.0);
            }

            float4 FragAO(Varyings input) {
                float centerHeight = SampleHeight(input.uv);
                float2 texelSize = _MainTex_TexelSize.xy;
                float totalOcclusion = 0;
                float randomStartAngle = random(input.uv) * TWO_PI;
                
                for (int i = 0; i < _AoSamples; i++) {
                    float angle = randomStartAngle + ((float)i / (float)_AoSamples) * TWO_PI;
                    float2 sampleUv = input.uv + float2(cos(angle), sin(angle)) * texelSize * _AoRadius;
                    totalOcclusion += saturate((SampleHeight(sampleUv) - centerHeight) * _AoStrength);
                }
                
                float ao = 1.0 - saturate(totalOcclusion / float(_AoSamples));
                return float4(ao, ao, ao, 1.0);
            }
            
            float4 FragMetallic(Varyings input) {
                float height = SampleHeight(input.uv);
                float contrastedHeight = pow(height, _MetallicContrast);
                float metallic = lerp(_MetallicLow, _MetallicHigh, contrastedHeight);
                return float4(metallic, metallic, metallic, 1.0);
            }

            float4 frag(Varyings input) : SV_Target {
                #if _BAKE_NORMAL
                    return FragNormal(input);
                #elif _BAKE_CURVATURE
                    return FragCurvature(input);
                #elif _BAKE_AO
                    return FragAO(input);
                #elif _BAKE_METALLIC
                    return FragMetallic(input);
                #endif
                return float4(1, 0, 1, 1);
            }
            ENDHLSL
        }
    }
}