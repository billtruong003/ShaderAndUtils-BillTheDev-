Shader "BillTheDev/VAT/URP_VAT_Instanced_DepthNormalOnly"
{
    Properties
    {
        [Header(VAT Properties)]
        _PositionTexture ("Position Texture (VAT)", 2D) = "white" {}
        _PositionMin ("Position Min (Local Space)", Vector) = (0,0,0,0)
        _PositionMax ("Position Max (Local Space)", Vector) = (0,0,0,0)

        [Header(Outline Properties)]
        _OutlineDepthOffset ("Outline Depth Offset", Range(-0.1, 0.1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex VertDepthNormals
            #pragma fragment FragDepthNormals
            #pragma multi_compile_instancing
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct AgentRenderData
            {
                float4x4 objectToWorld;
                float4 animationData;
            };

            StructuredBuffer<AgentRenderData> _AgentDataBuffer;
            
            TEXTURE2D(_PositionTexture); SAMPLER(sampler_PositionTexture);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _PositionMin, _PositionMax;
                float _OutlineDepthOffset;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 vertexIdUV   : TEXCOORD1;
                uint instanceID     : SV_InstanceID;
            };

            struct VaryingsDepthNormals
            {
                float4 positionCS   : SV_POSITION;
                float3 normalWS     : TEXCOORD0;
            };
            
            VaryingsDepthNormals VertDepthNormals(Attributes v)
            {
                VaryingsDepthNormals o;

                AgentRenderData agentData = _AgentDataBuffer[v.instanceID];
                float4 animData = agentData.animationData;
                float4x4 worldMatrix = agentData.objectToWorld;
                
                float vertexU = v.vertexIdUV.x;
                float currentAnimV = animData.x;
                float previousAnimV = animData.y;
                float blendWeight = animData.z;

                float3 encodedPos = SAMPLE_TEXTURE2D_LOD(_PositionTexture, sampler_PositionTexture, float2(vertexU, currentAnimV), 0).xyz;
                float3 localPosition = lerp(_PositionMin.xyz, _PositionMax.xyz, encodedPos);

                if (blendWeight > 0.001h)
                {
                    float3 prevEncodedPos = SAMPLE_TEXTURE2D_LOD(_PositionTexture, sampler_PositionTexture, float2(vertexU, previousAnimV), 0).xyz;
                    float3 previousLocalPosition = lerp(_PositionMin.xyz, _PositionMax.xyz, prevEncodedPos);
                    localPosition = lerp(previousLocalPosition, localPosition, blendWeight);
                }

                float3 worldPosition = mul(worldMatrix, float4(localPosition, 1.0)).xyz;
                
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - worldPosition);
                worldPosition += viewDirection * _OutlineDepthOffset;

                o.positionCS = TransformWorldToHClip(worldPosition);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                
                return o;
            }

            void FragDepthNormals(VaryingsDepthNormals i, out half4 outNormalWS : SV_Target0)
            {
                float3 normalWS = normalize(i.normalWS);
                outNormalWS = half4(normalWS, 1.0);
            }
            ENDHLSL
        }
    }
}