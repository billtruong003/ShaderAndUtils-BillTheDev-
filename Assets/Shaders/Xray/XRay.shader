Shader "CleanCode/XRay"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _XRayColor("X-Ray Color", Color) = (0,1,0,0.5)
        _FresnelColor("Fresnel Color", Color) = (1,1,1,1)
        _FresnelPower("Fresnel Power", Range(0.1, 10.0)) = 2.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        // Khai báo chung cho cả hai pass
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
        half4 _BaseColor;
        half4 _XRayColor;
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
            half3 normalWS      : TEXCOORD0; // Sửa lỗi: Truyền normal vào đây
            half fresnelTerm    : TEXCOORD1; // Chỉ truyền giá trị, không phải màu
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings vert(Attributes input)
        {
            Varyings output = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            
            float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            float3 viewDirWS = normalize(_WorldSpaceCameraPos.xyz - positionWS);
            
            half fresnelDot = 1.0 - saturate(dot(output.normalWS, viewDirWS));
            output.fresnelTerm = pow(fresnelDot, _FresnelPower);

            return output;
        }
        ENDHLSL

        // PASS 1: Dành cho vật thể khi bị che khuất
        Pass
        {
            Name "XRay"
            Tags { "LightMode" = "UniversalForward" }
            
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            Stencil
            {
                Ref 1
                Comp Equal
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_xray

            half4 frag_xray(Varyings input) : SV_Target
            {
                // Kết hợp màu X-ray và hiệu ứng Fresnel
                half3 fresnelEffect = input.fresnelTerm * _FresnelColor.rgb;
                half3 finalColor = _XRayColor.rgb + fresnelEffect;
                return half4(finalColor, _XRayColor.a);
            }
            ENDHLSL
        }

        // PASS 2: Dành cho vật thể khi nhìn thấy bình thường
        Pass
        {
            Name "Opaque"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            ZTest LEqual
            Cull Back

            Stencil
            {
                Ref 1
                Comp NotEqual
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_opaque

            half4 frag_opaque(Varyings input) : SV_Target
            {
                // Sửa lỗi: Sử dụng đúng normalWS
                half3 normalWS = normalize(input.normalWS);
                
                // Lấy ánh sáng chính
                Light mainLight = GetMainLight();
                half dotNL = saturate(dot(normalWS, mainLight.direction));
                half3 directLighting = dotNL * mainLight.color;
                
                // Sửa lỗi: Sử dụng SampleSH để lấy ánh sáng môi trường
                half3 ambientLighting = SampleSH(normalWS);
                
                // Kết hợp ánh sáng và màu cơ bản
                half3 litColor = _BaseColor.rgb * (directLighting + ambientLighting);

                // Thêm hiệu ứng Fresnel
                half3 fresnelEffect = input.fresnelTerm * _FresnelColor.rgb;
                litColor += fresnelEffect;

                return half4(litColor, _BaseColor.a);
            }
            ENDHLSL
        }
    }
}