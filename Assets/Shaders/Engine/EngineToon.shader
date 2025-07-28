Shader "Custom/ToonUberShader_WithEngine_Fixed"
{
    Properties
    {
        [Header(Surface Pipeline)]
        [Enum(Opaque, 0, Transparent, 1, Metallic, 2, Foliage, 3)] _SurfaceType("Surface Type", Float) = 0
        [Enum(None, 0, Inverted Hull, 1, Fresnel, 2)] _OutlineMode("Outline Mode", Float) = 0

        [Header(Engine Animation Uses Vertex Color RGB and Baked UV2)]
        [Enum(Off, 0, On, 1)] _EngineAnimationMode("Enable Engine Animation", Float) = 0
        _Speed("Engine Speed", Range(0, 50)) = 10.0
        _StepDelay("Step Delay (Time)", Float) = 0.2
        _PistonDisplacement("Piston Displacement (Red)", Range(0, 1)) = 0.2
        _PistonAxis("Piston Movement Axis (Red)", Vector) = (0, 1, 0, 0)
        _RotationPivot("Rotation Pivot (Green)", Vector) = (0, 0, 0, 0)
        _RotationAxis("Rotation Axis (Green)", Vector) = (0, 0, 1, 0)
        
        [Header(Advanced Shake Properties)]
        _ShakeFrequency("Shake Base Frequency (Blue)", Range(0, 100)) = 15.0
        _ShakeAmplitude("Shake Base Amplitude (Blue)", Range(0, 0.2)) = 0.01
        _ShakeOctaves("Shake Octaves (Detail)", Int) = 4
        _ShakeLacunarity("Shake Lacunarity (Frequency Multiplier)", Range(1.0, 4.0)) = 2.0
        _ShakePersistence("Shake Persistence (Amplitude Multiplier)", Range(0.1, 1.0)) = 0.5

        [Header(Base Properties)]
        _BaseMap("Albedo (RGB) Alpha (A)", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        
        [Header(Alpha Clipping)]
        [Enum(Off, 0, On, 1)] _AlphaClipMode("Enable Alpha Clip", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        
        [Header(Emission)]
        [Enum(Off, 0, On, 1)] _EmissionMode("Enable Emission", Float) = 0
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,1)
        _EmissionMap("Emission Map", 2D) = "black" {}
        
        [Header(Lighting)]
        [Enum(Off, 0, On, 1)] _FakeLightMode("Enable Fake Light", Float) = 1
        _FakeLightColor("Fake Light Color", Color) = (0.8, 0.8, 0.8, 1)
        _FakeLightDirection("Fake Light Direction", Vector) = (0.5, 0.5, -0.5, 0)
        
        [Header(Toon Shading)]
        _ToonRampOffset("Ramp Offset", Range(0.0, 1.0)) = 0.5
        _ToonRampSmoothness("Ramp Smoothness", Range(0.001, 1.0)) = 0.05
        _ShadowTint("Shadow Tint", Color) = (0.1, 0.1, 0.2, 1.0)
        
        [Header(Stylized Metal)]
        _Ramp("Toon Ramp (RGB)", 2D) = "white" {} 
        _Brightness("Specular Brightness", Range(0, 2)) = 1.3  
        _Offset("Specular Size", Range(0, 1)) = 0.8
        _SpecuColor("Specular Color", Color) = (0.8,0.45,0.2,1)
        
        [Header(Highlight)]
        _HighlightOffset("Highlight Size", Range(0, 1)) = 0.9  
        _HiColor("Highlight Color", Color) = (1,1,1,1)
        
        [Header(Rim)]
        _RimColor("Rim Color", Color) = (1,0.3,0.3,1)
        _RimPower("Rim Power", Range(0, 20)) = 6
        
        [Header(Foliage)]
        _WindFrequency("Wind Frequency", Range(0.1, 10)) = 2.0
        _WindAmplitude("Wind Amplitude", Range(0, 1)) = 0.1
        _WindDirection("Wind Direction", Vector) = (1, 0, 0.5, 0)
        [HDR] _TranslucencyColor("Translucency Color", Color) = (0.7, 0.9, 0.3, 1)
        _TranslucencyStrength("Translucency Strength", Range(0, 5)) = 1.0
        
        [Header(Stylized Glass)]
        _GlassColor("Glass Color & Opacity", Color) = (0.8, 0.9, 1.0, 0.5)
        _FresnelColor("Fresnel (Edge) Color", Color) = (1,1,1,1)
        _FresnelPower("Fresnel Power", Range(1, 10)) = 5.0
        _RefractionStrength("Refraction Strength", Range(0, 0.1)) = 0.01
        _GlassSpecularPower("Specular Power", Range(1, 50)) = 20.0
        _GlassSpecularIntensity("Specular Intensity", Range(0, 5)) = 1.0
        
        [Header(Outline Properties (Inverted Hull))]
        _OutlineColor("Color", Color) = (0, 0, 0, 1)
        _OutlineWidth("Width", Range(0.0, 10)) = 0.01
        [Enum(Off, 0, On, 1)] _OutlineScaleWithDistance("Scale With Distance", Float) = 1
        _DistanceFadeStart("Distance Fade Start", Float) = 20
        _DistanceFadeEnd("Distance Fade End", Float) = 30
        
        [Header(Outline Properties (Fresnel))]
        _FresnelOutlineColor("Color", Color) = (0, 0, 0, 1)
        _FresnelOutlineWidth("Width", Range(0.001, 1.0)) = 0.1
        _FresnelOutlinePower("Power", Range(1.0, 20.0)) = 5.0

        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0
        [HideInInspector] _Cull ("__cull", Float) = 2.0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }

        HLSLINCLUDE
        half LerpWhiteTo(half b, half t) { return lerp(1.0h, b, t); }

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        
        #include "Assets/Shaders/Toon/ToonUberShader/ToonShading.hlsl" 
        #include "Assets/Shaders/Toon/ToonUberShader/Foliage.hlsl"
        
        CBUFFER_START(UnityPerMaterial)
            float _EngineAnimationMode;
            float4 _PistonAxis, _RotationPivot, _RotationAxis;
            float _Speed, _StepDelay, _PistonDisplacement;
            float _ShakeFrequency, _ShakeAmplitude;
            int _ShakeOctaves;
            float _ShakeLacunarity, _ShakePersistence;
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float _Cutoff;
            float4 _EmissionColor;
            float4 _FakeLightColor, _FakeLightDirection;
            float _ToonRampOffset, _ToonRampSmoothness;
            float4 _ShadowTint;
            float _Brightness, _Offset, _HighlightOffset, _RimPower;
            float4 _SpecuColor, _HiColor, _RimColor;
            float4 _GlassColor, _FresnelColor;
            float _FresnelPower, _RefractionStrength, _GlassSpecularPower, _GlassSpecularIntensity;
            float _WindFrequency, _WindAmplitude;
            float3 _WindDirection;
            float3 _TranslucencyColor;
            float _TranslucencyStrength;
            float4 _OutlineColor;
            float _OutlineWidth, _DistanceFadeStart, _DistanceFadeEnd;
            float4 _FresnelOutlineColor;
            float _FresnelOutlineWidth, _FresnelOutlinePower;
        CBUFFER_END

        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);
        TEXTURE2D(_Ramp); SAMPLER(sampler_Ramp);
        TEXTURE2D_X_FLOAT(_CameraOpaqueTexture); SAMPLER(sampler_CameraOpaqueTexture);

        float3 mod289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
        float4 mod289(float4 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
        float4 permute(float4 x) { return mod289(((x*34.0)+1.0)*x); }
        float4 taylorInvSqrt(float4 r) { return 1.79284291400159 - 0.85373472095314 * r; }

        float snoise(float3 v)
        {
            const float2 C = float2(1.0/6.0, 1.0/3.0);
            const float4 D = float4(0.0, 0.5, 1.0, 2.0);
            float3 i  = floor(v + dot(v, C.yyy));
            float3 x0 = v - i + dot(i, C.xxx);
            float3 g = step(x0.yzx, x0.xyz);
            float3 l = 1.0 - g;
            float3 i1 = min(g.xyz, l.zxy);
            float3 i2 = max(g.xyz, l.zxy);
            float3 x1 = x0 - i1 + C.xxx;
            float3 x2 = x0 - i2 + C.yyy;
            float3 x3 = x0 - D.yyy;
            i = mod289(i);
            float4 p = permute(permute(i.z + float4(0.0, i1.z, i2.z, 1.0)) + i.y + float4(0.0, i1.y, i2.y, 1.0)) + i.x + float4(0.0, i1.x, i2.x, 1.0);
            float3 ns = 0.142857142857 * (D.wyz - D.xzx);
            float4 j = p - 49.0 * floor(p * ns.z * ns.z);
            float4 x_ = floor(j * ns.z);
            float4 y_ = floor(j - 7.0 * x_);
            float4 x = x_ * ns.x + ns.yyyy;
            float4 y = y_ * ns.x + ns.yyyy;
            float4 h = 1.0 - abs(x) - abs(y);
            float4 b0 = float4(x.xy, y.xy);
            float4 b1 = float4(x.zw, y.zw);
            float4 s0 = floor(b0)*2.0 + 1.0;
            float4 s1 = floor(b1)*2.0 + 1.0;
            float4 sh = -step(h, 0.0);
            float4 a0 = b0.xzyw + s0.xzyw*sh.xxyy;
            float4 a1 = b1.xzyw + s1.xzyw*sh.zzww;
            float3 p0 = float3(a0.xy,h.x);
            float3 p1 = float3(a0.zw,h.y);
            float3 p2 = float3(a1.xy,h.z);
            float3 p3 = float3(a1.zw,h.w);
            float4 norm = taylorInvSqrt(float4(dot(p0,p0), dot(p1,p1), dot(p2,p2), dot(p3,p3)));
            p0 *= norm.x; p1 *= norm.y; p2 *= norm.z; p3 *= norm.w;
            float4 m = max(0.6 - float4(dot(x0,x0), dot(x1,x1), dot(x2,x2), dot(x3,x3)), 0.0);
            m = m * m;
            return 42.0 * dot(m*m, float4(dot(p0,x0), dot(p1,x1), dot(p2,x2), dot(p3,x3)));
        }

        float fbm(float3 p, int octaves, float lacunarity, float persistence)
        {
            float total = 0;
            float frequency = 1.0;
            float amplitude = 1.0;
            float maxValue = 0.0;
            for (int i = 0; i < octaves; i++)
            {
                total += snoise(p * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            return total / maxValue;
        }

        float3x3 rotationMatrix(float3 axis, float angle)
        {
            axis = normalize(axis);
            float s = sin(angle);
            float c = cos(angle);
            float oc = 1.0 - c;
            return float3x3(
                oc * axis.x * axis.x + c, oc * axis.x * axis.y - axis.z * s, oc * axis.z * axis.x + axis.y * s,
                oc * axis.x * axis.y + axis.z * s, oc * axis.y * axis.y + c, oc * axis.y * axis.z - axis.x * s,
                oc * axis.z * axis.x - axis.y * s, oc * axis.y * axis.z + axis.x * s, oc * axis.z * axis.z + c
            );
        }

        float3 ApplyEngineAnimation(float3 positionOS, float4 vertexColor, float stepIndex)
        {
            if (_EngineAnimationMode < 0.5) return positionOS;
            
            float currentTime = _Time.y;
            float delayedTime = (currentTime * _Speed) - (stepIndex * _StepDelay);
            
            float3 finalPosition = positionOS;
            float3 totalOffset = float3(0, 0, 0);

            if (vertexColor.g > 0.5) 
            {
                float3 pivot = _RotationPivot.xyz;
                float3 axis = _RotationAxis.xyz;
                float3 relativePos = finalPosition - pivot;
                float3x3 rotMatrix = rotationMatrix(axis, delayedTime);
                relativePos = mul(rotMatrix, relativePos);
                finalPosition = relativePos + pivot;
            }
            
            if (vertexColor.r > 0.5) 
            {
                float3 pistonDirection = normalize(_PistonAxis.xyz);
                float pistonOffset = sin(delayedTime) * _PistonDisplacement;
                totalOffset += pistonDirection * pistonOffset;
            } 
            
            if (vertexColor.b > 0.5)
            {
                float speedFactor = saturate(_Speed / 25.0);
                float timeFactor = currentTime * speedFactor;

                float3 noiseCoord_x = float3(positionOS.x, positionOS.y, timeFactor * _ShakeFrequency);
                float3 noiseCoord_y = float3(positionOS.y, positionOS.z, timeFactor * _ShakeFrequency + 10.0);
                float3 noiseCoord_z = float3(positionOS.z, positionOS.x, timeFactor * _ShakeFrequency - 10.0);

                float3 shakeOffset;
                shakeOffset.x = fbm(noiseCoord_x, _ShakeOctaves, _ShakeLacunarity, _ShakePersistence);
                shakeOffset.y = fbm(noiseCoord_y, _ShakeOctaves, _ShakeLacunarity, _ShakePersistence);
                shakeOffset.z = fbm(noiseCoord_z, _ShakeOctaves, _ShakeLacunarity, _ShakePersistence);

                totalOffset += shakeOffset * _ShakeAmplitude * speedFactor;
            }
            
            return finalPosition + totalOffset;
        }
        ENDHLSL

        Pass 
        {
            Name "Outline"
            Tags { "RenderType"="Opaque" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #pragma shader_feature_local _OUTLINEMODE_INVERTED_HULL
            #pragma shader_feature_local _OUTLINE_SCALE_WITH_DISTANCE
            #pragma shader_feature_local _ENGINEANIMATIONMODE_ON
            
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 color : COLOR; float2 uv1 : TEXCOORD1; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            #if defined(_OUTLINEMODE_INVERTED_HULL)
            Varyings OutlineVert(Attributes input) 
            {
                Varyings o = (Varyings)0;
                input.positionOS.xyz = ApplyEngineAnimation(input.positionOS.xyz, input.color, input.uv1.x);
                
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float4 posCS = TransformWorldToHClip(posWS);
                
                float camDist = length(posWS - _WorldSpaceCameraPos.xyz);
                float distFade = 1.0 - saturate((camDist - _DistanceFadeStart) / (_DistanceFadeEnd - _DistanceFadeStart + 1e-5));
                float scaledWidth = _OutlineWidth * distFade;
                
                #if defined(_OUTLINE_SCALE_WITH_DISTANCE)
                    scaledWidth *= posCS.w * 0.01;
                #endif
                
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 normalVS = TransformWorldToView(normalWS);
                float2 projectedNormal = normalize(TransformWViewToHClip(float4(normalVS, 0)).xy);
                
                posCS.xy += projectedNormal * scaledWidth;
                o.positionCS = posCS;
                return o;
            }
            half4 OutlineFrag(Varyings i) : SV_Target { return _OutlineColor; }
            #else
            Varyings OutlineVert(Attributes i) { Varyings o = (Varyings)0; return o; }
            half4 OutlineFrag(Varyings i) : SV_Target { clip(-1); return 0; }
            #endif
            ENDHLSL
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "RenderType"="Opaque" "Queue"="Geometry" "LightMode"="UniversalForward" }
            
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma shader_feature_local _ENGINEANIMATIONMODE_ON
            #pragma shader_feature_local _SURFACETYPE_OPAQUE _SURFACETYPE_TRANSPARENT _SURFACETYPE_METALLIC _SURFACETYPE_FOLIAGE
            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local_fragment _EMISSION_ON
            #pragma shader_feature_local_fragment _OUTLINEMODE_FRESNEL
            #pragma shader_feature_local_fragment _FAKELIGHT_ON
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; float4 color : COLOR; float2 uv1 : TEXCOORD1; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float2 uv : TEXCOORD2; float4 color : COLOR; };

            Varyings vert(Attributes v) 
            {
                Varyings o;
                v.positionOS.xyz = ApplyEngineAnimation(v.positionOS.xyz, v.color, v.uv1.x);
                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(v.positionOS.xyz, v.normalOS, v.color, _WindFrequency, _WindAmplitude, _WindDirection);
                #endif
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target 
            {
                half3 surfaceColor = 0;
                half surfaceAlpha = 1;
                float3 viewDir = SafeNormalize(_WorldSpaceCameraPos.xyz - i.positionWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                
                #if defined(_FAKELIGHT_ON)
                if (all(mainLight.color < 0.001)) 
                { 
                    mainLight.direction = normalize(_FakeLightDirection.xyz); 
                    mainLight.color = _FakeLightColor.rgb;
                    mainLight.shadowAttenuation = 1.0; 
                }
                #endif

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                #if defined(_ALPHACLIP_ON)
                    clip(albedo.a - _Cutoff);
                #endif

                #if defined(_SURFACETYPE_OPAQUE)
                    float3 lighting = CalculateToonLighting(i.normalWS, _ToonRampSmoothness, i.positionWS, _ShadowTint, _ToonRampOffset, mainLight);
                    surfaceColor = albedo.rgb * (lighting + SampleSH(i.normalWS));
                    surfaceAlpha = albedo.a;
                #elif defined(_SURFACETYPE_METALLIC)
                    half d = dot(i.normalWS, mainLight.direction) * 0.5 + 0.5;
                    half3 ramp = SAMPLE_TEXTURE2D(_Ramp, sampler_Ramp, float2(d, d)).rgb;
                    surfaceColor = albedo.rgb * mainLight.color * ramp * (mainLight.shadowAttenuation * 2);
                    float3 halfVec = normalize(viewDir + mainLight.direction);
                    float specDot = saturate(dot(halfVec, i.normalWS));
                    surfaceColor += step(_Offset, specDot) * _SpecuColor.rgb * _Brightness * mainLight.color * mainLight.shadowAttenuation;
                    float highlightDot = saturate(dot(i.normalWS, mainLight.direction));
                    surfaceColor += step(_HighlightOffset, highlightDot) * _HiColor.rgb * mainLight.color * mainLight.shadowAttenuation;
                    half rim = 1.0 - saturate(dot(viewDir, i.normalWS));
                    surfaceColor += _RimColor.rgb * pow(rim, _RimPower);
                    surfaceAlpha = albedo.a;
                #elif defined(_SURFACETYPE_FOLIAGE)
                    float3 lighting = CalculateFoliageLighting(i.normalWS, i.positionWS, mainLight, _TranslucencyStrength, _TranslucencyColor);
                    surfaceColor = albedo.rgb * (lighting + SampleSH(i.normalWS));
                    surfaceAlpha = albedo.a;
                #elif defined(_SURFACETYPE_TRANSPARENT)
                    float fresnelDot = 1.0 - saturate(dot(i.normalWS, viewDir));
                    float fresnel = pow(fresnelDot, _FresnelPower);
                    float2 screenUV = i.positionCS.xy / i.positionCS.w;
                    float2 distortion = i.normalWS.xy * _RefractionStrength;
                    float3 sceneColor = SAMPLE_TEXTURE2D_X_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + distortion, 0).rgb;
                    surfaceColor = lerp(sceneColor, _GlassColor.rgb, _GlassColor.a);
                    surfaceColor = lerp(surfaceColor, _FresnelColor.rgb, fresnel);
                    float3 reflectDir = reflect(-mainLight.direction, i.normalWS);
                    float spec = pow(saturate(dot(viewDir, reflectDir)), _GlassSpecularPower);
                    surfaceColor += mainLight.color * spec * _GlassSpecularIntensity * mainLight.shadowAttenuation;
                    surfaceAlpha = _GlassColor.a;
                #endif

                #if defined(_EMISSION_ON)
                    surfaceColor += SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, i.uv).rgb * _EmissionColor.rgb;
                #endif

                #if defined(_OUTLINEMODE_FRESNEL)
                    float fresnelDotOutline = 1.0 - saturate(dot(i.normalWS, viewDir));
                    float fresnelOutline = pow(fresnelDotOutline, _FresnelOutlinePower);
                    float outlineFactor = smoothstep(1.0 - _FresnelOutlineWidth, 1.0 - _FresnelOutlineWidth + 0.05, fresnelOutline);
                    surfaceColor = lerp(surfaceColor, _FresnelOutlineColor.rgb, outlineFactor);
                #endif

                return half4(surfaceColor, surfaceAlpha);
            }
            ENDHLSL
        }

        Pass 
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On 
            ZTest LEqual 
            ColorMask 0 
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            
            #pragma shader_feature_local _ENGINEANIMATIONMODE_ON
            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local _SURFACETYPE_FOLIAGE

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; float4 color : COLOR; float2 uv1 : TEXCOORD1; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;

                input.positionOS.xyz = ApplyEngineAnimation(input.positionOS.xyz, input.color, input.uv1.x);
                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(input.positionOS.xyz, input.normalOS, input.color, _WindFrequency, _WindAmplitude, _WindDirection);
                #endif
                
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = GetShadowPositionHClip(positionInputs, normalInputs);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 ShadowFrag(Varyings i) : SV_Target 
            {
                #if defined(_ALPHACLIP_ON)
                    half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                    clip(albedo.a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }
    }
    CustomEditor "ToonEngineShaderGUI"
}