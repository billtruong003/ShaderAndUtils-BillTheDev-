Shader "Custom/PortalShader"
{
    Properties
    {
        [Header(Portal View)]
        _MainTex ("Render Texture", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (1,1,1,1)

        [Header(Portal Shape Edge)]
        _PortalSize ("Portal Size", Range(0,1)) = 0.5
        _Softness ("Softness", Range(0,0.1)) = 0.01
        _EdgeNoiseTex ("Edge Noise Texture", 2D) = "white" {}
        _EdgeNoiseStrength ("Edge Noise Strength", Range(0,0.1)) = 0.01
        _EdgeNoiseScale ("Edge Noise Scale", Float) = 1.0

        [Header(View Distortion)]
        _NoiseTex ("Distort Noise Texture", 2D) = "white" {}
        _NoiseDistortStrength ("Noise Distort Strength", Range(0,0.1)) = 0.01
        _DistortNoiseScale ("Distort Noise Scale", Float) = 1.0

        [Header(Animation)]
        _NoiseAnimSpeed ("Noise Animation Speed", Float) = 0.5
        _NoiseAnimDirection ("Noise Animation Direction", Vector) = (1,0,0,0)

        [Header(Edge Glow)]
        _GlowColor ("Glow Color", Color) = (1,1,1,1)
        _EdgeGradientColor ("Edge Gradient Color", Color) = (0.5,0.5,1,1)
        _GlowIntensity ("Glow Intensity", Range(0,1)) = 0.5

        [Header(Depth Pattern)]
        _DepthPatternColor ("Depth Pattern Color", Color) = (0.7,0.9,1,1)
        _DepthPatternDistance ("Depth Pattern Distance", Range(1,50)) = 20.0
        [Toggle] _MovePatternDepth ("Move Pattern Depth", Float) = 0
        _DepthPatternSpeed ("Depth Pattern Speed", Float) = 1.0
        _DepthPatternNoiseStrength ("Depth Pattern Noise Strength", Range(0,1)) = 0.1
    }
    SubShader
    {
        // CORRECTED TAGS:
        // RenderType should be "Transparent" to work correctly with systems like screen space effects.
        // Queue must be "Transparent" because the shader uses alpha blending (Blend SrcAlpha...)
        // and should be rendered after opaque objects.
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            // Blending enabled for transparency.
            Blend SrcAlpha OneMinusSrcAlpha
            // Turn off depth writing so objects behind the portal can be seen.
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // Texture and Sampler declarations
            TEXTURE2D(_MainTex);         SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);        SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_EdgeNoiseTex);    SAMPLER(sampler_EdgeNoiseTex);

            // Property variables
            float _PortalSize;
            float _Softness;
            float _EdgeNoiseStrength;
            float _EdgeNoiseScale;
            float _NoiseDistortStrength;
            float _DistortNoiseScale;
            float _NoiseAnimSpeed;
            float2 _NoiseAnimDirection;
            float4 _GlowColor;
            float4 _EdgeGradientColor;
            float _GlowIntensity;
            float4 _TintColor;
            float4 _DepthPatternColor;
            float _MovePatternDepth;
            float _DepthPatternSpeed;
            float _DepthPatternNoiseStrength;
            float _DepthPatternDistance;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float4 frag (Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float2 center = float2(0.5, 0.5);

                // --- 1. Create the Portal Mask ---
                // Calculate distance from center to create the basic circle shape.
                float dist = distance(uv, center);

                // Animate UVs for the edge noise texture.
                float2 edgeNoiseUV = uv * _EdgeNoiseScale + _NoiseAnimDirection * _Time.y * _NoiseAnimSpeed;
                float edgeNoise = SAMPLE_TEXTURE2D(_EdgeNoiseTex, sampler_EdgeNoiseTex, edgeNoiseUV).r;

                // Add noise to the distance to create a wobbly, unstable edge.
                // (edgeNoise - 0.5) remaps the noise from [0, 1] to [-0.5, 0.5].
                float noisyDist = dist + (edgeNoise - 0.5) * _EdgeNoiseStrength;

                // Use smoothstep to create a soft-edged mask.
                // The result is 1 inside the portal, 0 outside, with a smooth transition.
                float mask = 1.0 - smoothstep(_PortalSize - _Softness, _PortalSize, noisyDist);
                mask = saturate(mask); // Ensure mask is between 0 and 1.

                // --- 2. Create the View Distortion ---
                // Animate UVs for the distortion noise texture.
                float2 distortNoiseUV = uv * _DistortNoiseScale + _NoiseAnimDirection * _Time.y * _NoiseAnimSpeed;

                // Sample the noise texture twice with offsets to get 2D distortion.
                float noiseX = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, distortNoiseUV + float2(0.1, 0.2)).r - 0.5;
                float noiseY = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, distortNoiseUV + float2(0.3, 0.4)).r - 0.5;

                // Increase distortion strength near the portal's edge for a more dynamic effect.
                float distortFactor = smoothstep(_PortalSize - _Softness, _PortalSize, dist);
                float2 noiseDistort = float2(noiseX, noiseY) * _NoiseDistortStrength * (1.0 + distortFactor * 5.0);

                // Apply the distortion to the UVs used for sampling the main render texture.
                float2 finalUV = uv + noiseDistort;
                float4 portalColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, finalUV);

                // Apply the tint color.
                portalColor *= _TintColor;

                // --- 3. Add the Depth Pattern ---
                // Create concentric rings using a sine wave based on distance from the center.
                float depthPattern = 0.5 + 0.5 * sin(dist * _DepthPatternDistance);

                // If toggled, add movement and noise to the pattern to make it feel more alive.
                if (_MovePatternDepth > 0.5)
                {
                    float2 depthNoiseUV = uv * 2.0 + _Time.y * 0.1;
                    float depthNoise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, depthNoiseUV).r;
                    float movement = _Time.y * _DepthPatternSpeed + depthNoise * _DepthPatternNoiseStrength;
                    depthPattern = 0.5 + 0.5 * sin(dist * _DepthPatternDistance + movement);
                }
                
                // Blend the depth pattern color into the portal view.
                portalColor.rgb = lerp(portalColor.rgb, _DepthPatternColor.rgb, depthPattern * _DepthPatternColor.a);


                // --- 4. Calculate the Edge Glow ---
                // This clever trick creates a glow only in the soft transition area of the mask.
                // (1-x)*x gives a curve that peaks at x=0.5. Multiplying by 4 normalizes the peak to 1.
                float glow = (1.0 - mask) * mask * 4.0 * _GlowIntensity;

                // Create a color gradient for the glow, from the inner glow color to the outer edge color.
                float4 edgeColor = lerp(_GlowColor, _EdgeGradientColor, saturate(dist / _PortalSize));

                // --- 5. Final Composition ---
                // Combine the portal view (visible only where mask > 0) with the edge glow.
                float4 finalColor = portalColor * mask + edgeColor * glow;

                // The final alpha is the mask's alpha plus the glow's alpha, clamped at 1.
                float finalAlpha = saturate(mask + glow);

                return float4(finalColor.rgb, finalAlpha);
            }
            ENDHLSL
        }
    }
}