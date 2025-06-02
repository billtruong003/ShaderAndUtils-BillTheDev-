Shader "Universal Render Pipeline/Wireframe/DisplacementFade-SimpleLit-Compute"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" { }
        _Normal ("Normal Map", 2D) = "bump" { }
        _WireThickness ("Wire Thickness", Range(0, 800)) = 100
        [HDR] _WireColor ("Wire Color", Color) = (0,1,1,1)
        [Toggle(INVERT)] _INVERT("Invert", Float) = 1
        _MovingSlider ("Moving Slider", Range(-10, 10)) = 0
        _Extrude("Extrude Amount", Range(-10, 10)) = 0
        _WireFrameStay ("Wire Stay", Range(-1, 1)) = 0
        _ClipOffset ("Clip Offset", Range(-1, 1)) = 0.2
    }
    SubShader
    {
        Tags {"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" 
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"    
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #pragma shader_feature INVERT

        CBUFFER_START(UnityPerMaterial)
        float _WireThickness;
        float4 _WireColor;
        sampler2D _MainTex;
        float _MovingSlider;
        float _Extrude;
        float _WireFrameStay;
        float _ClipOffset;
        sampler2D _Normal;
        CBUFFER_END

        // Buffer chứa khoảng cách đến các cạnh (từ compute shader)
        StructuredBuffer<float3> _EdgeDistances;

        struct Attributes
        {
            float4 positionOS : POSITION;
            uint vertexID : SV_VertexID;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            float2 uv : TEXCOORD0;
            float3 normal : NORMAL;
        };

        struct v2f
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            float4 worldSpacePosition : TEXCOORD1;
            float movingPos : TEXCOORD2;
            float3 normal : NORMAL;
            float3 worldNormal : TEXCOORD3;
            uint vertexID : TEXCOORD4;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        v2f vert(Attributes input)
        {
            v2f output = (v2f)0;

            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            // Tính movingPos cho hiệu ứng dissolve
            #if INVERT
            float movingPos = input.positionOS.y + _MovingSlider;
            #else
            float movingPos = 1 - input.positionOS.y + _MovingSlider;
            #endif

            // Áp dụng displacement
            input.positionOS.xyz -= saturate(1 - movingPos) * input.normal * _Extrude;

            // Chuyển đổi vị trí sang clip space
            VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
            output.positionCS = vertexInput.positionCS;
            output.worldSpacePosition = mul(UNITY_MATRIX_M, input.positionOS);
            output.uv = input.uv;
            output.worldNormal = TransformObjectToWorldNormal(input.normal);
            output.normal = input.normal;
            output.movingPos = movingPos;
            output.vertexID = input.vertexID;

            return output;
        }

        float3 Lambert(float3 lightColor, float3 lightDir, float3 normal)
        {
            float NdotL = saturate(dot(normal, lightDir));
            return lightColor * NdotL;
        }

        float3 Unity_NormalBlend(float3 A, float3 B)
        {
            return normalize(float3(A.rg + B.rg, A.b * B.b));
        }

        half4 frag(v2f i) : SV_Target
        {
            // Lấy khoảng cách đến các cạnh từ compute shader
            float3 edgeDistances = _EdgeDistances[i.vertexID];
            float minDistanceToEdge = min(min(edgeDistances.x, edgeDistances.y), edgeDistances.z);

            // Xử lý normal map và ánh sáng
            float3 normal = UnpackNormal(tex2D(_Normal, i.uv));
            #if SHADOWS_SCREEN
                half4 shadowCoord = ComputeScreenPos(i.positionCS);
            #else
                half4 shadowCoord = TransformWorldToShadowCoord(i.worldSpacePosition.xyz);
            #endif 
            #if _MAIN_LIGHT_SHADOWS_CASCADE || _MAIN_LIGHT_SHADOWS
                Light light = GetMainLight(shadowCoord);
            #else
                Light light = GetMainLight();
            #endif

            float3 lightCol = Lambert(light.color.rgb * unity_LightData.z, light.direction, Unity_NormalBlend(normal, i.worldNormal)) * light.shadowAttenuation;

            int lightsCount = GetAdditionalLightsCount();
            for (int j = 0; j < lightsCount; j++)
            {
                Light lightAdd = GetAdditionalLight(j, i.worldSpacePosition.xyz);
                lightCol += Lambert(lightAdd.color * (lightAdd.distanceAttenuation * lightAdd.shadowAttenuation), lightAdd.direction, Unity_NormalBlend(normal, i.normal));
            }

            float4 mainTex = tex2D(_MainTex, i.uv);
            float3 combinedAmbient = SampleSH(i.worldNormal);
            mainTex.rgb *= lightCol + combinedAmbient;

            // Hiển thị wireframe hoặc texture
            if (minDistanceToEdge > 0.9 * (1 - saturate(i.movingPos - _WireFrameStay)))
            {
                clip(i.movingPos - _ClipOffset);
                return mainTex;
            }
            clip(i.movingPos);
            return _WireColor;
        }

        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBIN to
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_instancing
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}