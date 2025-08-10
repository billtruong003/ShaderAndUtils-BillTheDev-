Shader "Clean/ScopeURP_AdjustableShape"
{
    Properties
    {
        [Header(Base Properties)]
        _BaseMap("Base Map (UV0)", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)
        
        [Header(ScreenSpace Scope Effects)]
        _ReticleTexture("Reticle Texture", 2D) = "white" {}
        _ReticleColor("Reticle Color", Color) = (1,0,0,1)
        _Zoom("Zoom", Range(1.0, 15.0)) = 2.0
        _Sharpness("Sharpness", Range(0.0, 1.0)) = 0.5
        
        [Header(Scope Shape and Size)]
        _ScopeCenter("Scope Center (XY)", Vector) = (0.5, 0.5, 0, 0)
        _ScopeRadiusXY("Scope Radius (Width, Height)", Vector) = (0.4, 0.4, 0, 0)
        _BorderSoftness("Border Softness", Range(0.001, 5)) = 0.02

        [Header(Render Settings)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; 
                float4 _BaseColor; 
                float4 _ReticleColor;
                float4 _ScopeCenter;
                float4 _ScopeRadiusXY;
                float _Zoom; 
                float _Sharpness;
                float _BorderSoftness;
            CBUFFER_END

            TEXTURE2D(_CameraOpaqueTexture); SAMPLER(sampler_CameraOpaqueTexture);
            float4 _CameraOpaqueTexture_TexelSize;
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_ReticleTexture); SAMPLER(sampler_ReticleTexture);

            struct Attributes 
            {
                float4 positionOS   : POSITION;
                float2 uv0          : TEXCOORD0;
                float3 normalOS     : NORMAL;
            };

            struct Varyings 
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float2 uv0          : TEXCOORD1;
                float4 screenPos    : TEXCOORD2;
                float3 normalWS     : TEXCOORD3;
            };

            Varyings vert(Attributes i) 
            {
                Varyings o = (Varyings)0;
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.uv0 = TRANSFORM_TEX(i.uv0, _BaseMap);
                o.screenPos = ComputeScreenPos(o.positionCS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target 
            {
                // --- 1. Tính toán ánh sáng cho model vũ khí (hiển thị bên ngoài scope) ---
                half4 weaponTexture = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv0);
                half4 weaponColor = weaponTexture * _BaseColor;

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float3 normalWS = normalize(i.normalWS);
                half3 ambient = SampleSH(normalWS);
                half directDiffuse = saturate(dot(normalWS, mainLight.direction));
                half3 directLighting = mainLight.color * mainLight.shadowAttenuation * directDiffuse;
                half3 litWeaponColor = weaponColor.rgb * (directLighting + ambient);

                // --- 2. Tính toán mặt nạ scope hình elip trong không gian màn hình ---
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float2 relativeCoords = screenUV - _ScopeCenter.xy;
                // Chuẩn hóa tọa độ để biến elip thành hình tròn đơn vị
                float2 normalizedCoords = relativeCoords / _ScopeRadiusXY.xy;
                float ellipseDistance = length(normalizedCoords); // Giá trị là 1.0 tại cạnh elip
                
                half scopeMask = smoothstep(1.0, 1.0 - _BorderSoftness, ellipseDistance);
                
                // --- 3. Tính toán hình ảnh bên trong scope ---
                float2 zoomedUV = (screenUV - _ScopeCenter.xy) / _Zoom + _ScopeCenter.xy;
                half3 sceneSample = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, zoomedUV).rgb;

                if (_Sharpness > 0.0)
                {
                    float2 texelStep = _CameraOpaqueTexture_TexelSize.xy;
                    half3 top    = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, zoomedUV + float2(0, texelStep.y)).rgb;
                    half3 bottom = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, zoomedUV - float2(0, texelStep.y)).rgb;
                    half3 left   = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, zoomedUV - float2(texelStep.x, 0)).rgb;
                    half3 right  = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, zoomedUV + float2(texelStep.x, 0)).rgb;
                    
                    half3 sharpenedColor = sceneSample * 5.0 - (top + bottom + left + right);
                    sceneSample = lerp(sceneSample, sharpenedColor, _Sharpness);
                }

                // --- 4. Vẽ tâm ngắm (Reticle) co giãn theo hình dạng scope ---
                float2 reticleUV = relativeCoords / (_ScopeRadiusXY.xy * 2.0) + 0.5;
                half4 reticleSample = SAMPLE_TEXTURE2D(_ReticleTexture, sampler_ReticleTexture, reticleUV);
                half4 reticleEffect = reticleSample * _ReticleColor;
                half3 finalScopeView = lerp(sceneSample, reticleEffect.rgb, reticleEffect.a);

                // --- 5. Kết hợp vũ khí và scope view bằng mặt nạ elip ---
                half3 finalColor = lerp(litWeaponColor, finalScopeView, scopeMask);
                
                return half4(finalColor, weaponColor.a);
            }
            ENDHLSL
        }
        
        Pass 
        {
            Name "ShadowCaster" Tags { "LightMode" = "ShadowCaster" }
            Cull [_Cull] ZWrite On ZTest LEqual
            HLSLPROGRAM
            #pragma vertex vert #pragma fragment frag #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Attributes { float4 p:POSITION; float3 n:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 p:SV_POSITION; };
            Varyings vert(Attributes i) {
                Varyings o; UNITY_SETUP_INSTANCE_ID(i);
                float3 posWS = TransformObjectToWorld(i.p.xyz); float3 nrmWS = TransformObjectToWorldNormal(i.n);
                o.p = TransformWorldToHClip(ApplyShadowBias(posWS, nrmWS, _MainLightPosition.xyz));
                #if UNITY_REVERSED_Z
                o.p.z = min(o.p.z, o.p.w * UNITY_NEAR_CLIP_VALUE);
                #else
                o.p.z = max(o.p.z, o.p.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                return o;
            }
            half4 frag(Varyings i) : SV_TARGET { return 0; }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}