Shader "Hidden/Advanced Edge Detection"
{
    Properties
    {
        _OutlineThickness("Outline Thickness", Float) = 1
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)

        [Header(Sensitivities)]
        _DepthSensitivity("Depth Sensitivity", Float) = 1
        _NormalSensitivity("Normal Sensitivity", Float) = 1
        _LuminanceSensitivity("Luminance Sensitivity", Float) = 1

        [Header(Appearance)]
        _OutlineSoftness("Outline Softness", Range(0.01, 1.0)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ADVANCED_EDGE_DETECTION_OUTLINE"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float _OutlineThickness;
            float4 _OutlineColor;
            float _DepthSensitivity;
            float _NormalSensitivity;
            float _LuminanceSensitivity;
            float _OutlineSoftness;

            float sobelOperator(float samples[9])
            {
                const float Gx[9] = {-1, 0, 1, -2, 0, 2, -1, 0, 1};
                const float Gy[9] = {-1, -2, -1, 0, 0, 0, 1, 2, 1};

                float edgeX = 0;
                float edgeY = 0;
                for (int i = 0; i < 9; i++)
                {
                    edgeX += samples[i] * Gx[i];
                    edgeY += samples[i] * Gy[i];
                }

                return sqrt(edgeX * edgeX + edgeY * edgeY);
            }

            float sobelOperator(float3 samples[9])
            {
                const float Gx[9] = {-1, 0, 1, -2, 0, 2, -1, 0, 1};
                const float Gy[9] = {-1, -2, -1, 0, 0, 0, 1, 2, 1};

                float3 edgeX = float3(0, 0, 0);
                float3 edgeY = float3(0, 0, 0);
                for (int i = 0; i < 9; i++)
                {
                    edgeX += samples[i] * Gx[i];
                    edgeY += samples[i] * Gy[i];
                }

                return length(float2(length(edgeX), length(edgeY)));
            }

            float3 getSceneNormals(float2 uv)
            {
                return SampleSceneNormals(uv);
            }
            
            float getSceneLuminance(float2 uv)
            {
                float3 sceneColor = SampleSceneColor(uv);
                return dot(sceneColor, float3(0.299, 0.587, 0.114));
            }
            
            float getLinearEyeDepth(float2 uv)
            {
                return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
            }

            half4 Frag(Varyings IN) : SV_TARGET
            {
                float2 uv = IN.texcoord;
                float2 texelSize = _ScreenParams.zw * _OutlineThickness;

                float uvsX[3] = {uv.x - texelSize.x, uv.x, uv.x + texelSize.x};
                float uvsY[3] = {uv.y + texelSize.y, uv.y, uv.y - texelSize.y};
                
                float3 normalSamples[9];
                float depthSamples[9];
                float luminanceSamples[9];
                
                int index = 0;
                for(int y = 0; y < 3; y++)
                {
                    for(int x = 0; x < 3; x++)
                    {
                        float2 sampleUV = float2(uvsX[x], uvsY[y]);
                        depthSamples[index] = getLinearEyeDepth(sampleUV);
                        normalSamples[index] = getSceneNormals(sampleUV);
                        luminanceSamples[index] = getSceneLuminance(sampleUV);
                        index++;
                    }
                }

                float sobelDepth = sobelOperator(depthSamples) * _DepthSensitivity;
                float sobelNormal = sobelOperator(normalSamples) * _NormalSensitivity;
                float sobelLuminance = sobelOperator(luminanceSamples) * _LuminanceSensitivity;
                
                float maxSobel = max(sobelDepth, max(sobelNormal, sobelLuminance));
                
                float edgeFactorThreshold = 1.0 - _OutlineSoftness;
                float edgeFactor = smoothstep(edgeFactorThreshold, 1.0, maxSobel);

                float4 finalColor = _OutlineColor;
                finalColor.a *= edgeFactor;

                return finalColor;
            }
            ENDHLSL
        }
    }
}