Shader "CleanCode/UltimateCosmicGlassParallax"
{
    Properties
    {
        [Header(Depth and Parallax)]
        _ParallaxDepth("Parallax Depth", Range(-0.5, 0.5)) = 0.04
        _GalaxyDepthBias("Galaxy Brightness/Depth", Range(0, 2)) = 1.0

        [Header(Lighting and Cel Shading)]
        _FakeLightDirection("Fake Light Direction", Vector) = (0.5, 0.5, 0, 0)
        [HDR]_LightColor("Light Color", Color) = (1,1,1,1)
        [HDR]_ShadowColor("Shadow Color", Color) = (0.1, 0.1, 0.2, 1)
        _ShadowAmount("Shadow Amount", Range(0, 1)) = 0.5
        _CelSmoothness("Cel Smoothness", Range(0.001, 0.2)) = 0.05

        [Header(Fake Toon Specular)]
        [HDR]_SpecularColor("Specular Color", Color) = (1,1,1,1)
        _SpecularHardness("Specular Hardness", Range(0.5, 0.99)) = 0.95
        _SpecularSmoothness("Specular Smoothness", Range(0.001, 0.1)) = 0.01

        [Header(Rim Effect)]
        _RimGradientTex("Rim Gradient (1D)", 2D) = "white" {}
        [HDR]_RimColor("Rim Tint", Color) = (1,1,1,1)
        _RimPower("Rim Power", Range(0.1, 20.0)) = 5.0
        _RimIntensity("Rim Intensity", Range(0.0, 10.0)) = 1.5

        [Header(Galaxy Noise and Stars)]
        _NoiseTex1("Noise Texture 1 (Near)", 2D) = "white" {}
        [HDR]_NoiseColor1("Noise Color 1", Color) = (0.8, 0.2, 1.0, 1)
        _NoiseScale1("Noise Scale 1", Float) = 1.0
        _NoiseScrollSpeed1("Noise Scroll Speed 1", Vector) = (0.01, 0.015, 0, 0)
        _NoiseTex2("Noise Texture 2 (Mid)", 2D) = "white" {}
        [HDR]_NoiseColor2("Noise Color 2", Color) = (0.2, 0.8, 1.0, 1)
        _NoiseScale2("Noise Scale 2", Float) = 1.5
        _NoiseScrollSpeed2("Noise Scroll Speed 2", Vector) = (-0.02, -0.01, 0, 0)
        _StarsTex("Stars Texture (Far)", 2D) = "white" {}
        _StarsScale("Stars Scale", Float) = 0.5
        _StarsScrollSpeed("Stars Scroll Speed", Vector) = (0.005, 0.005, 0, 0)
        _StarsIntensity("Stars Intensity", Range(0, 10)) = 2.0
        
        [Header(Triplanar Blending)]
        _BlendSharpness("Blend Sharpness", Range(1, 10)) = 4.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalRenderPipeline" "Queue"="Geometry" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 p:POSITION; float3 n:NORMAL; };
            struct Varyings { float4 p:SV_POSITION; float3 posWS:TEXCOORD0; float3 normWS:NORMAL; float3 viewDirWS:TEXCOORD1; };

            CBUFFER_START(UnityPerMaterial)
                float _ParallaxDepth; half _GalaxyDepthBias;
                float4 _FakeLightDirection;
                half4 _LightColor, _ShadowColor;
                half _ShadowAmount, _CelSmoothness;
                half4 _SpecularColor;
                half _SpecularHardness, _SpecularSmoothness;
                half4 _RimColor;
                float _RimPower;
                half _RimIntensity;
                half4 _NoiseColor1, _NoiseColor2;
                float _NoiseScale1, _NoiseScale2, _StarsScale;
                float2 _NoiseScrollSpeed1, _NoiseScrollSpeed2, _StarsScrollSpeed;
                half _StarsIntensity;
                float _BlendSharpness;
            CBUFFER_END

            TEXTURE2D(_NoiseTex1); SAMPLER(sampler_NoiseTex1);
            TEXTURE2D(_NoiseTex2); SAMPLER(sampler_NoiseTex2);
            TEXTURE2D(_StarsTex);  SAMPLER(sampler_StarsTex);
            TEXTURE2D(_RimGradientTex); SAMPLER(sampler_RimGradientTex);

            half4 SampleTriplanar(TEXTURE2D_PARAM(tex,smp), float3 p, float3 n, float scale, float2 scroll)
            {
                float2 uvX=p.yz*scale+scroll*_Time.y, uvY=p.xz*scale+scroll*_Time.y, uvZ=p.xy*scale+scroll*_Time.y;
                half4 cX=SAMPLE_TEXTURE2D(tex,smp,uvX), cY=SAMPLE_TEXTURE2D(tex,smp,uvY), cZ=SAMPLE_TEXTURE2D(tex,smp,uvZ);
                half3 w = pow(abs(n), _BlendSharpness);
                return (cX * w.x + cY * w.y + cZ * w.z) / (w.x + w.y + w.z);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.posWS = TransformObjectToWorld(IN.p.xyz);
                OUT.normWS = TransformObjectToWorldNormal(IN.n);
                OUT.p = TransformWorldToHClip(OUT.posWS);
                OUT.viewDirWS = GetCameraPositionWS() - OUT.posWS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normWS);
                float3 viewDirWS = normalize(IN.viewDirWS);
                
                float3 parallaxOffset = viewDirWS * _ParallaxDepth;
                float3 posStars = IN.posWS + parallaxOffset * 2.0;
                float3 posNoise2 = IN.posWS + parallaxOffset * 1.0;
                float3 posNoise1 = IN.posWS + parallaxOffset * 0.5;

                half4 n1 = SampleTriplanar(TEXTURE2D_ARGS(_NoiseTex1,sampler_NoiseTex1),posNoise1,normalWS,_NoiseScale1,_NoiseScrollSpeed1);
                half4 n2 = SampleTriplanar(TEXTURE2D_ARGS(_NoiseTex2,sampler_NoiseTex2),posNoise2,normalWS,_NoiseScale2,_NoiseScrollSpeed2);
                half s = SampleTriplanar(TEXTURE2D_ARGS(_StarsTex,sampler_StarsTex),posStars,normalWS,_StarsScale,_StarsScrollSpeed).r;
                half3 galaxyBase = ((n1.rgb*_NoiseColor1.rgb) + (n2.rgb*_NoiseColor2.rgb) + (s*_StarsIntensity)) * _GalaxyDepthBias;
                
                float3 lightDirWS = normalize(_FakeLightDirection.xyz);
                half NdotL = saturate(dot(normalWS, lightDirWS));
                half celFactor = 1.0;
                if (_ShadowAmount > 0.001) { celFactor = smoothstep(_ShadowAmount - _CelSmoothness, _ShadowAmount + _CelSmoothness, NdotL); }
                half3 celLighting = lerp(_ShadowColor.rgb, _LightColor.rgb, celFactor);

                float3 halfDir = normalize(lightDirWS + viewDirWS);
                half NdotH = saturate(dot(normalWS, halfDir));
                half specularFactor = smoothstep(_SpecularHardness - _SpecularSmoothness, _SpecularHardness + _SpecularSmoothness, NdotH);
                half3 specular = _SpecularColor.rgb * specularFactor;

                half fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _RimPower);
                half3 rimGradient = SAMPLE_TEXTURE2D(_RimGradientTex, sampler_RimGradientTex, float2(fresnel, 0.5)).rgb;
                half3 rim = rimGradient * _RimColor.rgb * _RimIntensity * fresnel;
                
                half3 finalColor = (galaxyBase * celLighting) + specular + rim;

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}