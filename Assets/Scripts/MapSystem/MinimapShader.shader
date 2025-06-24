Shader "Unlit/AdvancedMinimapShader_V4_NoBorder"
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

        [Header(Circular Mask)]
        _FadeStart ("Minimap Fade Start (0-0.5)", Range(0, 0.5)) = 0.48
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
            float2 _PlayerPosUV, _PlayerForward, _PlayerIconScreenUV;
            float _ZoomLevel, _DetailZoomThreshold, _DetailFadeRange;
            half4 _SightConeColor;
            float _SightConeAngle, _SightConeSoftness;
            float _FadeStart;

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
                fixed4 final_color = lerp(base_color, detail_color, lod_blend);

                float in_bounds = (map_uv.x > 0 && map_uv.x < 1 && map_uv.y > 0 && map_uv.y < 1);
                float dist_from_center = length(i.uv - 0.5);
                float circle_mask = 1.0 - smoothstep(_FadeStart, 0.5, dist_from_center);
                
                final_color.a *= in_bounds * circle_mask;

                #if !defined(HIDE_SIGHT_CONE)
                    float2 local_uv = i.uv - _PlayerIconScreenUV.xy;
                    float2 pixel_dir = normalize(local_uv);
                    float dot_product = dot(pixel_dir, _PlayerForward.xy);
                    float cone_angle_rad = _SightConeAngle * 0.5 * (3.14159265 / 180.0);
                    float cone_threshold = cos(cone_angle_rad);
                    float cone_strength = smoothstep(cone_threshold, cone_threshold + _SightConeSoftness, dot_product);
                    
                    final_color.rgb += _SightConeColor.rgb * cone_strength * _SightConeColor.a * circle_mask;
                #endif
                
                return final_color;
            }
            ENDCG
        }
    }
}