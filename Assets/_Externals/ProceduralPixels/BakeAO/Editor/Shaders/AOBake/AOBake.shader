Shader "Hidden/BakeAO/Bake"
{
    Properties {}

    SubShader
    {
        Tags {}

        Pass
        {
            ZTest LEqual
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4x4 _AOBake_MatrixM;
            float4x4 _AOBake_MatrixV;
            float4x4 _AOBake_MatrixP;
            float _AOBake_PixelScaleWS;
            float _MaxOccluderDistance;

            struct vertexData
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct fragmentData
            {
                float4 clipPos : SV_POSITION;
                float3 positionVS : TEXCOORD0;
            };

            fragmentData vert(vertexData input)
            {
                fragmentData output;
                float4 positionWS = mul(_AOBake_MatrixM, float4(input.positionOS.xyz, 1.0));

                // if (_AOBake_PixelScaleWS > 0.000001f)
                // {
                //     float4 normalWS = mul(_AOBake_MatrixM, float4(input.normalOS, 0.0));
                //     if (dot(input.normalOS, input.normalOS) < 0.001f)
                //         normalWS.xyz = float3(0.0, 1.0, 0.0);
                //     positionWS.xyz -= normalize(normalWS.xyz) * _AOBake_PixelScaleWS;
                // }

                // //#if UNITY_REVERSED_Z
                //     float depth = output.clipPos.z / output.clipPos.w;
                //     depth = 1.0 - depth;
                //     output.clipPos.z = depth * output.clipPos.w;
                // //#endif

                float4 positionVS = mul(_AOBake_MatrixV, float4(positionWS.xyz, 1.0));
                output.clipPos = mul(_AOBake_MatrixP, positionVS);

                output.positionVS.xyz = positionVS.xyz;
                return output;
            }

            float4 frag(fragmentData input) : SV_Target
            { 
                float occluderDistance = length(input.positionVS);
                if (occluderDistance > _MaxOccluderDistance)
                    discard;
                float t = clamp(occluderDistance / _MaxOccluderDistance, 0.0, 1.0);
                float occlusion = t * t;
                return float4(occlusion, occlusion, occlusion, 0.0);
            }

            ENDHLSL
        }
    }
}
