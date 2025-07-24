Shader "Hidden/AdvancedEdgeDetection"
{
    Properties {}

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ADVANCED_EDGE_DETECTION_PASS"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma shader_feature_local _ALGORITHM_ROBERTS_CROSS
            #pragma shader_feature_local _ALGORITHM_PREWITT
            #pragma shader_feature_local _ALGORITHM_SOBEL
            #pragma shader_feature_local _ALGORITHM_LAPLACIAN

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_CameraDepthTexture);
            TEXTURE2D(_CameraNormalsTexture);
            SAMPLER(sampler_point_clamp);

            float4 _OutlineColor;
            float _OutlineThickness;
            float _DepthSensitivity;
            float _NormalSensitivity;
            float _LuminanceSensitivity;
            
            float3 SampleNormal(float2 uv)
            {
                float3 rawNormal = SAMPLE_TEXTURE2D(_CameraNormalsTexture, sampler_point_clamp, uv).xyz;
                return rawNormal * 2.0 - 1.0;
            }

            float SampleDepth(float2 uv)
            {
                return Linear01Depth(SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_point_clamp, uv).r, _ZBufferParams);
            }

            float SampleLuminance(float2 uv)
            {
                return Luminance(SAMPLE_TEXTURE2D(_BlitTexture, sampler_point_clamp, uv).rgb);
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float2 texelSize = _BlitTexture_TexelSize * _OutlineThickness;

                float depthEdge = 0.0;
                float normalEdge = 0.0;
                float luminanceEdge = 0.0;

            #if defined(_ALGORITHM_ROBERTS_CROSS)
                float depthSamples[4] = {
                    SampleDepth(uv + texelSize * float2(-0.5, 0.5)), SampleDepth(uv + texelSize * float2(0.5, 0.5)),
                    SampleDepth(uv + texelSize * float2(-0.5, -0.5)), SampleDepth(uv + texelSize * float2(0.5, -0.5))
                };
                float3 normalSamples[4] = {
                    SampleNormal(uv + texelSize * float2(-0.5, 0.5)), SampleNormal(uv + texelSize * float2(0.5, 0.5)),
                    SampleNormal(uv + texelSize * float2(-0.5, -0.5)), SampleNormal(uv + texelSize * float2(0.5, -0.5))
                };
                float lumSamples[4] = {
                    SampleLuminance(uv + texelSize * float2(-0.5, 0.5)), SampleLuminance(uv + texelSize * float2(0.5, 0.5)),
                    SampleLuminance(uv + texelSize * float2(-0.5, -0.5)), SampleLuminance(uv + texelSize * float2(0.5, -0.5))
                };
                
                float d1 = depthSamples[1] - depthSamples[2]; float d2 = depthSamples[0] - depthSamples[3];
                depthEdge = sqrt(d1 * d1 + d2 * d2);
                float3 n1 = normalSamples[1] - normalSamples[2]; float3 n2 = normalSamples[0] - normalSamples[3];
                normalEdge = sqrt(dot(n1, n1) + dot(n2, n2));
                float l1 = lumSamples[1] - lumSamples[2]; float l2 = lumSamples[0] - lumSamples[3];
                luminanceEdge = sqrt(l1 * l1 + l2 * l2);

            #elif defined(_ALGORITHM_PREWITT) || defined(_ALGORITHM_SOBEL)
                float depthKernelX[9], depthKernelY[9];
                float3 normalKernelX[9], normalKernelY[9];
                float lumKernelX[9], lumKernelY[9];

                for(int i = -1; i <= 1; i++)
                {
                    for(int j = -1; j <= 1; j++)
                    {
                        int index = (i + 1) * 3 + (j + 1);
                        float2 sampleUV = uv + texelSize * float2(j, i);
                        depthKernelX[index] = depthKernelY[index] = SampleDepth(sampleUV);
                        normalKernelX[index] = normalKernelY[index] = SampleNormal(sampleUV);
                        lumKernelX[index] = lumKernelY[index] = SampleLuminance(sampleUV);
                    }
                }
                
                #if defined(_ALGORITHM_SOBEL)
                    float Gx[9] = {-1, 0, 1, -2, 0, 2, -1, 0, 1};
                    float Gy[9] = {-1,-2,-1,  0, 0, 0,  1, 2, 1};
                #else // Prewitt
                    float Gx[9] = {-1, 0, 1, -1, 0, 1, -1, 0, 1};
                    float Gy[9] = {-1,-1,-1,  0, 0, 0,  1, 1, 1};
                #endif

                float depthGx = 0, depthGy = 0;
                float3 normalGx = 0, normalGy = 0;
                float lumGx = 0, lumGy = 0;

                for(int k = 0; k < 9; k++)
                {
                    depthGx += depthKernelX[k] * Gx[k]; depthGy += depthKernelY[k] * Gy[k];
                    normalGx += normalKernelX[k] * Gx[k]; normalGy += normalKernelY[k] * Gy[k];
                    lumGx += lumKernelX[k] * Gx[k]; lumGy += lumKernelY[k] * Gy[k];
                }

                depthEdge = sqrt(depthGx * depthGx + depthGy * depthGy);
                normalEdge = sqrt(dot(normalGx, normalGx) + dot(normalGy, normalGy));
                luminanceEdge = sqrt(lumGx * lumGx + lumGy * lumGy);

            #elif defined(_ALGORITHM_LAPLACIAN)
                float centerDepth = SampleDepth(uv);
                float3 centerNormal = SampleNormal(uv);
                float centerLum = SampleLuminance(uv);

                float surroundingDepth = 0;
                float3 surroundingNormal = 0;
                float surroundingLum = 0;

                float2 offsets[8] = {
                    float2(-1,-1), float2(0,-1), float2(1,-1),
                    float2(-1, 0),              float2(1, 0),
                    float2(-1, 1), float2(0, 1), float2(1, 1)
                };

                for(int i = 0; i < 8; i++)
                {
                    float2 sampleUV = uv + texelSize * offsets[i];
                    surroundingDepth += SampleDepth(sampleUV);
                    surroundingNormal += SampleNormal(sampleUV);
                    surroundingLum += SampleLuminance(sampleUV);
                }

                depthEdge = abs(8 * centerDepth - surroundingDepth);
                normalEdge = length(8 * centerNormal - surroundingNormal);
                luminanceEdge = abs(8 * centerLum - surroundingLum);
            #endif

                float depthThreshold = 1.0 / _DepthSensitivity;
                float normalThreshold = 1.0 / _NormalSensitivity;
                float luminanceThreshold = 1.0 / _LuminanceSensitivity;
                
                depthEdge = step(depthThreshold, depthEdge);
                normalEdge = step(normalThreshold, normalEdge);
                luminanceEdge = step(luminanceThreshold, luminanceEdge);
                
                float finalEdge = max(depthEdge, max(normalEdge, luminanceEdge));
                
                half4 outline = _OutlineColor * finalEdge;
                outline.a = saturate(outline.a);
                
                return outline;
            }
            ENDHLSL
        }
    }
}