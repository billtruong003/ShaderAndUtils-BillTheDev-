Shader "Hidden/BakeAO/Dilate"
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
            float _TextureSize;

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

            bool GetColorFrom(float2 uv, float2 offset, out float4 color)
            {
                color = tex2D(_MainTex, uv + offset);
                return color.a > 0.5;
            }

            float4 frag(fragmentData input) : SV_Target
            { 
                float texelSize = 1.0 / _TextureSize;
                float4 color;

                if (GetColorFrom(input.uv, float2(0.0, 0.0) * texelSize, color))
                    return color;
                else if (GetColorFrom(input.uv, float2(-1.0, 0.0) * texelSize, color))
                    return color;
                else if (GetColorFrom(input.uv, float2(1.0, 0.0) * texelSize, color))
                    return color;
                else if (GetColorFrom(input.uv, float2(0.0, -1.0) * texelSize, color))
                    return color;
                else if (GetColorFrom(input.uv, float2(0.0, 1.0) * texelSize, color))
                    return color;
                else if (GetColorFrom(input.uv, float2(-1.0, -1.0) * texelSize, color))
                    return color;
                else if (GetColorFrom(input.uv, float2(-1.0, 1.0) * texelSize, color))
                    return color;
                else if (GetColorFrom(input.uv, float2(1.0, -1.0) * texelSize, color))
                    return color;
                else if (GetColorFrom(input.uv, float2(1.0, 1.0) * texelSize, color))
                    return color;


                return color;
            }

            ENDHLSL
        }
    }
}
