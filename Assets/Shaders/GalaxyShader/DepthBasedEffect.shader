Shader "Universal Render Pipeline/Custom/ProceduralAdvancedEffect"
{
    Properties
    {
        [Header(Core Colors)]
        _BaseColor("Base Color", Color) = (0.05, 0.05, 0.1, 1.0)
        
        [Header(Depth Pattern)]
        [Enum(Lines, 0, Hex Grid, 1)] _PatternType ("Pattern Type", Float) = 0
        _PatternColor("Pattern Color", Color) = (0, 0.5, 1, 1)
        _PatternScale("Pattern Scale", Float) = 10.0
        _PatternThickness("Pattern Thickness", Range(0.01, 0.5)) = 0.1

        [Header(Depth Effect)]
        _DepthMaxDistance("Depth Max Distance", Float) = 10.0
        _DepthFalloff("Depth Falloff", Range(0.1, 10.0)) = 2.5

        [Header(Fresnel Effect)]
        _FresnelEdgeColor("Fresnel Edge Color", Color) = (1, 1, 1, 1)
        _FresnelMidColor("Fresnel Mid Color", Color) = (0, 0.8, 1, 1)
        _FresnelPower("Fresnel Power", Range(0.1, 15.0)) = 5.0
        
        [Header(Procedural Distortion)]
        _DistortionStrength("Distortion Strength", Range(0, 0.1)) = 0.01
        _DistortionFrequency("Distortion Frequency", Float) = 20.0
        _DistortionSpeed("Distortion Speed", Float) = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalRenderPipeline"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float4 positionNDC  : TEXCOORD0;
                float3 normalWS     : NORMAL;
                float3 viewDirWS    : TEXCOORD1;
                float2 uv           : TEXCOORD2;
            };

            TEXTURE2D_X_FLOAT(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _PatternColor;
                float _PatternScale;
                float _PatternThickness;
                float _DepthMaxDistance;
                float _DepthFalloff;
                float4 _FresnelEdgeColor;
                float4 _FresnelMidColor;
                float _FresnelPower;
                float _DistortionStrength;
                float _DistortionFrequency;
                float _DistortionSpeed;
                float _PatternType;
            CBUFFER_END

            // --- Procedural Generation Functions ---

            float2 procedural_distortion(float2 uv, float time, float frequency, float strength)
            {
                float angle = time + uv.y * frequency;
                float2 offset = float2(sin(angle), cos(angle)) * strength;
                return offset;
            }

            float procedural_lines(float2 uv, float scale, float thickness)
            {
                float lineValue = abs(frac(uv.y * scale) * 2.0 - 1.0);
                return 1.0 - smoothstep(thickness, thickness + 0.05, lineValue);
            }

            float procedural_hex_grid(float2 uv, float scale, float thickness)
            {
                const float2 hexMagic1 = float2(1.0, 1.73205);
                const float3 hexMagic2 = float3(1.0, 0.5, 1.73205);

                float2 gridUV = uv * scale;
                float2 p = abs(frac(gridUV / hexMagic1.xy) - 0.5);
                float d = abs(p.x - p.y * 0.5) * 2.0;
                float hexLine = min(d, p.y * hexMagic2.z);
                
                return 1.0 - smoothstep(thickness, thickness + 0.05, hexLine);
            }

            // --- Main Shaders ---

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionNDC = output.positionCS;
                
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);
                output.uv = input.uv;
                
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float time = _Time.y * _DistortionSpeed;
                float2 distortionOffset = procedural_distortion(input.uv, time, _DistortionFrequency, _DistortionStrength);
                float2 distortedUV = input.uv + distortionOffset;

                float patternMask = 0.0;
                if (_PatternType < 0.5) // Lines
                {
                    patternMask = procedural_lines(distortedUV, _PatternScale, _PatternThickness);
                }
                else // Hex Grid
                {
                    patternMask = procedural_hex_grid(distortedUV, _PatternScale, _PatternThickness);
                }
                
                float2 screenUV = input.positionNDC.xy / input.positionNDC.w;
                float sceneRawDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV).r;
                float sceneLinearDepth = LinearEyeDepth(sceneRawDepth, _ZBufferParams);
                
                float fragmentLinearDepth = input.positionNDC.z;
                float depthDifference = sceneLinearDepth - fragmentLinearDepth;
                float depthFactor = pow(1.0 - saturate(depthDifference / _DepthMaxDistance), _DepthFalloff);

                half3 patternColor = lerp(_BaseColor.rgb, _PatternColor.rgb, patternMask);
                half3 sceneAwareColor = lerp(patternColor, _BaseColor.rgb, depthFactor);

                float3 viewDir = normalize(input.viewDirWS);
                float3 normalDir = normalize(input.normalWS);
                float fresnelDot = dot(normalDir, viewDir);
                float fresnelFactor = pow(1.0 - saturate(fresnelDot), _FresnelPower);
                half3 fresnelColor = lerp(_FresnelMidColor.rgb, _FresnelEdgeColor.rgb, fresnelFactor);

                half3 finalColor = sceneAwareColor + fresnelColor * fresnelFactor;
                half alpha = _BaseColor.a + fresnelFactor * _FresnelEdgeColor.a;

                return half4(finalColor, saturate(alpha));
            }
            ENDHLSL
        }
    }
}