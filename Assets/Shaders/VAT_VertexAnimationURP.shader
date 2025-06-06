Shader "VAT/VertexAnimationURP"
{
    Properties
    {
        [Header(Base Properties)]
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)

        [Header(Toon Shading)]
        _ToonThreshold ("Toon Threshold", Range(0,1)) = 0.5
        _ToonSmoothness ("Toon Smoothness", Range(0.001,0.5)) = 0.05
        [HDR] _ToonLitColor ("Toon Lit Color", Color) = (1,1,1,1)
        [HDR] _ToonShadowColor ("Toon Shadow Color", Color) = (0.5,0.5,0.5,1)
        [HDR] _AmbientColor ("Ambient Color (Overall)", Color) = (0.2, 0.2, 0.2, 1)

        [Header(VAT)]
        _VAT_PositionTex ("VAT Position Texture", 2D) = "white" {}
        _VAT_NormalTex ("VAT Normal Texture", 2D) = "white" {}
        _VAT_FrameCount ("VAT Frame Count", Float) = 1
        _VAT_VertexCount ("VAT Vertex Count", Float) = 1
        _VAT_AnimationLength ("VAT Animation Length", Float) = 1
        _AnimTime ("Animation Time", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _VAT_BAKE_NORMALS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Định nghĩa cấu trúc input và output
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };

            // Khai báo texture và sampler
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_VAT_PositionTex);
            SAMPLER(sampler_VAT_PositionTex);
            TEXTURE2D(_VAT_NormalTex);
            SAMPLER(sampler_VAT_NormalTex);

            // Khai báo biến material
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _ToonThreshold;
                float _ToonSmoothness;
                float4 _ToonLitColor;
                float4 _ToonShadowColor;
                float4 _AmbientColor;
                float _VAT_FrameCount;
                float _VAT_VertexCount;
                float _VAT_AnimationLength;
                float _AnimTime;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                // Tính toán UV để lấy mẫu texture VAT
                float normTime = saturate(_AnimTime / _VAT_AnimationLength);
                float frame = min(floor(normTime * _VAT_FrameCount), _VAT_FrameCount - 1);
                float U = (frame + 0.5) / _VAT_FrameCount;
                float V = (input.vertexID + 0.5) / _VAT_VertexCount;
                float2 vatUV = float2(U, V);

                // Lấy vị trí từ texture VAT
                float4 positionOS = SAMPLE_TEXTURE2D_LOD(_VAT_PositionTex, sampler_VAT_PositionTex, vatUV, 0);

                // Lấy pháp tuyến, tùy thuộc vào việc có bake hay không
                float3 normalOS;
                #if defined(_VAT_BAKE_NORMALS)
                    normalOS = SAMPLE_TEXTURE2D_LOD(_VAT_NormalTex, sampler_VAT_NormalTex, vatUV, 0).xyz;
                #else
                    normalOS = input.normalOS;
                #endif

                // Biến đổi sang không gian thế giới và clip
                output.positionWS = TransformObjectToWorld(positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                float3 normalWS = normalize(input.normalWS);

                // Lấy thông tin ánh sáng chính
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));

                // Tính toán NdotL cho toon shading
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float toonFactor = smoothstep(_ToonThreshold, _ToonThreshold + _ToonSmoothness, NdotL);

                // Áp dụng đổ bóng
                toonFactor *= mainLight.shadowAttenuation;

                // Tính màu toon
                float3 toonShadedColor = lerp(_ToonShadowColor.rgb, _ToonLitColor.rgb, toonFactor);
                float3 finalColor = baseColor.rgb * toonShadedColor + _AmbientColor.rgb;

                return float4(finalColor, baseColor.a);
            }

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}