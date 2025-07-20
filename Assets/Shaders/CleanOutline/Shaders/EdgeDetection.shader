Shader "Hidden/Advanced Edge Detection"
{
    Properties
    {
        _OutlineThickness("Outline Thickness", Float) = 1
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)

        [Header(Sensitivities)]
        _DepthSensitivity("Depth Sensitivity", Float) = 200
        _NormalSensitivity("Normal Sensitivity", Float) = 4
        _LuminanceSensitivity("Luminance Sensitivity", Float) = 2
        _EdgeSoftness("Edge Softness", Range(0.01, 2.0)) = 1.0 // Tham số mới
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
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            float _OutlineThickness;
            float4 _OutlineColor;
            float _DepthSensitivity;
            float _NormalSensitivity;
            float _LuminanceSensitivity;
            float _EdgeSoftness;

            // -- THUẬT TOÁN SOBEL MẠNH MẼ HƠN --
            float sobelOperator(float samples[9])
            {
                // Trọng số của kernel Sobel
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

            // Overload cho vector3 (dùng cho Normal)
            float sobelOperator(float3 samples[9])
            {
                const float Gx[9] = {-1, 0, 1, -2, 0, 2, -1, 0, 1};
                const float Gy[9] = {-1, -2, -1, 0, 0, 0, 1, 2, 1};

                float3 edgeX = float3(0,0,0);
                float3 edgeY = float3(0,0,0);
                for (int i = 0; i < 9; i++)
                {
                    edgeX += samples[i] * Gx[i];
                    edgeY += samples[i] * Gy[i];
                }

                return length(float2(length(edgeX), length(edgeY)));
            }
            // -- KẾT THÚC THUẬT TOÁN SOBEL --

            float3 sampleSceneNormalsRemapped(float2 uv)
            {
                return SampleSceneNormals(uv) * 0.5 + 0.5;
            }

            float sampleSceneLuminance(float2 uv)
            {
                return dot(SampleSceneColor(uv), float3(0.299, 0.587, 0.114));
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                float2 uv = IN.texcoord;
                float2 texelSize = _ScreenParams.zw * _OutlineThickness;

                // Lấy 9 mẫu trong lưới 3x3
                float uvsX[3] = {uv.x - texelSize.x, uv.x, uv.x + texelSize.x};
                float uvsY[3] = {uv.y + texelSize.y, uv.y, uv.y - texelSize.y};
                
                float3 normalSamples[9];
                float depthSamples[9], luminanceSamples[9];
                
                int index = 0;
                for(int y = 0; y < 3; y++)
                {
                    for(int x = 0; x < 3; x++)
                    {
                        float2 sampleUV = float2(uvsX[x], uvsY[y]);
                        depthSamples[index] = SampleSceneDepth(sampleUV);
                        normalSamples[index] = sampleSceneNormalsRemapped(sampleUV);
                        luminanceSamples[index] = sampleSceneLuminance(sampleUV);
                        index++;
                    }
                }

                // Áp dụng thuật toán Sobel
                float edgeDepth = sobelOperator(depthSamples);
                float edgeNormal = sobelOperator(normalSamples);
                float edgeLuminance = sobelOperator(luminanceSamples);
                
                // Sử dụng Smoothstep để có cạnh viền mềm mại
                float depthThreshold = 1.0 / _DepthSensitivity;
                float normalThreshold = 1.0 / _NormalSensitivity;
                float luminanceThreshold = 1.0 / _LuminanceSensitivity;

                float depthEdgeResult = smoothstep(depthThreshold, depthThreshold * _EdgeSoftness, edgeDepth);
                float normalEdgeResult = smoothstep(normalThreshold, normalThreshold * _EdgeSoftness, edgeNormal);
                float luminanceEdgeResult = smoothstep(luminanceThreshold, luminanceThreshold * _EdgeSoftness, edgeLuminance);

                float combinedEdges = max(depthEdgeResult, max(normalEdgeResult, luminanceEdgeResult));

                return combinedEdges * _OutlineColor;
            }
            ENDHLSL
        }
    }
}