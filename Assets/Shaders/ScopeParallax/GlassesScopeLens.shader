Shader "Stylized/TrueUV_Independent_Glass_URP"
{
    Properties
    {
        [Header(Core Appearance)]
        _BaseColor("Base 'Transparency' Color", Color) = (0.8, 0.9, 1.0, 0.1)

        [Header(Procedural Surface Reflection)]
        _RimColorTop ("Reflection Top Color", Color) = (0.5, 0.7, 1.0, 1.0)
        _RimColorBottom ("Reflection Bottom Color", Color) = (0.2, 0.15, 0.1, 1.0)
        _RimBlendSharpness("Reflection Blend Sharpness", Range(0.1, 5.0)) = 1.5

        [Header(Colored Edge Effect)]
        _EdgeColor("Fresnel Edge Color & Opacity", Color) = (1, 0.5, 0, 1)
        _EdgeFresnelPower("Edge Fresnel Power", Range(1.0, 20.0)) = 7.0

        [Header(Direct Lighting)]
        _SpecularColor("Specular Highlight Color", Color) = (1,1,1,1)
        _Glossiness("Glossiness", Range(1, 256)) = 100
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS       : SV_POSITION;
                float3 normalWS         : TEXCOORD0;
                float3 viewDirWS        : TEXCOORD1;
            };
            
            float4 _BaseColor, _RimColorTop, _RimColorBottom, _EdgeColor, _SpecularColor;
            float _RimBlendSharpness, _EdgeFresnelPower, _Glossiness;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);

                // --- 1. Procedural Reflection: Surface color based on world orientation ---
                float rimFactor = saturate(dot(normalWS, float3(0, 1, 0)) * 0.5 + 0.5);
                rimFactor = pow(rimFactor, _RimBlendSharpness);
                float3 proceduralReflection = lerp(_RimColorBottom.rgb, _RimColorTop.rgb, rimFactor);
                
                // --- 2. Fresnel: The master controller for blending ---
                float fresnel = pow(1.0 - saturate(dot(viewDirWS, normalWS)), _EdgeFresnelPower);
                
                // --- 3. Blend from 'transparent' base to reflective surface ---
                // At center (fresnel=0), we see the BaseColor. At edges (fresnel=1), we see the Reflection.
                float3 surfaceColor = lerp(_BaseColor.rgb, proceduralReflection, fresnel);

                // --- 4. Override with the explicit Edge Color at the very edge ---
                float3 finalColor = lerp(surfaceColor, _EdgeColor.rgb, fresnel);

                // --- 5. Add Specular Highlight on top ---
                Light mainLight = GetMainLight();
                float3 halfVector = SafeNormalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfVector));
                float specularFalloff = pow(NdotH, _Glossiness);
                float3 specularHighlight = specularFalloff * mainLight.color * _SpecularColor.rgb;
                finalColor += specularHighlight;

                // --- 6. Final Alpha Calculation ---
                // Blend from base opacity to edge opacity, then add specular opacity.
                float finalAlpha = lerp(_BaseColor.a, _EdgeColor.a, fresnel);
                finalAlpha = saturate(finalAlpha + specularFalloff);
                
                return float4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
    Fallback "Transparent/VertexLit"
}