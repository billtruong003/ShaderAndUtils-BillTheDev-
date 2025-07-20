Shader "Toon/ToonAura"
{
    Properties
    {
        [Header(Workflow Selection)]
        [Toggle(_AURA_ON)] _AuraToggle("Enable Aura Effect", Float) = 1
        [Enum(Toon,0,Metallic,1,Foliage,2)] _SurfaceType("Surface Type", Float) = 0

        [Header(Aura Effect)]
        _AuraInnerColor("Aura Inner Color", Color) = (0,0,1,1)
        _AuraRimColor("Aura Rim Color", Color) = (0,1,1,1)
        _AuraWidth("Aura Width", Range(0.002, 0.8)) = .3
        _AuraZOffset("Aura Z Offset", Range(-.06, 0)) = -.05
        _AuraNoiseTex("Aura Noise Texture", 2D) = "white" {}
        _AuraNoiseScale("Aura Noise Scale (X, Y)", Vector) = (1, 1, 0, 0)
        _AuraSpeedX("Aura Speed X", Range(-10, 10)) = 1.0
        _AuraSpeedY("Aura Speed Y", Range(-10, 10)) = 1.0
        _AuraNoiseOpacity("Aura Noise Opacity", Range(0.01, 10.0)) = 10
        _AuraBrightness("Aura Brightness", Range(0.5, 3)) = 2
        _AuraRimEdge("Aura Rim Edge", Range(0.0, 1)) = 0.1
        _AuraRimPower("Aura Rim Power", Range(0.01, 10.0)) = 1
        
        [Header(Distance Fade)]
        _AuraFadeStart("Fade Start Distance", Float) = 20
        _AuraFadeEnd("Fade End Distance", Float) = 30

        [Header(Base Properties)]
        _BaseMap("Albedo (RGB) Alpha (A)", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [Header(Alpha Clipping)]
        [Toggle(_ALPHACLIP_ON)] _AlphaClipMode("Enable Alpha Clip", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        
        [Header(Emission)]
        [Toggle(_EMISSION_ON)] _EmissionMode("Enable Emission", Float) = 0
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,1)
        _EmissionMap("Emission Map", 2D) = "black" {}

        [Header(Lighting)]
        [Toggle(_FAKELIGHT_ON)] _FakeLightMode("Enable Fake Light", Float) = 1
        _FakeLightColor("Fake Light Color", Color) = (0.8, 0.8, 0.8, 1)
        _FakeLightDirection("Fake Light Direction", Vector) = (0.5, 0.5, -0.5, 0)

        [Header(Toon Shading)]
        _ToonRampOffset("Ramp Offset", Range(0.0, 1.0)) = 0.5
        _ToonRampSmoothness("Ramp Smoothness", Range(0.001, 1.0)) = 0.05
        _ShadowTint("Shadow Tint", Color) = (0.1, 0.1, 0.2, 1.0)
        _AmbientColor("Ambient Color", Color) = (0.5, 0.5, 0.5, 0)

        [Header(Stylized Metal)]
        _Ramp("Toon Ramp (RGB)", 2D) = "white" {} 
        _Brightness("Specular Brightness", Range(0, 2)) = 1.3  
        _Offset("Specular Size", Range(0, 1)) = 0.8
        [HDR] _SpecuColor("Specular Color", Color) = (0.8,0.45,0.2,1)
        _HighlightOffset("Highlight Size", Range(0, 1)) = 0.9  
        [HDR] _HiColor("Highlight Color", Color) = (1,1,1,1)
        _RimColor("Rim Color", Color) = (1,0.3,0.3,1)
        _RimPower("Rim Power", Range(0, 20)) = 6
        
        [Header(Foliage)]
        _WindFrequency("Wind Frequency", Range(0.1, 10)) = 2.0
        _WindAmplitude("Wind Amplitude", Range(0, 1)) = 0.1
        _WindDirection("Wind Direction", Vector) = (1, 0, 0.5, 0)
        [HDR] _TranslucencyColor("Translucency Color", Color) = (0.7, 0.9, 0.3, 1)
        _TranslucencyStrength("Translucency Strength", Range(0, 5)) = 1.0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        
        struct Attributes
        {
            float4 positionOS   : POSITION;
            float3 normalOS     : NORMAL;
            float2 uv           : TEXCOORD0;
            float4 color        : COLOR;
        };

        struct Varyings
        {
            float4 positionCS   : SV_POSITION;
            float3 positionWS   : TEXCOORD0;
            float3 normalWS     : TEXCOORD1;
            float2 uv           : TEXCOORD2;
            float4 color        : COLOR;
        };

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float  _Cutoff;
            float4 _EmissionColor;
            float4 _FakeLightColor;
            float3 _FakeLightDirection;
            float  _ToonRampOffset;
            float  _ToonRampSmoothness;
            float4 _ShadowTint;
            float4 _AmbientColor;
            float  _Brightness;
            float  _Offset;
            float  _HighlightOffset;
            float  _RimPower;
            float4 _SpecuColor;
            float4 _HiColor;
            float4 _RimColor;
            float  _WindFrequency;
            float  _WindAmplitude;
            float3 _WindDirection;
            float3 _TranslucencyColor;
            float  _TranslucencyStrength;
            float4 _AuraInnerColor, _AuraRimColor;
            float _AuraWidth, _AuraZOffset, _AuraRimPower;
            float4 _AuraNoiseScale;
            float _AuraNoiseOpacity, _AuraRimEdge;
            float _AuraBrightness, _AuraSpeedX, _AuraSpeedY;
            float _AuraFadeStart, _AuraFadeEnd;
        CBUFFER_END

        TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_EmissionMap);    SAMPLER(sampler_EmissionMap);
        TEXTURE2D(_Ramp);           SAMPLER(sampler_Ramp);
        TEXTURE2D(_AuraNoiseTex);   SAMPLER(sampler_AuraNoiseTex);

        struct Varyings_Aura
        {
            float4 positionCS : SV_POSITION;
            float3 viewDir    : TEXCOORD1;
            float3 normalDir  : TEXCOORD2;
            float3 positionWS : TEXCOORD3;
        };
        
        Varyings_Aura AuraVert(Attributes v)
        {
            Varyings_Aura o;
            float3 worldPos = TransformObjectToWorld(v.positionOS.xyz);
            o.positionWS = worldPos;
            o.positionCS = TransformWorldToHClip(worldPos);
            
            float3 scale = float3(
                length(unity_ObjectToWorld._m00_m10_m20),
                length(unity_ObjectToWorld._m01_m11_m21),
                length(unity_ObjectToWorld._m02_m12_m22)
            );

            o.normalDir = normalize(mul(float4(v.normalOS, 1.0), unity_WorldToObject).xyz);
            o.viewDir = GetWorldSpaceViewDir(worldPos);
            
            float3 norm = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normalOS)) * scale;
            float2 offset = mul((float2x2)UNITY_MATRIX_P, norm.xy);
            
            o.positionCS.xy += offset * _AuraWidth;
            o.positionCS.z *= 0.01;
            o.positionCS.z -= _AuraZOffset;
            
            return o;
        }

        half4 AuraFrag(Varyings_Aura i) : SV_Target
        {
            float2 noiseUV = i.positionWS.xz * _AuraNoiseScale.xy;
            noiseUV.x += _Time.y * _AuraSpeedX;
            noiseUV.y += _Time.y * _AuraSpeedY;

            float noise = SAMPLE_TEXTURE2D(_AuraNoiseTex, sampler_AuraNoiseTex, noiseUV).r;
            float rim = pow(saturate(dot(i.viewDir, i.normalDir)), _AuraRimPower).r;
            rim -= noise;
            
            float4 texturedAura = saturate(rim * _AuraNoiseOpacity);
            float4 extraRim = saturate((_AuraRimEdge + rim) * _AuraNoiseOpacity) - texturedAura;
            
            float4 result = (_AuraInnerColor * texturedAura) + (_AuraRimColor * extraRim);
            
            float camDist = distance(_WorldSpaceCameraPos.xyz, i.positionWS);
            float distFade = 1.0 - saturate((camDist - _AuraFadeStart) / (_AuraFadeEnd - _AuraFadeStart + 1e-5));
            
            result *= distFade;

            return saturate(result) * _AuraBrightness;
        }
        
        void ApplyAlphaClipFromUV(float2 uv)
        {
            #if defined(_ALPHACLIP_ON)
                half albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).a * _BaseColor.a;
                clip(albedoAlpha - _Cutoff);
            #endif
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" "RenderType"="Opaque" "Queue"="Geometry" }
            Cull Back
            ZWrite On
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            #pragma multi_compile_local _SURFACETYPE_TOON _SURFACETYPE_METALLIC _SURFACETYPE_FOLIAGE
            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local_fragment _EMISSION_ON
            #pragma shader_feature_local_fragment _FAKELIGHT_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Assets/Shaders/Toon/ToonUberBreakDownShader/Includes/ToonAura_Core.hlsl"
            #include "Assets/Shaders/Toon/ToonUberBreakDownShader/Includes/ToonAura_Functions.hlsl"

            Varyings Vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                float3 positionOS = v.positionOS.xyz;
                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(positionOS, v.color);
                #endif
                
                o.positionWS = TransformObjectToWorld(positionOS);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.color = v.color;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                ApplyAlphaClipFromUV(i.uv);
                
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                float3 viewDir = SafeNormalize(_WorldSpaceCameraPos.xyz - i.positionWS);
                Light mainLight = GetEffectiveMainLight(i.positionWS);
                
                half3 sceneAmbient = SampleSH(i.normalWS);
                half3 ambient = lerp(sceneAmbient, _AmbientColor.rgb, _AmbientColor.a);
                
                half3 lighting = 0;
                #if defined(_SURFACETYPE_TOON)
                    lighting = CalculateToonLighting(i.normalWS, i.positionWS, mainLight);
                #elif defined(_SURFACETYPE_METALLIC)
                    lighting = CalculateMetallicLighting(i.normalWS, viewDir, mainLight);
                #elif defined(_SURFACETYPE_FOLIAGE)
                    lighting = CalculateFoliageLighting(i.normalWS, i.positionWS, mainLight);
                #endif

                half3 surfaceColor = albedo.rgb * (lighting + ambient);
                
                #if defined(_EMISSION_ON)
                    surfaceColor += SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, i.uv).rgb * _EmissionColor.rgb;
                #endif

                return half4(surfaceColor, albedo.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Aura"
            Tags { "RenderType"="Transparent" "Queue"="Transparent" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex AuraVert
            #pragma fragment AuraFrag
            #pragma shader_feature_local _AURA_ON
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma multi_compile_local _ _SURFACETYPE_FOLIAGE
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            
            #include "Assets/Shaders/Toon/ToonUberBreakDownShader/Includes/ToonAura_Functions.hlsl"
            
            struct ShadowVaryings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            
            ShadowVaryings ShadowVert(Attributes input)
            {
                ShadowVaryings o;
                float3 positionOS = input.positionOS.xyz;
                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(positionOS, input.color);
                #endif
                VertexPositionInputs posInputs = GetVertexPositionInputs(positionOS);
                o.positionCS = GetShadowCoord(posInputs);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }
            
            half4 ShadowFrag(ShadowVaryings i) : SV_Target
            {
                ApplyAlphaClipFromUV(i.uv);
                return 0;
            }
            ENDHLSL
        }
    }
    CustomEditor "ToonAuraShaderGUI"
}