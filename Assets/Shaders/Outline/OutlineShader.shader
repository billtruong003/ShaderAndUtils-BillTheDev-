Shader "Custom/URP Lightweight Outline"
{
    Properties
    {
        _BaseMap ("Base Texture", 2D) = "white" {}
        _Metallic ("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.5
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _Occlusion ("Occlusion", Range(0.0, 1.0)) = 1.0
        _OutlineScale ("Outline Scale", Float) = 1.0
        _RobertsCrossMultiplier ("Roberts Cross Multiplier", Float) = 100.0
        _DepthThreshold ("Depth Threshold", Float) = 10.0
        _NormalThreshold ("Normal Threshold", Float) = 0.4
        _SteepAngleThreshold ("Steep Angle Threshold", Float) = 0.2
        _SteepAngleMultiplier ("Steep Angle Multiplier", Float) = 25.0
        _OutlineColor ("Outline Color", Color) = (0.0, 0.888, 1.0, 1.0)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        
        Pass
        {
            Name "MainLit"
            ZWrite On
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _SPECULARHIGHLIGHTS_OFF

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _Metallic;
                float _Smoothness;
                float _Occlusion;
                float _OutlineScale;
                float _RobertsCrossMultiplier;
                float _DepthThreshold;
                float _NormalThreshold;
                float _SteepAngleThreshold;
                float _SteepAngleMultiplier;
                float4 _OutlineColor;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);
            TEXTURE2D(_OutlineFilterTex);
            SAMPLER(sampler_OutlineFilterTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.viewDirWS = GetWorldSpaceViewDir(TransformObjectToWorld(input.positionOS.xyz));
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
                return output;
            }

            half4 Frag(Varyings input) : SV_TARGET
            {
                // Initialize SurfaceData
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
                surfaceData.metallic = _Metallic;
                surfaceData.specular = half3(0.0, 0.0, 0.0); // Metallic workflow
                surfaceData.smoothness = _Smoothness;
                surfaceData.alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a;
                surfaceData.normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv));
                surfaceData.occlusion = _Occlusion;
                surfaceData.emission = half3(0.0, 0.0, 0.0);

                // Initialize BRDFData
                BRDFData brdfData;
                InitializeBRDFData(surfaceData, brdfData);

                // Basic PBR lighting (main light only for simplicity)
                Light mainLight = GetMainLight();
                half3 lightDirWS = mainLight.direction;
                half3 color = DirectBRDF(brdfData, input.normalWS, lightDirWS, normalize(input.viewDirWS)) * mainLight.color;

                // Outline calculation
                float sceneDepth = SampleSceneDepth(input.uv);
                float3 sceneNormalWS = SampleSceneNormals(input.uv);
                float4 filterNormalSample = SAMPLE_TEXTURE2D(_OutlineFilterTex, sampler_OutlineFilterTex, input.uv);

                if (filterNormalSample.a < 0.5)
                {
                    return half4(color, surfaceData.alpha);
                }

                float NdotV = 1.0 - saturate(dot(sceneNormalWS, normalize(input.viewDirWS)));
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

                // Blend PBR color with outline
                return lerp(half4(color, surfaceData.alpha), _OutlineColor, edgeFactor * _OutlineColor.a);
            }
            ENDHLSL
        }
    }
    Fallback "Hidden/Shader Graph/FallbackError"
}