Shader "BillTheDev/VAT/URP_VAT_Toon_Instanced_FakeLight"
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
        Tags 
        { 
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"="Opaque" 
        }
        LOD 200
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
                float2 vertexIdUV   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float3 worldNormal  : TEXCOORD1;
                float4 positionCS   : SV_POSITION;
            };

            TEXTURE2D(_PositionTexture);    SAMPLER(sampler_PositionTexture);
            TEXTURE2D(_MainTex);            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _PositionMin;
                float4 _PositionMax;
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
            CBUFFER_END
            
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _AnimationData)
            UNITY_INSTANCING_BUFFER_END(Props)

            float3 DecodeLocalPosition(float vertexU, float timeV)
            {
                float4 encodedPosition = SAMPLE_TEXTURE2D_LOD(_PositionTexture, sampler_PositionTexture, float2(vertexU, timeV), 0);
                return lerp(_PositionMin.xyz, _PositionMax.xyz, encodedPosition.xyz);
            }

            Varyings vert (Attributes v)
            {
                Varyings o;
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
                
                o.worldNormal = TransformObjectToWorldNormal(v.normalOS);
                float3 worldPosition = TransformObjectToWorld(localPosition);
                o.positionCS = TransformWorldToHClip(worldPosition);
                o.uv = v.uv;
                
                return o;
            }
            
            half4 frag (Varyings i) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                
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
                
                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }
    }
    CustomEditor "BillTheDev.Editor.ToonVATInstancedShaderGUI"
}