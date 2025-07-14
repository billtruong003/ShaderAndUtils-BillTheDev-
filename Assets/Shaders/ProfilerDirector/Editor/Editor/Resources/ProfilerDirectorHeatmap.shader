Shader "Hidden/ProfilerDirector/HeatmapOverlay"
{
    Properties
    {
        _HeatColor("Heat Color", Color) = (1, 0, 0, 0.5)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Pass
        {
            // Pha trộn màu heatmap lên trên màu gốc
            Blend SrcAlpha OneMinusSrcAlpha
            // Không ghi vào depth buffer để không ảnh hưởng các đối tượng khác
            ZWrite Off
            // Chỉ vẽ heatmap lên trên đối tượng gốc, không vẽ lên các vật thể khác ở gần hơn
            ZTest LEqual
            // Bỏ qua các mặt sau (optional, nhưng thường là tốt)
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            fixed4 _HeatColor;

            v2f vert (appdata v)
            {
                v2f o;
                // Thêm một chút offset nhỏ để tránh z-fighting
                // Mặc dù ZTest LEqual đã giúp, đây là một biện pháp phòng ngừa tốt
                float4 pos = UnityObjectToClipPos(v.vertex);
                pos.z -= 0.00001;
                o.vertex = pos;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Chỉ trả về màu heatmap. Việc pha trộn được xử lý bởi lệnh Blend
                return _HeatColor;
            }
            ENDCG
        }
    }
    FallBack "Transparent/VertexLit"
}