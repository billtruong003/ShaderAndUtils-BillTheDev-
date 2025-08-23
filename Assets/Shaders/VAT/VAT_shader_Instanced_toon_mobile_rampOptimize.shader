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
        [NoScaleOffset] _RampTex ("Toon Ramp (LUT)", 2D) = "white" {}

        [Header(Fake Light Properties)]
        _FakeLightDirection ("Fake Light Direction", Vector) = (0.5, 0.5, 0, 0)
        [Range(0, 5)] _LightIntensity ("Light Intensity", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        LOD 200
        Cull Back

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 3.0

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
            TEXTURE2D(_RampTex);         SAMPLER(sampler_RampTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _PositionMin, _PositionMax;
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
                
                o.worldNormal = (half3)normalize(mul((float3x3)worldMatrix, v.normalOS));
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
                
                half3 rampColor = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(NdotL, 0.5h)).rgb;

                half3 finalColor = rampColor * albedo.rgb * _LightIntensity;
                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }
    }
}