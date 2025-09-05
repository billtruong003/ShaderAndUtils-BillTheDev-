Shader "BillTheDev/VAT/Production_VAT_ToonLit"
{
    Properties
    {
        [Header(VAT Data)]
        _PositionTexture ("Position Texture (VAT)", 2D) = "white" {}
        _NormalTexture ("Normal Texture (VAT)", 2D) = "white" {}
        _PositionMin ("Position Min (Local Space)", Vector) = (0,0,0,0)
        _PositionMax ("Position Max (Local Space)", Vector) = (0,0,0,0)
        
        [Header(Toon Properties)]
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Color ("Base Color", Color) = (1,1,1,1)
        _LightColor ("Light Color", Color) = (1,1,1,1)
        _ShadowColor ("Shadow Color", Color) = (0.2, 0.2, 0.2, 1)
        _ShadowThreshold ("Shadow Threshold", Range(0, 1)) = 0.5
        _Smoothness ("Transition Smoothness", Range(0.001, 1)) = 0.05

        [Header(Fake Light)]
        _FakeLightDirection ("Fake Light Direction", Vector) = (0.5, 0.5, 0, 0)
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

            #include "UnityCG.cginc"

            struct AppData
            {
                float4 vertex       : POSITION;
                float3 normal       : NORMAL;
                float2 uv           : TEXCOORD0;
                float2 vertexIdUV   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VertexToFragment
            {
                float2 uv           : TEXCOORD0;
                float3 worldNormal  : TEXCOORD1;
                float4 vertex       : SV_POSITION;
            };

            sampler2D _PositionTexture;
            sampler2D _NormalTexture;
            float4 _PositionMin;
            float4 _PositionMax;
            
            UNITY_INSTANCING_BUFFER_START(PerInstance)
                UNITY_DEFINE_INSTANCED_PROP(float, _CurrentAnimNormalizedTime)
                UNITY_DEFINE_INSTANCED_PROP(float, _PreviousAnimNormalizedTime)
                UNITY_DEFINE_INSTANCED_PROP(float, _AnimationBlendWeight)
            UNITY_INSTANCING_BUFFER_END(PerInstance)

            float3 slerp_safe(float3 a, float3 b, float t)
            {
                float dot_ab = dot(a, b);
                if (dot_ab > 0.9999) return a;
                float theta = acos(dot_ab);
                return (sin((1.0 - t) * theta) * a + sin(t * theta) * b) / sin(theta);
            }

            float3 DecodePosition(float u, float v)
            {
                float4 encoded = tex2Dlod(_PositionTexture, float4(u, v, 0, 0));
                return lerp(_PositionMin.xyz, _PositionMax.xyz, encoded.xyz);
            }
            
            float3 DecodeNormal(float u, float v)
            {
                float4 encoded = tex2Dlod(_NormalTexture, float4(u, v, 0, 0));
                return normalize(encoded.xyz * 2.0 - 1.0);
            }

            VertexToFragment vert (AppData v)
            {
                VertexToFragment o;
                UNITY_SETUP_INSTANCE_ID(v);
                
                float u = v.vertexIdUV.x;
                float currentV = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _CurrentAnimNormalizedTime);
                float blendW = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _AnimationBlendWeight);

                float3 localPos = DecodePosition(u, currentV);
                float3 localNorm = DecodeNormal(u, currentV);

                if (blendW > 0.001)
                {
                    float previousV = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _PreviousAnimNormalizedTime);
                    localPos = lerp(DecodePosition(u, previousV), localPos, blendW);
                    localNorm = slerp_safe(DecodeNormal(u, previousV), localNorm, blendW);
                }
                
                o.worldNormal = UnityObjectToWorldNormal(localNorm);
                float4 worldPos = mul(unity_ObjectToWorld, float4(localPos, 1.0));
                o.vertex = mul(UNITY_MATRIX_VP, worldPos);
                o.uv = v.uv;
                
                return o;
            }
            
            sampler2D _MainTex;
            fixed4 _Color;
            half4 _LightColor;
            half4 _ShadowColor;
            half _ShadowThreshold;
            half _Smoothness;
            float3 _FakeLightDirection;

            fixed4 frag (VertexToFragment i) : SV_Target
            {
                fixed4 albedo = tex2D(_MainTex, i.uv) * _Color;
                half3 lightDir = normalize((half3)_FakeLightDirection.xyz);
                half3 worldNormal = normalize(i.worldNormal);
                half NdotL = saturate(dot(worldNormal, lightDir));
                
                half smoothnessFactor = _Smoothness * 0.5h;
                half ramp = smoothstep(_ShadowThreshold - smoothnessFactor, _ShadowThreshold + smoothnessFactor, NdotL);
                
                half3 rampColor = lerp(_ShadowColor.rgb, _LightColor.rgb, ramp);
                
                return fixed4(albedo.rgb * rampColor, albedo.a);
            }
            ENDHLSL
        }
    }
}