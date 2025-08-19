Shader "BillTheDev/QuickOutline/Outline Mask" {
    Properties {
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 0
    }
    SubShader {
        Tags { "Queue" = "Transparent+100" }
        Pass {
            Name "Mask"
            Cull Off
            ZWrite Off
            ZTest [_ZTest]
            ColorMask 0

            Stencil {
                Ref 1
                Pass Replace
            }
        }
    }
}