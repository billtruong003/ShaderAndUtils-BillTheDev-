Shader "Custom/CircleShaderURP"
{
    Properties
    {
        [HDR] _InnerColor ("Inner Color (HDR)", Color) = (1, 1, 1, 1) // Màu bên trong
        [HDR] _OuterColor ("Outer Color (HDR)", Color) = (1, 1, 1, 1) // Màu viền
        _Thickness ("Thickness", Range(0.0, 0.5)) = 0.1 // Độ dày viền
        _Emission ("Emission Intensity", Range(0.0, 5.0)) = 1.0 // Cường độ phát sáng
        [Enum(Outline Only, 0, Outline and Fill, 1, Full, 2)] _FillMode ("Fill Mode", Float) = 1 // Chế độ vẽ
        _NoiseTex ("Noise Texture", 2D) = "white" {} // Texture noise
        _NoiseScale ("Noise Scale", Range(0.0, 10.0)) = 1.0 // Tỷ lệ noise
        [Toggle] _FlickerEnabled ("Enable Flicker", Float) = 0 // Bật tắt hiệu ứng flicker
        _FlickerSpeed ("Flicker Speed", Range(0.0, 10.0)) = 1.0 // Tốc độ flicker
        _FlickerIntensity ("Flicker Intensity", Range(0.0, 1)) = 0.05 // Cường độ flicker tại viền
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    CBUFFER_START(UnityPerMaterial)
        float4 _InnerColor;
        float4 _OuterColor;
        float _Thickness;
        float _Emission;
        float _FillMode;
        float _NoiseScale;
        float _FlickerEnabled;
        float _FlickerSpeed;
        float _FlickerIntensity;
    CBUFFER_END

    TEXTURE2D(_NoiseTex);
    SAMPLER(sampler_NoiseTex);

    struct Attributes
    {
        float4 positionOS : POSITION;
        float2 uv : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float2 uv : TEXCOORD0;
        float4 positionHCS : SV_POSITION;
        UNITY_VERTEX_OUTPUT_STEREO
    };
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Trung tâm UV (0,0) ở giữa hình tròn
                float2 centeredUV = IN.uv - 0.5;
                float dist = length(centeredUV); // Khoảng cách từ tâm

                // Tính toán bán kính hình tròn
                float outerRadius = 0.5; // Hình tròn vừa với UV (0-1)
                float innerRadius = outerRadius - _Thickness;
                float feather = 0.01;

                // Áp dụng noise cho flicker tại viền
                float flicker = 0.0;
                if (_FlickerEnabled)
                {
                    float2 flickerUV = IN.uv * _NoiseScale + float2(_Time.y * _FlickerSpeed, 0);
                    flicker = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, flickerUV).r;
                    flicker = (flicker - 0.5) * _FlickerIntensity; // Điều chỉnh flicker
                    outerRadius += flicker; // Làm viền dao động
                    innerRadius += flicker; // Giữ độ dày viền ổn định
                }

                // Tạo masks
                float fillMask = 1.0 - smoothstep(innerRadius - feather, innerRadius + feather, dist);
                float outlineMask = smoothstep(innerRadius - feather, innerRadius + feather, dist) 
                                 - smoothstep(outerRadius - feather, outerRadius + feather, dist);
                outlineMask = saturate(outlineMask);
                float circleMask = 1.0 - smoothstep(outerRadius - feather, outerRadius + feather, dist);

                half4 color = 0;

                if (_FillMode == 0) // Chỉ vẽ viền
                {
                    color = _OuterColor * outlineMask;
                    color.a = outlineMask * _OuterColor.a;
                }
                else if (_FillMode == 1) // Vẽ viền + bên trong
                {
                    color.rgb = _InnerColor.rgb * fillMask + _OuterColor.rgb * outlineMask;
                    color.a = max(fillMask * _InnerColor.a, outlineMask * _OuterColor.a);
                }
                else if (_FillMode == 2) // Vẽ full
                {
                    color = _InnerColor * circleMask;
                    color.a = circleMask * _InnerColor.a;
                }

                // Áp dụng emission
                color.rgb *= _Emission;

                // Clip ngoài hình tròn
                clip(circleMask - 0.01);

                return color;
            }
            ENDHLSL
        }
    }
    Fallback Off
}