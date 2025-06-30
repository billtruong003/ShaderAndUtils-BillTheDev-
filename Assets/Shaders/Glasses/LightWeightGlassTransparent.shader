// This is an extremely lightweight shader for transparent glass, designed to be
// faster than Unity's default Unlit and Simple Lit shaders.
// It achieves this by stripping out all non-essential features like texture
// sampling and complex lighting, focusing only on a base color and a cheap
// Fresnel effect for volume.
Shader "Minimalist/Ultra Lightweight Transparent Glass"
{
    Properties
    {
        [MainColor] _BaseColor("Color & Transparency", Color) = (1, 1, 1, 0.25)

        [Header(Rim Effect)]
        [HDR] _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower("Rim Power", Range(0.1, 10.0)) = 3.0
    }

    SubShader
    {
        // Tags are crucial. We set it to the Transparent queue.
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }

        Pass
        {
            // --- Render State Commands ---
            // Standard alpha blending
            Blend SrcAlpha OneMinusSrcAlpha
            // Do not write to the depth buffer, essential for transparent objects
            ZWrite Off
            // Render both front and back faces, common for glass
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _RimColor;
                half _RimPower;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 normalWS     : TEXCOORD0;
                float3 viewDirWS    : TEXCOORD1;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs posInputs = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(v.normalOS);
                o.positionCS = posInputs.positionCS;
                o.normalWS = normInputs.normalWS;
                o.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // --- Calculations ---
                // Normalize vectors for accuracy.
                half3 normalWS = normalize(i.normalWS);
                half3 viewDirWS = normalize(i.viewDirWS);

                // Calculate a cheap Fresnel/Rim effect. This is the only "effect"
                // we have, and it's very fast. It gives the glass a sense of shape.
                half rimDot = 1.0 - saturate(dot(viewDirWS, normalWS));
                half rimIntensity = pow(rimDot, _RimPower);
                half3 rim = _RimColor.rgb * rimIntensity;

                // --- Final Composition ---
                // Add the rim highlight to the base color's RGB.
                // The final alpha is taken directly from the Base Color's alpha channel.
                half3 finalColor = _BaseColor.rgb + rim;
                half finalAlpha = _BaseColor.a;

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
    // No fallback needed, it's simple enough to run anywhere.
}