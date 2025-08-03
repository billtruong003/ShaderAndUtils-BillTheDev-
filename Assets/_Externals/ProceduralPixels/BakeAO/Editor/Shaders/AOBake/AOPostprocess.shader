Shader "Hidden/BakeAO/Postprocess"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags {}
        Cull Off 
        ZWrite Off 
        ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "./ShaderLibrary/AOBakeCore.hlsl"

            sampler2D _MainTex;

            struct vertexData
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct fragmentData
            {
                float4 clipPos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fragmentData vert(vertexData input)
            {
                fragmentData output;
                output.clipPos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, float4(input.positionOS.xyz, 1.0)));
                output.uv = input.uv;
                return output;
            }

            float4 frag(fragmentData input) : SV_Target
            { 
                float4 color = tex2D(_MainTex, input.uv);
                if (color.a > 0.5)
                    color /= color.a;
                return color;
            }

            ENDHLSL
        }
    }
}
