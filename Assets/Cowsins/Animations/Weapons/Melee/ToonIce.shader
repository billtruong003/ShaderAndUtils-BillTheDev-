// Tên file: SnowOverlay.shader
Shader "VfxTeam/Snow Overlay"
{
    Properties
    {
        [Header(Snow Effect)]
        _SnowDirection("Snow Direction (World Space)", Vector) = (0, 1, 0, 0)
        _TransitionProgress("Transition Progress", Range(0, 1)) = 0
        _SnowBuildupHeight("Snow Buildup Height", Range(0, 0.2)) = 0.1
        _TransitionHardness("Transition Hardness", Range(1, 50)) = 10.0

        [Header(Noise Mask)]
        _NoiseMap("Noise Mask (R channel)", 2D) = "white" {}
        _NoiseTiling("Noise Tiling", Float) = 1.0

        [Header(Snow Material)]
        _SnowBaseColor("Snow Base Color", Color) = (0.8, 0.8, 1, 1)
        _SnowTopColor("Snow Top Color", Color) = (1, 1, 1, 1)
        _SnowRamp("Snow Toon Ramp", 2D) = "gray" {}
        _SnowRimColor("Snow Rim Color", Color) = (0.5, 0.8, 1, 1)
        _SnowRimPower("Snow Rim Power", Range(0.1, 8)) = 3.0
        
        [Header(Icy Edge Effect)]
        _IceEdgeColor("Ice Edge Color", Color) = (0.5, 0.9, 1, 1)
        _IceEdgeWidth("Ice Edge Width", Range(0.01, 1.0)) = 0.15
        _IceRimPower("Ice Rim Power", Range(1, 10)) = 4.0
        _IcePulseSpeed("Ice Pulse Speed", Range(0, 20)) = 5.0
        _IcePulseStrength("Ice Pulse Strength", Range(0, 1)) = 0.5

        [HideInInspector] _ObjectHeight("Object Height", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalRenderPipeline" }
        
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS      : SV_POSITION;
                float3 positionWS      : TEXCOORD0;
                float3 normalWS        : TEXCOORD1;
                float baseSnowFactor   : TEXCOORD2; // Hệ số tuyết gốc, chưa có noise
                float finalSnowFactor  : TEXCOORD3; // Hệ số tuyết cuối cùng, đã áp noise
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _SnowDirection;
            float  _TransitionProgress;
            float  _SnowBuildupHeight;
            float  _TransitionHardness;
            float  _NoiseTiling;
            float4 _SnowBaseColor;
            float4 _SnowTopColor;
            float4 _SnowRimColor;
            float  _SnowRimPower;
            float4 _IceEdgeColor;
            float  _IceEdgeWidth;
            float  _IceRimPower;
            float  _IcePulseSpeed;
            float  _IcePulseStrength;
            float  _ObjectHeight;
            CBUFFER_END

            sampler2D _SnowRamp;
            sampler2D _NoiseMap;

            // Hàm Triplanar Mapping để áp noise không bị dãn
            float SampleTriplanar(sampler2D tex, float3 pos, float3 normal, float tiling)
            {
                float3 weights = abs(normal);
                weights /= (weights.x + weights.y + weights.z);

                float x_sample = tex2Dlod(tex, float4(pos.yz * tiling, 0, 0)).r;
                float y_sample = tex2Dlod(tex, float4(pos.xz * tiling, 0, 0)).r;
                float z_sample = tex2Dlod(tex, float4(pos.xy * tiling, 0, 0)).r;
                
                return dot(float3(x_sample, y_sample, z_sample), weights);
            }
            
            Varyings Vertex(Attributes IN)
            {
                Varyings OUT;
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);

                // 1. Tính toán hệ số tuyết gốc (chưa có noise)
                float dotProduct = dot(normalWS, normalize(_SnowDirection.xyz));
                float snowThreshold = lerp(1.0, -1.0, _TransitionProgress);
                OUT.baseSnowFactor = saturate((dotProduct - snowThreshold) * _TransitionHardness);
                
                // 2. Lấy giá trị noise bằng Triplanar mapping
                float noiseValue = SampleTriplanar(_NoiseMap, positionWS, normalWS, _NoiseTiling);

                // 3. Hệ số tuyết cuối cùng để tạo lỗ hổng và đắp chiều cao
                OUT.finalSnowFactor = OUT.baseSnowFactor * noiseValue;

                // 4. Áp dụng chiều cao tuyết dựa trên hệ số cuối cùng
                float3 positionOS = IN.positionOS.xyz + IN.normalOS * _SnowBuildupHeight * OUT.finalSnowFactor;

                OUT.positionWS = TransformObjectToWorld(positionOS);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = normalWS;
                return OUT;
            }

            half4 Fragment(Varyings IN) : SV_Target
            {
                // Loại bỏ những pixel hoàn toàn không có tuyết (sau khi áp noise)
                clip(IN.finalSnowFactor - 0.001);

                float3 normalWS = normalize(IN.normalWS);
                float3 viewDirectionWS = SafeNormalize(GetCameraPositionWS() - IN.positionWS);

                // --- Tính toán Ánh sáng và Màu sắc của Tuyết ---
                Light mainLight = GetMainLight();
                float lightDot = saturate(dot(normalWS, mainLight.direction)) * 0.5 + 0.5;
                float3 rampSnow = tex2D(_SnowRamp, float2(lightDot, lightDot)).rgb;
                float3 objectOriginOffset = mul(unity_WorldToObject, float4(0,0,0,1)).xyz;
                float localHeight = (IN.positionWS.y - objectOriginOffset.y) / max(0.001, _ObjectHeight);
                float3 albedoSnow = lerp(_SnowBaseColor.rgb, _SnowTopColor.rgb, saturate(localHeight));
                float3 ambientLight = SampleSH(normalWS);
                float3 mainLightContribution = mainLight.color * rampSnow;
                float3 litSnowColor = albedoSnow * (ambientLight + mainLightContribution);

                // --- Tính toán Viền sáng của Tuyết (chỉ xuất hiện ở nơi có tuyết) ---
                float rimFactor = 1.0 - saturate(dot(viewDirectionWS, normalWS));
                float3 emissionSnow = _SnowRimColor.rgb * pow(rimFactor, _SnowRimPower);

                // --- TÍNH TOÁN HIỆU ỨNG BĂNG GIÁ ---
                // 'iceFactor' được tính từ baseSnowFactor (chưa có noise) để viền băng không bị thủng
                float iceFactor = 1.0 - smoothstep(0.0, _IceEdgeWidth, IN.baseSnowFactor);
                
                float pulse = 1.0 - (_IcePulseStrength * (0.5 * sin(_Time.y * _IcePulseSpeed) + 0.5));
                float finalIceFactor = iceFactor * pulse;

                // Viền sáng băng giá cũng dùng iceFactor
                float3 emissionIce = _IceEdgeColor.rgb * pow(rimFactor, _IceRimPower) * finalIceFactor;
                
                // Hoà trộn màu tuyết với màu băng ở viền
                float3 finalColor = lerp(litSnowColor, _IceEdgeColor.rgb, finalIceFactor);
                
                // Cộng dồn các lớp phát sáng. Viền tuyết chỉ hiện khi không phải là viền băng.
                float3 finalEmission = (emissionSnow * (1 - finalIceFactor)) + emissionIce;
                
                // Alpha cuối cùng dựa trên finalSnowFactor để tạo lỗ hổng và biên mờ
                float finalAlpha = IN.finalSnowFactor;

                return float4(finalColor + finalEmission, finalAlpha);
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Transparent"
}