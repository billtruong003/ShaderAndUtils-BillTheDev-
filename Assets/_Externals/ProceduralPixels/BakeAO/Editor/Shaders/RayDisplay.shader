Shader "Hidden/BakeAO/DebugLine"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Cull Off
            ZWrite Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            struct SampleData
            {
                float2 uv;
                float3 positionWS;
                float3 normalWS;
            };

            StructuredBuffer<SampleData> _RayData;
            int _SamplesCount;
            float4 _Color;
            float4 _SelectionColor;

            float4x4 _MatrixP;
            float4x4 _MatrixV;
            float4x4 _MatrixVP;
            float4 _ScreenParams;
            uint _SelectedRayIndex;
            uint _MinBoldRayIndex;
            uint _MaxBoldRayIndex;

            struct appdata
            {
                float2 uv : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 color : COLOR0;
                float4 clipPos : SV_POSITION;
            };

            // There is an assumption that vertex contains only uv, and only quads are drawn.
            v2f vert (appdata v)
            {
                v2f o;

                SampleData sampleData = _RayData[v.instanceID];

                float vectorDistance = 0.1f;
                float4 firstVertexWS = float4(sampleData.positionWS.xyz, 1.0);
                float4 secondVertexWS = float4(sampleData.positionWS.xyz + sampleData.normalWS.xyz * vectorDistance, 1.0);

                float4 firstVertexCS = mul(_MatrixVP, firstVertexWS);
                float4 secondVertexCS = mul(_MatrixVP, secondVertexWS);

                firstVertexCS.xyz /= firstVertexCS.w;
                secondVertexCS.xyz /= secondVertexCS.w;

                float2 pixelSize = float2(1.0, 1.0) / _ScreenParams.xy;
                float2 normalCS = secondVertexCS.xy - firstVertexCS.xy;
                float2 tangentCS = normalize(normalCS.yx * float2(1.0, -1.0));

                float3 color = _Color.rgb;

                if (v.instanceID >= _MinBoldRayIndex && v.instanceID <= _MaxBoldRayIndex)
                {
                    pixelSize *= 2.0;
                    color = float3(1.0, 1.0, 0.0);
                }

                if (v.instanceID == _SelectedRayIndex)
                {
                    pixelSize *= 2.0;
                    color = _SelectionColor.rgb;
                }


                float4 corner00CS = float4(firstVertexCS.xy + tangentCS * pixelSize, firstVertexCS.zw);
                float4 corner10CS = float4(firstVertexCS.xy - tangentCS * pixelSize, firstVertexCS.zw);

                float4 corner01CS = float4(secondVertexCS.xy + tangentCS * pixelSize, secondVertexCS.zw);
                float4 corner11CS = float4(secondVertexCS.xy - tangentCS * pixelSize, secondVertexCS.zw);

                float4 renderedVertexCS = lerp(lerp(corner00CS, corner10CS, v.uv.x), lerp(corner01CS, corner11CS, v.uv.x), v.uv.y);
                renderedVertexCS.xyz *= renderedVertexCS.w;

                // float4 positionWS = float4(v.uv, 0.0, 1.0);
                // positionWS.z += v.instanceID;
                // float4 renderedVertexCS = mul(_MatrixVP, positionWS); 

                o.clipPos = renderedVertexCS;
                o.uv = v.uv;
                o.color = color;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 col = float4(i.color, 1.0);
                return col;
            }

            ENDHLSL
        }
    }
}
