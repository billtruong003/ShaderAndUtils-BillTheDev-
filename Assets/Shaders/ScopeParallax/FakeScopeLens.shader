Shader "CleanCode/FakeScopeLens"
{
    Properties
    {
        [Header(Base Properties)]
        _BaseMap("Lens Texture (Albedo)", 2D) = "white" {}
        _TintColor("Tint Color", Color) = (0.8, 0.9, 1.0, 0.1)
        
        [Header(Surface Details)]
        _NormalMap("Normal Map", 2D) = "bump" {}
        [PowerSlider(5.0)] _NormalStrength("Normal Strength", Range(0.0, 2.0)) = 1.0

        [Header(Reflection)]
        _Cubemap("Reflection Cubemap", Cube) = "_Default" {}
        _ReflectionStrength("Reflection Strength", Range(0.0, 1.0)) = 0.4
        _FresnelPower("Fresnel Power", Range(0.1, 10.0)) = 2.5

        [Header(Render State)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 2 // Back
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4 // LEqual
        [Enum(Off, 0, On, 1)] _ZWrite("ZWrite", Float) = 0 // Off
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            Cull [_Cull]
            ZTest [_ZTest]
            ZWrite [_ZWrite]
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 viewDirWS    : TEXCOORD1;
                float3 normalWS     : TEXCOORD2;
                float3 tangentWS    : TEXCOORD3;
                float3 bitangentWS  : TEXCOORD4;
            };

            TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);      SAMPLER(sampler_NormalMap);
            TEXTURECUBE(_Cubemap);      SAMPLER(sampler_Cubemap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _TintColor;
                half _NormalStrength;
                half _ReflectionStrength;
                half _FresnelPower;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);
                
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = normalInputs.tangentWS;
                output.bitangentWS = normalInputs.bitangentWS;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _TintColor;
                
                float3 tangentNormal = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv), _NormalStrength);
                float3x3 tbn = float3x3(input.tangentWS, input.bitangentWS, input.normalWS);
                float3 worldNormal = normalize(mul(tangentNormal, tbn));

                float3 viewDir = normalize(input.viewDirWS);
                float3 reflectVec = reflect(-viewDir, worldNormal);

                half4 reflection = SAMPLE_TEXTURECUBE(_Cubemap, sampler_Cubemap, reflectVec);
                reflection.rgb *= _ReflectionStrength;

                half fresnel = pow(1.0 - saturate(dot(viewDir, worldNormal)), _FresnelPower);
                
                half3 finalColor = lerp(albedo.rgb, reflection.rgb, fresnel);
                
                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }
    }
    FallBack "Transparent/VertexLit"
}