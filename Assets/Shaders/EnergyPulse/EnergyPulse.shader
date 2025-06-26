Shader "Stylized/TrueObjectSpace_MultiPulse_Improved"
{
    Properties
    {
        [Header(Base Wire Appearance)]
        _BaseMap("Base Material (RGB)", 2D) = "white" {}
        _WireShapeMask("Wire Shape Mask (A channel)", 2D) = "white" {}
        _WireColorTint("Wire Tint Color", Color) = (1, 1, 1, 1)

        [Header(Pulse Appearance)]
        [NoScaleOffset] _PulseGradient("Pulse Color (RGB)", 2D) = "white" {}
        [NoScaleOffset] _NoiseTex("Pulse Noise Mask", 2D) = "gray" {}
        _NoiseScale("Noise Tiling", Float) = 1.0

        [Header(Global Shape Animation)]
        _ObjectDirection("Pulse Object Direction", Vector) = (0, 1, 0, 0)
        _PulseScale("Object Space Tiling", Float) = 1.0
        _PulseFeather("Pulse Edge Softness", Range(0.001, 0.1)) = 0.01
        [HDR] _EmissionIntensity("Global Emission Intensity", Float) = 1.0
        
        [HideInInspector] _PulseCount ("Pulse Count", Int) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #define MAX_PULSES 10

            CBUFFER_START(UnityPerMaterial)
                half4 _WireColorTint;
                half _EmissionIntensity, _PulseFeather;
                float4 _ObjectDirection;
                float _PulseScale, _NoiseScale;
                int _PulseCount;
                // Dữ liệu cho mỗi pulse
                float _PulseWidths[MAX_PULSES], _PulseSpeeds[MAX_PULSES], _TimeOffsets[MAX_PULSES];
                // <-- CẢI TIẾN: Thêm mảng cho hiệu ứng nhấp nháy
                float _FlickerStrengths[MAX_PULSES], _FlickerFrequencies[MAX_PULSES];
            CBUFFER_END

            TEXTURE2D(_BaseMap);       SAMPLER(sampler_BaseMap);
            TEXTURE2D(_WireShapeMask); SAMPLER(sampler_WireShapeMask);
            TEXTURE2D(_PulseGradient); SAMPLER(sampler_PulseGradient);
            TEXTURE2D(_NoiseTex);      SAMPLER(sampler_NoiseTex);
            
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float objectSpaceV : TEXCOORD1; };

            Varyings vert(Attributes v) {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.objectSpaceV = dot(v.positionOS.xyz, normalize(_ObjectDirection.xyz)) * _PulseScale;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half wireAlpha = SAMPLE_TEXTURE2D(_WireShapeMask, sampler_WireShapeMask, i.uv).a * _WireColorTint.a;
                half3 baseMaterialColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb * _WireColorTint.rgb;
                float pulseV = frac(i.objectSpaceV);

                half3 totalEmissionColor = half3(0, 0, 0);
                half totalPulsePresence = 0;

                // <-- CẢI TIẾN: Vòng lặp for với số lần lặp cố định
                for (int j = 0; j < MAX_PULSES; j++)
                {
                    // Chỉ tính toán nếu đây là một pulse hợp lệ
                    if (j >= _PulseCount) break;

                    float pulseWidth = _PulseWidths[j];
                    float pulseSpeed = _PulseSpeeds[j];
                    float timeOffset = _TimeOffsets[j];
                    float headPos = frac(-_Time.y * pulseSpeed + timeOffset);
                    float tailPos = headPos - pulseWidth;

                    half inPulse;
                    if (tailPos < 0.0)
                    {
                        half pulse_tail_part = smoothstep(tailPos + 1.0, tailPos + 1.0 + _PulseFeather, pulseV);
                        half pulse_head_part = 1.0 - smoothstep(headPos - _PulseFeather, headPos, pulseV);
                        inPulse = saturate(pulse_tail_part + pulse_head_part);
                    }
                    else
                    {
                        inPulse = smoothstep(tailPos, tailPos + _PulseFeather, pulseV) - smoothstep(headPos - _PulseFeather, headPos, pulseV);
                    }
                    
                    if (inPulse > 0.001) {
                        half pulseT;
                        if (tailPos < 0.0) {
                            float remappedV = pulseV < headPos ? pulseV + 1.0 : pulseV;
                            pulseT = (remappedV - (tailPos + 1.0)) / pulseWidth;
                        } else {
                            pulseT = (pulseV - tailPos) / pulseWidth;
                        }

                        // <-- CẢI TIẾN: Tính toán hiệu ứng nhấp nháy
                        half flicker = 1.0;
                        float flickerFreq = _FlickerFrequencies[j];
                        if (flickerFreq > 0) {
                            // Tạo một giá trị giả ngẫu nhiên dựa trên thời gian và tần số
                            half randomWave = frac(sin(j * 13.37 + _Time.y * flickerFreq) * 43758.5453);
                            flicker = lerp(1.0 - _FlickerStrengths[j], 1.0, randomWave);
                        }
                        
                        totalEmissionColor += SAMPLE_TEXTURE2D(_PulseGradient, sampler_PulseGradient, float2(saturate(pulseT), 0.5)).rgb * inPulse * flicker;
                        totalPulsePresence += inPulse * flicker;
                    }
                }
                
                totalPulsePresence = saturate(totalPulsePresence);
                float2 noiseUV = i.uv * _NoiseScale;
                half noiseMask = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
                totalPulsePresence *= noiseMask;

                half3 finalBaseColor = baseMaterialColor * (1.0 - totalPulsePresence);
                half3 finalEmission = totalEmissionColor * totalPulsePresence * _EmissionIntensity;
                half3 finalColor = finalBaseColor + finalEmission;

                return half4(finalColor, wireAlpha);
            }
            ENDHLSL
        }
    }
}