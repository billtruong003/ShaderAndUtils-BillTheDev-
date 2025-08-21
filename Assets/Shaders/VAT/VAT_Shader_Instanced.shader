Shader "BillTheDev/VAT/Optimized_VAT_Instanced"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _PositionTexture ("Position Texture (VAT)", 2D) = "white" {}
        _PositionMin ("Position Min (Local Space)", Vector) = (0,0,0,0)
        _PositionMax ("Position Max (Local Space)", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 4.5

            #include "UnityCG.cginc"

            struct AppData
            {
                float4 vertex       : POSITION;
                float2 uv           : TEXCOORD0;
                float2 vertexIdUV   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VertexToFragment
            {
                float2 uv       : TEXCOORD0;
                float4 vertex   : SV_POSITION;
            };

            sampler2D _PositionTexture;
            float4 _PositionMin;
            float4 _PositionMax;
            
            // Dữ liệu animation được truyền vào qua một mảng Vector4
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _AnimationData) // x: currentV, y: previousV, z: blendWeight
            UNITY_INSTANCING_BUFFER_END(Props)

            float3 DecodeLocalPosition(float vertexU, float timeV)
            {
                float4 encodedPosition = tex2Dlod(_PositionTexture, float4(vertexU, timeV, 0, 0));
                return lerp(_PositionMin.xyz, _PositionMax.xyz, encodedPosition.xyz);
            }

            VertexToFragment vert (AppData v)
            {
                VertexToFragment o;
                UNITY_SETUP_INSTANCE_ID(v);
                
                float vertexU = v.vertexIdUV.x;
                
                float4 animData = UNITY_ACCESS_INSTANCED_PROP(Props, _AnimationData);
                float currentAnimV = animData.x;
                float previousAnimV = animData.y;
                float blendWeight = animData.z;

                float3 localPosition = DecodeLocalPosition(vertexU, currentAnimV);

                if (blendWeight > 0.001)
                {
                    float3 previousLocalPosition = DecodeLocalPosition(vertexU, previousAnimV);
                    localPosition = lerp(previousLocalPosition, localPosition, blendWeight);
                }
                
                o.vertex = mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, float4(localPosition, 1.0)));
                o.uv = v.uv;
                
                return o;
            }
            
            sampler2D _MainTex;

            fixed4 frag (VertexToFragment i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Mobile/VertexLit"
}