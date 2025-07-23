// Tên file: ToonGrid.shader
Shader "CleanCodeShaders/ToonGridPrototype"
{
    Properties
    {
        [Header(Grid Settings)]
        _GridTiling("Grid Tiling", Range(1, 10000)) = 10
        _GridThickness("Grid Thickness", Range(0.001, 0.5)) = 0.05
        _GridColor("Grid Color", Color) = (0, 0, 0, 1)

        [Header(Toon Shading Settings)]
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _ShadowColor("Shadow Color", Color) = (0.5, 0.5, 0.5, 1)
        _ToonStep("Toon Step", Range(0, 1)) = 0.5
        _Smoothness("Smoothness", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertexMain
            #pragma fragment FragmentMain

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct VertexInput
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct VertexOutput
            {
                float4 positionCS   : SV_POSITION;
                float3 normalWS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float _GridTiling;
                float _GridThickness;
                float4 _GridColor;
                float4 _BaseColor;
                float4 _ShadowColor;
                float _ToonStep;
                float _Smoothness;
            CBUFFER_END

            VertexOutput VertexMain(VertexInput IN)
            {
                VertexOutput OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv;
                return OUT;
            }

            float CalculateGridFactor(float2 uv, float tiling, float thickness)
            {
                float2 tiledUV = frac(uv * tiling);
                float2 distanceToCenter = abs(tiledUV - 0.5);
                float maxDistance = max(distanceToCenter.x, distanceToCenter.y);
                float halfThickness = thickness * 0.5;
                
                // Anti-aliasing for smoother lines
                float edgeSoftness = fwidth(maxDistance) * 1.5;
                
                return smoothstep(0.5 - halfThickness - edgeSoftness, 0.5 - halfThickness, maxDistance);
            }

            float3 CalculateToonShading(float3 worldNormal, float3 baseColor, float3 shadowColor, float toonStep)
            {
                Light mainLight = GetMainLight();
                float3 lightDirection = mainLight.direction;
                float lightIntensity = saturate(dot(worldNormal, lightDirection));
                
                float lightingStep = step(toonStep, lightIntensity);
                
                return lerp(shadowColor, baseColor, lightingStep);
            }

            float4 FragmentMain(VertexOutput IN) : SV_Target
            {
                float3 normalizedNormal = normalize(IN.normalWS);
                
                float3 toonShadedColor = CalculateToonShading(normalizedNormal, _BaseColor.rgb, _ShadowColor.rgb, _ToonStep);
                
                float gridFactor = CalculateGridFactor(IN.uv, _GridTiling, _GridThickness);
                
                float3 finalColor = lerp(_GridColor.rgb, toonShadedColor, gridFactor);
                
                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}