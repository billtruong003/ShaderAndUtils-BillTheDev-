Shader "Hidden/BakeAO/DebugDraw"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _UVChannel ("UV Channel", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float2 uv3 : TEXCOORD3;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _UVChannel = 0.0;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                [branch]
                if (_UVChannel < 0.5)
                    o.uv = v.uv0;
                else if (_UVChannel < 1.5)
                    o.uv = v.uv1;
                else if (_UVChannel < 2.5)
                    o.uv = v.uv2;
                else
                    o.uv = v.uv3;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 col = tex2D(_MainTex, i.uv).xyz;
                return float4(col, 1.0);
            }
            ENDCG
        }
    }
}
