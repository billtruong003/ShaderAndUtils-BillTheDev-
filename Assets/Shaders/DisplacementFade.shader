Shader "Universal Render Pipeline/Wireframe/DisplacementFade"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" { }
        _WireThickness ("Wire Thickness", Range(-10, 10)) = 0.01
        [HDR] _WireColor ("Wire Color", Color) = (0,1,1,1)
        [Toggle(INVERT)] _INVERT("Invert", Float) = 1
        _MovingSlider ("Moving Slider", Range(-10, 10)) = 10
        _Extrude("Extrude Amount", Range(-10, 10)) = 10
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
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : TEXCOORD1;
                float movingPos : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float _WireThickness;
            float4 _WireColor;
            sampler2D _MainTex;
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
                output.color = input.color;
                output.movingPos = movingPos;
                return output;
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 bary = i.color.rgb;
                float minBary = min(min(bary.x, bary.y), bary.z);
                float wire = step(minBary, _WireThickness);
                float4 mainTex = tex2D(_MainTex, i.uv);
                float dissolveValue = saturate(i.movingPos);

                // Chỉ hiển thị wireframe khi gần cạnh và dissolveValue trong khoảng nhất định
                if (wire < 0.5 && dissolveValue > 0.5 && dissolveValue < 1.0)
                {
                    clip(i.movingPos + 0.2);
                    if (minBary > 0.9 * (1 - saturate(i.movingPos - _WireFrameStay)))
                    {
                        return mainTex;
                    }
                    return _WireColor;
                }
                else
                {
                    // Hiển thị texture với hiệu ứng dissolve
                    clip(i.movingPos - 0.2);
                    if (minBary > 0.9 * (1 - saturate(i.movingPos - _WireFrameStay)))
                    {
                        return mainTex;
                    }
                    return _WireColor;
                }
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}