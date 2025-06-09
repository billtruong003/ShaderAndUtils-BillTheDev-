Shader "Custom/URP/LightweightToonFadeDistance"
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

        [Header(Near Fade Properties)]
        _FadeStartDistance ("Fade Start Distance (World Units)", Float) = 1.0
        _FadeEndDistance ("Fade End Distance (World Units)", Float) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fw_and_shadows

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _ToonThreshold;
                float _ToonSmoothness;
                float4 _ToonLitColor;
                float4 _ToonShadowColor;
                float4 _AmbientColor;
                float _FadeStartDistance;
                float _FadeEndDistance;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                float3 normalWS = normalize(input.normalWS);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));

                float NdotL = saturate(dot(normalWS, mainLight.direction));

                float toonFactor = smoothstep(_ToonThreshold, _ToonThreshold + _ToonSmoothness, NdotL);

                toonFactor *= mainLight.shadowAttenuation;

                float3 toonShadedColor = lerp(_ToonShadowColor.rgb, _ToonLitColor.rgb, toonFactor);

                float3 cameraPosWS = _WorldSpaceCameraPos.xyz;

                float distToCamera = distance(input.positionWS, cameraPosWS);

                float fadeFactor = saturate((distToCamera - _FadeEndDistance) / (_FadeStartDistance - _FadeEndDistance));

                baseColor.a *= fadeFactor;

                float3 finalColorRGB = baseColor.rgb * toonShadedColor + _AmbientColor.rgb;

                return float4(finalColorRGB, baseColor.a);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}