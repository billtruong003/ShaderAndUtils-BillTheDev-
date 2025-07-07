Shader "URP/After Image Effect"
{
    Properties
    {
        [HDR]_Color("Extra Color", Color) = (1,1,1,1)
        [HDR]_RimColor("Rim Color", Color) = (0,1,1,1)
        _MainTex("Main Texture", 2D) = "white" {}
        _NoiseTex("Noise Texture", 2D) = "white" {} // Noise texture for dissolve
        _RimPower("Rim Power", Range(1,50)) = 20
        _Fade("Fade Amount", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha // Enable alpha blending
        ZWrite Off // Disable writing to depth buffer for transparency

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _RimColor;
                float _RimPower;
                float _Fade;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.viewDirWS = GetWorldSpaceViewDir(worldPos);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample texture and apply extra color
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 color = texColor * _Color;

                // Rim lighting calculation
                half rim = 1.0 - saturate(dot(normalize(input.normalWS), normalize(input.viewDirWS)));
                half rimEffect = pow(rim, _RimPower);
                color.rgb += _RimColor.rgb * rimEffect;

                // Dissolve effect using noise texture
                half noiseValue = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, input.uv).r;
                half dissolve = step(noiseValue, _Fade); // Clip pixels where noise < fade
                color.a *= dissolve;

                return color;
            }
            ENDHLSL
        }
    }
}