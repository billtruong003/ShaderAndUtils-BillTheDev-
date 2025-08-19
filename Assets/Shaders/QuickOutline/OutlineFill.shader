Shader "BillTheDev/QuickOutline/Outline Fill" {
    Properties {
        _OutlineColor("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth("Outline Width", Range(0, 10)) = 2
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 0
    }
    SubShader {
        Tags {
            "Queue" = "Transparent+110"
            "DisableBatching" = "True"
        }
        Pass {
            Name "Fill"
            Cull Off
            ZWrite Off
            ZTest [_ZTest]
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask RGB

            Stencil {
                Ref 1
                Comp NotEqual
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float3 smoothNormal : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 position : SV_POSITION;
                fixed4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            uniform fixed4 _OutlineColor;
            uniform float _OutlineWidth;

            v2f vert(appdata input) {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 normal = any(input.smoothNormal) ? input.smoothNormal : input.normal;
                float3 viewPosition = UnityObjectToViewPos(input.vertex);
                float3 viewNormal = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, normal));
                
                // Scale width by view depth and a constant factor for better consistency
                float scaledWidth = _OutlineWidth * 0.001 * -viewPosition.z;
                viewPosition += viewNormal * scaledWidth;

                output.position = UnityViewToClipPos(viewPosition);
                output.color = _OutlineColor;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target {
                return input.color;
            }
            ENDCG
        }
    }
}