Shader "BillTheDev/StylizedOverlay_VR_Simple_Icicles"
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

        // THAY ĐỔI: Thuộc tính Icicles đã được làm lại cho trực quan hơn
        [Header(Icicle Settings)]
        [Toggle(_ICICLES_ON)] _EnableIcicles("Enable Icicles", Float) = 0
        _IcicleLength("Icicle Length (World Units)", Range(0.01, 50)) = 0.5
        _IcicleFrequency("Icicle Frequency", Float) = 10.0
        _IcicleIntensity("Icicle Intensity", Range(0, 2)) = 1.0
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

            #pragma shader_feature_local_fragment _ICICLES_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Includes/SimplexNoise.hlsl"

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
                float3 positionOS   : TEXCOORD3;
            };
            
            TEXTURE2D(_NoiseMap); SAMPLER(sampler_NoiseMap);

            CBUFFER_START(UnityPerMaterial)
            float4 _OverlayDirection; float _TransitionProgress; float _DisplacementHeight; float _TransitionHardness;
            float _NoiseTiling;
            float4 _IceBaseColor; float4 _EdgeColor; float _EdgeWidth;
            float _EdgePulseSpeed; float _EdgePulseStrength; float4 _EdgeSpecularColor; float _EdgeSpecularPower;
            float4 _BlingColor; float _BlingScale; float _BlingDensity; float _BlingHardness; float _BlingSpeed;
            float _EnableIcicles; float _IcicleLength; float _IcicleFrequency; float _IcicleIntensity;
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

                // Note: Displacement does not apply to icicles in this version to keep it simple.
                float3 displacedPositionOS = IN.positionOS.xyz + IN.normalOS * (_DisplacementHeight * OUT.overlayMask);

                OUT.positionWS = TransformObjectToWorld(displacedPositionOS);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = normalWS;
                return OUT;
            }

            half4 Fragment(Varyings IN) : SV_Target
            {
                float finalOverlayMask = IN.overlayMask;

                #if defined(_ICICLES_ON)
                    // 1. Chỉ tạo thạch nhũ ở những bề mặt hướng xuống và đã có băng
                    float downFacingMask = saturate(dot(IN.normalWS, float3(0, -1, 0)));
                    float icicleBase = IN.overlayMask * downFacingMask;

                    // 2. Tạo họa tiết nhiễu theo phương ngang (XZ) để xác định vị trí các thạch nhũ
                    // Dùng snoise để trông tự nhiên hơn là sin
                    float3 horizontalPos = float3(IN.positionWS.x, 0, IN.positionWS.z);
                    float dripNoise = snoise(horizontalPos * _IcicleFrequency) * 0.5 + 0.5;
                    dripNoise = smoothstep(0.6, 1.0, dripNoise); // Làm cho các "giọt" sắc nét hơn

                    // 3. TẠO GRADIENT DỌC để "kéo dài" thạch nhũ xuống
                    // frac() tạo ra một gradient lặp lại từ 0 đến 1.
                    // (1.0 - frac(...)) đảo ngược nó thành 1 xuống 0.
                    // Chia cho _IcicleLength để kiểm soát độ dài thực tế của gradient.
                    float verticalStretch = 1.0 - frac(IN.positionWS.y / _IcicleLength);
                    verticalStretch = pow(verticalStretch, 4.0); // Làm cho đầu nhọn hơn

                    // 4. Kết hợp tất cả lại
                    float icicleMask = icicleBase * dripNoise * verticalStretch * _IcicleIntensity;
                    
                    finalOverlayMask = saturate(IN.overlayMask + icicleMask);
                #endif

                float overlayMask = finalOverlayMask;
                clip(overlayMask - 0.001);

                float3 normalWS = normalize(IN.normalWS);
                float3 viewDirectionWS = SafeNormalize(GetCameraPositionWS() - IN.positionWS);
                Light mainLight = GetMainLight();
                
                float3 finalColor = _IceBaseColor.rgb;
                float finalAlpha = _IceBaseColor.a * overlayMask;

                float edgeFactor = 1.0 - smoothstep(0.0, _EdgeWidth, overlayMask);
                float pulseFactor = 1.0 - (_EdgePulseStrength * (0.5 * sin(_Time.y * _EdgePulseSpeed) + 0.5));
                float3 edgeEmission = edgeFactor * pulseFactor * _EdgeColor.rgb;
                
                float3 halfVec = SafeNormalize(mainLight.direction + viewDirectionWS);
                float specDot = saturate(dot(normalWS, halfVec));
                float specToon = pow(specDot, _EdgeSpecularPower);
                float3 specularEmission = _EdgeSpecularColor.rgb * specToon * mainLight.color * (1.0 - edgeFactor);

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