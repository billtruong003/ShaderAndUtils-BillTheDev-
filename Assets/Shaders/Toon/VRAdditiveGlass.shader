Shader "Custom/VR Additive Glass"
{
    Properties
    {
        _GlassColor("Glass Color & Opacity", Color) = (0.8, 0.9, 1.0, 0.2)
        _FresnelColor("Fresnel (Edge) Color & Opacity", Color) = (1, 1, 1, 0.5)
        _FresnelPower("Fresnel Power", Range(0.1, 10.0)) = 5.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        
        Pass
        {
            // Đây là chế độ hoà trộn cho kính vật lý
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _GlassColor;
                half4 _FresnelColor;
                half _FresnelPower;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                half fresnelDot     : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(positionWS);
                
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                float3 viewDirWS = normalize(_WorldSpaceCameraPos.xyz - positionWS);
                
                o.fresnelDot = dot(normalWS, viewDirWS);

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // 1. Tính Fresnel
                half fresnelDotVal = 1.0 - saturate(i.fresnelDot);
                half fresnelAmount = pow(fresnelDotVal, _FresnelPower);
                
                // 2. Trộn màu và alpha
                // Trộn màu kính và màu fresnel
                half3 finalColor = lerp(_GlassColor.rgb, _FresnelColor.rgb, fresnelAmount);
                // Trộn độ trong suốt: Ở giữa sẽ trong hơn, ở cạnh sẽ đặc hơn
                half finalAlpha = lerp(_GlassColor.a, _FresnelColor.a, fresnelAmount);

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}