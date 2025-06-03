Shader "Custom/URP/AdvancedDissolveToonShader"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _DissolveThreshold ("Dissolve Threshold", Range(-10, 10)) = 0.5
        _EdgeWidth ("Edge Width", Range(0, 10)) = 0.02
        [HDR] _EdgeColor ("Edge Color", Color) = (1, 0.5, 0, 1)
        [IntRange] _DissolveType ("Dissolve Type", Range(0,4)) = 0 // 0: Noise, 1: Linear Gradient, 2: Radial Gradient, 3: Pattern, 4: Alpha Blend
        _Direction ("Direction (X, Y, Z)", Vector) = (0, 1, 0, 0)
        [IntRange] _PatternType ("Pattern Type", Range(0,2)) = 0 // 0: SinCos, 1: Checker, 2: Grid
        _PatternFrequency ("Pattern Frequency", Float) = 10
        _TimeScale ("Time Scale", Float) = 1
        [Toggle] _UseTimeAnimation ("Use Time Animation", Float) = 0
        [Toggle] _ZWrite ("ZWrite", Range(0,1)) = 0
        _ToonRampSmoothness ("Toon Ramp Smoothness", Range(0,1)) = 0.1
        _ToonRampOffset ("Toon Ramp Offset", Range(0,1)) = 0.5
        _ToonRampTinting ("Toon Ramp Tinting", Color) = (1,1,1,1)
        [Toggle(LOCAL)] _LocalSpace ("Use Local Space", Float) = 0
        _NoiseStrength ("Noise Strength", Range(0,1)) = 0.1
        _VertexDisplacement ("Vertex Displacement", Range(0,1)) = 0
        _AmbientColor ("Ambient Color", Color) = (0.2, 0.2, 0.2, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            ZWrite [_ZWrite]
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ LOCAL
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
                float3 positionOS : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                float dissolveValue : TEXCOORD4; // Pass dissolve value to fragment
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NoiseTex_ST;
                float _DissolveThreshold;
                float _EdgeWidth;
                float4 _EdgeColor;
                int _DissolveType;
                float4 _Direction;
                int _PatternType;
                float _PatternFrequency;
                float _TimeScale;
                float _UseTimeAnimation;
                float _ZWrite;
                float _ToonRampSmoothness;
                float _ToonRampOffset;
                float4 _ToonRampTinting;
                float _LocalSpace;
                float _NoiseStrength;
                float _VertexDisplacement;
                float4 _AmbientColor;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionOS = input.positionOS.xyz;
                float noise = frac(sin(dot(input.uv, float2(12.9898, 78.233))) * 43758.5453); // Procedural noise

                // Compute dissolve value based on type
                float dissolveValue = 0;
                float3 position = _LocalSpace ? input.positionOS.xyz : TransformObjectToWorld(input.positionOS.xyz);

                if (_DissolveType == 0) // Noise
                {
                    dissolveValue = noise;
                }
                else if (_DissolveType == 1) // Linear Gradient
                {
                    dissolveValue = dot(position, normalize(_Direction.xyz));
                }
                else if (_DissolveType == 2) // Radial Gradient
                {
                    float2 center = float2(0.5, 0.5);
                    dissolveValue = length(input.uv - center);
                }
                else if (_DissolveType == 3) // Pattern
                {
                    dissolveValue = sin(input.uv.x * _PatternFrequency) * cos(input.uv.y * _PatternFrequency) * 0.5 + 0.5; // Simplified pattern
                }
                else if (_DissolveType == 4) // Alpha Blend
                {
                    dissolveValue = noise;
                }

                float timeAnim = _UseTimeAnimation > 0.5 ? sin(_Time.y * _TimeScale) * 0.05 : 0;
                float threshold = _DissolveThreshold + timeAnim;
                float perturbedDissolveValue = dissolveValue + (noise - 0.5) * _NoiseStrength;

                // Vertex displacement based on dissolve edge
                float edgeFactor = smoothstep(threshold - _EdgeWidth, threshold, perturbedDissolveValue);
                float displacement = edgeFactor * (1.0 - edgeFactor) * _VertexDisplacement; // Peaks at the edge
                input.positionOS.xyz += input.normalOS * displacement;

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionOS = positionOS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.dissolveValue = dissolveValue; // Pass to fragment for consistency
                return output;
            }

            float GetPatternValue(float2 uv, int patternType, float frequency)
            {
                float pattern = 0;
                if (patternType == 0) // SinCos
                {
                    pattern = sin(uv.x * frequency) * cos(uv.y * frequency);
                }
                else if (patternType == 1) // Checker
                {
                    float2 checker = floor(uv * frequency);
                    pattern = fmod(checker.x + checker.y, 2.0);
                }
                else if (patternType == 2) // Grid
                {
                    float2 grid = frac(uv * frequency);
                    pattern = step(0.1, grid.x) * step(0.1, grid.y);
                }
                return pattern * 0.5 + 0.5;
            }

            void ToonShading_float(in float3 Normal, in float ToonRampSmoothness, in float3 ClipSpacePos, in float3 WorldPos, in float4 ToonRampTinting,
                in float ToonRampOffset, out float3 ToonRampOutput, out float3 Direction)
            {
                #ifdef SHADERGRAPH_PREVIEW
                    ToonRampOutput = float3(0.5, 0.5, 0);
                    Direction = float3(0.5, 0.5, 0);
                #else
                    #if SHADOWS_SCREEN
                        half4 shadowCoord = ComputeScreenPos(ClipSpacePos);
                    #else
                        half4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
                    #endif 

                    #if _MAIN_LIGHT_SHADOWS_CASCADE || _MAIN_LIGHT_SHADOWS
                        Light light = GetMainLight(shadowCoord);
                    #else
                        Light light = GetMainLight();
                    #endif

                    half d = dot(Normal, light.direction) * 0.5 + 0.5;
                    half toonRamp = smoothstep(ToonRampOffset, ToonRampOffset + ToonRampSmoothness, d);
                    toonRamp *= light.shadowAttenuation;
                    ToonRampOutput = light.color * (toonRamp + ToonRampTinting);
                    Direction = light.direction;

                    // Fallback for no light
                    if (dot(light.color, light.color) == 0)
                    {
                        ToonRampOutput = float3(1, 1, 1);
                    }
                #endif
            }

            float4 frag(Varyings input) : SV_TARGET
            {
                float3 position = _LocalSpace ? input.positionOS : input.positionWS;
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, input.uv).r;
                float dissolveValue = input.dissolveValue; // Use precomputed dissolve value from vertex

                if (_DissolveType == 3) // Pattern
                {
                    dissolveValue = GetPatternValue(input.uv, _PatternType, _PatternFrequency);
                }

                float timeAnim = _UseTimeAnimation > 0.5 ? sin(_Time.y * _TimeScale) * 0.05 : 0;
                float threshold = _DissolveThreshold + timeAnim;
                float perturbedDissolveValue = dissolveValue + (noise - 0.5) * _NoiseStrength;

                // Compute dissolve transition
                float edge = smoothstep(threshold - _EdgeWidth, threshold, perturbedDissolveValue);
                float alpha = step(threshold - _EdgeWidth, perturbedDissolveValue); // Ensures full transparency when dissolved

                float4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                float3 toonRampOutput;
                float3 lightDirection;
                ToonShading_float(input.normalWS, _ToonRampSmoothness, input.positionCS.xyz, input.positionWS, _ToonRampTinting, _ToonRampOffset, toonRampOutput, lightDirection);
                float3 litColor = baseColor.rgb * toonRampOutput;
                litColor += _AmbientColor.rgb;

                float4 finalColor;
                finalColor.rgb = lerp(_EdgeColor.rgb, litColor, edge);
                finalColor.a = alpha;

                clip(alpha - 0.01); // Ensure full dissolve by discarding fully transparent pixels

                return finalColor;
            }
            ENDHLSL
        }
    }
}