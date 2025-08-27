Shader "Custom/URP_OutlineOnlyPass"
{
    Properties
    {
        [HDR]_OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineThickness ("Outline Thickness", Range(0, 0.5)) = 0.01

        [Header(Toggles)]
        [Toggle(_ENABLE_OUTLINE)] _EnableOutline ("Enable Outline", Float) = 1
        [Toggle(_SOFT_OUTLINE)] _SoftOutline ("Soft Outline", Float) = 0
        _Softness ("Softness", Range(0.01, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "OutlineOnly"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite Off

            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment
            
            #pragma multi_compile_local _ _ENABLE_OUTLINE
            #pragma multi_compile_local _ _SOFT_OUTLINE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float  fogCoord     : TEXCOORD0;
            };
            
            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineThickness;
                float _Softness;
            CBUFFER_END

            Varyings OutlineVertex(Attributes input)
            {
                Varyings output;
                
                float3 extrudedPositionOS = input.positionOS.xyz + input.normalOS * _OutlineThickness;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(extrudedPositionOS);
                output.positionCS = positionInputs.positionCS;
                
                output.fogCoord = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 OutlineFragment(Varyings input) : SV_Target
            {
                #if !_ENABLE_OUTLINE
                    discard;
                #endif

                half4 finalColor = _OutlineColor;

                #if _SOFT_OUTLINE
                    finalColor.a *= saturate(input.fogCoord / _Softness);
                #endif

                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}