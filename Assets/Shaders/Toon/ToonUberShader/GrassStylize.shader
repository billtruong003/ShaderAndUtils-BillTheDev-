Shader "Stylized/URP/Grass Terrain Toon (No Outline)"
{
    Properties
    {
        [Header(Texture and Alpha Clipping)]
        _MainTex("Grass Texture (A = Opacity)", 2D) = "white" {}
        _AlphaCutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [Header(Color Settings)]
        _BaseColor("Base Color", Color) = (0.2, 0.5, 0.1, 1.0)
        _TipColor("Tip Color", Color) = (0.5, 0.8, 0.2, 1.0)
        _ShadowColor("Shadow Color", Color) = (0.1, 0.3, 0.05, 1.0)
        _ColorHeight("Color Height", Range(0, 5)) = 1.0

        [Header(Toon Shading)]
        _CelShadingThreshold("Cel Shading Threshold", Range(0, 1)) = 0.5

        [Header(Wind Effect)]
        _WindSpeed("Wind Speed", Range(0, 5)) = 1.0
        _WindStrength("Wind Strength", Range(0, 1)) = 0.1
        _WindScale("Wind Scale", Range(0.1, 10)) = 2.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderPipeline" = "UniversalRenderPipeline" 
            "RenderType"="TransparentCutout"
            "Queue"="AlphaTest"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _AlphaCutoff;
                float4 _BaseColor;
                float4 _TipColor;
                float4 _ShadowColor;
                float _ColorHeight;
                float _CelShadingThreshold;
                float _WindSpeed;
                float _WindStrength;
                float _WindScale;
            CBUFFER_END
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float2 uv           : TEXCOORD2;
            };

            void ApplyWind(inout float3 positionWS)
            {
                float windOffsetX = sin(_Time.y * _WindSpeed + positionWS.x * _WindScale) * _WindStrength;
                float windOffsetZ = cos(_Time.y * _WindSpeed + positionWS.z * _WindScale) * _WindStrength;
                positionWS.x += windOffsetX;
                positionWS.z += windOffsetZ;
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                
                ApplyWind(o.positionWS);

                o.positionHCS = TransformWorldToHClip(o.positionWS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                clip(texColor.a - _AlphaCutoff);

                float heightFactor = saturate(i.positionWS.y / _ColorHeight);
                half4 baseColor = lerp(_BaseColor, _TipColor, heightFactor);
                baseColor.rgb *= texColor.rgb;

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float3 normalDir = normalize(i.normalWS);
                float NdotL = dot(normalDir, mainLight.direction);
                float lightIntensity = step(_CelShadingThreshold, NdotL) * mainLight.shadowAttenuation;

                float3 ambient = SampleSH(normalDir);
                float3 finalColor = lerp(_ShadowColor.rgb, baseColor.rgb, lightIntensity) * mainLight.color;
                finalColor += ambient * baseColor.rgb;

                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _AlphaCutoff;
                float _WindSpeed;
                float _WindStrength;
                float _WindScale;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };
            
            void ApplyWind(inout float3 positionWS)
            {
                float windOffsetX = sin(_Time.y * _WindSpeed + positionWS.x * _WindScale) * _WindStrength;
                float windOffsetZ = cos(_Time.y * _WindSpeed + positionWS.z * _WindScale) * _WindStrength;
                positionWS.x += windOffsetX;
                positionWS.z += windOffsetZ;
            }

            Varyings ShadowVert(Attributes v)
            {
                Varyings o;
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                Light mainLight = GetMainLight();

                ApplyWind(positionWS);
                
                float3 biasedPositionWS = ApplyShadowBias(positionWS, normalWS, mainLight.direction);
                o.positionCS = TransformWorldToHClip(biasedPositionWS);

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 ShadowFrag(Varyings i) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                clip(texColor.a - _AlphaCutoff);
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}