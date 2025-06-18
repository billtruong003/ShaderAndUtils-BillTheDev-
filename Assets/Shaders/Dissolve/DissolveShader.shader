Shader "Custom/URP/AdvancedDissolveToonShader"
{
Properties
{
    // Main Settings
    _MainTex ("Base Texture", 2D) = "white" {}
    _ToonRampSmoothness ("Toon Ramp Smoothness", Range(0,1)) = 0.1
    _ToonRampOffset ("Toon Ramp Offset", Range(0,1)) = 0.5
    [HDR] _ToonRampTinting ("Toon Ramp Tinting", Color) = (1,1,1,1)
    [HDR] _AmbientColor ("Ambient Color", Color) = (0.2, 0.2, 0.2, 1)

    // Dissolve Settings
    _NoiseTex ("Noise Texture", 2D) = "white" {}
    _DissolveThreshold ("Dissolve Threshold", Range(-20, 20)) = 0.5
    _EdgeWidth ("Edge Width", Range(0, 10)) = 0.02
    [HDR] _EdgeColor ("Edge Color", Color) = (1, 0.5, 0, 1)
    _NoiseStrength ("Noise Strength", Range(0,2)) = 0.1
    [Toggle] _UseTimeAnimation ("Use Time Animation", Float) = 0
    _TimeScale ("Time Scale", Float) = 1
    [Toggle] _ZWrite ("ZWrite", Range(0,1)) = 0

    // Dissolve Type (Managed by Editor Script, 0-5)
    [IntRange] _DissolveType ("Dissolve Type", Range(0,5)) = 0 // 0: Noise, 1: Linear Gradient, 2: Radial Gradient, 3: Pattern, 4: Alpha Blend, 5: Shatter

    // Linear/Shatter Specific
    _Direction ("Direction (X, Y, Z)", Vector) = (0, 1, 0, 0)

    // Pattern Specific
    [IntRange] _PatternType ("Pattern Type", Range(0,2)) = 0 // 0: SinCos, 1: Checker, 2: Grid
    _PatternFrequency ("Pattern Frequency", Float) = 10

    // Alpha Blend Specific
    _AlphaFadeRange ("Alpha Blend Fade Range", Range(0.01, 1)) = 0.5

    // Vertex Displacement / Shatter Effect
    [Toggle(LOCAL)] _LocalSpace ("Use Local Space (Vertex Disp)", Float) = 0
    _VertexDisplacement ("Vertex Displacement / Outward Push", Range(-10,10)) = 0
    [Header(Shatter Effect)]
    _ShatterStrength ("Shatter Overall Strength", Range(0, 5)) = 1
    _ShatterLiftSpeed ("Shatter Lift Speed", Float) = 1
    _ShatterOffsetStrength ("Shatter Offset Strength", Float) = 0.5
    _ShatterTriggerRange ("Shatter Trigger Range", Range(0, 1)) = 0.1
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
        #pragma multi_compile_local _ LOCAL // Use _local for local space toggle, not LOCAL directly
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
            float3 positionOS_original : TEXCOORD2; // Store original OS position
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
            float _LocalSpace; // This now becomes LOCAL_ON in shader code
            float _NoiseStrength;
            float _VertexDisplacement;
            float4 _AmbientColor;
            float _AlphaFadeRange; // New property for Alpha Blend

            // Shatter specific properties
            float _ShatterStrength;
            float _ShatterLiftSpeed;
            float _ShatterOffsetStrength;
            float _ShatterTriggerRange;
        CBUFFER_END

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        TEXTURE2D(_NoiseTex);
        SAMPLER(sampler_NoiseTex);

        // Simple random hash function for vertex variation
        float hash(float3 p)
        {
            return frac(sin(dot(p, float3(12.9898, 78.233, 45.123))) * 43758.5453);
        }

        Varyings vert(Attributes input)
        {
            Varyings output;
            float3 originalPositionOS = input.positionOS.xyz;
            float noise = hash(originalPositionOS); // Use procedural noise per vertex for consistency

            // Compute dissolve value based on type
            float dissolveValue = 0;
            // Use original position for dissolve calculation to prevent displacement from affecting the threshold
            float3 positionForDissolve = input.positionOS.xyz; 
            #ifndef LOCAL_ON // If not using local space for displacement, convert to world space for calculation
                positionForDissolve = TransformObjectToWorld(input.positionOS.xyz);
            #endif

            if (_DissolveType == 0) // Noise
            {
                dissolveValue = noise; // Use procedural noise as base
            }
            else if (_DissolveType == 1) // Linear Gradient
            {
                dissolveValue = dot(positionForDissolve, normalize(_Direction.xyz));
            }
            else if (_DissolveType == 2) // Radial Gradient
            {
                // Radial gradient on UVs for 2D feel, or based on position for 3D
                // Using UV for simplicity and consistency with current shader for radial
                float2 center = float2(0.5, 0.5);
                dissolveValue = length(input.uv - center);
            }
            else if (_DissolveType == 3) // Pattern (Handled in Fragment for complexity, but a simple base value here)
            {
                // Pattern will be re-calculated in fragment, but need a base here for thresholding
                dissolveValue = sin(input.uv.x * _PatternFrequency) * cos(input.uv.y * _PatternFrequency) * 0.5 + 0.5;
            }
            else if (_DissolveType == 4) // Alpha Blend
            {
                dissolveValue = noise; // Similar to noise, but final alpha will be different
            }
            else if (_DissolveType == 5) // Shatter
            {
                dissolveValue = dot(positionForDissolve, normalize(_Direction.xyz)); // Base shatter on linear gradient
            }


            float timeAnim = _UseTimeAnimation > 0.5 ? sin(_Time.y * _TimeScale) * 0.05 : 0;
            float threshold = _DissolveThreshold + timeAnim;
            float perturbedDissolveValue = dissolveValue + (noise - 0.5) * _NoiseStrength;

            float3 displacedPositionOS = originalPositionOS; // Start with original position
            float3 currentNormalOS = input.normalOS; // Normal might also be displaced if we want to rotate fragments

            // Apply Vertex Displacement or Shatter Effect
            if (_DissolveType == 5) // Shatter Effect
            {
                // Shatter activation is strongest when the dissolve value is just below the threshold
                float shatterActivation = saturate(1.0 - smoothstep(threshold - _ShatterTriggerRange, threshold, perturbedDissolveValue));

                if (shatterActivation > 0.001) // Only apply if there's actual shattering
                {
                    // Per-vertex random seed based on its original position for consistent but varied motion
                    float rand_seed = hash(originalPositionOS);

                    // 1. Outward Push: Push fragments away from the object's pivot/center
                    float3 objectSpaceOrigin = float3(0,0,0); // Assumes object pivot is at origin
                    // Use a slightly randomized direction, normalized
                    float3 outwardPushDir = normalize(originalPositionOS - objectSpaceOrigin + (rand_seed - 0.5) * _ShatterOffsetStrength);
                    
                    // 2. Upward Lift: Move fragments along the specified _Direction
                    float3 liftDir = normalize(_Direction.xyz + 0.0001); // Add epsilon to prevent NaN if _Direction is zero

                    // Combine push and lift, scale by strength and activation
                    float3 totalShatterDisplacement = 
                        (outwardPushDir * _VertexDisplacement) + // _VertexDisplacement controls outward push strength
                        (liftDir * _ShatterLiftSpeed * _Time.y * rand_seed); // Time and random seed for varied lift

                    displacedPositionOS += totalShatterDisplacement * shatterActivation * _ShatterStrength;
                }
            }
            else // Regular Vertex Displacement for other dissolve types
            {
                // Compute edge factor for standard vertex displacement
                float edgeFactor = smoothstep(threshold - _EdgeWidth, threshold, perturbedDissolveValue);
                
                float timeOscillation = sin(_Time.y * _TimeScale * 2.0 + originalPositionOS.x * 5.0) * 0.5 + 0.5;
                float spatialVariation = 0.5 + noise * 0.5; // Use hash noise for spatial variation

                float displacementMagnitude = edgeFactor * (1.0 - edgeFactor) * _VertexDisplacement * (timeOscillation * 0.3 + spatialVariation * 0.7);

                // Add slight randomness to displacement direction
                float3 displacementDir = currentNormalOS + float3(noise - 0.5, noise - 0.5, noise - 0.5) * 0.1;
                displacementDir = normalize(displacementDir);

                // Apply displacement
                displacedPositionOS += displacementDir * displacementMagnitude;
            }


            // Transform the (potentially displaced) position to world and clip space
            output.positionCS = TransformObjectToHClip(displacedPositionOS);
            output.uv = TRANSFORM_TEX(input.uv, _MainTex);
            output.positionWS = TransformObjectToWorld(displacedPositionOS);
            output.positionOS_original = originalPositionOS; // Keep original for consistency in fragment or debug
            output.normalWS = TransformObjectToWorldNormal(currentNormalOS); // Use potentially rotated normal if implemented
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
                pattern = step(0.1, grid.x) * step(0.1, grid.y) * step(0.1, 1.0 - grid.x) * step(0.1, 1.0 - grid.y); // Basic grid
            }
            return pattern * 0.5 + 0.5; // Normalize to 0-1
        }

        // Toon Shading function (remains mostly unchanged)
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
                toonRamp *= light.shadowAttenuation ;
                ToonRampOutput = light.color * (toonRamp + ToonRampTinting) ;
                Direction = light.direction;

                // Fallback for no light to avoid black surface
                if (dot(light.color, light.color) == 0)
                {
                    ToonRampOutput = float3(1, 1, 1) * _AmbientColor;
                }
            #endif
        }

        float4 frag(Varyings input) : SV_TARGET
        {
            // Use original OS position to generate noise texture sample consistently
            // This is crucial for per-vertex noise, or use input.uv for texture sampling.
            float noiseTexSample = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, input.uv).r;
            float dissolveValue = input.dissolveValue; // Use precomputed dissolve value from vertex

            // Re-calculate pattern in fragment to ensure pixel-perfect edges for patterns
            if (_DissolveType == 3) // Pattern
            {
                dissolveValue = GetPatternValue(input.uv, _PatternType, _PatternFrequency);
            }

            float timeAnim = _UseTimeAnimation > 0.5 ? sin(_Time.y * _TimeScale) * 0.05 : 0;
            float threshold = _DissolveThreshold + timeAnim;
            float perturbedDissolveValue = dissolveValue + (noiseTexSample - 0.5) * _NoiseStrength;

            float4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

            float3 toonRampOutput;
            float3 lightDirection;
            ToonShading_float(input.normalWS, _ToonRampSmoothness, input.positionCS.xyz, input.positionWS, _ToonRampTinting, _ToonRampOffset, toonRampOutput, lightDirection);
            float3 litColor = baseColor.rgb * toonRampOutput;

            float3 edgeColorWithAmbient = _EdgeColor.rgb + _AmbientColor.rgb;

            // Calculate the edge factor for color blending
            float edgeFactor = smoothstep(threshold - _EdgeWidth, threshold, perturbedDissolveValue);

            float finalAlpha;
            if (_DissolveType == 4) // Alpha Blend with threshold control
            {
                // 1. Calculate alpha for the main body of the object.
                // This makes the object transition from transparent (0) to fully opaque (1).
                // The transition zone starts from (threshold - _EdgeWidth - _AlphaFadeRange)
                // and ends at (threshold - _EdgeWidth).
                // If perturbedDissolveValue is less than the start point, bodyAlpha will be 0 (transparent).
                // If perturbedDissolveValue is greater than the end point, bodyAlpha will be 1 (opaque).
                float bodyAlpha = smoothstep(threshold - _EdgeWidth - _AlphaFadeRange, threshold - _EdgeWidth, perturbedDissolveValue);

                // 2. Determine the opacity of the edge. We want the edge to always be clearly visible.
                float edgeAlphaOpacity = 1.0; // Edge will be fully opaque

                // 3. Combine bodyAlpha and edgeAlphaOpacity.
                // When edgeFactor = 0 (outside the edge region, in the main body part), finalAlpha = bodyAlpha.
                // When edgeFactor = 1 (at the end of the edge region), finalAlpha = edgeAlphaOpacity.
                // This ensures that the main body fades according to `bodyAlpha`, but the edge remains opaque.
                finalAlpha = lerp(bodyAlpha, edgeAlphaOpacity, edgeFactor);
                
                // Ensure that parts completely *before* the edge threshold are fully transparent.
                // This prevents the edge color from appearing in fully dissolved areas.
                finalAlpha *= step(threshold - _EdgeWidth - 0.001, perturbedDissolveValue); // Small epsilon for robustness

            }
            else // For other dissolve types (Noise, Linear, Radial, Pattern, Shatter)
            {
                // Default alpha for other types (sharp cut-off)
                finalAlpha = step(threshold - _EdgeWidth, perturbedDissolveValue);
            }

            float4 finalColor;
            // The color is always blended between the edge color and the lit base color based on 'edgeFactor'.
            finalColor.rgb = lerp(edgeColorWithAmbient, litColor, edgeFactor);
            finalColor.a = finalAlpha; // Use the calculated alpha

            // Discard pixels that are almost fully transparent
            clip(finalColor.a - 0.01);

            return finalColor;
        }
        ENDHLSL
    }
}
}