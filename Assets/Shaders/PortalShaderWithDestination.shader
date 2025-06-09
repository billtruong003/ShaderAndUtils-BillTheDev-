Shader "Custom/PortalShaderWithDestination"
{
    Properties
    {
        _MainTexture ("Main Texture", 2D) = "white" {}
        _PortalSize ("Portal Size", Range(0.1, 1.0)) = 0.4
        _PortalSoftness ("Portal Softness", Float) = 3.0
        [HDR] _MainColor ("Main Color", Color) = (0,0,1,0.7)
        _DestinationTexture ("Destination Texture", 2D) = "white" {}
        _DestinationAlpha ("Destination Alpha", Range(0.0, 1.0)) = 0.5
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                TEXTURE2D(_MainTexture); SAMPLER(sampler_MainTexture);
                float _PortalSize;
                float _PortalSoftness;
                float4 _MainColor;
                TEXTURE2D(_DestinationTexture); SAMPLER(sampler_DestinationTexture);
                float _DestinationAlpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // Tính toán mask của portal
                float2 centeredUV = uv - float2(0.5, 0.5);
                float distFromCenter = length(centeredUV);
                float rawMask = distFromCenter / _PortalSize;
                float portalAlphaMask = pow(saturate(1.0 - rawMask), _PortalSoftness);
                portalAlphaMask = saturate(portalAlphaMask);

                // Lấy mẫu texture chính
                float4 baseColor = SAMPLE_TEXTURE2D(_MainTexture, sampler_MainTexture, uv) * _MainColor;
                float4 finalColor = baseColor * portalAlphaMask;
                finalColor.a = portalAlphaMask;

                // Lấy mẫu texture điểm đến
                float4 destinationColor = SAMPLE_TEXTURE2D(_DestinationTexture, sampler_DestinationTexture, uv);
                destinationColor.a *= _DestinationAlpha * portalAlphaMask;

                // Kết hợp texture điểm đến với màu sắc của portal
                finalColor = lerp(finalColor, destinationColor, destinationColor.a);

                return finalColor;
            }
            ENDHLSL
        }
    }
}