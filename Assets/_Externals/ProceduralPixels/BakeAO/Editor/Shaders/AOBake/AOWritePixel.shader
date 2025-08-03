Shader "Hidden/BakeAO/WritePixel"
{
    Properties {}

    SubShader
    {
        Tags {}

        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off
            Blend One One

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            float2 _PixelCoord;
            float _TextureSize;
            float _GammaFactor = 1.0f;
            sampler2D _TracerTarget;

            struct vertexData
            {
                float4 positionOS : POSITION;
            };

            struct fragmentData
            {
                float4 clipPos : SV_POSITION;
            };

            fragmentData vert(vertexData input)
            {
                fragmentData output;
                output.clipPos = float4((input.positionOS.xy * 0.5 + _PixelCoord) * (2.0 / _TextureSize) - 1.0, 0.0, 1.0);
                return output;
            }

            #define TRACER_RESOLUTION 33.0

            #define HALF_PI 1.5707963

            #define TRACER_FOV_DEG 170.0
            #define DEG_TO_RAD 0.0174532925

            float4 frag(fragmentData input) : SV_Target
            { 
                float sum = 0.0;
                float maxValue = 0.0;
                for (float y = 0; y < TRACER_RESOLUTION; y += 1.0)
                {
                    for (float x = 0; x < TRACER_RESOLUTION; x += 1.0)
                    {
                        float2 uv = float2(x, y) / TRACER_RESOLUTION;
                        float l = length(uv - 0.5) * 2.0;
                        if (l <= 1.0)
                        {
                            float tracerFovRad = TRACER_FOV_DEG * DEG_TO_RAD;
                            float newL = tan(tracerFovRad * l / 2.0) * tan(HALF_PI - (tracerFovRad / 2.0));
                            float strength = pow(abs(1.0 - newL), 3.5) * 0.01;
                            uv = (normalize(uv - 0.5) * newL) * 0.5 + 0.5;
                            sum += tex2Dlod(_TracerTarget, float4(uv, 0.0, 0.0)).r * strength;
                            maxValue += strength;
                        }
                    }
                }
                
                float value = sum / maxValue;
                value = pow(max(0.0, value), _GammaFactor);
                
                return float4(value, value, value, 1.0);
            }

            ENDHLSL
        }
    }
}
