Shader "Unlit/Hole"
{
    Properties
    {
        [HDR]_Color("Color", Color) = (1,1,1,0.5)
        [KeywordEnum(Texture2D, CubeMap)] _TextureType ("Texture Type", Float) = 0
        _MainTex ("Texture 2D", 2D) = "white" {}
        _CubeTex ("Cube Map", Cube) = "" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry+2"}
        ColorMask RGB
        ZTest Off
        ZWrite On
        Cull Front
        Lighting Off

        Stencil
        {
            Ref 1
            Comp equal
            Pass zero
            Fail zero
            ZFail zero
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _TEXTURETYPE_TEXTURE2D _TEXTURETYPE_CUBEMAP

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float3 viewDir : TEXCOORD1;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
                UNITY_FOG_COORDS(3)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            samplerCUBE _CubeTex;
            float4 _Color;
            float _TextureType;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(_WorldSpaceCameraPos - o.worldPos);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col;
                #ifdef _TEXTURETYPE_TEXTURE2D
                    col = tex2D(_MainTex, i.uv);
                #else
                    col = texCUBE(_CubeTex, i.viewDir);
                #endif
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col * _Color;
            }
            ENDCG
        }
    }
}