Shader "Universal Render Pipeline/Wireframe/HologramDisplacementFade"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" { }
        _WireThickness ("Wire Thickness", Range(0, 1)) = 0.01
        [HDR] _WireColor ("Wire Color", Color) = (0,1,1,1)
        [Toggle(INVERT)] _INVERT("Invert", Float) = 1
        _MovingSlider ("Moving Slider", Range(-10, 10)) = 10
        _Extrude("Extrude Amount", Range(-10, 10)) = 10
        _WireFrameStay ("Wire Stay", Range(-1, 1)) = 0
        _ClipPose ("Clip Pose", Range(-10, 10)) = 0
        _TintColor("Tint Color", Color) = (0, 0.5, 1, 1)
        _RimColor("Rim Color", Color) = (0, 1, 1, 1)
        _GlitchTime("Glitches Over Time", Range(0.01, 3.0)) = 1.0
        _WorldScale("Line Amount", Range(1, 200)) = 20
    }
    SubShader
    {
        Tags {"RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent"}
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask RGB
            Cull Back
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature INVERT
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
                float3 worldPos : TEXCOORD3;
                float3 normalWS : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float _WireThickness;
            float4 _WireColor;
            sampler2D _MainTex;
            float _MovingSlider;
            float _Extrude;
            float _WireFrameStay;
            float _ClipPose;
            float4 _TintColor;
            float4 _RimColor;
            float _GlitchTime;
            float _WorldScale;

            v2f vert(Attributes input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Vertex glitching from Hologram Effect
                float optTime = sin(_TimeParameters.w * _GlitchTime);
                float glitchTime = step(0.99, optTime);
                float glitchPos = input.positionOS.y + sin(_TimeParameters.y);
                float glitchPosClamped = step(0, glitchPos) * step(glitchPos, 0.2);
                input.positionOS.xz += glitchPosClamped * 0.1 * glitchTime * sin(_TimeParameters.y);

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
                output.worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = normalize(TransformObjectToWorldNormal(input.normal));

                return output;
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 bary = i.color.rgb;
                float minBary = min(min(bary.x, bary.y), bary.z);
                float wire = step(minBary, _WireThickness);
                float4 mainTex = tex2D(_MainTex, i.uv);
                float dissolveValue = saturate(i.movingPos);

                // Tính toán clip pose
                float clipValue = i.movingPos - _ClipPose;
                clip(clipValue);

                // Texture từ Hologram Effect
                half4 text = mainTex * _TintColor;

                // Rim lighting từ Hologram Effect
                float3 viewDirection = normalize(GetWorldSpaceViewDir(i.worldPos));
                half rim = 1.0 - saturate(dot(viewDirection, i.normalWS));

                // Small scanlines down từ Hologram Effect
                float fracLines = frac((i.worldPos.y * _WorldScale) + _TimeParameters.y);
                float scanLines = step(fracLines, 0.5);

                // Big scanline up từ Hologram Effect
                float bigFracLine = frac((i.worldPos.y) - _TimeParameters.x * 4.0);

                // Chỉ hiển thị wireframe khi gần cạnh và dissolveValue trong khoảng nhất định
                if (wire < 0.5 && dissolveValue > 0.5 && dissolveValue < 1.0)
                {
                    // Kết hợp wireframe với hiệu ứng hologram
                    half4 col = _WireColor + (bigFracLine * 0.4 * _TintColor) + (rim * _RimColor);
                    col.a = 0.8 * (scanLines + rim + bigFracLine);
                    return col;
                }
                else
                {
                    // Hiển thị texture với hiệu ứng dissolve và hologram
                    clip(i.movingPos - 0.2);
                    if (minBary > 0.9 * (1 - saturate(i.movingPos - _WireFrameStay)))
                    {
                        half4 col = text + (bigFracLine * 0.4 * _TintColor) + (rim * _RimColor);
                        col.a = 0.8 * (scanLines + rim + bigFracLine);
                        return col;
                    }
                    return _WireColor;
                }
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}