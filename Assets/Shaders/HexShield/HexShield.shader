// Dịch từ hướng dẫn Shader Graph của "Daniel Ilett"
// Shader này tái tạo hiệu ứng lá chắn khoa học viễn tưởng cho URP trong HLSL.
// Phiên bản cuối cùng: Sửa lỗi 'invalid subscript' bằng cách di chuyển TBN vào khối #if.
// Phiên bản sửa đổi: Thêm chức năng Tiling/Scale cho Hexagon Texture.
Shader "Tutorial/FuturisticShield_With_Tiling"
{
    Properties
    {
        [Header(PBR Features)]
        _BaseColor("Base Color", Color) = (0.2, 0.8, 1, 0.25)
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.5
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        [HDR] _EmissiveColor("Emissive Color", Color) = (0.2, 0.8, 1, 1)

        [Header(Edge Glow)]
        [Toggle(_USEEDGEGLOW_ON)] _UseEdgeGlow("Use Edge Glow", Float) = 1
        _EdgeGlowStrength("Edge Glow Strength", Range(0, 5)) = 1
        _EdgeGlowThickness("Edge Glow Thickness (X, Y)", Vector) = (0.1, 0.2, 0, 0)

        [Header(Intersection)]
        [Toggle(_USEINTERSECTION_ON)] _UseIntersection("Use Intersection", Float) = 1
        _IntersectionStrength("Intersection Strength", Range(0, 10)) = 2
        _IntersectionThickness("Intersection Thickness", Range(0, 2)) = 0.5

        [Header(Collision Ripples)]
        [Toggle(_USECOLLISIONRIPPLES_ON)] _UseCollisionRipples("Use Collision Ripples", Float) = 1
        _RippleOrigin("Ripple Origin (UV)", Vector) = (0.5, 0.5, 0, 0)
        _RippleThickness("Ripple Thickness", Float) = 0.1
        _RippleSpeed("Ripple Speed", Float) = 2.0
        _RippleTime("Ripple Start Time", Float) = -10.0
        _AspectRatio("Aspect Ratio (H/W)", Float) = 0.5

        [Header(Glowing Surface)]
        [Toggle(_USEHEXAGONGLOW_ON)] _UseHexagonGlow("Use Hexagon Glow", Float) = 1
        _GlowStrengthMinMax("Glow Strength (Min, Max)", Vector) = (0.1, 0.5, 0, 0)
        _GlowNoiseScale("Glow Noise Scale", Float) = 5.0
        _GlowSpeed("Glow Speed", Float) = 0.5

        [Header(Hexagon Texture)]
        _HexagonTexture("Hexagon Texture (R)", 2D) = "white" {}
        _HexagonTiling("Hexagon Tiling (X, Y)", Vector) = (1, 1, 0, 0) // <-- THÊM MỚI: Thuộc tính để điều khiển tiling
        [Toggle(_USEHEIGHTMAP_ON)] _UseHeightmap("Use Hexagon Heightmap", Float) = 1
        _HeightmapStrength("Heightmap Strength", Range(0, 1)) = 0.1

        [Header(Big Scanline)]
        [Toggle(_USEBIGSCANLINE_ON)] _UseBigScanline("Use Big Scanline", Float) = 1
        _BigScanlineStrength("Big Scanline Strength", Range(0, 5)) = 1.0
        _BigScanlineSpeed("Big Scanline Speed", Float) = 0.5
        _BigScanlineThickness("Big Scanline Thickness", Range(0.01, 1.0)) = 0.05

        [Header(Smaller Scanlines)]
        [Toggle(_USESCANLINES_ON)] _UseScanlines("Use Scanlines", Float) = 1
        _ScanlineStrength("Scanline Strength", Range(0, 1)) = 0.1
        _ScanlineTexture("Scanline Texture (R)", 2D) = "white" {}
        _ScanlineVelocity("Scanline Velocity (UV)", Vector) = (0, 0.2, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _USEEDGEGLOW_ON
            #pragma multi_compile _ _USEINTERSECTION_ON
            #pragma multi_compile _ _USECOLLISIONRIPPLES_ON
            #pragma multi_compile _ _USEHEXAGONGLOW_ON
            #pragma multi_compile _ _USEHEIGHTMAP_ON
            #pragma multi_compile _ _USEBIGSCANLINE_ON
            #pragma multi_compile _ _USESCANLINES_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
            };

            struct Varyings {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
                float3 normalWS     : TEXCOORD2;
                float3 tangentWS    : TEXCOORD3;
                float3 bitangentWS  : TEXCOORD4;
                float4 screenPos    : TEXCOORD5;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor; float _Metallic; float _Smoothness; float4 _EmissiveColor;
                float _EdgeGlowStrength; float2 _EdgeGlowThickness;
                float _IntersectionStrength; float _IntersectionThickness;
                float2 _RippleOrigin; float _RippleThickness; float _RippleSpeed; float _RippleTime; float _AspectRatio;
                float2 _GlowStrengthMinMax; float _GlowNoiseScale; float _GlowSpeed;
                float2 _HexagonTiling; // <-- THÊM MỚI: Khai báo biến tiling
                float _HeightmapStrength;
                float _BigScanlineStrength; float _BigScanlineSpeed; float _BigScanlineThickness;
                float _ScanlineStrength; float2 _ScanlineVelocity;
            CBUFFER_END

            TEXTURE2D(_HexagonTexture);         SAMPLER(sampler_HexagonTexture);
            TEXTURE2D(_ScanlineTexture);        SAMPLER(sampler_ScanlineTexture);
            TEXTURE2D(_CameraDepthTexture);     SAMPLER(sampler_CameraDepthTexture);

            float2 hash(float2 p) { p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3))); return -1.0 + 2.0 * frac(sin(p) * 43758.5453123); }
            float noise(float2 p) { float2 i = floor(p); float2 f = frac(p); float2 u = f * f * (3.0 - 2.0 * f); return lerp(lerp(dot(hash(i + float2(0.0, 0.0)), f - float2(0.0, 0.0)), dot(hash(i + float2(1.0, 0.0)), f - float2(1.0, 0.0)), u.x), lerp(dot(hash(i + float2(0.0, 1.0)), f - float2(0.0, 1.0)), dot(hash(i + float2(1.0, 1.0)), f - float2(1.0, 1.0)), u.x), u.y); }

            Varyings vert(Attributes v) {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.uv = v.uv;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.tangentWS = TransformObjectToWorldDir(v.tangentOS.xyz);
                o.bitangentWS = cross(o.normalWS, o.tangentWS) * v.tangentOS.w;
                o.screenPos = o.positionCS;
                return o;
            }

            float4 frag(Varyings i) : SV_Target {
                SurfaceData surfaceData = (SurfaceData)0;
                InputData inputData = (InputData)0;
                
                surfaceData.albedo = _BaseColor.rgb;
                surfaceData.alpha = _BaseColor.a;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.occlusion = 1.0;

                float totalEmission = 0;
                // --- All emission calculations ---
                #if defined(_USEEDGEGLOW_ON)
                    float2 centeredUV = abs(i.uv - 0.5) * 2.0;
                    float2 edgeFactor = smoothstep(1.0, 1.0 - _EdgeGlowThickness, centeredUV);
                    totalEmission += (1.0 - min(edgeFactor.x, edgeFactor.y)) * _EdgeGlowStrength;
                #endif
                #if defined(_USEINTERSECTION_ON)
                    float sceneDepth = LinearEyeDepth(SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, i.screenPos.xy / i.screenPos.w).r, _ZBufferParams);
                    float depthDiff = sceneDepth - i.screenPos.w;
                    float intersectionGlow = 1.0 - smoothstep(0, _IntersectionThickness, depthDiff);
                    totalEmission += intersectionGlow * _IntersectionStrength;
                #endif
                float ripple = 0;
                #if defined(_USECOLLISIONRIPPLES_ON)
                    float2 correctedUV = i.uv; correctedUV.y *= _AspectRatio;
                    float2 rippleOriginCorrected = _RippleOrigin; rippleOriginCorrected.y *= _AspectRatio;
                    float dist = distance(correctedUV, rippleOriginCorrected);
                    float timeSinceHit = _Time.y - _RippleTime;
                    float rippleCurrentRadius = timeSinceHit * _RippleSpeed;
                    float inner = smoothstep(rippleCurrentRadius - _RippleThickness, rippleCurrentRadius, dist);
                    float outer = 1.0 - smoothstep(rippleCurrentRadius, rippleCurrentRadius + _RippleThickness, dist);
                    ripple = inner * outer * saturate(1.5 - timeSinceHit);
                #endif
                float surfaceGlow = 0;
                #if defined(_USEHEXAGONGLOW_ON)
                    float2 noiseUV = i.uv * _GlowNoiseScale; noiseUV.y += _Time.y * _GlowSpeed;
                    float noiseVal = (noise(noiseUV) + 1.0) * 0.5;
                    surfaceGlow = lerp(_GlowStrengthMinMax.x, _GlowStrengthMinMax.y, noiseVal);
                #else
                    surfaceGlow = _GlowStrengthMinMax.y;
                #endif

                // <-- SỬA ĐỔI: Tính toán UV cho hexagon với tiling
                float2 hexagonUV = i.uv * _HexagonTiling.xy;
                
                float hexagonMask = SAMPLE_TEXTURE2D(_HexagonTexture, sampler_HexagonTexture, hexagonUV).r;
                float hexagonPattern = saturate(surfaceGlow + ripple) * hexagonMask;
                totalEmission += hexagonPattern;

                #if defined(_USEBIGSCANLINE_ON)
                    float scanlineClock = 1.0 - frac(_Time.y * _BigScanlineSpeed);
                    float remappedY = lerp(1.0 + _BigScanlineThickness, -_BigScanlineThickness, scanlineClock);
                    float distFromScan = abs(remappedY - i.uv.y);
                    float bigScanline = 1.0 - smoothstep(0, _BigScanlineThickness, distFromScan);
                    totalEmission += bigScanline * _BigScanlineStrength;
                #endif
                #if defined(_USESCANLINES_ON)
                    float2 scanlineUV = i.uv + _Time.y * _ScanlineVelocity.xy;
                    float smallScanlines = SAMPLE_TEXTURE2D(_ScanlineTexture, sampler_ScanlineTexture, scanlineUV).r;
                    totalEmission += smallScanlines * _ScanlineStrength;
                #endif
                surfaceData.emission = totalEmission * _EmissiveColor.rgb;
                
                // --- Final Normal Calculation ---
                float3 finalNormalWS = normalize(i.normalWS);
                #if defined(_USEHEIGHTMAP_ON)
                    float3x3 TBN = float3x3(i.tangentWS, i.bitangentWS, i.normalWS);

                    // <-- SỬA ĐỔI: Tính đạo hàm trên UV đã được scale để normal map khớp với texture
                    float2 ddx_uv = ddx(hexagonUV);
                    float2 ddy_uv = ddy(hexagonUV);
                    
                    // <-- SỬA ĐỔI: Sử dụng hexagonUV đã được scale
                    float h_center = SAMPLE_TEXTURE2D(_HexagonTexture, sampler_HexagonTexture, hexagonUV).r;
                    float h_x = SAMPLE_TEXTURE2D(_HexagonTexture, sampler_HexagonTexture, hexagonUV + ddx_uv).r;
                    float h_y = SAMPLE_TEXTURE2D(_HexagonTexture, sampler_HexagonTexture, hexagonUV + ddy_uv).r;
                    
                    float3 tangentTS  = float3(1, 0, (h_x - h_center) * _HeightmapStrength / max(length(ddx_uv), 1e-5f));
                    float3 bitangentTS= float3(0, 1, (h_y - h_center) * _HeightmapStrength / max(length(ddy_uv), 1e-5f));
                    float3 normalTS = normalize(cross(tangentTS, bitangentTS));
                    finalNormalWS = normalize(mul(normalTS, TBN));
                #endif

                // --- Final Assignment ---
                inputData.positionWS = i.positionWS;
                inputData.normalWS = finalNormalWS;
                inputData.viewDirectionWS = GetWorldSpaceViewDir(i.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(i.positionWS);

                return UniversalFragmentPBR(inputData, surfaceData);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}