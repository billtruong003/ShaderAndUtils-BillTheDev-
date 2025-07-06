// Made with Clean Code Principles
// Final Lean Version: Hyper-focused on the stylized rock aesthetic. No redundant logic.

Shader "Stylized/URP/Stylized Rock (Lean)"
{
    Properties
    {
        [Header(Base Properties)]
        _BaseMap("Base Map (RGB)", 2D) = "white" {}
        _BaseColor("Lit Color", Color) = (0.7, 0.7, 0.7, 1.0)
        _ShadowColor("Shadow Color", Color) = (0.4, 0.4, 0.45, 1.0)
        _ToonRampThreshold("Light/Shadow Threshold", Range(0, 1)) = 0.5

        [Header(Surface Detail)]
        _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalStrength("Normal Strength", Range(0, 2)) = 1.0

        [Header(Stylized Effects)]
        _RimColor("Rim Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _RimPower("Rim Power", Range(0.1, 10)) = 3.0
        _RimThreshold("Rim Threshold", Range(0, 1)) = 0.5

        [Header(Directional Overlay)]
        _OverlayColor("Moss/Snow Color", Color) = (0.2, 0.6, 0.2, 1.0)
        _OverlayDirection("Overlay Direction (World)", Vector) = (0, 1, 0)
        _OverlayFalloff("Overlay Falloff", Range(0.01, 1)) = 0.5
        _OverlayThreshold("Overlay Threshold", Range(-1, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" "LightMode"="UniversalForward" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : NORMAL;
                float3 tangentWS    : TANGENT;
                float3 bitangentWS  : BITANGENT;
                float2 uv           : TEXCOORD1;
                float4 shadowCoord  : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                sampler2D _BaseMap;
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _ShadowColor;
                float _ToonRampThreshold;

                sampler2D _NormalMap;
                float4 _NormalMap_ST;
                float _NormalStrength;
                
                float4 _RimColor;
                float _RimPower;
                float _RimThreshold;

                float4 _OverlayColor;
                float3 _OverlayDirection;
                float _OverlayFalloff;
                float _OverlayThreshold;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;

                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.tangentWS = TransformObjectToWorldDir(input.tangentOS.xyz);
                output.bitangentWS = cross(output.normalWS, output.tangentWS) * input.tangentOS.w;
                
                output.shadowCoord = GetShadowCoord(positionInputs);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // --- Surface Detail ---
                float3 normalTS = UnpackNormalScale(tex2D(_NormalMap, input.uv), _NormalStrength);
                float3x3 TBN = float3x3(normalize(input.tangentWS), normalize(input.bitangentWS), normalize(input.normalWS));
                float3 finalNormalWS = normalize(mul(normalTS, TBN));

                // --- Core Toon Lighting ---
                Light mainLight = GetMainLight(input.shadowCoord);
                float NdotL = saturate(dot(finalNormalWS, mainLight.direction));

                // A single factor that determines if a pixel is lit or in shadow (either by angle or cast shadow)
                float lightingFactor = step(_ToonRampThreshold, NdotL) * mainLight.shadowAttenuation;
                
                // Pure, artist-defined toon shading
                float3 toonColor = lerp(_ShadowColor.rgb, _BaseColor.rgb, lightingFactor);
                
                float4 baseMapColor = tex2D(_BaseMap, input.uv);
                float3 finalColor = toonColor * baseMapColor.rgb;

                // --- Stylized Effects ---
                float3 viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
                
                // Rim Light
                float rimDot = 1.0 - saturate(dot(viewDirection, finalNormalWS));
                float rimIntensity = pow(rimDot, _RimPower);
                rimIntensity = smoothstep(_RimThreshold, 1.0, rimIntensity);
                finalColor += _RimColor.rgb * rimIntensity;

                // Directional Overlay (Moss/Snow)
                float overlayDot = dot(finalNormalWS, normalize(_OverlayDirection));
                float overlayMask = smoothstep(_OverlayThreshold, _OverlayThreshold + _OverlayFalloff, overlayDot);
                finalColor = lerp(finalColor, _OverlayColor.rgb, overlayMask);
                
                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Lit"
}