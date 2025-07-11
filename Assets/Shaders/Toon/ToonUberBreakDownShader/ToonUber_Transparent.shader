Shader "Custom/Toon Uber/Transparent"
{
    Properties
    {
        [HideInInspector] _SurfaceType("Surface Type", Float) = 1 // Giá trị mặc định là Transparent
        [HideInInspector] _OutlineMode("Outline Mode", Float) = 0

        // Chỉ chứa các thuộc tính liên quan đến Transparent
        [Header(Base Properties)]
        _BaseMap("Albedo (RGB) Alpha (A)", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [Header(Emission)]
        [Enum(Off, 0, On, 1)] _EmissionMode("Enable Emission", Float) = 0
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,1)
        _EmissionMap("Emission Map", 2D) = "black" {}

        [Header(Lighting)]
        [Enum(Off, 0, On, 1)] _FakeLightMode("Enable Fake Light", Float) = 1
        _FakeLightColor("Fake Light Color", Color) = (0.8, 0.8, 0.8, 1)
        _FakeLightDirection("Fake Light Direction", Vector) = (0.5, 0.5, -0.5, 0)
        
        [Header(Stylized Glass)]
        _GlassColor("Glass Color & Opacity", Color) = (0.8, 0.9, 1.0, 0.5)
        _FresnelColor("Fresnel (Edge) Color", Color) = (1,1,1,1)
        _FresnelPower("Fresnel Power", Range(1, 10)) = 5.0
        _RefractionStrength("Refraction Strength", Range(0, 0.1)) = 0.01
        _GlassSpecularPower("Specular Power", Range(1, 50)) = 20.0
        _GlassSpecularIntensity("Specular Intensity", Range(0, 5)) = 1.0

        [Header(Outline Properties (Fresnel))]
        [Enum(None, 0, Fresnel, 2)] _FresnelOutlineToggle("Enable Fresnel Outline", Float) = 0
        _FresnelOutlineColor("Color", Color) = (0, 0, 0, 1)
        _FresnelOutlineWidth("Width", Range(0.001, 1.0)) = 0.1
        _FresnelOutlinePower("Power", Range(1.0, 20.0)) = 5.0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            Cull Back
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma shader_feature_local_fragment _EMISSION_ON
            #pragma shader_feature_local_fragment _FAKELIGHT_ON
            #pragma shader_feature_local_fragment _OUTLINEMODE_FRESNEL

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            
            #include "Assets/Shaders/Toon/ToonUberBreakDownShader/Includes/ToonUberCore.hlsl"

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.color = v.color;
                o.screenPos = o.positionCS;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 viewDir = SafeNormalize(_WorldSpaceCameraPos.xyz - i.positionWS);
                Light mainLight = GetEffectiveMainLight(i.positionWS);
                
                float fresnelDot = 1.0 - saturate(dot(i.normalWS, viewDir));
                float fresnel = pow(fresnelDot, _FresnelPower);
                
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float2 distortion = i.normalWS.xy * _RefractionStrength;
                float3 sceneColor = SAMPLE_TEXTURE2D_X_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + distortion, 0).rgb;
                
                half3 surfaceColor = lerp(sceneColor, _GlassColor.rgb, _GlassColor.a);
                surfaceColor = lerp(surfaceColor, _FresnelColor.rgb, fresnel);

                float3 reflectDir = reflect(-mainLight.direction, i.normalWS);
                float spec = pow(saturate(dot(viewDir, reflectDir)), _GlassSpecularPower);
                surfaceColor += mainLight.color * spec * _GlassSpecularIntensity * mainLight.shadowAttenuation;
                
                surfaceColor = ApplyEmission(surfaceColor, i.uv);
                surfaceColor = ApplyFresnelOutline(surfaceColor, i.normalWS, viewDir);

                return half4(surfaceColor, _GlassColor.a);
            }
            ENDHLSL
        }
        // Transparent objects typically do not cast shadows in a stylized context,
        // but a ShadowCaster pass could be added here if needed.
    }
    CustomEditor "ToonUberShaderSeparateGUI"
}