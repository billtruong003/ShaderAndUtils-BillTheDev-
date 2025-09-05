Shader "BillTheDev/VAT/URP_VAT_Toon_Instanced_GPU_Driven_Final"
{
    Properties
    {
        [Header(VAT Properties)]
        _PositionTexture ("Position Texture (VAT)", 2D) = "white" {}
        _PositionMin ("Position Min (Local Space)", Vector) = (0,0,0,0)
        _PositionMax ("Position Max (Local Space)", Vector) = (0,0,0,0)

        [Header(Toon Properties)]
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        [HDR] _LightColor ("Light Color", Color) = (1,1,1,1)
        [HDR] _ShadowColor ("Shadow Color", Color) = (0.2, 0.2, 0.2, 1)
        _ShadowThreshold ("Shadow Threshold", Range(0, 1)) = 0.5
        _Smoothness ("Transition Smoothness", Range(0.001, 1)) = 0.05

        [Header(Fake Light Properties)]
        _FakeLightDirection ("Fake Light Direction", Vector) = (0.5, 0.5, 0, 0)
        [Range(0, 5)] _LightIntensity ("Light Intensity", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct AgentRenderData
            {
                float4x4 objectToWorld;
                float4 animationData;
            };

            StructuredBuffer<AgentRenderData> _AgentDataBuffer;

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
                float2 vertexIdUV   : TEXCOORD1;
                uint instanceID     : SV_InstanceID;
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                half3 worldNormal   : TEXCOORD1;
                float4 positionCS   : SV_POSITION;
            };

            TEXTURE2D(_PositionTexture); SAMPLER(sampler_PositionTexture);
            TEXTURE2D(_MainTex);         SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _PositionMin, _PositionMax;
                half4 _LightColor;
                half4 _ShadowColor;
                half _ShadowThreshold;
                half _Smoothness;
                float3 _FakeLightDirection;
                half _LightIntensity;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                
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
                
                o.worldNormal = TransformObjectToWorldNormal(v.normalOS);
                float3 worldPosition = mul(worldMatrix, float4(localPosition, 1.0)).xyz;
                o.positionCS = TransformWorldToHClip(worldPosition);
                o.uv = v.uv;
                
                return o;
            }
            
            half4 frag (Varyings i) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half3 lightDirection = normalize((half3)_FakeLightDirection.xyz);
                half3 worldNormal = normalize(i.worldNormal);
                half NdotL = saturate(dot(worldNormal, lightDirection));
                
                half smoothnessFactor = _Smoothness * 0.5h;
                half lightingFactor = smoothstep(_ShadowThreshold - smoothnessFactor, _ShadowThreshold + smoothnessFactor, NdotL);
                
                half3 rampColor = lerp(_ShadowColor.rgb, _LightColor.rgb, lightingFactor);

                half3 finalColor = rampColor * albedo.rgb * _LightIntensity;
                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }
        
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

            struct AgentRenderData
            {
                float4x4 objectToWorld;
                float4 animationData;
            };

            StructuredBuffer<AgentRenderData> _AgentDataBuffer;
            
            TEXTURE2D(_PositionTexture); SAMPLER(sampler_PositionTexture);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _PositionMin, _PositionMax;
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

            half4 CustomPackNormal(float3 normalWS)
            {
                return half4(normalWS * 0.5h + 0.5h, 1.0h);
            }
            
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

                VertexPositionInputs positionInputs = GetVertexPositionInputs(localPosition);
                o.positionCS = positionInputs.positionCS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                
                return o;
            }

            half4 FragDepthNormals(VaryingsDepthNormals i) : SV_Target
            {
                float3 normalWS = normalize(i.normalWS);
                return CustomPackNormal(normalWS);
            }
            ENDHLSL
        }
    }
}