Shader "Custom/URP/ToonWhiteEye_Pro"
{
    Properties
    {
        [Header(Base Eye Sclera)]
        _BaseColor ("Base Sclera Color", Color) = (1,1,1,1) // Màu nền của tròng trắng

        [Header(Toon Shading)]
        _ToonThreshold ("Toon Threshold", Range(0,1)) = 0.5 // Ngưỡng chuyển giữa sáng/tối
        _ToonSmoothness ("Toon Smoothness", Range(0.001,0.5)) = 0.05 // Độ mềm mại của vùng chuyển
        [HDR] _ToonLitColor ("Toon Lit Color", Color) = (1,1,1,1) // Màu vùng sáng
        [HDR] _ToonShadowColor ("Toon Shadow Color", Color) = (0.5,0.5,0.5,1) // Màu vùng tối
        [HDR] _AmbientColor ("Ambient Color (Overall)", Color) = (0.2, 0.2, 0.2, 1) // Ánh sáng môi trường
        _ToonOverallStrength ("Toon Overall Strength", Range(0, 2)) = 1.0 // Cường độ tổng thể của Toon shading

        [Header(Specular Highlight)]
        _SpecularMask ("Specular Mask (Grayscale)", 2D) = "white" {} // Mask hình dạng điểm sáng
        [HDR] _SpecularColor ("Specular Color", Color) = (1,1,1,1) // Màu điểm sáng (có thể HDR)
        _Shininess ("Shininess (Power)", Range(1, 200)) = 30.0 // Độ bóng của điểm sáng
        _SpecularStrength ("Specular Strength", Range(0, 5)) = 1.0 // Cường độ tổng thể của điểm sáng
        _SpecularFresnelPower ("Specular Fresnel Power", Range(0, 5)) = 0.0 // Fresnel cho điểm sáng (0 = tắt)

        [Header(Iris Occlusion Shadow)]
        [Toggle(_IRIS_OCCLUSION_SHADOW_ENABLED)] _IrisOcclusionShadowEnabled ("Enable Iris Occlusion Shadow", Float) = 0
        [HDR] _IrisOcclusionColor ("Iris Occlusion Color", Color) = (0.1,0.1,0.1,1) // Màu của bóng đổ
        _IrisOcclusionRadius ("Iris Cutout Radius (UV)", Range(0, 0.5)) = 0.2 // Bán kính tương đối của "lỗ" iris trên UV sclera
        _IrisOcclusionSoftness ("Iris Occlusion Softness", Range(0.001, 0.2)) = 0.05 // Độ mềm của cạnh bóng đổ
        _IrisOcclusionStrength ("Iris Occlusion Strength", Range(0,1)) = 1.0 // Cường độ tổng thể của bóng đổ

        [Header(Blood Vessels General)]
        [Toggle(_BLOOD_VESSELS_ENABLED)] _BloodVesselEnabled ("Enable Blood Vessels", Float) = 0 // Bật/tắt toàn bộ hệ thống vân máu
        _BloodVesselOverallStrength ("Blood Vessel Overall Strength", Range(0,1)) = 1.0 // Cường độ tổng thể của vân máu
        _BloodVesselAlphaThreshold ("Blood Vessel Alpha Threshold", Range(0.2,1)) = 0.01 // MỚI: Ngưỡng alpha để loại bỏ noise nền

        [Header(Blood Vessels Distortion Layer)]
        [Toggle(_BLOOD_VESSELS_DISTORTION)] _BloodVesselDistortionEnabled ("Enable Distortion", Float) = 0 // Bật/tắt biến dạng
        _DistortionTexture ("Distortion Map (RG for XY offset)", 2D) = "gray" {} // Texture biến dạng (vd: noise)
        _DistortionStrength ("Distortion Strength", Range(0,1)) = 0.1 // Cường độ biến dạng
        _DistortionScale ("Distortion UV Scale", Float) = 1.0 // Tỷ lệ UV của texture biến dạng
        _DistortionSpeed ("Distortion Scroll Speed (XY)", Vector) = (0.01, 0.01, 0, 0) // Tốc độ cuộn của texture biến dạng

        [Header(Blood Vessels Layer 1)]
        _BloodVesselTexture1 ("Vessel Texture 1 (RGBA)", 2D) = "white" {} // Texture vân máu lớp 1
        [HDR] _BloodVesselColor1 ("Vessel Color 1", Color) = (0.5,0,0,1) // Màu của vân máu lớp 1
        _BloodVesselStrength1 ("Vessel Strength 1", Range(0,1)) = 1.0 // Cường độ hiển thị lớp 1
        _BloodVesselScale1 ("Vessel UV Scale 1 (XY)", Vector) = (1.0, 1.0, 0, 0) // Tỷ lệ UV lớp 1
        _BloodVesselSpeed1 ("Vessel Scroll Speed 1 (XY)", Vector) = (0.0, 0.0, 0, 0) // Tốc độ cuộn lớp 1

        [Header(Blood Vessels Layer 2)]
        [Toggle(_BLOOD_VESSELS_LAYER2_ENABLED)] _BloodVesselLayer2Enabled ("Enable Vessel Layer 2", Float) = 0 // Bật/tắt lớp 2
        _BloodVesselTexture2 ("Vessel Texture 2 (RGBA)", 2D) = "white" {} // Texture vân máu lớp 2
        [HDR] _BloodVesselColor2 ("Vessel Color 2", Color) = (0.5,0,0,1) // Màu của vân máu lớp 2
        _BloodVesselStrength2 ("Vessel Strength 2", Range(0,1)) = 1.0 // Cường độ hiển thị lớp 2
        _BloodVesselScale2 ("Vessel UV Scale 2 (XY)", Vector) = (1.0, 1.0, 0, 0) // Tỷ lệ UV lớp 2
        _BloodVesselSpeed2 ("Vessel Scroll Speed 2 (XY)", Vector) = (0.0, 0.0, 0, 0) // Tốc độ cuộn lớp 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" } // Sclera thường là opaque
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fw_and_shadows
            #pragma shader_feature_local _BLOOD_VESSELS_ENABLED // Bật/tắt toàn bộ vân máu
            #pragma shader_feature_local _BLOOD_VESSELS_DISTORTION // Bật/tắt biến dạng vân máu
            #pragma shader_feature_local _BLOOD_VESSELS_LAYER2_ENABLED // Bật/tắt lớp vân máu thứ 2
            #pragma shader_feature_local _IRIS_OCCLUSION_SHADOW_ENABLED // Bật/tắt bóng đổ Iris

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3; // Hướng nhìn từ pixel đến camera
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;

                float _ToonThreshold;
                float _ToonSmoothness;
                float4 _ToonLitColor;
                float4 _ToonShadowColor;
                float4 _AmbientColor;
                float _ToonOverallStrength;

                TEXTURE2D(_SpecularMask); SAMPLER(sampler_SpecularMask);
                float4 _SpecularColor;
                float _Shininess;
                float _SpecularStrength;
                float _SpecularFresnelPower;

                float4 _IrisOcclusionColor;
                float _IrisOcclusionRadius;
                float _IrisOcclusionSoftness;
                float _IrisOcclusionStrength;

                float _BloodVesselOverallStrength;
                float _BloodVesselAlphaThreshold; // MỚI: Ngưỡng alpha

                TEXTURE2D(_DistortionTexture); SAMPLER(sampler_DistortionTexture);
                float _DistortionStrength;
                float _DistortionScale;
                float2 _DistortionSpeed;

                TEXTURE2D(_BloodVesselTexture1); SAMPLER(sampler_BloodVesselTexture1);
                float4 _BloodVesselColor1;
                float _BloodVesselStrength1;
                float2 _BloodVesselScale1;
                float2 _BloodVesselSpeed1;

                TEXTURE2D(_BloodVesselTexture2); SAMPLER(sampler_BloodVesselTexture2);
                float4 _BloodVesselColor2;
                float _BloodVesselStrength2;
                float2 _BloodVesselScale2;
                float2 _BloodVesselSpeed2;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv; 
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = normalize(_WorldSpaceCameraPos.xyz - output.positionWS); // Hướng từ pixel về camera
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 currentScleraColor = _BaseColor.rgb; // Bắt đầu với màu trắng cơ bản

                // --- Iris Occlusion Shadow ---
                #ifdef _IRIS_OCCLUSION_SHADOW_ENABLED
                    float2 centeredUV = input.uv - float2(0.5, 0.5);
                    float radialDist = length(centeredUV); 
                    float shadowFactor = smoothstep(_IrisOcclusionRadius, _IrisOcclusionRadius + _IrisOcclusionSoftness, radialDist);
                    currentScleraColor = lerp(currentScleraColor, _IrisOcclusionColor.rgb, shadowFactor * _IrisOcclusionStrength);
                #endif

                // --- Blood Vessels (Vân máu - Tùy chọn) ---
                #ifdef _BLOOD_VESSELS_ENABLED
                    float2 uv_vein = input.uv;

                    #ifdef _BLOOD_VESSELS_DISTORTION
                        float2 distortionUV = uv_vein * _DistortionScale + _Time.y * _DistortionSpeed;
                        float4 distortionTex = SAMPLE_TEXTURE2D(_DistortionTexture, sampler_DistortionTexture, distortionUV);
                        float2 distortionOffset = (distortionTex.rg * 2.0 - 1.0) * _DistortionStrength; 
                        uv_vein += distortionOffset;
                    #endif

                    // Layer 1
                    float2 vesselUV1 = uv_vein * _BloodVesselScale1 + _Time.y * _BloodVesselSpeed1;
                    float4 bloodVesselTex1 = SAMPLE_TEXTURE2D(_BloodVesselTexture1, sampler_BloodVesselTexture1, vesselUV1);
                    
                    // Áp dụng ngưỡng alpha MỚI
                    float alphaMask1 = step(_BloodVesselAlphaThreshold, bloodVesselTex1.a); // 0 nếu alpha < ngưỡng, 1 nếu alpha >= ngưỡng
                    float blendFactor1 = alphaMask1 * _BloodVesselStrength1;

                    // Pha trộn lớp 1 vào màu sclera hiện tại
                    currentScleraColor = lerp(currentScleraColor, bloodVesselTex1.rgb * _BloodVesselColor1.rgb, blendFactor1);

                    // Layer 2 if enabled
                    #ifdef _BLOOD_VESSELS_LAYER2_ENABLED
                        float2 vesselUV2 = uv_vein * _BloodVesselScale2 + _Time.y * _BloodVesselSpeed2;
                        float4 bloodVesselTex2 = SAMPLE_TEXTURE2D(_BloodVesselTexture2, sampler_BloodVesselTexture2, vesselUV2);
                        
                        // Áp dụng ngưỡng alpha MỚI
                        float alphaMask2 = step(_BloodVesselAlphaThreshold, bloodVesselTex2.a); // 0 nếu alpha < ngưỡng, 1 nếu alpha >= ngưỡng
                        float blendFactor2 = alphaMask2 * _BloodVesselStrength2;

                        // Pha trộn lớp 2 vào màu sclera hiện tại
                        currentScleraColor = lerp(currentScleraColor, bloodVesselTex2.rgb * _BloodVesselColor2.rgb, blendFactor2);
                    #endif

                    // Cuối cùng, áp dụng cường độ tổng thể của vân máu.
                    // Lưu ý: Nếu muốn OverallStrength áp dụng cho TỪNG lớp trước khi trộn thì có thể nhân vào blendFactor1/2.
                    // Hiện tại, nó áp dụng cho TỔNG CỘNG sau khi các lớp đã được trộn vào sclera.
                    currentScleraColor = lerp(_BaseColor.rgb, currentScleraColor, _BloodVesselOverallStrength); 
                    // Dòng này được điều chỉnh để đảm bảo base color gốc vẫn là điểm bắt đầu của lerp overall strength
                    // nếu bạn muốn OverallStrength điều khiển sự hiển thị của *tất cả* các mạch máu trên nền _BaseColor.
                    // Nếu bạn muốn nó điều khiển độ đậm/mờ của mạch máu *đã được trộn* vào nền sclera (có occlusion shadow),
                    // thì dòng này phải được đặt ngay trước Toon Shading. Tùy thuộc vào hiệu ứng mong muốn.
                    // Với mục đích khắc phục "loang màu đen", cách này giúp đảm bảo sự kiểm soát tốt hơn.
                #endif

                float3 finalColorRGB = currentScleraColor; // Lấy màu sclera đã có vân máu và bóng đổ

                // --- Toon Shading Calculation ---
                float3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                
                float toonFactor = smoothstep(_ToonThreshold, _ToonThreshold + _ToonSmoothness, NdotL);
                toonFactor *= mainLight.shadowAttenuation;

                float3 toonShadedColor = lerp(_ToonShadowColor.rgb, _ToonLitColor.rgb, toonFactor);
                
                finalColorRGB *= (toonShadedColor * _ToonOverallStrength); 
                finalColorRGB += _AmbientColor.rgb;

                // --- Specular Highlight ---
                float specMask = SAMPLE_TEXTURE2D(_SpecularMask, sampler_SpecularMask, input.uv).r;
                float3 halfDir = normalize(mainLight.direction + input.viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                
                float specular = pow(NdotH, _Shininess);
                if (_SpecularFresnelPower > 0.001)
                {
                    float fresnel = pow(1.0 - saturate(dot(normalWS, input.viewDirWS)), _SpecularFresnelPower);
                    specular *= fresnel;
                }
                specular *= specMask;
                
                finalColorRGB += specular * _SpecularColor.rgb * mainLight.color * _SpecularStrength; 

                return float4(finalColorRGB, 1.0); // Alpha luôn là 1.0 (hoàn toàn opaque)
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}