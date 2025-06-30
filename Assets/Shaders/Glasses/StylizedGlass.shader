// Shader for creating a high-performance, stylized glass effect using
// alpha clipping and dithering in Unity's Universal Render Pipeline (URP).
//
// This shader is significantly faster than traditional transparent shaders
// because it runs in the Opaque render queue, allowing it to use the Z-buffer
// and avoid expensive overdraw and alpha blending.
Shader "Stylized/Dithered Glass"
{
    // Properties block to expose controls in the Material Inspector.
    Properties
    {
        [Header(Main Properties)]
        _BaseColor("Base Color", Color) = (0.5, 0.8, 1.0, 1.0)
        [HDR] _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower("Rim Power", Range(0.1, 10.0)) = 2.5
        _Transparency("Transparency", Range(0.0, 1.0)) = 0.5

        [Header(Specular Highlight)]
        [HDR] _SpecColor("Specular Color", Color) = (1, 1, 1, 1)
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.8
    }

    SubShader
    {
        // Tags are crucial for URP to understand how to render this shader.
        // "RenderType"="Opaque" is the key to our performance optimization.
        // We use the AlphaTest queue to ensure it renders correctly with other objects.
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="AlphaTest"
        }

        Pass
        {
            // HLSL code block
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Include URP's core and lighting libraries.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Define a CBUFFER to link the properties to our HLSL code.
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _RimColor;
                half _RimPower;
                half _Transparency;
                half4 _SpecColor;
                half _Smoothness;
            CBUFFER_END

            // A simple 4x4 Bayer matrix for dithering.
            // This pattern creates the illusion of transparency.
            static const float dither_matrix_4x4[16] =
            {
                1,  9,  3, 11,
                13, 5, 15,  7,
                4, 12,  2, 10,
                16, 8, 14,  6
            };

            // Input structure for the vertex shader.
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            // Output structure from the vertex shader, passed to the fragment shader.
            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 normalWS     : TEXCOORD0;
                float3 viewDirWS    : TEXCOORD1;
                float4 screenPos    : TEXCOORD2; // For dithering
            };

            // The Vertex Shader
            Varyings vert(Attributes v)
            {
                Varyings o;
                // Transform position and normal from object space to world/clip space.
                VertexPositionInputs posInputs = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(v.normalOS);

                o.positionCS = posInputs.positionCS;
                o.normalWS = normInputs.normalWS;
                o.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
                
                // Calculate screen position for dithering pattern.
                o.screenPos = ComputeScreenPos(o.positionCS);
                return o;
            }

            // The Fragment Shader
            half4 frag(Varyings i) : SV_Target
            {
                // --- Dithering and Alpha Clipping ---
                // This is the core of the technique.
                // We calculate a dither value based on the pixel's screen position.
                float2 ditherPos = i.screenPos.xy / i.screenPos.w * _ScreenParams.xy;
                int index = (int(ditherPos.x) % 4) * 4 + (int(ditherPos.y) % 4);
                float ditherValue = (dither_matrix_4x4[index] - 1) / 16.0;

                // The clip() function discards the pixel if the input is negative.
                // By comparing the dither value to our transparency property, we can
                // selectively discard pixels to create a see-through effect.
                clip(_Transparency - ditherValue);
                
                // --- Vector Normalization ---
                // Normalize vectors for accurate lighting calculations.
                i.normalWS = normalize(i.normalWS);
                i.viewDirWS = normalize(i.viewDirWS);

                // --- Rim Light (Fresnel Effect) ---
                // Creates a glow on the edges of the object.
                half rimDot = 1.0 - saturate(dot(i.viewDirWS, i.normalWS));
                half rimIntensity = pow(rimDot, _RimPower);
                half3 rimColor = _RimColor.rgb * rimIntensity;

                // --- Specular Highlight (Blinn-Phong) ---
                // Creates a shiny highlight based on the main light source.
                Light mainLight = GetMainLight();
                half3 lightDir = mainLight.direction;
                half3 halfVec = normalize(lightDir + i.viewDirWS);
                half specDot = saturate(dot(i.normalWS, halfVec));
                // Convert smoothness (0-1) to a power value for pow().
                half specPower = exp2(_Smoothness * 11.0 + 1.0);
                half specular = pow(specDot, specPower);
                half3 specularColor = _SpecColor.rgb * specular * mainLight.color;

                // --- Final Color Composition ---
                // Combine the base color, rim, and specular highlight.
                // Using addition gives a bright, stylized look.
                half3 finalColor = _BaseColor.rgb;
                finalColor += rimColor;
                finalColor += specularColor;

                // Return the final color. Alpha is 1.0 because we are in the Opaque queue.
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    // Fallback for older hardware or different render pipelines.
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}