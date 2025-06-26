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
        _NoiseStrength("Noise Strength", Range(0, 1)) = 1.0
        _NoiseScale("Noise Tiling", Float) = 1.0

        [Header(Global Shape Animation)]
        _ObjectDirection("Pulse Object Direction", Vector) = (0, 1, 0, 0)
        _PulseScale("Object Space Tiling", Float) = 1.0
        _PulseFeather("Pulse Edge Softness", Range(0.001, 0.1)) = 0.01
        [HDR] _EmissionIntensity("Global Emission Intensity", Float) = 1.0
        
        [HideInInspector] _PulseCount ("Pulse Count", Int) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendSrc ("Blend Source", Float) = 1  // Nguồn blend
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendDst ("Blend Destination", Float) = 0  // Đích blend
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest Mode", Float) = 4  // Chế độ kiểm tra depth
        [Toggle] _ZWrite ("ZWrite On/Off", Float) = 1  // Bật/tắt ghi depth
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }  // Tags cơ bản
        ZWrite [_ZWrite]  // Sử dụng giá trị từ Properties
        ZTest [_ZTest]    // Sử dụng giá trị từ Properties
        Blend [_BlendSrc] [_BlendDst]  // Sử dụng giá trị từ Properties
        
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
                float _PulseScale, _NoiseScale, _NoiseStrength;
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

                for (int j = 0; j < _PulseCount; j++)
                {
                    float pulseWidth = _PulseWidths[j];
                    float pulseSpeed = _PulseSpeeds[j];
                    float timeOffset = _TimeOffsets[j];
                    float headPos = frac(-_Time.y * pulseSpeed + timeOffset);
                    float tailPos = headPos - pulseWidth;
                    float dist = pulseV - tailPos;

                    half inPulse = smoothstep(0, _PulseFeather, dist) - smoothstep(pulseWidth - _PulseFeather, pulseWidth, dist);
                    if (inPulse > 0.001) {
                        half pulseT = saturate(dist / pulseWidth);

                        half flicker = 1.0;
                        float flickerFreq = _FlickerFrequencies[j];
                        if (flickerFreq > 0) {
                            float seed = j + _Time.y * flickerFreq;
                            half random = frac(sin(seed) * 43758.5453);
                            flicker = lerp(1.0 - _FlickerStrengths[j], 1.0, random);
                        }

                        totalEmissionColor += SAMPLE_TEXTURE2D(_PulseGradient, sampler_PulseGradient, float2(pulseT, 0.5)).rgb * inPulse * flicker;
                        totalPulsePresence += inPulse * flicker;
                    }
                }

                totalPulsePresence = saturate(totalPulsePresence);
                float2 noiseUV = i.uv * _NoiseScale;
                half noiseMask = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
                totalPulsePresence *= lerp(1.0, noiseMask, _NoiseStrength);

                half3 finalBaseColor = baseMaterialColor * (1.0 - totalPulsePresence);
                half3 finalEmission = totalEmissionColor * totalPulsePresence * saturate(_EmissionIntensity);
                half3 finalColor = finalBaseColor + finalEmission;

                return half4(finalColor, wireAlpha);
            }
            ENDHLSL
        }
    }
}