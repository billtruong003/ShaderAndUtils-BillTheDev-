Shader "FX/Waterfall Interactive URP"
{
    Properties
    {
        [Space]
        [Header(Water)]
        _TColor("Deep Tint", Color) = (0,1,1,1)
        _WaterColor("Edge Tint", Color) = (0,0.6,1,1)
        _DepthOffset("Depth Offset", Range(-10,10)) = 0
        _Stretch("Depth Stretch", Range(0,5)) = 2
        _Brightness("Water Brightness", Range(0.5,2)) = 1.2

        [Space]
        [Header(Surface Noise and Movement)]
        _SideNoiseTex("Side Water Texture", 2D) = "white" {}
        _TopNoiseTex("Top Water Texture", 2D) = "white" {}
        _HorSpeed("Horizontal Flow Speed", Range(0,4)) = 0.14
        _VertSpeed("Vertical Flow Speed", Range(0,60)) = 6.8
        _TopScale("Top Noise Scale", Range(0,1)) = 0.4
        _NoiseScale("Side Noise Scale", Range(0,1)) = 0.04
        [Toggle(VERTEX_FLOW)] _VERTEX("Use Vertex Colors for Flow", Float) = 0

        [Space]
        [Header(Foam)]
        _FoamColor("Foam Tint", Color) = (1,1,1,1)
        _Foam("Edgefoam Width", Range(1,50)) = 2.35
        _TopSpread("Foam Position", Range(-1,6)) = 0.05
        _Softness("Foam Softness", Range(0,0.5)) = 0.1
        _EdgeWidth("Foam Width", Range(0,2)) = 0.4

        [Space]
        [Header(Rim Light)]
        _RimPower("Rim Power", Range(1,20)) = 18
        _RimColor("Rim Color", Color) = (0,0.5,0.25,1)

        [Space]
        [Header(Vertex Movement)]
        _Amount("Wave Amount", Range(0,10)) = 0.6
        _SpeedV("Speed", Range(0,10)) = 0.5
        _Height("Wave Height", Range(0,1)) = 0.1

        [Space]
        [Header(Reflections)]
        _ReflectionTex("Refl Texture", 2D) = "black" {}
        _Reflectivity("Reflectivity", Range(0,1)) = 0.6
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }
        LOD 200

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile _ VERTEX_FLOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float3 viewDirWS    : TEXCOORD2;
                float4 screenPos    : TEXCOORD3;
                float4 color        : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _TColor;
                half4 _WaterColor;
                half _DepthOffset;
                half _Stretch;
                half _Brightness;
                half _HorSpeed;
                half _VertSpeed;
                half _TopScale;
                half _NoiseScale;
                half4 _FoamColor;
                half _Foam;
                half _TopSpread;
                half _Softness;
                half _EdgeWidth;
                half _RimPower;
                half4 _RimColor;
                half _Amount;
                half _SpeedV;
                half _Height;
                half _Reflectivity;
                float3 _Position;
                float _OrthographicCamSize;
            CBUFFER_END

            TEXTURE2D(_SideNoiseTex);       SAMPLER(sampler_SideNoiseTex);
            TEXTURE2D(_TopNoiseTex);        SAMPLER(sampler_TopNoiseTex);
            TEXTURE2D(_ReflectionTex);      SAMPLER(sampler_ReflectionTex);
            TEXTURE2D(_GlobalEffectRT);     SAMPLER(sampler_GlobalEffectRT);
            TEXTURE2D(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);

            Varyings Vertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                half3 noiseSample = SAMPLE_TEXTURE2D_LOD(_SideNoiseTex, sampler_SideNoiseTex, positionWS.xz * _TopScale, 0).rgb;
                float3 movement = sin(_Time.z * _SpeedV + (input.positionOS.x * input.positionOS.z * _Amount * noiseSample)) * _Height * (1 - normalWS.y);
                
                input.positionOS.xyz += movement;

                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(output.positionWS);
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.color = input.color;

                return output;
            }

            float3 BlendNormal(float3 baseNormal, float3 detailNormal)
            {
                return normalize(baseNormal + detailNormal);
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float3 viewDir = SafeNormalize(input.viewDirWS);
                float3 blendNormal = saturate(pow(input.normalWS * 1.4, 4));

                float3 flowDir;
                #if defined(VERTEX_FLOW)
                    flowDir = (input.color.rgb * 2.0f) - 1.0f;
                #else
                    flowDir = -(input.normalWS * 2.0f) - 1.0f;
                #endif
                flowDir *= _HorSpeed;

                float timing = frac(_Time.y * 0.5 + 0.5);
                float timing2 = frac(_Time.y * 0.5);
                float timingLerp = abs((0.5 - timing) / 0.5);

                half3 topTex1 = SAMPLE_TEXTURE2D(_TopNoiseTex, sampler_TopNoiseTex, (input.positionWS.xz * _TopScale) + flowDir.xz * timing).rgb;
                half3 topTex2 = SAMPLE_TEXTURE2D(_TopNoiseTex, sampler_TopNoiseTex, (input.positionWS.xz * _TopScale) + flowDir.xz * timing2).rgb;

                float vertFlow = _Time.y * _VertSpeed;

                float2 rtUV = input.positionWS.xz - _Position.xz;
                rtUV = rtUV / (_OrthographicCamSize * 2);
                rtUV += 0.5;
                half ripples = SAMPLE_TEXTURE2D(_GlobalEffectRT, sampler_GlobalEffectRT, rtUV).b;

                half3 topFoamNoise = lerp(topTex1, topTex2, timingLerp) + ripples;

                half3 sideFoamNoiseZ = SAMPLE_TEXTURE2D(_SideNoiseTex, sampler_SideNoiseTex, float2(input.positionWS.z * 10, input.positionWS.y + vertFlow) * _NoiseScale).rgb;
                half3 sideFoamNoiseX = SAMPLE_TEXTURE2D(_SideNoiseTex, sampler_SideNoiseTex, float2(input.positionWS.x * 10, input.positionWS.y + vertFlow) * _NoiseScale).rgb;
                half3 sideFoamNoiseZE = SAMPLE_TEXTURE2D(_TopNoiseTex, sampler_TopNoiseTex, float2(input.positionWS.z * 10, input.positionWS.y + vertFlow) * _NoiseScale / 3).rgb;
                half3 sideFoamNoiseXE = SAMPLE_TEXTURE2D(_TopNoiseTex, sampler_TopNoiseTex, float2(input.positionWS.x * 10, input.positionWS.y + vertFlow) * _NoiseScale / 3).rgb;

                half3 noiseTexture = (sideFoamNoiseX + sideFoamNoiseXE) / 2;
                noiseTexture = lerp(noiseTexture, (sideFoamNoiseZ + sideFoamNoiseZE) / 2, blendNormal.x);
                noiseTexture = lerp(noiseTexture, topFoamNoise, blendNormal.y);

                float3 blendedNormal = BlendNormal(input.normalWS, noiseTexture * 2);
                blendedNormal = BlendNormal(blendedNormal, float3(ripples, ripples, ripples) * 2);

                float sceneRawDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, input.screenPos.xy / input.screenPos.w).r;
                float sceneLinearEyeDepth = LinearEyeDepth(sceneRawDepth, _ZBufferParams);
                
                float surfaceLinearEyeDepth = input.positionCS.w;
                float depthDifference = sceneLinearEyeDepth - surfaceLinearEyeDepth;

                half foamFromDepth = 1 - saturate(_Foam * noiseTexture.r * depthDifference);
                foamFromDepth = smoothstep(0.5, 0.8, foamFromDepth);

                half rim = 1.0 - saturate(dot(viewDir, blendedNormal));
                half3 rimColor = _RimColor.rgb * pow(rim, _RimPower);

                half worldNormalDotNoise = dot(blendedNormal, float3(0,1,0) + 0.3) * noiseTexture.r;
                half foamFromNormals = smoothstep(worldNormalDotNoise, worldNormalDotNoise + _Softness, _TopSpread + _EdgeWidth) * saturate(1 - input.normalWS.y);
                foamFromNormals *= 4;

                half3 combinedFoam = (foamFromNormals + foamFromDepth + ripples) * _FoamColor.rgb;

                half waterDepth = saturate((depthDifference + _DepthOffset * noiseTexture.r) * _Stretch);
                half4 waterBaseColor = lerp(_WaterColor, _TColor, waterDepth) * _Brightness;
                
                half3 albedo = waterBaseColor.rgb;

                float4 projCoord = input.screenPos;
                projCoord.xy += worldNormalDotNoise;
                float2 reflectionUV = projCoord.xy / projCoord.w;
                half4 rtReflections = SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, reflectionUV);
                rtReflections.rgb *= dot(blendedNormal, input.normalWS.y);

                half3 emission = combinedFoam + rimColor + rtReflections.rgb;
                albedo += emission;
                
                half alpha = saturate(waterBaseColor.a + (rtReflections.a * _Reflectivity) + combinedFoam.r + foamFromDepth + ripples);

                return half4(albedo, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Transparent"
}