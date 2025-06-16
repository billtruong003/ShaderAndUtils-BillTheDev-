Shader "Hidden/ScreenSpaceOutlines"
{
    Properties
    {
        _OutlineScale ("Outline Scale", Float) = 1.0
        _RobertsCrossMultiplier ("Roberts Cross Multiplier", Float) = 100.0
        _DepthThreshold ("Depth Threshold", Float) = 10.0
        _NormalThreshold ("Normal Threshold", Float) = 0.4
        _SteepAngleThreshold ("Steep Angle Threshold", Float) = 0.2
        _SteepAngleMultiplier ("Steep Angle Multiplier", Float) = 25.0
        _OutlineColor ("Outline Color", Color) = (0, 0.888169, 1, 1)
        [ToggleUI] _UseNormalOutline ("Use Normal Outline", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        
        Pass
        {
            Name "DrawProcedural"
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _OutlineScale;
                float _RobertsCrossMultiplier;
                float _DepthThreshold;
                float _NormalThreshold;
                float _SteepAngleThreshold;
                float _SteepAngleMultiplier;
                float4 _OutlineColor;
                float _UseNormalOutline;
            CBUFFER_END

            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture_PointClamp);

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                float4 position : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                
                // Generate fullscreen triangle
                float2 uv = float2(input.vertexID & 1, input.vertexID >> 1);
                output.positionCS = float4(uv * 2.0 - 1.0, 0.0, 1.0);
                output.uv = uv;
                
                // Compute view direction
                float4 worldPos = mul(unity_MatrixInvVP, output.positionCS);
                worldPos.xyz /= worldPos.w;
                output.viewDir = worldPos.xyz - _WorldSpaceCameraPos;
                
                // Use a simpler approach for view direction in perspective/ortho
                output.viewDir = normalize(output.viewDir);
                
                return output;
            }

            float4 Frag(Varyings input) : SV_TARGET
            {
                float2 uv = input.uv;
                float4 normalSample = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture_PointClamp, uv);
                if (normalSample.a < 0.5) // Fallback to scene normals if _BlitTexture is not set correctly
                {
                    normalSample.rgb = SampleSceneNormals(uv);
                }
                float3 normal = normalSample.rgb * 2.0 - 1.0;
                
                float3 viewDir = normalize(input.viewDir);
                float depth = SampleSceneDepth(uv);
                float linearDepth = LinearEyeDepth(depth, _ZBufferParams);
                float viewDepth = linearDepth;
                
                float3 worldPos = viewDir * viewDepth + _WorldSpaceCameraPos;
                float3 viewNormal = mul((float3x3)UNITY_MATRIX_V, worldPos);
                viewNormal = mul((float3x3)UNITY_MATRIX_I_V, viewNormal) * 2.0 - 1.0;
                
                float normalDiff = dot(normal, viewNormal);
                normalDiff = 1.0 - normalDiff - _NormalThreshold;
                float normalRange = 2.0 - _NormalThreshold;
                float normalFactor = saturate(normalDiff * normalRange);
                normalFactor = normalFactor * normalFactor * (3.0 - 2.0 * normalFactor);
                normalFactor = 1.0 + _SteepAngleMultiplier * normalFactor;
                depth *= normalFactor;
                
                float2 texelSize = _ScreenParams.zw - 1.0;
                float2 offsets[4];
                offsets[0] = uv + _OutlineScale * float2(-texelSize.x * 0.5, texelSize.y * 0.5);
                offsets[1] = uv + _OutlineScale * float2(texelSize.x * 0.5, texelSize.y * 0.5);
                offsets[2] = uv + _OutlineScale * float2(texelSize.x * 0.5, -texelSize.y * 0.5);
                offsets[3] = uv + _OutlineScale * float2(-texelSize.x * 0.5, -texelSize.y * 0.5);
                
                float depthSamples[4];
                for (int i = 0; i < 4; i++)
                {
                    depthSamples[i] = SampleSceneDepth(offsets[i]);
                }
                
                float depthDiff1 = depthSamples[1] - depthSamples[0];
                float depthDiff2 = depthSamples[2] - depthSamples[3];
                float depthEdge = sqrt(depthDiff1 * depthDiff1 + depthDiff2 * depthDiff2);
                depthEdge *= _RobertsCrossMultiplier;
                
                float depthEdgeFactor = depthEdge >= _DepthThreshold;
                
                float4 normalSamples[4];
                for (i = 0; i < 4; i++)
                {
                    normalSamples[i] = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture_PointClamp, offsets[i]);
                    if (normalSamples[i].a < 0.5) // Fallback to scene normals
                    {
                        normalSamples[i].rgb = SampleSceneNormals(offsets[i]);
                    }
                }
                
                float3 normalDiff1 = normalSamples[1].rgb - normalSamples[0].rgb;
                float3 normalDiff2 = normalSamples[2].rgb - normalSamples[3].rgb;
                float normalEdge = dot(normalDiff1, normalDiff1) + dot(normalDiff2, normalDiff2);
                normalEdge = sqrt(normalEdge);
                
                float normalEdgeFactor = normalEdge >= _NormalThreshold && _UseNormalOutline > 0.5;
                
                float edgeFactor = max(depthEdgeFactor, normalEdgeFactor) * _OutlineColor.a;
                
                return edgeFactor * _OutlineColor;
            }
            ENDHLSL
        }
    }
    
    CustomEditor "UnityEditor.Rendering.Fullscreen.ShaderGraph.FullscreenShaderGUI"
    Fallback "Hidden/Shader Graph/FallbackError"
} 