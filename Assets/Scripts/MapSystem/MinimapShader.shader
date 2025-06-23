Shader "Unlit/AdvancedMinimapShader_V4_MovableSightCone"
{
    Properties
    {
        [Header(Map Textures)]
        _MainTex ("Base Map Texture (RGBA)", 2D) = "white" {}
        _DetailTex ("Detail Map Texture (RGBA)", 2D) = "black" {}

        [Header(Map Control)]
        _PlayerPosUV ("Player Position (UV)", Vector) = (0.5, 0.5, 0, 0)
        _PlayerForward ("Player Forward Vector (XZ)", Vector) = (0, 1, 0, 0)
        _ZoomLevel ("Zoom Level", Range(0.01, 1)) = 0.1
        _PlayerIconScreenUV ("Player Icon On-Screen UV", Vector) = (0.5, 0.5, 0, 0)

        [Header(Level of Detail)]
        _DetailZoomThreshold ("Detail Map Zoom Threshold", Range(0, 1)) = 0.2
        _DetailFadeRange ("Detail Map Fade Range", Range(0, 0.1)) = 0.05

        [Header(Player Sight Cone)]
        [HDR] _SightConeColor ("Sight Cone Color", Color) = (1, 1, 0, 0.25)
        _SightConeAngle ("Sight Cone Angle (Degrees)", Range(10, 180)) = 90
        _SightConeSoftness ("Sight Cone Edge Softness", Range(0.01, 1.0)) = 0.1
        [Toggle(HIDE_SIGHT_CONE)] _HideSightCone("Hide Sight Cone", Float) = 0

        [Header(Edge and Border Effects)]
        [HDR] _WalkableEdgeColor ("Walkable Area Edge Color", Color) = (0, 1, 1, 1)
        _WalkableEdgeThickness ("Walkable Edge Thickness", Range(0, 0.01)) = 0.002
        [HDR] _BorderColor ("Minimap Border Color", Color) = (1, 1, 1, 1)
        _BorderSize ("Minimap Border Size", Range(0, 0.1)) = 0.02
        _BorderFadeStart ("Minimap Fade Start (0-0.5)", Range(0, 0.5)) = 0.48
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature HIDE_SIGHT_CONE
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            sampler2D _MainTex, _DetailTex;
            float4 _MainTex_TexelSize;
            float2 _PlayerPosUV, _PlayerForward, _PlayerIconScreenUV;
            float _ZoomLevel, _DetailZoomThreshold, _DetailFadeRange;
            half4 _WalkableEdgeColor, _BorderColor, _SightConeColor;
            float _SightConeAngle, _SightConeSoftness;
            float _WalkableEdgeThickness, _BorderSize, _BorderFadeStart;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 offset = (i.uv - 0.5) * _ZoomLevel;
                float2 map_uv = _PlayerPosUV + offset;

                fixed4 base_color = tex2D(_MainTex, map_uv);
                fixed4 detail_color = tex2D(_DetailTex, map_uv);
                float lod_blend = smoothstep(_DetailZoomThreshold + _DetailFadeRange, _DetailZoomThreshold - _DetailFadeRange, _ZoomLevel);
                fixed4 blended_map_color = lerp(base_color, detail_color, lod_blend);

                float2 texelSize = _MainTex_TexelSize.xy * _WalkableEdgeThickness * 100;
                float alpha_center = blended_map_color.a;
                float alpha_up = tex2D(_MainTex, map_uv + float2(0, texelSize.y)).a;
                float alpha_down = tex2D(_MainTex, map_uv - float2(0, texelSize.y)).a;
                float edge_factor = saturate(abs(alpha_up - alpha_down) + abs(tex2D(_MainTex, map_uv + float2(texelSize.x, 0)).a - tex2D(_MainTex, map_uv - float2(texelSize.x, 0)).a));
                edge_factor = step(0.1, alpha_center) * edge_factor;
                fixed4 final_map_color = lerp(blended_map_color, _WalkableEdgeColor, edge_factor);

                #if !defined(HIDE_SIGHT_CONE)
                    float2 local_uv = i.uv - _PlayerIconScreenUV.xy;
                    float2 pixel_dir = normalize(local_uv);
                    float dot_product = dot(pixel_dir, _PlayerForward.xy);
                    float cone_angle_rad = _SightConeAngle * 0.5 * (3.14159265 / 180.0);
                    float cone_threshold = cos(cone_angle_rad);
                    float cone_strength = smoothstep(cone_threshold, cone_threshold + _SightConeSoftness, dot_product);
                    final_map_color.rgb += _SightConeColor.rgb * cone_strength * _SightConeColor.a;
                #endif

                float dist_from_center = length(i.uv - 0.5);
                float border_factor = smoothstep(_BorderFadeStart - _BorderSize, _BorderFadeStart, dist_from_center);
                border_factor -= smoothstep(_BorderFadeStart, _BorderFadeStart + 0.005, dist_from_center);
                final_map_color = lerp(final_map_color, _BorderColor, border_factor);
                final_map_color.a *= 1.0 - smoothstep(_BorderFadeStart, 0.5, dist_from_center);

                return final_map_color;
            }
            ENDCG
        }
    }
}