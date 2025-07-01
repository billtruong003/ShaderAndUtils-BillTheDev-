Shader "Custom/LightweightVolumetricNoiseFog"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.5, 0.6, 0.7, 1.0)
        _FogDensity ("Fog Density", Range(0, 2)) = 0.1
        
        [Header(Height Fog)]
        _FogHeight ("Fog Height Level", Float) = 0.0
        _FogHeightFalloff ("Height Falloff", Range(0.01, 1)) = 0.2

        [Header(Noise Settings)]
        _NoiseTex ("Noise Texture (Grayscale)", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Float) = 1.0
        _NoiseSpeedX ("Noise Speed X", Float) = 0.1
        _NoiseSpeedY ("Noise Speed Y", Float) = 0.1
        _NoiseInfluence ("Noise Influence", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Pass
        {
            // --- Cài đặt trạng thái render ---
            Blend SrcAlpha OneMinusSrcAlpha // Alpha blending tiêu chuẩn
            Cull Off                       // Không cull mặt sau (quan trọng cho các object bao quanh camera)
            ZWrite Off                     // Không ghi vào depth buffer
            ZTest LEqual                   // Render nếu ở phía trước hoặc tại cùng một điểm

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // Thư viện CG của Unity
            #include "UnityCG.cginc"

            // --- Khai báo các biến từ Properties ---
            fixed4 _FogColor;
            float _FogDensity;
            float _FogHeight;
            float _FogHeightFalloff;
            
            sampler2D _NoiseTex;
            float4 _NoiseTex_ST; // Hỗ trợ tiling và offset cho texture
            float _NoiseScale;
            float _NoiseSpeedX;
            float _NoiseSpeedY;
            float _NoiseInfluence;

            // Texture chứa độ sâu của cảnh (Unity tự cung cấp)
            sampler2D _CameraDepthTexture;

            // --- Struct cho vertex shader input và output ---
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;       // Vị trí clip space
                float2 uv : TEXCOORD0;          // Tọa độ UV cho noise
                float4 screenPos : TEXCOORD1;   // Vị trí màn hình để lấy depth
                float3 worldPos : TEXCOORD2;    // Vị trí thế giới của vertex
            };

            // --- Vertex Shader ---
            // Tính toán các giá trị cần thiết và chuyển cho fragment shader
            v2f vert (appdata v)
            {
                v2f o;
                // Chuyển vị trí vertex từ local space sang clip space
                o.pos = UnityObjectToClipPos(v.vertex);
                // Chuyển vị trí vertex từ local space sang world space
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                // Tính toán vị trí trên màn hình để lấy depth
                o.screenPos = ComputeScreenPos(o.pos);
                // Chuyển tọa độ UV cho noise texture
                o.uv = TRANSFORM_TEX(v.uv, _NoiseTex);
                return o;
            }

            // --- Fragment Shader ---
            // Tính toán màu sắc cuối cùng của từng pixel
            fixed4 frag (v2f i) : SV_Target
            {
                // 1. Lấy độ sâu của cảnh vật phía sau pixel sương mù hiện tại
                // Tọa độ màn hình chuẩn hóa (0-1)
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                // Lấy giá trị depth từ texture (giá trị này không tuyến tính)
                float sceneRawDepth = tex2D(_CameraDepthTexture, screenUV).r;
                // Tuyến tính hóa giá trị depth để có khoảng cách thực từ camera
                float sceneLinearEyeDepth = LinearEyeDepth(sceneRawDepth);

                // 2. Tính toán hệ số sương mù dựa trên khoảng cách (Distance Fog)
                // Công thức sương mù theo hàm mũ (exponential fog)
                // Khoảng cách càng xa, sương mù càng dày
                float distanceFactor = exp(-sceneLinearEyeDepth * _FogDensity);

                // 3. Tính toán hệ số sương mù dựa trên chiều cao (Height Fog)
                // Tái tạo lại vị trí thế giới của pixel trên cảnh vật
                float3 viewDir = normalize(i.worldPos - _WorldSpaceCameraPos.xyz);
                float3 sceneWorldPos = _WorldSpaceCameraPos.xyz + viewDir * sceneLinearEyeDepth;
                
                // Tính toán độ dày sương mù dựa trên chiều cao so với _FogHeight
                // Càng ở dưới _FogHeight, sương mù càng dày. Càng lên cao, sương mù càng mỏng đi.
                float heightDifference = _FogHeight - sceneWorldPos.y;
                float heightFactor = saturate(heightDifference / _FogHeightFalloff);
                
                // 4. Lấy giá trị noise
                // Làm cho UV của noise di chuyển theo thời gian để tạo hiệu ứng cuộn
                float2 noiseUV = i.uv * _NoiseScale + _Time.y * float2(_NoiseSpeedX, _NoiseSpeedY);
                float noiseValue = tex2D(_NoiseTex, noiseUV).r; // Chỉ cần kênh R vì noise là grayscale

                // 5. Kết hợp các yếu tố
                // Tổng hợp độ dày sương mù từ khoảng cách và chiều cao
                float combinedFogFactor = saturate(heightFactor * (1.0 - distanceFactor));

                // Điều chỉnh độ dày sương mù bằng noise
                // Lerp giữa giá trị không có noise (1) và có noise (noiseValue)
                float noiseModulation = lerp(1.0, noiseValue, _NoiseInfluence);
                float finalFogAlpha = combinedFogFactor * noiseModulation;

                // 6. Trả về màu sắc cuối cùng
                // Màu sương mù với độ trong suốt (alpha) đã được tính toán
                return fixed4(_FogColor.rgb, _FogColor.a * finalFogAlpha);
            }
            ENDCG
        }
    }
    FallBack "Transparent/VertexLit"
}