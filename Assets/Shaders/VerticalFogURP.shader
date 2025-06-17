Shader "MyShaders/VerticalFogURP"
{
    Properties
    {
        [Header(Fog Settings)]
        _FogColor ("Fog Color", Color) = (1,1,1,1)
        _DepthStart ("Depth Start", Float) = 1
        _DepthEnd ("Depth End", Float) = 2
        
        [Header(Wave Settings)]
        _WaveSpeed ("Wave Speed", Float) = 1
        _WaveFrequency ("Wave Frequency", Float) = 1
        _WaveAmplitude ("Wave Amplitude", Float) = 0.1
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "DisableBatching" = "True" }

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float4 screenPos    : TEXCOORD0;
                float3 worldPos     : TEXCOORD1;
            };

            TEXTURE2D_X(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            CBUFFER_START(UnityPerMaterial)
            float4 _FogColor;
            float _DepthStart;
            float _DepthEnd;
            float _WaveSpeed;
            float _WaveFrequency;
            float _WaveAmplitude;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                // Vertex displacement for waves
                float4 positionOS = IN.positionOS;
                float wave = sin(_Time.y * _WaveSpeed + positionOS.x * _WaveFrequency) * _WaveAmplitude;
                positionOS.y += wave;

                OUT.worldPos = mul(unity_ObjectToWorld, positionOS).xyz;
                OUT.positionHCS = mul(unity_MatrixVP, float4(OUT.worldPos, 1.0));
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Get scene depth
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV).r;
                
                // Reconstruct world position of the object behind the fog
                float4 hcsPos = float4(screenUV * 2.0 - 1.0, depth, 1.0);
                float4 worldPosBehind = mul(unity_CameraInvProjection, hcsPos);
                worldPosBehind /= worldPosBehind.w;
                worldPosBehind = mul(unity_CameraToWorld, worldPosBehind);
                
                // Calculate fog opacity based on depth difference
                float sceneDepth = length(worldPosBehind.xyz - _WorldSpaceCameraPos);
                float fogDepth = length(IN.worldPos - _WorldSpaceCameraPos);
                float depthDifference = sceneDepth - fogDepth;

                float fogDensity = saturate((depthDifference - _DepthStart) / (_DepthEnd - _DepthStart));
                
                return float4(_FogColor.rgb, _FogColor.a * fogDensity);
            }
            ENDHLSL
        }
    }
    FallBack "Transparent/VertexLit"
} 