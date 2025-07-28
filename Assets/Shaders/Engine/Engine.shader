Shader "Custom/EngineStepped"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.8, 0.8, 0.8, 1.0)
        _Speed("Engine Speed", Range(0, 50)) = 10.0

        [Header(Stepped Movement)]
        _StepOrigin("Step Origin Point", Vector) = (0,0,0,0)
        _StepDirection("Step Direction", Vector) = (0,0,1,0)
        _StepSize("Step Size (Distance)", Float) = 0.5
        _StepDelay("Step Delay (Time)", Float) = 0.2

        [Header(Parts Movement)]
        _PistonDisplacement("1. Piston Displacement", Range(0, 1)) = 0.2
        _PistonAxis("1. Piston Movement Axis", Vector) = (0, 1, 0, 0)
        _RotationPivot("2. Rotation Pivot", Vector) = (0, 0, 0, 0)
        _RotationAxis("2. Rotation Axis", Vector) = (0, 0, 1, 0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalRenderPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // =============================================================
            // SỬA LỖI TẠI ĐÂY
            // =============================================================
            struct Attributes
            {
                float4 positionOS   : POSITION; // Đã sửa lại từ 'p' thành 'positionOS'
                float4 color        : COLOR;    // Đã sửa lại từ 'c' thành 'color'
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor, _StepOrigin, _StepDirection, _PistonAxis, _RotationPivot, _RotationAxis;
                float _Speed, _StepSize, _StepDelay, _PistonDisplacement;
            CBUFFER_END

            float3x3 rotationMatrix(float3 a, float ang){a=normalize(a);float s=sin(ang),c=cos(ang),oc=1.0-c;return float3x3(oc*a.x*a.x+c,oc*a.x*a.y-a.z*s,oc*a.z*a.x+a.y*s,oc*a.x*a.y+a.z*s,oc*a.y*a.y+c,oc*a.y*a.z-a.x*s,oc*a.z*a.x-a.y*s,oc*a.y*a.z+a.x*s,oc*a.z*a.z+c);}

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 position = IN.positionOS.xyz;

                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                float3 dir = normalize(_StepDirection.xyz);
                float distance = dot(worldPos - _StepOrigin.xyz, dir);
                float stepIndex = floor(distance / _StepSize);
                float delayedTime = (_Time.y * _Speed) - (stepIndex * _StepDelay);

                if (IN.color.r > 0.5)
                {
                    float3 pistonDirection = normalize(_PistonAxis.xyz);
                    float pistonOffset = sin(delayedTime) * _PistonDisplacement;
                    position += pistonDirection * pistonOffset;
                }
                else if (IN.color.g > 0.5)
                {
                    float3 pivot = _RotationPivot.xyz;
                    float3 axis = _RotationAxis.xyz;
                    float3 relativePos = position - pivot;
                    float3x3 rotMatrix = rotationMatrix(axis, delayedTime);
                    relativePos = mul(rotMatrix, relativePos);
                    position = relativePos + pivot;
                }
                
                OUT.positionHCS = TransformObjectToHClip(position);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target { return _BaseColor; }
            ENDHLSL
        }
    }
}   