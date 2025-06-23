// Tái tạo shader "Hidden/S_ScreenSpaceOutlines" từ mã biên dịch
// *** ĐÃ SỬA LỖI SAMPLER ***
Shader "Hidden/S_ScreenSpaceOutlines"
{
    Properties
    {
        _OutlineScale ("OutlineScale", Float) = 1.0
        _RobertsCrossMultiplier ("RobertsCrossMultiplier", Float) = 100.0
        _DepthThreshold ("DepthThreshold", Float) = 10.0
        _NormalThreshold ("NormalThreshold", Float) = 0.4
        _SteepAngleThreshold ("SteepAngleThreshold", Float) = 0.2
        _SteepAngleMultiplier ("SteepAngleMultiplier", Float) = 25.0
        _OutlineColor ("OutlineColor", Color) = (0.0, 0.888, 1.0, 1.0)
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        
        Pass
        {
            Name "OutlineFinal"
            ZWrite Off Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

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
            CBUFFER_END

            // Texture chứa màu sắc của scene
            TEXTURE2D(_BlitTexture);
            // Texture chứa normal của các vật thể được lọc ra
            TEXTURE2D(_OutlineFilterTex);
            // Khai báo sampler theo đúng quy ước của URP
            // Tên sampler phải khớp với tên texture
            SAMPLER(sampler_OutlineFilterTex);

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
            };

            Varyings Vert(uint vertexID : SV_VertexID)
            {
                Varyings output;
                output.uv = float2((vertexID << 1) & 2, vertexID & 2);
                output.positionCS = float4(output.uv * 2.0 - 1.0, 0.0, 1.0);
                #if UNITY_UV_STARTS_AT_TOP
                output.uv.y = 1.0 - output.uv.y;
                #endif
                output.viewDir = GetWorldSpaceViewDir(mul(unity_MatrixInvVP, float4(output.positionCS.xy, 1, 1)).xyz);
                return output;
            }

            float4 Frag(Varyings input) : SV_TARGET
            {
                float sceneDepth = SampleSceneDepth(input.uv);
                float3 sceneNormalWS = SampleSceneNormals(input.uv);
                
                // Sử dụng sampler đã khai báo đúng: sampler_OutlineFilterTex
                float4 filterNormalSample = SAMPLE_TEXTURE2D(_OutlineFilterTex, sampler_OutlineFilterTex, input.uv);

                if (filterNormalSample.a < 0.5)
                {
                    return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.uv);
                }
                
                float NdotV = 1.0 - saturate(dot(sceneNormalWS, normalize(input.viewDir)));
                float steepAngleFactor = smoothstep(_SteepAngleThreshold, 1.0, NdotV);
                float steepAngleDepth = sceneDepth + steepAngleFactor * _SteepAngleMultiplier * 0.001;

                float2 texelSize = _ScreenParams.zw - 1.0;
                float2 offset = texelSize * _OutlineScale;

                float d1 = SampleSceneDepth(input.uv + offset);
                float3 n1 = SampleSceneNormals(input.uv + offset);
                float d2 = SampleSceneDepth(input.uv + float2(offset.x, -offset.y));
                float3 n2 = SampleSceneNormals(input.uv + float2(offset.x, -offset.y));
                float d3 = SampleSceneDepth(input.uv + float2(-offset.x, offset.y));
                float3 n3 = SampleSceneNormals(input.uv + float2(-offset.x, offset.y));

                float depthGrad1 = d1 - steepAngleDepth;
                float depthGrad2 = d2 - d3;
                float depthEdge = sqrt(depthGrad1 * depthGrad1 + depthGrad2 * depthGrad2) * _RobertsCrossMultiplier;
                
                float3 normalGrad1 = n1 - sceneNormalWS;
                float3 normalGrad2 = n2 - n3;
                float normalEdge = sqrt(dot(normalGrad1, normalGrad1) + dot(normalGrad2, normalGrad2));

                float depthFactor = step(_DepthThreshold * 0.0001, depthEdge);
                float normalFactor = step(_NormalThreshold, normalEdge);

                float edgeFactor = max(depthFactor, normalFactor);
                
                half4 originalColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.uv);
                
                return lerp(originalColor, _OutlineColor, edgeFactor * _OutlineColor.a);
            }
            ENDHLSL
        }
    }
    Fallback "Hidden/Shader Graph/FallbackError"
}