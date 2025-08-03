Shader "Skybox/Procedural Starry Sky Final"
{
    // Khối Properties không thay đổi
    Properties
    {
        [Header(Atmosphere)]
        [KeywordEnum(Gradient, Solid Color)] _SKY_MODE ("Sky Mode", Float) = 0
        _TopColor ("Top Color (Zenith)", Color) = (0.05, 0.05, 0.15, 1.0)
        _HorizonColor ("Horizon Color", Color) = (0.15, 0.1, 0.2, 1.0)
        _SolidColor ("Solid Sky Color", Color) = (0.08, 0.08, 0.18, 1.0)
        _HorizonExponent ("Horizon Falloff", Range(0.1, 10.0)) = 2.5

        [Header(Procedural Stars)]
        _StarDensity ("Star Density", Range(0.9, 0.999)) = 0.995
        _StarIntensity ("Star Intensity", Range(0, 5.0)) = 1.5
        _StarTwinkleSpeed ("Star Twinkle Speed", Range(0, 10.0)) = 2.0

        [Header(Nebula Galaxy)]
        [Toggle(_USE_NEBULA)] _UseNebula("Enable Nebula", Float) = 0.0
        [NoScaleOffset] _NebulaTexture ("Nebula Noise (Seamless)", 2D) = "gray" {}
        _NebulaColor("Nebula Color", Color) = (1,1,1,1)
        _NebulaTiling("Nebula Tiling", Float) = 0.2
        _NebulaIntensity("Nebula Intensity", Range(0, 2.0)) = 0.3
        _NebulaScrollSpeed("Nebula Scroll Speed", Float) = 0.01
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Skybox"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            // ================================================================= //
            //                        LOGIC ĐÃ SỬA ĐỔI                         //
            // ================================================================= //
            ZWrite Off
            Cull Off
            ZTest LEqual // [FIX] Dòng này là chìa khóa để sửa lỗi.

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma shader_feature_local _SKY_MODE_GRADIENT _SKY_MODE_SOLID_COLOR
            #pragma shader_feature_local _USE_NEBULA

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Structs, khai báo biến, vertex và fragment shader giữ nguyên
            // không cần thay đổi gì thêm.
            
            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 viewDirectionWS : TEXCOORD0;
            };

            half4 _TopColor, _HorizonColor, _SolidColor;
            float _HorizonExponent, _StarDensity, _StarIntensity, _StarTwinkleSpeed;
            TEXTURE2D(_NebulaTexture);
            SAMPLER(sampler_NebulaTexture);
            half4 _NebulaColor;
            float _NebulaTiling, _NebulaIntensity, _NebulaScrollSpeed;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionCS.z = output.positionCS.w;
                output.viewDirectionWS = normalize(input.positionOS.xyz);
                return output;
            }

            float random(float3 seed)
            {
                return frac(sin(dot(seed, float3(12.9898, 78.233, 45.543))) * 43758.5453);
            }

            half3 GetProceduralStars(float3 viewDir)
            {
                half3 stars = 0;
                float starValue1 = random(viewDir * 100.0);
                if (starValue1 > _StarDensity) { stars += pow(starValue1, 6) * _StarIntensity; }
                float starValue2 = random(viewDir * 300.0);
                if (starValue2 > _StarDensity) { stars += pow(starValue2, 8) * _StarIntensity * 0.7; }
                float twinkle = random(viewDir + _Time.y * 0.01 * _StarTwinkleSpeed);
                stars *= saturate(twinkle * 2.0);
                return stars;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 viewDir = normalize(input.viewDirectionWS);

                half3 skyColor;
                #if defined(_SKY_MODE_GRADIENT)
                    float gradientFactor = pow(saturate(viewDir.y), _HorizonExponent);
                    skyColor = lerp(_HorizonColor.rgb, _TopColor.rgb, gradientFactor);
                #else
                    skyColor = _SolidColor.rgb;
                #endif

                half3 stars = GetProceduralStars(viewDir);
                half3 finalColor = skyColor + stars;

                #if defined(_USE_NEBULA)
                    float2 nebulaUV = viewDir.xz * _NebulaTiling;
                    nebulaUV.y += _Time.y * _NebulaScrollSpeed;
                    half nebulaValue = SAMPLE_TEXTURE2D(_NebulaTexture, sampler_NebulaTexture, nebulaUV).r;
                    finalColor += pow(nebulaValue, 2) * _NebulaColor.rgb * _NebulaIntensity;
                #endif

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Skybox/Cubemap"
}