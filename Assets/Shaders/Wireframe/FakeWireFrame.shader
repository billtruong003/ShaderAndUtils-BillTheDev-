Shader "Universal Render Pipeline/Wireframe/DisplacementFadeFakeWireFrame"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _WireTex ("Wireframe Texture", 2D) = "white" {}
        _WireThickness ("Wire Thickness", Range(0, 0.1)) = 0.02
        [HDR] _WireColor ("Wire Color", Color) = (0,1,1,1)
        [Toggle(INVERT)] _INVERT("Invert", Float) = 1
        _MovingSlider ("Moving Slider", Range(-10, 10)) = 10
        _Extrude ("Extrude Amount", Range(-10, 10)) = 10
        _WireFrameStay ("Wire Stay", Range(-10, 10)) = 0
    }
    SubShader
    {
        Tags {"RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature INVERT
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float movingPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float _WireThickness;
            float4 _WireColor;
            sampler2D _MainTex;
            sampler2D _WireTex;
            float _MovingSlider;
            float _Extrude;
            float _WireFrameStay;

            v2f vert(Attributes input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                #if INVERT
                float movingPos = input.positionOS.y + _MovingSlider;
                #else
                float movingPos = 1 - input.positionOS.y + _MovingSlider;
                #endif
                input.positionOS.xyz -= saturate(1 - movingPos) * input.normal * _Extrude;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.movingPos = movingPos;
                return output;
            }

            half4 frag(v2f i) : SV_Target
            {
                // Sample wireframe texture with scaled UVs
                float2 wireUV = i.uv * (1.0 / _WireThickness);
                float wire = tex2D(_WireTex, wireUV).r;
                wire = smoothstep(0.4, 0.6, wire);

                float4 mainTex = tex2D(_MainTex, i.uv);
                float dissolveValue = saturate(i.movingPos);

                // Adjust wireStayInfluence to follow dissolve direction
                float wireStayInfluence = saturate(dissolveValue - _WireFrameStay); // Same for both INVERT and non-INVERT

                // Control wireframe visibility with dissolve effect
                if (dissolveValue > 0.2 && dissolveValue < 1.0)
                {
                    clip(dissolveValue - 0.2); // Apply dissolve effect
                    // Show wireframe based on texture and WireFrameStay
                    if (wire < 0.5 && wireStayInfluence > 0.0 && wireStayInfluence < 1.0)
                    {
                        return _WireColor * wireStayInfluence; // Blend wireframe with stay influence
                    }
                    return mainTex; // Show texture
                }
                else
                {
                    clip(dissolveValue - 0.2); // Apply dissolve outside wireframe
                    return mainTex; // Default to texture
                }
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}