Shader "Hidden/BakeAO/RenderUVBuffer"
{
    Properties {}

    SubShader
    {
        Tags {}

        Pass
        {
            Blend One Zero
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ UV_CHANNEL_0 UV_CHANNEL_1 UV_CHANNEL_2 UV_CHANNEL_3

            struct vertexData
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                #ifdef UV_CHANNEL_1
                    float2 uv : TEXCOORD1;  
                #elif UV_CHANNEL_2
                    float2 uv : TEXCOORD2;  
                #elif UV_CHANNEL_3
                    float2 uv : TEXCOORD3;  
                #else
                    float2 uv : TEXCOORD0;  
                #endif
            };

            struct fragmentData
            {
                float4 clipPos : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            struct UVBufferTarget
            {
                float4 positionWS : SV_TARGET0;
                float4 normalWS : SV_TARGET1;
            };

            float4 _ScaleAndTransform;
            float4x4 _AOBake_MatrixM;
            float4x4 _AOBake_MatrixMInv;

            fragmentData vert(vertexData input)
            {
                fragmentData output;

                float2 uv = (input.uv - _ScaleAndTransform.zw) / _ScaleAndTransform.xy;
                output.clipPos = float4(uv * 2.0 - 1.0, 0.0, 1.0);
                output.positionWS = mul(_AOBake_MatrixM, float4(input.positionOS.xyz, 1.0)).xyz;
                output.normalWS = normalize(mul(input.normalOS, (float3x3)_AOBake_MatrixMInv));

                return output;
            }

            UVBufferTarget frag(fragmentData input)
            { 
                UVBufferTarget uvBuffer;

                uvBuffer.positionWS = float4(input.positionWS, 1.0);
                uvBuffer.normalWS = float4(normalize(input.normalWS.xyz), 1.0);

                return uvBuffer;
            }

            ENDHLSL
        }
    }
}
