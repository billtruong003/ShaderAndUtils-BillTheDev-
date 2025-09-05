Shader "BillTheDev/VAT/Builtin_VAT_Toon_Unlit"
{
    Properties
    {
        [Header(VAT Properties)]
        _PositionTexture ("Position Texture (VAT)", 2D) = "white" {}
        _PositionMin ("Position Min (Local Space)", Vector) = (0,0,0,0)
        _PositionMax ("Position Max (Local Space)", Vector) = (0,0,0,0)

        [Header(Toon Properties)]
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _HighlightColor ("Highlight Color", Color) = (1,1,1,1)
        _BaseColor ("Base Color", Color) = (0.8, 0.8, 0.8, 1)
        _MidtoneColor("Midtone Color", Color) = (0.5, 0.5, 0.5, 1)
        _ShadowColor ("Shadow Color", Color) = (0.2, 0.2, 0.2, 1)
        
        [Space(10)]
        [Range(0, 1)] _HighlightThreshold ("Highlight Threshold", Float) = 0.8
        [Range(0, 1)] _MidtoneThreshold ("Midtone Threshold", Float) = 0.6
        [Range(0, 1)] _ShadowThreshold ("Shadow Threshold", Float) = 0.4
        [Range(0.001, 1)] _Smoothness ("Transition Smoothness", Float) = 0.05

        [Header(Fake Light Properties)]
        _FakeLightDirection ("Fake Light Direction", Vector) = (0.5, 0.5, 0, 0)
        [Range(0, 5)] _LightIntensity ("Light Intensity", Float) = 1.0
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
            #pragma target 3.5

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
            float4 _PositionMin;
            float4 _PositionMax;
            
            UNITY_INSTANCING_BUFFER_START(PerInstance)
                UNITY_DEFINE_INSTANCED_PROP(float, _CurrentAnimNormalizedTime)
                UNITY_DEFINE_INSTANCED_PROP(float, _PreviousAnimNormalizedTime)
                UNITY_DEFINE_INSTANCED_PROP(float, _AnimationBlendWeight)
            UNITY_INSTANCING_BUFFER_END(PerInstance)

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
                
                float currentAnimTime = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _CurrentAnimNormalizedTime);
                float blendWeight = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _AnimationBlendWeight);

                float3 localPosition = DecodeLocalPosition(vertexU, currentAnimTime);

                if (blendWeight > 0.001)
                {
                    float previousAnimTime = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _PreviousAnimNormalizedTime);
                    float3 previousLocalPosition = DecodeLocalPosition(vertexU, previousAnimTime);
                    localPosition = lerp(previousLocalPosition, localPosition, blendWeight);
                }
                
                float4 worldPosition = mul(unity_ObjectToWorld, float4(localPosition, 1.0));
                o.vertex = mul(UNITY_MATRIX_VP, worldPosition);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv;
                
                return o;
            }
            
            sampler2D _MainTex;
            half4 _HighlightColor;
            half4 _BaseColor;
            half4 _MidtoneColor;
            half4 _ShadowColor;
            half _HighlightThreshold;
            half _MidtoneThreshold;
            half _ShadowThreshold;
            half _Smoothness;
            float3 _FakeLightDirection;
            half _LightIntensity;

            fixed4 frag (VertexToFragment i) : SV_Target
            {
                fixed4 albedo = tex2D(_MainTex, i.uv);
                
                half3 lightDirection = normalize(_FakeLightDirection.xyz);
                half3 worldNormal = normalize(i.worldNormal);

                half NdotL = saturate(dot(worldNormal, lightDirection));
                
                half smoothnessFactor = _Smoothness * 0.5;
                
                half shadowFactor = smoothstep(_ShadowThreshold - smoothnessFactor, _ShadowThreshold + smoothnessFactor, NdotL);
                half midtoneFactor = smoothstep(_MidtoneThreshold - smoothnessFactor, _MidtoneThreshold + smoothnessFactor, NdotL);
                half highlightFactor = smoothstep(_HighlightThreshold - smoothnessFactor, _HighlightThreshold + smoothnessFactor, NdotL);

                half3 rampColor = lerp(_ShadowColor.rgb, _MidtoneColor.rgb, shadowFactor);
                rampColor = lerp(rampColor, _BaseColor.rgb, midtoneFactor);
                rampColor = lerp(rampColor, _HighlightColor.rgb, highlightFactor);

                half3 finalColor = rampColor * albedo.rgb * _LightIntensity;
                
                return fixed4(finalColor, albedo.a);
            }
            ENDHLSL
        }
    }
    FallBack "Mobile/VertexLit"
}