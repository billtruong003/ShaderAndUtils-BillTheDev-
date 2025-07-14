Shader "TextMeshPro/Optimized/Mobile SDF (Opaque Cutout)" {

    Properties {
        _FaceColor          ("Face Color", Color) = (1,1,1,1)
        _FaceDilate         ("Face Dilate", Range(-1,1)) = 0
    
        _WeightNormal       ("Weight Normal", float) = 0
        _WeightBold         ("Weight Bold", float) = .5
    
        _MainTex            ("Font Atlas", 2D) = "white" {}
        _GradientScale      ("Gradient Scale", float) = 5
        _Sharpness          ("Sharpness", Range(-1,1)) = 0
        
        _Cutoff             ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
    
        _VertexOffsetX      ("Vertex OffsetX", float) = 0
        _VertexOffsetY      ("Vertex OffsetY", float) = 0
        
        _CullMode           ("Cull Mode", Float) = 0
    }
    
    SubShader {
        Tags { "Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout" }
    
        Cull [_CullMode]
        ZWrite On
        Lighting Off
        Fog { Mode Off }
        ZTest LEqual
        Blend Off 
    
        Pass {
            CGPROGRAM
            #pragma vertex VertShader
            #pragma fragment PixShader
            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            
            uniform sampler2D _MainTex;
            uniform fixed4 _FaceColor;
            uniform half _FaceDilate;
            uniform float _WeightNormal;
            uniform float _WeightBold;
            uniform float _GradientScale;
            uniform half _Sharpness;
            uniform half _Cutoff;
            uniform float _VertexOffsetX;
            uniform float _VertexOffsetY;
            uniform float4 _ClipRect;
            
            struct vertex_t {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4  vertex          : POSITION;
                float3  normal          : NORMAL;
                fixed4  color           : COLOR;
                float4  texcoord0       : TEXCOORD0;
            };
    
            struct pixel_t {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
                float4  vertex          : SV_POSITION;
                fixed4  faceColor       : COLOR;
                float4  texcoord0       : TEXCOORD0;
                half2   param           : TEXCOORD1;
                #if UNITY_UI_CLIP_RECT
                float2  mask            : TEXCOORD2;
                #endif
            };
    
            pixel_t VertShader(vertex_t input)
            {
                pixel_t output;
                UNITY_INITIALIZE_OUTPUT(pixel_t, output);
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    
                float bold = step(input.texcoord0.w, 0);
    
                float4 vert = input.vertex;
                vert.x += _VertexOffsetX;
                vert.y += _VertexOffsetY;
                float4 vPosition = UnityObjectToClipPos(vert);
    
                float scale = abs(input.texcoord0.w) * _GradientScale * (_Sharpness + 1);
    
                float weight = lerp(_WeightNormal, _WeightBold, bold) / 4.0;
                weight += _FaceDilate * 0.5;
    
                float bias = (0.5 - weight) * scale - 0.5;
    
                output.vertex = vPosition;
                output.faceColor = input.color * _FaceColor;
                output.texcoord0 = input.texcoord0;
                output.param = half2(scale, bias);
                
                #if UNITY_UI_CLIP_RECT
                output.mask = (vert.xy - _ClipRect.xy) / (_ClipRect.zw - _ClipRect.xy);
                #endif
    
                return output;
            }
    
            fixed4 PixShader(pixel_t input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
    
                half signedDistance = tex2D(_MainTex, input.texcoord0.xy).a;
                half fontAlpha = saturate(signedDistance * input.param.x - input.param.y);
                
                clip(fontAlpha - _Cutoff);
    
                fixed4 finalColor = input.faceColor;
                
                #if UNITY_UI_CLIP_RECT
                half2 m = step(abs(input.mask - 0.5), 0.5);
                finalColor.a *= m.x * m.y;
                #endif
    
                return finalColor;
            }
            ENDCG
        }
    }
}