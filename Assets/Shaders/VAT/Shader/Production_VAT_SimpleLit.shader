Shader "BillTheDev/URP/VAT/Production_VAT_SimpleLit"
{
    Properties
    {
        [Header(Texture Maps)]
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)

        [Header(Vertex Animation Textures)]
        _PositionTexture ("Position Texture (VAT)", 2D) = "black" {}
        _NormalTexture ("Normal Texture (NAT)", 2D) = "grey" {}

        [Header(Animation Bounds)]
        _PositionMin ("Position Min (Local Space)", Vector) = (0,0,0,0)
        _PositionMax ("Position Max (Local Space)", Vector) = (0,0,0,0)
        _NormalMin ("Normal Min (Local Space)", Vector) = (-1,-1,-1,0)
        _NormalMax ("Normal Max (Local Space)", Vector) = (1,1,1,0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        
        LOD 200
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
                float3 normalWS     : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                sampler2D _PositionTexture, _NormalTexture, _MainTex;
                float4 _MainTex_ST;
                float4 _PositionMin, _PositionMax;
                float4 _NormalMin, _NormalMax;
                half4 _Color;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(PerInstance)
                UNITY_DEFINE_INSTANCED_PROP(float, _CurrentAnimNormalizedTime)
                UNITY_DEFINE_INSTANCED_PROP(float, _PreviousAnimNormalizedTime)
                UNITY_DEFINE_INSTANCED_PROP(float, _AnimationBlendWeight)
            UNITY_INSTANCING_BUFFER_END(PerInstance)

            float3 DecodeVector3FromTexture(sampler2D tex, float2 uv, float3 minBounds, float3 maxBounds)
            {
                float4 encodedVector = tex2Dlod(tex, float4(uv, 0, 0));
                return lerp(minBounds, maxBounds, encodedVector.xyz);
            }

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                float vertexU = input.vertexIdUV.x;
                float currentAnimTime = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _CurrentAnimNormalizedTime);
                float blendWeight = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _AnimationBlendWeight);

                float3 localPosition = DecodeVector3FromTexture(_PositionTexture, float2(vertexU, currentAnimTime), _PositionMin.xyz, _PositionMax.xyz);
                float3 localNormal = DecodeVector3FromTexture(_NormalTexture, float2(vertexU, currentAnimTime), _NormalMin.xyz, _NormalMax.xyz);

                if (blendWeight > 0.001)
                {
                    float previousAnimTime = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _PreviousAnimNormalizedTime);
                    float3 previousPos = DecodeVector3FromTexture(_PositionTexture, float2(vertexU, previousAnimTime), _PositionMin.xyz, _PositionMax.xyz);
                    float3 previousNormal = DecodeVector3FromTexture(_NormalTexture, float2(vertexU, previousAnimTime), _NormalMin.xyz, _NormalMax.xyz);

                    localPosition = lerp(previousPos, localPosition, blendWeight);
                    localNormal = lerp(previousNormal, localNormal, blendWeight);
                }

                output.positionWS = TransformObjectToWorld(localPosition);
                output.normalWS = TransformObjectToWorldNormal(localNormal);
                output.positionHCS = TransformWorldToHClip(output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = tex2D(_MainTex, input.uv).rgb * _Color.rgb;
                surfaceData.alpha = 1.0h;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalize(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                
                // For dynamic objects, calculate indirect lighting from Light Probes (Spherical Harmonics).
                // This correctly replaces the faulty lightmap logic.
                inputData.bakedGI = SampleSH(inputData.normalWS);

                half4 finalColor = UniversalFragmentBlinnPhong(inputData, surfaceData);

                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Simple Lit"
}