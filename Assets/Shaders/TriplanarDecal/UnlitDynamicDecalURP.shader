Shader "Tutorial/UnlitDynamicDecalURP_Final"
{
    Properties
    {
        [HDR] _Color ("Tint", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent-1"
            "DisableBatching" = "True"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float4 screenPos    : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                return OUT;
            }

            float2 GetProjectedUVs(float4 screenPos)
            {
                float2 screenUV = screenPos.xy / screenPos.w;
                float sceneDepth = SampleSceneDepth(screenUV);
                float3 worldPos = ComputeWorldSpacePosition(screenUV, sceneDepth, UNITY_MATRIX_I_VP);
                
                float3 objectPos = TransformWorldToObject(worldPos);

                clip(0.5 - abs(objectPos.xyz));
                
                return objectPos.xz + 0.5;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = GetProjectedUVs(IN.screenPos);
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half4 finalColor = texColor * _Color;

                return finalColor;
            }
            ENDHLSL
        }
    }
}