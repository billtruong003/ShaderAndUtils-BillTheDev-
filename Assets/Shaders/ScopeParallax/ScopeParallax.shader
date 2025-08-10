Shader "Unlit/ScopeParallax_Ultimate"
{
    Properties
    {
        [Header(Render State)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2 // Back
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4 // LEqual
        [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Float) = 0 // Off

        [Header(Textures)]
        [MainTexture] _ScopeTexture("Scope Render Texture", 2D) = "white" {}
        _CrosshairTex("Crosshair Texture (RGBA)", 2D) = "white" {}

        [Header(Optical Effects)]
        _VignetteColor("Vignette Color", Color) = (0,0,0,1)
        _VignetteIntensity("Vignette Intensity", Range(1, 50)) = 2.0
        _BarrelDistortion("Barrel Distortion", Range(-0.5, 0.5)) = 0.05
        _ChromaticAberration("Chromatic Aberration", Range(0, 0.05)) = 0.01

        [Header(Crosshair)]
        _CrosshairColor("Crosshair Color", Color) = (1,1,1,1)
        _CrosshairScale("Crosshair Scale", Range(0, 5.0)) = 1.0
        
        [Header(Film Effects)]
        _NoiseIntensity("Noise Intensity", Range(0, 0.2)) = 0.05
    }
    SubShader
    {
        Tags { "Queue"="Transparent-1" "RenderType"="Transparent" }
        LOD 300

        Pass
        {
            Cull [_Cull]
            ZTest [_ZTest]
            ZWrite [_ZWrite]
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4x4 _ScopeCameraVP;
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            TEXTURE2D(_ScopeTexture); SAMPLER(sampler_ScopeTexture);
            TEXTURE2D(_CrosshairTex); SAMPLER(sampler_CrosshairTex);
            
            half4 _VignetteColor;
            float _VignetteIntensity;
            float _BarrelDistortion;
            float _ChromaticAberration;
            half4 _CrosshairColor;
            float _CrosshairScale;
            float _NoiseIntensity;

            Varyings vert(Attributes i) { Varyings o; o.positionWS = TransformObjectToWorld(i.positionOS.xyz); o.positionCS = TransformWorldToHClip(o.positionWS); return o; }
            float2 applyBarrelDistortion(float2 uv, float s) { float2 c = (uv - 0.5) * 2.0; float r2 = dot(c,c); float2 d = c * (1.0 + r2 * s); return (d / 2.0) + 0.5; }
            float random(float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453); }

            half4 frag(Varyings i) : SV_Target
            {
                float4 scopeClipPos = mul(_ScopeCameraVP, float4(i.positionWS, 1.0));
                float2 scopeUV = (scopeClipPos.xy / scopeClipPos.w) * 0.5 + 0.5;
                scopeUV.x = 1.0 - scopeUV.x;

                float2 centeredUV = scopeUV * 2.0 - 1.0;
                float circularMask = length(centeredUV);
                if (circularMask > 1.0) discard;
                
                float2 distortedScopeUV = applyBarrelDistortion(scopeUV, _BarrelDistortion);
                
                float2 ca_offset = centeredUV * _ChromaticAberration;
                half r = SAMPLE_TEXTURE2D(_ScopeTexture, sampler_ScopeTexture, distortedScopeUV + ca_offset).r;
                half g = SAMPLE_TEXTURE2D(_ScopeTexture, sampler_ScopeTexture, distortedScopeUV).g;
                half b = SAMPLE_TEXTURE2D(_ScopeTexture, sampler_ScopeTexture, distortedScopeUV - ca_offset).b;
                half4 scopeColor = half4(r, g, b, 1.0);

                float vignetteFactor = pow(circularMask, 2) * _VignetteIntensity;
                scopeColor.rgb = lerp(scopeColor.rgb, _VignetteColor.rgb, saturate(vignetteFactor));

                float2 crosshairUV = (scopeUV - 0.5) / _CrosshairScale + 0.5;
                half4 crosshairTex = SAMPLE_TEXTURE2D(_CrosshairTex, sampler_CrosshairTex, crosshairUV);
                half4 finalCrosshairColor = half4(_CrosshairColor.rgb, _CrosshairColor.a * crosshairTex.a);

                if (all(saturate(crosshairUV) == crosshairUV))
                    scopeColor.rgb = lerp(scopeColor.rgb, finalCrosshairColor.rgb, finalCrosshairColor.a);
                
                float noise = (random(i.positionCS.xy * _Time.y) - 0.5) * _NoiseIntensity;
                scopeColor.rgb += noise;
                
                return half4(saturate(scopeColor.rgb), 1.0);
            }
            ENDHLSL
        }
    }
}