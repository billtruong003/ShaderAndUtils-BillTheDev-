Shader "CleanCode/StylizedTriplanarToonURP"
{
    Properties
    {
        [Header(Triplanar Settings)]
        _RockAlbedo("Rock Texture (Albedo)", 2D) = "white" {}
        _GrassAlbedo("Grass Texture (Albedo)", 2D) = "white" {}
        _Tiling("Texture Tiling", Float) = 1.0
        _BlendSharpness("Axis Blend Sharpness", Range(1.0, 20.0)) = 8.0
        
        [Header(Terrain Blending)]
        _GrassBlendStart("Grass Blend Start", Range(0.0, 1.0)) = 0.5
        _GrassBlendFalloff("Grass Blend Falloff", Range(0.01, 1.0)) = 0.2

        [Header(Toon Lighting)]
        _ToonSteps("Toon Steps", Range(1, 10)) = 3
        _SpecularColor("Specular Color", Color) = (1,1,1,1)
        _SpecularGloss("Specular Gloss", Range(0.0, 1.0)) = 0.8
        _RimColor("Rim Color", Color) = (1,1,1,1)
        _RimPower("Rim Power", Range(0.1, 10.0)) = 3.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 worldPos     : TEXCOORD0;
                float3 worldNormal  : TEXCOORD1;
                half3  viewDir      : TEXCOORD2;
            };

            TEXTURE2D(_RockAlbedo);
            SAMPLER(sampler_RockAlbedo);
            TEXTURE2D(_GrassAlbedo);
            SAMPLER(sampler_GrassAlbedo);

            CBUFFER_START(UnityPerMaterial)
                float _Tiling;
                float _BlendSharpness;
                float _GrassBlendStart;
                float _GrassBlendFalloff;
                int _ToonSteps;
                half4 _SpecularColor;
                half _SpecularGloss;
                half4 _RimColor;
                half _RimPower;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.worldNormal = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDir = GetWorldSpaceNormalizeViewDir(OUT.worldPos);
                return OUT;
            }

            half4 SampleTriplanar(TEXTURE2D_PARAM(tex, smp), float3 worldPos, float3 blendWeights)
            {
                float2 uvX = worldPos.zy;
                float2 uvY = worldPos.xz;
                float2 uvZ = worldPos.xy;

                half4 sampleX = SAMPLE_TEXTURE2D(tex, smp, uvX);
                half4 sampleY = SAMPLE_TEXTURE2D(tex, smp, uvY);
                half4 sampleZ = SAMPLE_TEXTURE2D(tex, smp, uvZ);

                return sampleX * blendWeights.x + sampleY * blendWeights.y + sampleZ * blendWeights.z;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 worldNormal = normalize(IN.worldNormal);
                
                float3 absoluteNormal = abs(worldNormal);
                float3 blendWeights = pow(absoluteNormal, _BlendSharpness);
                blendWeights /= dot(blendWeights, 1.0);

                float3 scaledWorldPos = IN.worldPos / _Tiling;

                half4 rockColor = SampleTriplanar(TEXTURE2D_ARGS(_RockAlbedo, sampler_RockAlbedo), scaledWorldPos, blendWeights);
                half4 grassColor = SampleTriplanar(TEXTURE2D_ARGS(_GrassAlbedo, sampler_GrassAlbedo), scaledWorldPos, blendWeights);
                
                float upwardNormalDot = dot(worldNormal, float3(0, 1, 0));
                float grassMask = smoothstep(_GrassBlendStart, _GrassBlendStart + _GrassBlendFalloff, upwardNormalDot);
                
                half4 albedo = lerp(rockColor, grassColor, grassMask);

                Light mainLight = GetMainLight();
                half3 lightDir = mainLight.direction;
                half NdotL = saturate(dot(worldNormal, lightDir));

                half toonDiffuse = floor(NdotL * _ToonSteps) / _ToonSteps;
                half3 diffuseLighting = mainLight.color * toonDiffuse;

                half3 halfwayDir = normalize(lightDir + IN.viewDir);
                half specDot = pow(saturate(dot(worldNormal, halfwayDir)), _SpecularGloss * 128.0);
                half specularHighlight = step(0.95, specDot);
                half3 specularLighting = specularHighlight * _SpecularColor.rgb * mainLight.color;
                
                half rimDot = 1.0 - saturate(dot(IN.viewDir, worldNormal));
                half rimIntensity = pow(rimDot, _RimPower);
                half3 rimLighting = rimIntensity * _RimColor.rgb;

                half3 ambient = SampleSH(worldNormal);

                half3 finalColor = (ambient + diffuseLighting) * albedo.rgb + specularLighting + rimLighting;
                
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}