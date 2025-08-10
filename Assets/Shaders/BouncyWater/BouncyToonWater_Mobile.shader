Shader "Creative/BouncyToonWater_Mobile"
{
    Properties
    {
        [Header(Master Feature Toggles)]
        [Toggle(_BOUNCY_ON)] _EnableBouncy("Enable Bouncy Effect", Float) = 1
        [Toggle(_SPECULAR_ON)] _EnableSpecular("Enable Specular", Float) = 1
        [Toggle(_RIM_ON)] _EnableRim("Enable Rim Light", Float) = 1
        [Toggle(_SMOOTH_EDGES_ON)] _EnableSmoothEdges("Enable Smooth Edges (Higher Quality)", Float) = 1
        [Space(20)]

        [Header(Surface Options)]
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (0, 0.5, 1, 1)
        _Alpha("Transparency", Range(0, 1)) = 0.75

        [Header(Toon Lighting)]
        _ToonRampThreshold("Toon Ramp Threshold", Range(0, 1)) = 0.5
        _ToonRampSmoothness("Toon Ramp Smoothness", Range(0.001, 0.1)) = 0.05

        [Header(Toon Specular)]
        _SpecularColor("Specular Color", Color) = (1, 1, 1, 1)
        _SpecularThreshold("Specular Threshold", Range(0.8, 1)) = 0.95
        _SpecularSmoothness("Specular Smoothness", Range(0.001, 0.1)) = 0.01
        
        [Header(Rim Light)]
        _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        _RimThreshold("Rim Threshold", Range(0, 1)) = 0.5
        _RimSmoothness("Rim Smoothness", Range(0.01, 1)) = 0.1

        [Header(Bouncy Effect)]
        [Enum(R,0,G,1,B,2,A,3)] _MaskChannel ("Vertex Color Mask Channel", Float) = 0
        _WaveAmplitude("Wave Amplitude", Float) = 0.1
        _WaveFrequency("Wave Frequency", Float) = 5.0
        _WaveSpeed("Wave Speed", Float) = 2.0
        _WaveAxis("Wave Propagation Axis (Object Space)", Vector) = (0, 1, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalRenderPipeline" "Queue"="Transparent" }

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On

            HLSLPROGRAM
            #pragma vertex VertexMain
            #pragma fragment FragmentMain

            #pragma multi_compile_local _ _BOUNCY_ON
            #pragma multi_compile_local _ _SPECULAR_ON
            #pragma multi_compile_local _ _RIM_ON
            #pragma multi_compile_local _ _SMOOTH_EDGES_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct VertexInput
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            struct VertexOutput
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float3 viewDirWS    : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST; float4 _BaseColor; float _Alpha;
            float _ToonRampThreshold; float _ToonRampSmoothness;
            float4 _SpecularColor; float _SpecularThreshold; float _SpecularSmoothness;
            float4 _RimColor; float _RimThreshold; float _RimSmoothness;
            int _MaskChannel;
            float _WaveAmplitude; float _WaveFrequency; float _WaveSpeed;
            float3 _WaveAxis;
            CBUFFER_END

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            VertexOutput VertexMain(VertexInput input)
            {
                VertexOutput output;
                float3 positionOS = input.positionOS.xyz;

                #if defined(_BOUNCY_ON)
                    // Select the mask from the specified vertex color channel.
                    float mask = input.color[_MaskChannel];
                    
                    // The wave propagates along the specified axis (e.g., down the Y-axis).
                    float wavePhase = dot(positionOS, normalize(_WaveAxis)) * _WaveFrequency + _Time.y * _WaveSpeed;

                    // Calculate a single displacement value based on the wave phase.
                    float displacement = sin(wavePhase) * _WaveAmplitude;
                    
                    // Apply the displacement outwards along the vertex normal, controlled by the mask.
                    positionOS += input.normalOS * displacement * mask;
                #endif

                output.positionCS = TransformObjectToHClip(positionOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.normalWS = normalInputs.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(TransformObjectToWorld(positionOS));
                return output;
            }

            // The fragment shader remains unchanged.
            half4 FragmentMain(VertexOutput input) : SV_Target
            {
                // ... (Fragment shader code is identical to the previous version)
                half4 baseMapColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 surfaceColor = baseMapColor * _BaseColor;

                Light mainLight = GetMainLight();
                half3 normalWS = normalize(input.normalWS);
                half3 lightDirWS = normalize(mainLight.direction);
                half shadowAttenuation = mainLight.shadowAttenuation;
                half attenuatedNdotL = dot(normalWS, lightDirWS) * shadowAttenuation;

                half toonLighting;
                #if defined(_SMOOTH_EDGES_ON)
                    toonLighting = smoothstep(_ToonRampThreshold - _ToonRampSmoothness, _ToonRampThreshold + _ToonRampSmoothness, attenuatedNdotL);
                #else
                    toonLighting = step(_ToonRampThreshold, attenuatedNdotL);
                #endif

                half3 finalColor = surfaceColor.rgb * (toonLighting * mainLight.color + SampleSH(normalWS));

                #if defined(_SPECULAR_ON)
                    half3 viewDirWS = normalize(input.viewDirWS);
                    half3 halfDirWS = normalize(lightDirWS + viewDirWS);
                    half NdotH = saturate(dot(normalWS, halfDirWS));
                    half specularIntensity;
                    #if defined(_SMOOTH_EDGES_ON)
                        specularIntensity = smoothstep(_SpecularThreshold - _SpecularSmoothness, _SpecularThreshold + _SpecularSmoothness, NdotH);
                    #else
                        specularIntensity = step(_SpecularThreshold, NdotH);
                    #endif
                    finalColor += specularIntensity * shadowAttenuation * _SpecularColor.rgb * mainLight.color;
                #endif

                #if defined(_RIM_ON)
                    half3 viewDirForRim = normalize(input.viewDirWS);
                    half rimFactor = 1.0 - saturate(dot(normalWS, viewDirForRim));
                    half rimIntensity;
                    #if defined(_SMOOTH_EDGES_ON)
                        rimIntensity = smoothstep(_RimThreshold - _RimSmoothness, _RimThreshold + _RimSmoothness, rimFactor);
                    #else
                        rimIntensity = step(_RimThreshold, rimFactor);
                    #endif
                    finalColor += rimIntensity * _RimColor.rgb;
                #endif
                
                return half4(finalColor, surfaceColor.a * _Alpha);
            }
            ENDHLSL
        }
    }
    CustomEditor "BouncyToonWaterMobileGUI"
}