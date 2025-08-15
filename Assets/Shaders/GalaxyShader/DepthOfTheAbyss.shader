Shader "CleanCode/DepthOfTheAbyss"
{
    Properties
    {
        [Header(Abyss Depths Parallax)]
        _ViewParallaxIntensity("View-Depth Parallax", Range(0, 0.1)) = 0.04
        _AbyssBrightness("Abyss Brightness", Range(0, 2)) = 1.0

        [Header(Deep Creature)]
        _CreatureTex("Creature Texture (A)", 2D) = "white" {}
        [HDR]_CreatureColor("Creature Color", Color) = (0.1, 0.8, 0.7, 1)
        _CreatureAnimationSpeed("Creature Animation Speed", Float) = 0.1
        _CreatureVisibility("Creature Visibility", Range(0, 1)) = 1.0
        _CreatureScale("Creature Scale", Float) = 0.5
        
        [Header(Caustics Effect)]
        _CausticsTex("Caustics Texture", 2D) = "white" {}
        [HDR]_CausticsColor("Caustics Color", Color) = (0.5, 0.8, 1.0, 1)
        _CausticsScale("Caustics Scale", Float) = 1.0
        _CausticsSpeed("Caustics Speed", Vector) = (0.02, 0.01, 0, 0)
        _CausticsIntensity("Caustics Intensity", Range(0, 5)) = 1.0

        [Header(Abyss Environment)]
        _PlanktonTex("Plankton Texture (R)", 2D) = "white" {}
        [HDR]_PlanktonColor("Plankton Color", Color) = (1, 1, 0.8, 1)
        _PlanktonScale("Plankton Scale", Float) = 2.0
        _PlanktonScrollSpeed("Plankton Scroll Speed", Vector) = (0.01, 0.015, 0, 0)
        _PlanktonThreshold("Plankton Threshold", Range(0.5, 1)) = 0.95
        _AbyssFogTex("Abyss Fog Texture", 2D) = "white" {}
        [HDR]_AbyssFogColor("Abyss Fog Color", Color) = (0.05, 0.1, 0.2, 1)
        _AbyssFogScale("Abyss Fog Scale", Float) = 1.5
        _AbyssFogScrollSpeed("Abyss Fog Scroll Speed", Vector) = (-0.004, -0.006, 0, 0)

        [Header(Surface Lighting)]
        _FakeLightDirection("Fake Light Direction", Vector) = (0.5, 0.5, 0, 0)
        [HDR]_LightColor("Light Color", Color) = (1,1,1,1)
        [HDR]_ShadowColor("Shadow Color", Color) = (0.1, 0.1, 0.2, 1)
        _ShadowAmount("Shadow Amount", Range(0, 1)) = 0.5
        [Header(Surface Specular and Rim)]
        [HDR]_SpecularColor("Specular Color", Color) = (1,1,1,1)
        _SpecularHardness("Specular Hardness", Range(0.5, 0.99)) = 0.95
        _RimGradientTex("Rim Gradient (1D)", 2D) = "white" {}
        [HDR]_RimColor("Rim Tint", Color) = (0.2, 0.9, 1.0, 1)
        _RimPower("Rim Power", Range(0.1, 20.0)) = 5.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalRenderPipeline" "Queue"="Geometry" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma require appdata_tan

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 p:POSITION; float3 n:NORMAL; float4 t:TANGENT; };
            struct Varyings { float4 p:SV_POSITION; float3 posOS:TEXCOORD0; float3 normWS:NORMAL; float3 tanWS:TEXCOORD1; float3 bitanWS:TEXCOORD2; };

            CBUFFER_START(UnityPerMaterial)
                float _ViewParallaxIntensity; half _AbyssBrightness;
                float4 _FakeLightDirection; half4 _LightColor, _ShadowColor; half _ShadowAmount;
                half4 _SpecularColor; half _SpecularHardness;
                half4 _RimColor; float _RimPower;
                half4 _CreatureColor; float _CreatureAnimationSpeed, _CreatureScale; half _CreatureVisibility;
                half4 _CausticsColor; float _CausticsScale, _CausticsIntensity; float2 _CausticsSpeed;
                half4 _PlanktonColor; float _PlanktonScale; float2 _PlanktonScrollSpeed; half _PlanktonThreshold;
                half4 _AbyssFogColor; float _AbyssFogScale; float2 _AbyssFogScrollSpeed;
            CBUFFER_END

            TEXTURE2D(_CreatureTex); SAMPLER(sampler_CreatureTex);
            TEXTURE2D(_CausticsTex); SAMPLER(sampler_CausticsTex);
            TEXTURE2D(_PlanktonTex); SAMPLER(sampler_PlanktonTex);
            TEXTURE2D(_AbyssFogTex); SAMPLER(sampler_AbyssFogTex);
            TEXTURE2D(_RimGradientTex); SAMPLER(sampler_RimGradientTex);

            half4 SampleOST(TEXTURE2D_PARAM(tex,smp), float3 p, float3 n, float scale, float2 scroll)
            {
                float2 uvX=p.yz*scale+scroll*_Time.y, uvY=p.xz*scale+scroll*_Time.y, uvZ=p.xy*scale+scroll*_Time.y;
                half4 cX=SAMPLE_TEXTURE2D(tex,smp,uvX), cY=SAMPLE_TEXTURE2D(tex,smp,uvY), cZ=SAMPLE_TEXTURE2D(tex,smp,uvZ);
                half3 w = pow(abs(n), 4.0);
                return (cX * w.x + cY * w.y + cZ * w.z) / (w.x + w.y + w.z);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.posOS = IN.p.xyz;
                OUT.normWS = TransformObjectToWorldNormal(IN.n);
                OUT.tanWS = TransformObjectToWorldDir(IN.t.xyz);
                OUT.bitanWS = cross(OUT.normWS, OUT.tanWS) * IN.t.w;
                OUT.p = TransformObjectToHClip(IN.p.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normWS);
                float3 viewDirWS = normalize(GetCameraPositionWS() - TransformObjectToWorld(IN.posOS));

                float3x3 worldToTangent = float3x3(normalize(IN.tanWS), normalize(IN.bitanWS), normalWS);
                float3 viewDirTS = mul(worldToTangent, viewDirWS);
                
                float2 parallaxOffset = viewDirTS.xy * _ViewParallaxIntensity;

                // --- ABYSS LAYERS CALCULATION ---
                float creaturePhase = _Time.y * _CreatureAnimationSpeed;
                float creatureDriftX = sin(creaturePhase);
                float creatureDriftY = cos(creaturePhase * 0.7);
                half creatureAlpha = (sin(creaturePhase * 0.5) * 0.5 + 0.5) * _CreatureVisibility;

                float3 posCreature = IN.posOS + float3(creatureDriftX * 0.2, creatureDriftY * 0.2, 0);
                posCreature.xy -= parallaxOffset * 2.5;
                half creatureMask = SampleOST(TEXTURE2D_ARGS(_CreatureTex, sampler_CreatureTex), posCreature, IN.normWS, _CreatureScale, 0).a;
                half3 creatureColor = creatureMask * _CreatureColor.rgb * creatureAlpha;

                float3 posFog = IN.posOS; posFog.xy -= parallaxOffset * 1.5;
                half3 fogColor = SampleOST(TEXTURE2D_ARGS(_AbyssFogTex, sampler_AbyssFogTex), posFog, IN.normWS, _AbyssFogScale, _AbyssFogScrollSpeed).rgb * _AbyssFogColor.rgb;

                float3 posPlankton = IN.posOS; posPlankton.xy -= parallaxOffset * 0.7;
                half planktonNoise = SampleOST(TEXTURE2D_ARGS(_PlanktonTex, sampler_PlanktonTex), posPlankton, IN.normWS, _PlanktonScale, _PlanktonScrollSpeed).r;
                half planktonMask = step(_PlanktonThreshold, planktonNoise);
                half3 planktonColor = planktonMask * _PlanktonColor.rgb;

                half3 caustics = SampleOST(TEXTURE2D_ARGS(_CausticsTex, sampler_CausticsTex), IN.posOS, IN.normWS, _CausticsScale, _CausticsSpeed).rgb * _CausticsColor.rgb;
                
                half3 abyssColor = fogColor + creatureColor + planktonColor;
                abyssColor += caustics * _CausticsIntensity;
                abyssColor *= _AbyssBrightness;

                // --- SURFACE LIGHTING CALCULATION ---
                float3 lightDirWS = normalize(_FakeLightDirection.xyz);
                half NdotL = saturate(dot(normalWS, lightDirWS));
                half celFactor = _ShadowAmount > 0.001 ? smoothstep(_ShadowAmount - 0.02, _ShadowAmount + 0.02, NdotL) : 1.0;
                half3 celLighting = lerp(_ShadowColor.rgb, _LightColor.rgb, celFactor);

                float3 halfDir = normalize(lightDirWS + viewDirWS);
                half NdotH = saturate(dot(normalWS, halfDir));
                half specularFactor = smoothstep(_SpecularHardness, _SpecularHardness + 0.02, NdotH);
                half3 specular = _SpecularColor.rgb * specularFactor;

                half fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _RimPower);
                half3 rimGradient = SAMPLE_TEXTURE2D(_RimGradientTex, sampler_RimGradientTex, float2(fresnel, 0.5)).rgb;
                half3 rim = rimGradient * _RimColor.rgb;
                
                half3 finalColor = (abyssColor * celLighting) + specular + rim;

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}