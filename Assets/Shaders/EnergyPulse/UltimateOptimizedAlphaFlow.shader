Shader "Unlit/PureOpaqueFlow"
{
    Properties
    {
        [Header(Core Textures)]
        _BaseMap("Base Texture (Albedo)", 2D) = "white" {}
        _NoiseTex("Noise Texture (Grayscale)", 2D) = "white" {}

        [Header(Energy Flow Control)]
        _PulseDirection("Pulse Direction (X,Y) and Origin (Z,W)", Vector) = (1, 0, 0, 0)
        [HDR]_PulseColor("Pulse Color (HDR)", Color) = (0, 10, 10, 1)
        _PulseSpeed("Pulse Speed", Float) = 1.0
        _PulseWidth("Pulse Width", Range(0.01, 1.0)) = 0.2
        _PulseIntensity("Pulse Intensity", Range(0, 20)) = 2.0
        _EdgeSoftness("Edge Softness", Range(0.0, 1.0)) = 0.5
        _NoiseScale("Noise Scale", Float) = 5.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _BaseMap;
            sampler2D _NoiseTex;
            
            float4 _PulseDirection;
            fixed4 _PulseColor;
            float _PulseSpeed;
            float _PulseWidth;
            float _PulseIntensity;
            float _NoiseScale;
            float _EdgeSoftness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Chỉ còn 1 phép tính đọc texture cho màu cơ bản
                fixed3 baseColor = tex2D(_BaseMap, i.uv).rgb;

                // Toàn bộ logic tính toán hiệu ứng dòng chảy
                float2 pulseOrigin = _PulseDirection.zw;
                float2 pulseDirection = normalize(_PulseDirection.xy);
                
                float2 uvFromOrigin = i.uv - pulseOrigin;
                float distanceAlongAxis = dot(uvFromOrigin, pulseDirection);
                
                float animatedTime = _Time.y * _PulseSpeed;
                float repeatingPulseProgress = frac(distanceAlongAxis - animatedTime);

                float pulseShapeValue = 1.0 - smoothstep(0.0, _PulseWidth, abs(repeatingPulseProgress - 0.5));
                
                // Đọc texture nhiễu để phá vỡ sự đơn điệu
                float noiseValue = tex2D(_NoiseTex, i.uv * _NoiseScale).r;
                float finalPulseIntensity = pulseShapeValue * noiseValue;

                finalPulseIntensity = smoothstep(0.0, 1.0 - _EdgeSoftness, finalPulseIntensity);

                // Cộng (additive) màu xung vào màu cơ bản
                fixed3 pulseEmission = _PulseColor.rgb * finalPulseIntensity * _PulseIntensity;
                fixed3 finalColor = baseColor + pulseEmission;

                // Alpha luôn là 1.0 cho vật thể Opaque
                return fixed4(finalColor, 1.0);
            }
            ENDCG
        }
    }
    FallBack "VertexLit"
}