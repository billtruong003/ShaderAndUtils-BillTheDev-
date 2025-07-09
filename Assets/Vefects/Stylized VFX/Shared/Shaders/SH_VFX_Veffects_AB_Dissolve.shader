// Made with Amplify Shader Editor - Converted for URP - Fixed by AI
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Vefects/URP/SH_VFX_Vefects_Distortion_01_URP"
{
    Properties
    {
        [Space(33)][Header(Cutout)][Space(13)]
        _CutoutTexture("Cutout Texture", 2D) = "white" {}
        _CutoutMaskSelector("Cutout Mask Selector", Vector) = (0,1,0,0)
        
        [Space(33)][Header(Distortion Noise)][Space(13)]
        _DistortionNoise("Distortion Noise", 2D) = "white" {}
        _DistortionNoiseSelector("Distortion Noise Selector", Vector) = (0,1,0,0)
        _DistUVS("Dist UV S", Vector) = (1,1,0,0)
        _DistUVP("Dist UV P", Vector) = (0,0,0,0)
        _DistortionLerp("Distortion Lerp", Range(0, 5)) = 1

        [Space(33)][Header(Distortion Dist)][Space(13)]
        _DistortionDist("Distortion Dist", 2D) = "white" {}
        _DistortionDistSelector("Distortion Dist Selector", Vector) = (0,1,0,0)
        _DistDistUVS("Dist Dist UV S", Vector) = (1,1,0,0)
        _DistDistUVP("Dist Dist UV P", Vector) = (0,0,0,0)
        _DistortionDistLerp("Distortion Dist Lerp", Range(0, 1)) = 0.1

        [Space(33)][Header(Render Settings)][Space(13)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 0 // Off
        [Enum(UnityEngine.Rendering.BlendMode)] _Src("Src", Float) = 5 // SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _Dst("Dst", Float) = 10 // OneMinusSrcAlpha
        [Enum(Off, 0, On, 1)] _ZWrite("ZWrite", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4 // LEqual
    }

    SubShader
    {
        Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "RenderPipeline" = "UniversalPipeline" "IsEmissive" = "true" }
        
        Pass
        {
            // Render states
            Cull [_Cull]
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Blend [_Src] [_Dst]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Structs
            struct Attributes
            {
                float4 positionOS   : POSITION;
                // THAY ĐỔI: Đổi từ float2 thành float4 để có thể truy cập thành phần .z
                float4 uv           : TEXCOORD0; 
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionCS     : SV_POSITION;
                float4 screenPos      : TEXCOORD0;
                float2 uv             : TEXCOORD1;
                float4 color          : COLOR;
                float  distortionMask : TEXCOORD2;
            };

            // URP Camera Opaque Texture
            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            // Properties
            TEXTURE2D(_CutoutTexture); SAMPLER(sampler_CutoutTexture);
            float4 _CutoutTexture_ST;
            float4 _CutoutMaskSelector;
            
            TEXTURE2D(_DistortionNoise); SAMPLER(sampler_DistortionNoise);
            float2 _DistUVS;
            float2 _DistUVP;
            float4 _DistortionNoiseSelector;
            float  _DistortionLerp;

            TEXTURE2D(_DistortionDist); SAMPLER(sampler_DistortionDist);
            float2 _DistDistUVS;
            float2 _DistDistUVP;
            float4 _DistortionDistSelector;
            float  _DistortionDistLerp;

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.screenPos = ComputeScreenPos(o.positionCS);
                // SỬA LẠI: Lấy xy từ i.uv (giờ là float4) để gán cho o.uv (vẫn là float2)
                o.uv = i.uv.xy; 
                o.color = i.color;
                // SỬA LẠI: Dòng này giờ đã hợp lệ vì i.uv là float4
                o.distortionMask = i.uv.z; 

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // Tính toán UV cho các texture noise
                float2 distUVPanner = ( _Time.y * _DistUVP + ( i.uv * _DistUVS ));
                float2 distDistUVPanner = ( _Time.y * _DistDistUVP + ( i.uv * _DistDistUVS ));

                // Lấy giá trị từ texture "distortion of distortion"
                float4 distDistSample = SAMPLE_TEXTURE2D(_DistortionDist, sampler_DistortionDist, distDistUVPanner);
                float distDistValue = dot(distDistSample, _DistortionDistSelector);
                float2 distDistOffset = lerp(float2(0,0), saturate(distDistValue).xx, _DistortionDistLerp);

                // Lấy giá trị từ texture noise chính, có offset từ texture trên
                float4 noiseSample = SAMPLE_TEXTURE2D(_DistortionNoise, sampler_DistortionNoise, distUVPanner + distDistOffset);
                float noiseValue = dot(noiseSample, _DistortionNoiseSelector);

                // Lấy giá trị từ texture cutout
                float2 cutoutUV = i.uv * _CutoutTexture_ST.xy + _CutoutTexture_ST.zw;
                float4 cutoutSample = SAMPLE_TEXTURE2D(_CutoutTexture, sampler_CutoutTexture, cutoutUV);
                float cutoutValue = dot(cutoutSample, _CutoutMaskSelector);

                // Kết hợp noise và cutout, sau đó lerp với hệ số distortion
                float combinedMask = saturate(saturate(noiseValue) * saturate(cutoutValue));
                float2 distortionOffset = lerp(float2(0,0), combinedMask.xx, _DistortionLerp * i.distortionMask);

                // Tính toán UV màn hình và áp dụng offset
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                
                // Lấy màu từ Opaque Texture tại vị trí đã bị làm méo
                half4 screenColor = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + distortionOffset);

                // Màu cuối cùng
                half4 finalColor;
                finalColor.rgb = screenColor.rgb;
                finalColor.a = i.color.a; // Alpha từ vertex color
                
                return finalColor;
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Unlit"
}