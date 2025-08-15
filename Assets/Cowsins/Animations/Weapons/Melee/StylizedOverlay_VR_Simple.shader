Shader "BillTheDev/StylizedOverlay_VR_Simple"
{
    Properties
    {
        [Header(General Overlay Settings)]
        _OverlayDirection("Overlay Direction (World Space)", Vector) = (0, 1, 0, 0)
        _TransitionProgress("Transition Progress", Range(-1, 1)) = 0.5
        _DisplacementHeight("Displacement Height", Range(0, 0.2)) = 0.05
        _TransitionHardness("Transition Hardness", Range(1, 100)) = 20

        [Header(Breakup Noise UV Based)]
        _NoiseMap("Breakup Noise (R)", 2D) = "white" {}
        _NoiseTiling("Noise Tiling", Float) = 1.0

        [Header(Ice Material)]
        _IceBaseColor("Ice Base Color", Color) = (0.7, 0.8, 1, 0.5)
        _EdgeColor("Edge Color", Color) = (0.5, 0.9, 1, 1)
        _EdgeWidth("Edge Width", Range(0.01, 1.0)) = 0.15
        _EdgePulseSpeed("Edge Pulse Speed", Range(0, 20)) = 5.0
        _EdgePulseStrength("Edge Pulse Strength", Range(0, 1)) = 0.5
        _EdgeSpecularColor("Specular Color", Color) = (1,1,1,1)
        _EdgeSpecularPower("Specular Power", Range(1, 128)) = 32

        [Header(Shared Bling Effect Simplex)]
        [HDR] _BlingColor("Bling Color", Color) = (1, 1.2, 1.5, 1)
        _BlingScale("Bling Scale", Float) = 15.0
        _BlingDensity("Bling Density Threshold", Range(-1, 1)) = 0.95
        _BlingHardness("Bling Hardness", Range(1, 512)) = 256
        _BlingSpeed("Bling Speed", Float) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalRenderPipeline" }
        
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            // ĐÃ XÓA: Các pragma không cần thiết, đặc biệt là _REFRACTION_ON gây lỗi VR
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Includes/SimplexNoise.hlsl" // Giữ lại cho hiệu ứng Bling

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float  overlayMask  : TEXCOORD2;
                float3 positionOS   : TEXCOORD3; // Giữ lại positionOS cho Bling và các hiệu ứng khác
            };
            
            TEXTURE2D(_NoiseMap); SAMPLER(sampler_NoiseMap);
            // ĐÃ XÓA: _CameraOpaqueTexture và _SnowToonRamp

            CBUFFER_START(UnityPerMaterial)
            float4 _OverlayDirection; float _TransitionProgress; float _DisplacementHeight; float _TransitionHardness;
            float _NoiseTiling;
            float4 _IceBaseColor; float4 _EdgeColor; float _EdgeWidth;
            float _EdgePulseSpeed; float _EdgePulseStrength; float4 _EdgeSpecularColor; float _EdgeSpecularPower;
            float4 _BlingColor; float _BlingScale; float _BlingDensity; float _BlingHardness; float _BlingSpeed;
            // ĐÃ XÓA: Các thuộc tính của Snow và Crystal
            CBUFFER_END
            
            Varyings Vertex(Attributes IN)
            {
                Varyings OUT;
                OUT.positionOS = IN.positionOS.xyz;
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                
                float dotProduct = dot(normalWS, normalize(_OverlayDirection.xyz));
                float heightBias = lerp(1.0, -1.0, _TransitionProgress * 0.5 + 0.5);
                float baseOverlayFactor = saturate((dotProduct - heightBias) * _TransitionHardness);
                
                float2 noiseUV = IN.uv * _NoiseTiling;
                float noiseValue = SAMPLE_TEXTURE2D_LOD(_NoiseMap, sampler_NoiseMap, noiseUV, 0).r;
                OUT.overlayMask = saturate(baseOverlayFactor * noiseValue);

                float displacement = _DisplacementHeight * OUT.overlayMask;
                
                // ĐÃ XÓA: Phần displacement phức tạp của Crystal
                
                float3 displacedPositionOS = IN.positionOS.xyz + IN.normalOS * displacement;

                OUT.positionWS = TransformObjectToWorld(displacedPositionOS);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = normalWS;
                return OUT;
            }

            half4 Fragment(Varyings IN) : SV_Target
            {
                float overlayMask = IN.overlayMask;
                clip(overlayMask - 0.001); // Nếu mask quá nhỏ, hủy bỏ pixel

                float3 normalWS = normalize(IN.normalWS);
                float3 viewDirectionWS = SafeNormalize(GetCameraPositionWS() - IN.positionWS);
                Light mainLight = GetMainLight();
                
                // --- 1. SET MÀU VÀ ALPHA CƠ BẢN ---
                float3 finalColor = _IceBaseColor.rgb;
                float finalAlpha = _IceBaseColor.a * overlayMask;

                // --- 2. TÍNH TOÁN CÁC HIỆU ỨNG PHÁT SÁNG (EMISSION) ---
                
                // Hiệu ứng cạnh viền
                float edgeFactor = 1.0 - smoothstep(0.0, _EdgeWidth, overlayMask);
                float pulseFactor = 1.0 - (_EdgePulseStrength * (0.5 * sin(_Time.y * _EdgePulseSpeed) + 0.5));
                float3 edgeEmission = edgeFactor * pulseFactor * _EdgeColor.rgb;
                
                // Hiệu ứng phản quang (Specular)
                float3 halfVec = SafeNormalize(mainLight.direction + viewDirectionWS);
                float specDot = saturate(dot(normalWS, halfVec));
                float specToon = pow(specDot, _EdgeSpecularPower);
                float3 specularEmission = _EdgeSpecularColor.rgb * specToon * mainLight.color * (1.0 - edgeFactor);

                // Hiệu ứng lấp lánh (Bling)
                float3 noisePos = IN.positionOS * _BlingScale + float3(0,0,_Time.y * _BlingSpeed);
                float blingNoise = snoise(noisePos);
                float bling = pow(saturate(blingNoise), _BlingHardness) * step(_BlingDensity, blingNoise);
                float3 blingEmission = bling * _BlingColor.rgb * _BlingColor.a * (1.0 - edgeFactor);

                float3 finalEmission = edgeEmission + specularEmission + blingEmission;

                return float4(finalColor + finalEmission, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Transparent"
}