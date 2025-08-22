Shader "BillTheDev/VAT/Boids_VAT_URP_Final"
{
    Properties
    {
        [Header(VAT Assets)]
        _PositionTexture ("Position Texture (VAT)", 2D) = "white" {}
        _PositionMin ("Position Min (Local Space)", Vector) = (0,0,0,0)
        _PositionMax ("Position Max (Local Space)", Vector) = (0,0,0,0)
        
        [Header(PBR Surface)]
        _BaseMap ("Albedo (RGB) Alpha (A)", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        [Space]
        _Metallic ("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 300
        Cull Back

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 4.5

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Boid { float3 position; float3 velocity; };

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
                float2 vertexIdUV   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
                float3 normalWS     : TEXCOORD2;
                float4 shadowCoord  : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            #if defined(UNITY_INSTANCING_ENABLED)
                StructuredBuffer<Boid> _BoidDataBuffer;
            #endif

            TEXTURE2D(_PositionTexture); SAMPLER(sampler_PositionTexture);
            float4 _PositionMin, _PositionMax;

            UNITY_INSTANCING_BUFFER_START(PerInstanceVAT)
                UNITY_DEFINE_INSTANCED_PROP(float, _CurrentAnimNormalizedTime)
                UNITY_DEFINE_INSTANCED_PROP(float, _PreviousAnimNormalizedTime)
                UNITY_DEFINE_INSTANCED_PROP(float, _AnimationBlendWeight)
            UNITY_INSTANCING_BUFFER_END(PerInstanceVAT)

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Metallic;
                half _Smoothness;
            CBUFFER_END
            
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            float3 DecodeLocalPosition(float vertexU, float timeV)
            {
                float4 encodedPos = SAMPLE_TEXTURE2D_LOD(_PositionTexture, sampler_PositionTexture, float2(vertexU, timeV), 0);
                return lerp(_PositionMin.xyz, _PositionMax.xyz, encodedPos.xyz);
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                Boid boidData;
                #if defined(UNITY_INSTANCING_ENABLED)
                    boidData = _BoidDataBuffer[UNITY_GET_INSTANCE_ID(input)];
                #else
                    boidData.position = float3(0, 0, 0); boidData.velocity = float3(0, 0, 1);
                #endif

                float vertexU = input.vertexIdUV.x;
                float currentAnimTime = UNITY_ACCESS_INSTANCED_PROP(PerInstanceVAT, _CurrentAnimNormalizedTime);
                float blendWeight = UNITY_ACCESS_INSTANCED_PROP(PerInstanceVAT, _AnimationBlendWeight);

                float3 localPosition = DecodeLocalPosition(vertexU, currentAnimTime);
                if (blendWeight > 0.001)
                {
                    float previousAnimTime = UNITY_ACCESS_INSTANCED_PROP(PerInstanceVAT, _PreviousAnimNormalizedTime);
                    float3 previousLocalPosition = DecodeLocalPosition(vertexU, previousAnimTime);
                    localPosition = lerp(previousLocalPosition, localPosition, blendWeight);
                }

                float3 forward = normalize(boidData.velocity);
                if (length(forward) < 0.01) forward = float3(0, 0, 1);
                float3 up = float3(0, 1, 0);
                float3 right = normalize(cross(up, forward));
                up = cross(forward, right);
                float3x3 rotationMatrix = float3x3(right, up, forward);

                float3 positionWS = mul(rotationMatrix, localPosition) + boidData.position;
                float3 normalWS = normalize(mul(rotationMatrix, input.normalOS));

                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(positionWS);
                output.shadowCoord = GetShadowCoord(positionInputs);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                SurfaceData surfaceData = (SurfaceData)0;
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                surfaceData.albedo = albedo.rgb;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.occlusion = 1.0h;
                surfaceData.alpha = albedo.a;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalize(input.normalWS);
                inputData.viewDirectionWS = SafeNormalize(GetCameraPositionWS() - input.positionWS);
                inputData.shadowCoord = input.shadowCoord;

                return UniversalFragmentPBR(inputData, surfaceData);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 vertexIdUV   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings { float4 positionCS : SV_POSITION; };
            struct Boid { float3 position; float3 velocity; };

            #if defined(UNITY_INSTANCING_ENABLED)
                StructuredBuffer<Boid> _BoidDataBuffer;
            #endif

            TEXTURE2D(_PositionTexture); SAMPLER(sampler_PositionTexture);
            float4 _PositionMin, _PositionMax;
            UNITY_INSTANCING_BUFFER_START(PerInstanceVAT)
                UNITY_DEFINE_INSTANCED_PROP(float, _CurrentAnimNormalizedTime)
                UNITY_DEFINE_INSTANCED_PROP(float, _PreviousAnimNormalizedTime)
                UNITY_DEFINE_INSTANCED_PROP(float, _AnimationBlendWeight)
            UNITY_INSTANCING_BUFFER_END(PerInstanceVAT)

            float3 DecodeLocalPosition(float vertexU, float timeV)
            {
                float4 encodedPos = SAMPLE_TEXTURE2D_LOD(_PositionTexture, sampler_PositionTexture, float2(vertexU, timeV), 0);
                return lerp(_PositionMin.xyz, _PositionMax.xyz, encodedPos.xyz);
            }
            
            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                Boid boidData;
                #if defined(UNITY_INSTANCING_ENABLED)
                    boidData = _BoidDataBuffer[UNITY_GET_INSTANCE_ID(input)];
                #else
                    boidData.position = float3(0, 0, 0); boidData.velocity = float3(0, 0, 1);
                #endif

                float vertexU = input.vertexIdUV.x;
                float currentAnimTime = UNITY_ACCESS_INSTANCED_PROP(PerInstanceVAT, _CurrentAnimNormalizedTime);
                float blendWeight = UNITY_ACCESS_INSTANCED_PROP(PerInstanceVAT, _AnimationBlendWeight);
                float3 localPosition = DecodeLocalPosition(vertexU, currentAnimTime);
                
                if (blendWeight > 0.001)
                {
                    float previousAnimTime = UNITY_ACCESS_INSTANCED_PROP(PerInstanceVAT, _PreviousAnimNormalizedTime);
                    float3 previousLocalPosition = DecodeLocalPosition(vertexU, previousAnimTime);
                    localPosition = lerp(previousLocalPosition, localPosition, blendWeight);
                }
                
                float3 forward = normalize(boidData.velocity);
                if (length(forward) < 0.01) forward = float3(0, 0, 1);
                float3 up = float3(0, 1, 0);
                float3 right = normalize(cross(up, forward));
                up = cross(forward, right);
                float3x3 rotationMatrix = float3x3(right, up, forward);
                
                float3 positionWS = mul(rotationMatrix, localPosition) + boidData.position;
                float3 normalWS = normalize(mul(rotationMatrix, input.normalOS));
                
                Light mainLight = GetMainLight();
                float3 lightDirection = mainLight.direction;
                
                positionWS = ApplyShadowBias(positionWS, normalWS, lightDirection);
                output.positionCS = TransformWorldToHClip(positionWS);

                return output;
            }
            half4 ShadowFrag(Varyings input) : SV_TARGET { return 0; }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}