Shader "BillTheDev/VAT/Production_VAT_Unlit"
{
    Properties
    {
        [Header(VAT Data)]
        _PositionTexture ("Position Texture (VAT)", 2D) = "white" {}
        _PositionMin ("Position Min (Local Space)", Vector) = (0,0,0,0)
        _PositionMax ("Position Max (Local Space)", Vector) = (0,0,0,0)
        
        [Header(Surface Properties)]
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

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
            
            UNITY_INSTANCING_BUFFER_START(PerInstance)
                UNITY_DEFINE_INSTANCED_PROP(float, _CurrentAnimNormalizedTime)
                UNITY_DEFINE_INSTANCED_PROP(float, _PreviousAnimNormalizedTime)
                UNITY_DEFINE_INSTANCED_PROP(float, _AnimationBlendWeight)
            UNITY_INSTANCING_BUFFER_END(PerInstance)

            float3 DecodePosition(float u, float v)
            {
                float4 encoded = tex2Dlod(_PositionTexture, float4(u, v, 0, 0));
                return lerp(_PositionMin.xyz, _PositionMax.xyz, encoded.xyz);
            }

            VertexToFragment vert (AppData v)
            {
                VertexToFragment o;
                UNITY_SETUP_INSTANCE_ID(v);
                
                float u = v.vertexIdUV.x;
                float currentV = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _CurrentAnimNormalizedTime);
                float blendW = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _AnimationBlendWeight);

                float3 localPos = DecodePosition(u, currentV);

                if (blendW > 0.001)
                {
                    float previousV = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _PreviousAnimNormalizedTime);
                    localPos = lerp(DecodePosition(u, previousV), localPos, blendW);
                }
                
                float4 worldPos = mul(unity_ObjectToWorld, float4(localPos, 1.0));
                o.vertex = mul(UNITY_MATRIX_VP, worldPos);
                o.uv = v.uv;
                
                return o;
            }
            
            sampler2D _MainTex;
            fixed4 _Color;

            fixed4 frag (VertexToFragment i) : SV_Target
            {
                return tex2D(_MainTex, i.uv) * _Color;
            }
            ENDHLSL
        }
    }
}