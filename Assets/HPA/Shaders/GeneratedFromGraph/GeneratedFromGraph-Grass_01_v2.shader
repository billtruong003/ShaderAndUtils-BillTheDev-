Shader "Grass_01_v2_Compatible"
{
    Properties
    {
        [NoScaleOffset]Texture2D_B18C2857("Texture2D", 2D) = "white" {}
        [NoScaleOffset]Texture2D_E933B04B("Normal", 2D) = "white" {}
        _Normal_Strength("Normal_Strength", Range(0, 1)) = 0.5
        Vector2_EC93FC28("Wind_Movement", Vector) = (6, 0, 0, 0)
        Vector1_B7976139("Wind_Density", Float) = 2
        Vector1_C39A3E90("Wind_Strength", Float) = 0.3
        [NoScaleOffset]Texture2D_C3D58803("Ramp", 2D) = "white" {}
        Alpha_Threshold("Alpha_Threshold", Range(0, 1)) = 0.5
        _Luminosity("Luminosity", Range(0, 5)) = 1
        [HideInInspector]_QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector]_QueueControl("_QueueControl", Float) = -1
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "UniversalMaterialType" = "Lit"
            "Queue"="AlphaTest"
            "ShaderGraphShader"="true"
            "ShaderGraphTargetId"="UniversalLitSubTarget"
        }
        Pass
        {
            Name "Universal Forward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
        
        // Render State
        Cull Off
        Blend One Zero
        ZTest LEqual
        ZWrite On
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 4.5
        #pragma exclude_renderers gles gles3 glcore
        #pragma multi_compile_instancing
        #pragma multi_compile_fog
        #pragma instancing_options renderinglayer
        #pragma multi_compile _ DOTS_INSTANCING_ON
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
        #pragma multi_compile _ LIGHTMAP_ON
        #pragma multi_compile _ DYNAMICLIGHTMAP_ON
        #pragma multi_compile _ DIRLIGHTMAP_COMBINED
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
        #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
        #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
        #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
        #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
        #pragma multi_compile_fragment _ _SHADOWS_SOFT
        #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
        #pragma multi_compile _ SHADOWS_SHADOWMASK
        #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
        #pragma multi_compile_fragment _ _LIGHT_LAYERS
        #pragma multi_compile_fragment _ DEBUG_DISPLAY
        #pragma multi_compile_fragment _ _LIGHT_COOKIES
        #pragma multi_compile _ _CLUSTERED_RENDERING
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define ATTRIBUTES_NEED_TEXCOORD2
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TANGENT_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define VARYINGS_NEED_VIEWDIRECTION_WS
        #define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
        #define VARYINGS_NEED_SHADOW_COORD
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_FORWARD
        #define _FOG_FRAGMENT 1
        #define _ALPHATEST_ON 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
             float4 uv1 : TEXCOORD1;
             float4 uv2 : TEXCOORD2;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float3 normalWS;
             float4 tangentWS;
             float4 texCoord0;
             float3 viewDirectionWS;
            #if defined(LIGHTMAP_ON)
             float2 staticLightmapUV;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
             float2 dynamicLightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
             float3 sh;
            #endif
             float4 fogFactorAndVertexLight;
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
             float4 shadowCoord;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 TangentSpaceNormal;
             float3 WorldSpacePosition;
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 WorldSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 WorldSpaceTangent;
             float3 ObjectSpaceBiTangent;
             float3 WorldSpaceBiTangent;
             float3 ObjectSpacePosition;
             float3 WorldSpacePosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float3 interp0 : INTERP0;
             float3 interp1 : INTERP1;
             float4 interp2 : INTERP2;
             float4 interp3 : INTERP3;
             float3 interp4 : INTERP4;
             float2 interp5 : INTERP5;
             float2 interp6 : INTERP6;
             float3 interp7 : INTERP7;
             float4 interp8 : INTERP8;
             float4 interp9 : INTERP9;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.interp0.xyz =  input.positionWS;
            output.interp1.xyz =  input.normalWS;
            output.interp2.xyzw =  input.tangentWS;
            output.interp3.xyzw =  input.texCoord0;
            output.interp4.xyz =  input.viewDirectionWS;
            #if defined(LIGHTMAP_ON)
            output.interp5.xy =  input.staticLightmapUV;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
            output.interp6.xy =  input.dynamicLightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
            output.interp7.xyz =  input.sh;
            #endif
            output.interp8.xyzw =  input.fogFactorAndVertexLight;
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
            output.interp9.xyzw =  input.shadowCoord;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.positionWS = input.interp0.xyz;
            output.normalWS = input.interp1.xyz;
            output.tangentWS = input.interp2.xyzw;
            output.texCoord0 = input.interp3.xyzw;
            output.viewDirectionWS = input.interp4.xyz;
            #if defined(LIGHTMAP_ON)
            output.staticLightmapUV = input.interp5.xy;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
            output.dynamicLightmapUV = input.interp6.xy;
            #endif
            #if !defined(LIGHTMAP_ON)
            output.sh = input.interp7.xyz;
            #endif
            output.fogFactorAndVertexLight = input.interp8.xyzw;
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
            output.shadowCoord = input.interp9.xyzw;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 Texture2D_B18C2857_TexelSize;
        float4 Texture2D_E933B04B_TexelSize;
        float2 Vector2_EC93FC28;
        float Vector1_B7976139;
        float Vector1_C39A3E90;
        float4 Texture2D_C3D58803_TexelSize;
        float Alpha_Threshold;
        float _Normal_Strength;
        float _Luminosity;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(Texture2D_B18C2857);
        SAMPLER(samplerTexture2D_B18C2857);
        TEXTURE2D(Texture2D_E933B04B);
        SAMPLER(samplerTexture2D_E933B04B);
        TEXTURE2D(Texture2D_C3D58803);
        SAMPLER(samplerTexture2D_C3D58803);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        
        float2 Unity_GradientNoise_Dir_float(float2 p)
        {
            // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
            p = p % 289;
            // need full precision, otherwise half overflows when p > 1
            float x = float(34 * p.x + 1) * p.x % 289 + p.y;
            x = (34 * x + 1) * x % 289;
            x = frac(x / 41) * 2 - 1;
            return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
        }
        
        void Unity_GradientNoise_float(float2 UV, float Scale, out float Out)
        {
            float2 p = UV * Scale;
            float2 ip = floor(p);
            float2 fp = frac(p);
            float d00 = dot(Unity_GradientNoise_Dir_float(ip), fp);
            float d01 = dot(Unity_GradientNoise_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
            float d10 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
            float d11 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
            fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
            Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_DotProduct_float4(float4 A, float4 B, out float Out)
        {
            Out = dot(A, B);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        void Unity_Hue_Degrees_float(float3 In, float Offset, out float3 Out)
        {
            // RGB to HSV
            float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
            float4 P = lerp(float4(In.bg, K.wz), float4(In.gb, K.xy), step(In.b, In.g));
            float4 Q = lerp(float4(P.xyw, In.r), float4(In.r, P.yzx), step(P.x, In.r));
            float D = Q.x - min(Q.w, Q.y);
            float E = 1e-10;
            float V = (D == 0) ? Q.x : (Q.x + E);
            float3 hsv = float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), V);
        
            float hue = hsv.x + Offset / 360;
            hsv.x = (hue < 0)
                    ? hue + 1
                    : (hue > 1)
                        ? hue - 1
                        : hue;
        
            // HSV to RGB
            float4 K2 = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
            float3 P2 = abs(frac(hsv.xxx + K2.xyz) * 6.0 - K2.www);
            Out = hsv.z * lerp(K2.xxx, saturate(P2 - K2.xxx), hsv.y);
        }
        
        void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
        {
            Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float _Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0 = Vector1_C39A3E90;
            float2 _Property_51db9fe5f8707d8fa4507c481749556e_Out_0 = Vector2_EC93FC28;
            float2 _Multiply_d93244631121ab899bc52aade4ded114_Out_2;
            Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Property_51db9fe5f8707d8fa4507c481749556e_Out_0, _Multiply_d93244631121ab899bc52aade4ded114_Out_2);
            float2 _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3;
            Unity_TilingAndOffset_float((IN.WorldSpacePosition.xy), float2 (1, 1), _Multiply_d93244631121ab899bc52aade4ded114_Out_2, _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3);
            float _Property_a99d72da27ad61808450823e32ff058d_Out_0 = Vector1_B7976139;
            float _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2;
            Unity_GradientNoise_float(_TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3, _Property_a99d72da27ad61808450823e32ff058d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2);
            float _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2;
            Unity_Multiply_float_float(_Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2, _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2);
            UnityTexture2D _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_C3D58803);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.tex, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.samplerstate, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.GetTransformedUV(IN.uv0.xy), 0);
            #endif
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_R_5 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.r;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_G_6 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.g;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_B_7 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.b;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_A_8 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.a;
            float _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2;
            Unity_DotProduct_float4(_SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0, float4(0, 0.5, 0, 0), _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2);
            float _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2;
            Unity_Multiply_float_float(_Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2, _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2, _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2);
            float _Split_00fd5896a472d482b73269374a947f07_R_1 = IN.WorldSpacePosition[0];
            float _Split_00fd5896a472d482b73269374a947f07_G_2 = IN.WorldSpacePosition[1];
            float _Split_00fd5896a472d482b73269374a947f07_B_3 = IN.WorldSpacePosition[2];
            float _Split_00fd5896a472d482b73269374a947f07_A_4 = 0;
            float _Add_811329329cfbd18fb3e807f2cd027976_Out_2;
            Unity_Add_float(_Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2, _Split_00fd5896a472d482b73269374a947f07_R_1, _Add_811329329cfbd18fb3e807f2cd027976_Out_2);
            float4 _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4;
            float3 _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5;
            float2 _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6;
            Unity_Combine_float(_Add_811329329cfbd18fb3e807f2cd027976_Out_2, _Split_00fd5896a472d482b73269374a947f07_G_2, _Split_00fd5896a472d482b73269374a947f07_B_3, 0, _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4, _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5, _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6);
            float4 _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0 = IN.uv0;
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_R_1 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[0];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[1];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_B_3 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[2];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_A_4 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[3];
            float3 _Lerp_671d23c02bbda885b7de9901db258bec_Out_3;
            Unity_Lerp_float3(IN.WorldSpacePosition, (_Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4.xyz), (_Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2.xxx), _Lerp_671d23c02bbda885b7de9901db258bec_Out_3);
            float3 _Transform_5289292aa2111a8493f276a8d330172a_Out_1 = TransformWorldToObject(_Lerp_671d23c02bbda885b7de9901db258bec_Out_3.xyz);
            description.Position = _Transform_5289292aa2111a8493f276a8d330172a_Out_1;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float3 NormalTS;
            float3 Emission;
            float Metallic;
            float Smoothness;
            float Occlusion;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float _Property_dd9b23bb0e8d4e23828a2903b0e8f1bd_Out_0 = _Luminosity;
            UnityTexture2D _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_B18C2857);
            float4 _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0 = SAMPLE_TEXTURE2D(_Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.tex, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.samplerstate, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.GetTransformedUV(IN.uv0.xy));
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_R_4 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.r;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_G_5 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.g;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_B_6 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.b;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.a;
            float _GradientNoise_668f1ac4661940099bbb63949e9cf082_Out_2;
            Unity_GradientNoise_float((IN.WorldSpacePosition.xy), 3, _GradientNoise_668f1ac4661940099bbb63949e9cf082_Out_2);
            float _Add_bca95b13bbde4c8b8a8d5c3c5edbfeab_Out_2;
            Unity_Add_float(_GradientNoise_668f1ac4661940099bbb63949e9cf082_Out_2, 0.6, _Add_bca95b13bbde4c8b8a8d5c3c5edbfeab_Out_2);
            float4 _Multiply_9c21544a05f2400b8735e3a7f02361f4_Out_2;
            Unity_Multiply_float4_float4(_SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0, (_Add_bca95b13bbde4c8b8a8d5c3c5edbfeab_Out_2.xxxx), _Multiply_9c21544a05f2400b8735e3a7f02361f4_Out_2);
            float4 _Multiply_2528287665c24e6d81e3369b4ca73a11_Out_2;
            Unity_Multiply_float4_float4((_Property_dd9b23bb0e8d4e23828a2903b0e8f1bd_Out_0.xxxx), _Multiply_9c21544a05f2400b8735e3a7f02361f4_Out_2, _Multiply_2528287665c24e6d81e3369b4ca73a11_Out_2);
            float3 _Hue_e41f1877edfd4476b9a0d85c9616cac1_Out_2;
            Unity_Hue_Degrees_float((_Multiply_2528287665c24e6d81e3369b4ca73a11_Out_2.xyz), 5, _Hue_e41f1877edfd4476b9a0d85c9616cac1_Out_2);
            UnityTexture2D _Property_b48e111d2b6a488c93d88bd4f120c5f9_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_E933B04B);
            float4 _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0 = SAMPLE_TEXTURE2D(_Property_b48e111d2b6a488c93d88bd4f120c5f9_Out_0.tex, _Property_b48e111d2b6a488c93d88bd4f120c5f9_Out_0.samplerstate, _Property_b48e111d2b6a488c93d88bd4f120c5f9_Out_0.GetTransformedUV(IN.uv0.xy));
            _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.rgb = UnpackNormal(_SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0);
            float _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_R_4 = _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.r;
            float _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_G_5 = _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.g;
            float _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_B_6 = _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.b;
            float _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_A_7 = _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.a;
            float _Property_032c33beaac840d58b4e508ae522a70c_Out_0 = _Normal_Strength;
            float3 _NormalStrength_9b9e9bf8069a42888b650f5735fe4abd_Out_2;
            Unity_NormalStrength_float((_SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.xyz), _Property_032c33beaac840d58b4e508ae522a70c_Out_0, _NormalStrength_9b9e9bf8069a42888b650f5735fe4abd_Out_2);
            float _Property_258b7abc9b6247baa769a139aaecb429_Out_0 = Alpha_Threshold;
            surface.BaseColor = _Hue_e41f1877edfd4476b9a0d85c9616cac1_Out_2;
            surface.NormalTS = _NormalStrength_9b9e9bf8069a42888b650f5735fe4abd_Out_2;
            surface.Emission = float3(0, 0, 0);
            surface.Metallic = 0;
            surface.Smoothness = 0;
            surface.Occlusion = 1;
            surface.Alpha = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7;
            surface.AlphaClipThreshold = _Property_258b7abc9b6247baa769a139aaecb429_Out_0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.WorldSpaceTangent =                          TransformObjectToWorldDir(input.tangentOS.xyz);
            output.ObjectSpaceBiTangent =                       normalize(cross(input.normalOS, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
            output.WorldSpaceBiTangent =                        TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
            output.ObjectSpacePosition =                        input.positionOS;
            output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
            output.uv0 =                                        input.uv0;
            output.TimeParameters =                             _TimeParameters.xyz;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
            // FragInputs from VFX come from two places: Interpolator or CBuffer.
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
            output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
        
        
            output.WorldSpacePosition = input.positionWS;
            output.uv0 = input.texCoord0;
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "GBuffer"
            Tags
            {
                "LightMode" = "UniversalGBuffer"
            }
        
        // Render State
        Cull Off
        Blend One Zero
        ZTest LEqual
        ZWrite On
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 4.5
        #pragma exclude_renderers gles gles3 glcore
        #pragma multi_compile_instancing
        #pragma multi_compile_fog
        #pragma instancing_options renderinglayer
        #pragma multi_compile _ DOTS_INSTANCING_ON
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        #pragma multi_compile _ LIGHTMAP_ON
        #pragma multi_compile _ DYNAMICLIGHTMAP_ON
        #pragma multi_compile _ DIRLIGHTMAP_COMBINED
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
        #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
        #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
        #pragma multi_compile_fragment _ _SHADOWS_SOFT
        #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
        #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
        #pragma multi_compile _ SHADOWS_SHADOWMASK
        #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
        #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
        #pragma multi_compile_fragment _ _LIGHT_LAYERS
        #pragma multi_compile_fragment _ _RENDER_PASS_ENABLED
        #pragma multi_compile_fragment _ DEBUG_DISPLAY
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define ATTRIBUTES_NEED_TEXCOORD2
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TANGENT_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define VARYINGS_NEED_VIEWDIRECTION_WS
        #define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
        #define VARYINGS_NEED_SHADOW_COORD
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_GBUFFER
        #define _FOG_FRAGMENT 1
        #define _ALPHATEST_ON 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
             float4 uv1 : TEXCOORD1;
             float4 uv2 : TEXCOORD2;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float3 normalWS;
             float4 tangentWS;
             float4 texCoord0;
             float3 viewDirectionWS;
            #if defined(LIGHTMAP_ON)
             float2 staticLightmapUV;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
             float2 dynamicLightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
             float3 sh;
            #endif
             float4 fogFactorAndVertexLight;
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
             float4 shadowCoord;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 TangentSpaceNormal;
             float3 WorldSpacePosition;
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 WorldSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 WorldSpaceTangent;
             float3 ObjectSpaceBiTangent;
             float3 WorldSpaceBiTangent;
             float3 ObjectSpacePosition;
             float3 WorldSpacePosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float3 interp0 : INTERP0;
             float3 interp1 : INTERP1;
             float4 interp2 : INTERP2;
             float4 interp3 : INTERP3;
             float3 interp4 : INTERP4;
             float2 interp5 : INTERP5;
             float2 interp6 : INTERP6;
             float3 interp7 : INTERP7;
             float4 interp8 : INTERP8;
             float4 interp9 : INTERP9;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.interp0.xyz =  input.positionWS;
            output.interp1.xyz =  input.normalWS;
            output.interp2.xyzw =  input.tangentWS;
            output.interp3.xyzw =  input.texCoord0;
            output.interp4.xyz =  input.viewDirectionWS;
            #if defined(LIGHTMAP_ON)
            output.interp5.xy =  input.staticLightmapUV;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
            output.interp6.xy =  input.dynamicLightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
            output.interp7.xyz =  input.sh;
            #endif
            output.interp8.xyzw =  input.fogFactorAndVertexLight;
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
            output.interp9.xyzw =  input.shadowCoord;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.positionWS = input.interp0.xyz;
            output.normalWS = input.interp1.xyz;
            output.tangentWS = input.interp2.xyzw;
            output.texCoord0 = input.interp3.xyzw;
            output.viewDirectionWS = input.interp4.xyz;
            #if defined(LIGHTMAP_ON)
            output.staticLightmapUV = input.interp5.xy;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
            output.dynamicLightmapUV = input.interp6.xy;
            #endif
            #if !defined(LIGHTMAP_ON)
            output.sh = input.interp7.xyz;
            #endif
            output.fogFactorAndVertexLight = input.interp8.xyzw;
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
            output.shadowCoord = input.interp9.xyzw;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 Texture2D_B18C2857_TexelSize;
        float4 Texture2D_E933B04B_TexelSize;
        float2 Vector2_EC93FC28;
        float Vector1_B7976139;
        float Vector1_C39A3E90;
        float4 Texture2D_C3D58803_TexelSize;
        float Alpha_Threshold;
        float _Normal_Strength;
        float _Luminosity;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(Texture2D_B18C2857);
        SAMPLER(samplerTexture2D_B18C2857);
        TEXTURE2D(Texture2D_E933B04B);
        SAMPLER(samplerTexture2D_E933B04B);
        TEXTURE2D(Texture2D_C3D58803);
        SAMPLER(samplerTexture2D_C3D58803);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        
        float2 Unity_GradientNoise_Dir_float(float2 p)
        {
            // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
            p = p % 289;
            // need full precision, otherwise half overflows when p > 1
            float x = float(34 * p.x + 1) * p.x % 289 + p.y;
            x = (34 * x + 1) * x % 289;
            x = frac(x / 41) * 2 - 1;
            return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
        }
        
        void Unity_GradientNoise_float(float2 UV, float Scale, out float Out)
        {
            float2 p = UV * Scale;
            float2 ip = floor(p);
            float2 fp = frac(p);
            float d00 = dot(Unity_GradientNoise_Dir_float(ip), fp);
            float d01 = dot(Unity_GradientNoise_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
            float d10 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
            float d11 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
            fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
            Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_DotProduct_float4(float4 A, float4 B, out float Out)
        {
            Out = dot(A, B);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        void Unity_Hue_Degrees_float(float3 In, float Offset, out float3 Out)
        {
            // RGB to HSV
            float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
            float4 P = lerp(float4(In.bg, K.wz), float4(In.gb, K.xy), step(In.b, In.g));
            float4 Q = lerp(float4(P.xyw, In.r), float4(In.r, P.yzx), step(P.x, In.r));
            float D = Q.x - min(Q.w, Q.y);
            float E = 1e-10;
            float V = (D == 0) ? Q.x : (Q.x + E);
            float3 hsv = float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), V);
        
            float hue = hsv.x + Offset / 360;
            hsv.x = (hue < 0)
                    ? hue + 1
                    : (hue > 1)
                        ? hue - 1
                        : hue;
        
            // HSV to RGB
            float4 K2 = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
            float3 P2 = abs(frac(hsv.xxx + K2.xyz) * 6.0 - K2.www);
            Out = hsv.z * lerp(K2.xxx, saturate(P2 - K2.xxx), hsv.y);
        }
        
        void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
        {
            Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float _Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0 = Vector1_C39A3E90;
            float2 _Property_51db9fe5f8707d8fa4507c481749556e_Out_0 = Vector2_EC93FC28;
            float2 _Multiply_d93244631121ab899bc52aade4ded114_Out_2;
            Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Property_51db9fe5f8707d8fa4507c481749556e_Out_0, _Multiply_d93244631121ab899bc52aade4ded114_Out_2);
            float2 _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3;
            Unity_TilingAndOffset_float((IN.WorldSpacePosition.xy), float2 (1, 1), _Multiply_d93244631121ab899bc52aade4ded114_Out_2, _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3);
            float _Property_a99d72da27ad61808450823e32ff058d_Out_0 = Vector1_B7976139;
            float _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2;
            Unity_GradientNoise_float(_TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3, _Property_a99d72da27ad61808450823e32ff058d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2);
            float _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2;
            Unity_Multiply_float_float(_Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2, _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2);
            UnityTexture2D _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_C3D58803);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.tex, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.samplerstate, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.GetTransformedUV(IN.uv0.xy), 0);
            #endif
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_R_5 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.r;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_G_6 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.g;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_B_7 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.b;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_A_8 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.a;
            float _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2;
            Unity_DotProduct_float4(_SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0, float4(0, 0.5, 0, 0), _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2);
            float _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2;
            Unity_Multiply_float_float(_Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2, _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2, _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2);
            float _Split_00fd5896a472d482b73269374a947f07_R_1 = IN.WorldSpacePosition[0];
            float _Split_00fd5896a472d482b73269374a947f07_G_2 = IN.WorldSpacePosition[1];
            float _Split_00fd5896a472d482b73269374a947f07_B_3 = IN.WorldSpacePosition[2];
            float _Split_00fd5896a472d482b73269374a947f07_A_4 = 0;
            float _Add_811329329cfbd18fb3e807f2cd027976_Out_2;
            Unity_Add_float(_Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2, _Split_00fd5896a472d482b73269374a947f07_R_1, _Add_811329329cfbd18fb3e807f2cd027976_Out_2);
            float4 _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4;
            float3 _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5;
            float2 _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6;
            Unity_Combine_float(_Add_811329329cfbd18fb3e807f2cd027976_Out_2, _Split_00fd5896a472d482b73269374a947f07_G_2, _Split_00fd5896a472d482b73269374a947f07_B_3, 0, _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4, _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5, _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6);
            float4 _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0 = IN.uv0;
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_R_1 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[0];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[1];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_B_3 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[2];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_A_4 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[3];
            float3 _Lerp_671d23c02bbda885b7de9901db258bec_Out_3;
            Unity_Lerp_float3(IN.WorldSpacePosition, (_Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4.xyz), (_Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2.xxx), _Lerp_671d23c02bbda885b7de9901db258bec_Out_3);
            float3 _Transform_5289292aa2111a8493f276a8d330172a_Out_1 = TransformWorldToObject(_Lerp_671d23c02bbda885b7de9901db258bec_Out_3.xyz);
            description.Position = _Transform_5289292aa2111a8493f276a8d330172a_Out_1;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float3 NormalTS;
            float3 Emission;
            float Metallic;
            float Smoothness;
            float Occlusion;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float _Property_dd9b23bb0e8d4e23828a2903b0e8f1bd_Out_0 = _Luminosity;
            UnityTexture2D _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_B18C2857);
            float4 _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0 = SAMPLE_TEXTURE2D(_Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.tex, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.samplerstate, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.GetTransformedUV(IN.uv0.xy));
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_R_4 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.r;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_G_5 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.g;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_B_6 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.b;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.a;
            float _GradientNoise_668f1ac4661940099bbb63949e9cf082_Out_2;
            Unity_GradientNoise_float((IN.WorldSpacePosition.xy), 3, _GradientNoise_668f1ac4661940099bbb63949e9cf082_Out_2);
            float _Add_bca95b13bbde4c8b8a8d5c3c5edbfeab_Out_2;
            Unity_Add_float(_GradientNoise_668f1ac4661940099bbb63949e9cf082_Out_2, 0.6, _Add_bca95b13bbde4c8b8a8d5c3c5edbfeab_Out_2);
            float4 _Multiply_9c21544a05f2400b8735e3a7f02361f4_Out_2;
            Unity_Multiply_float4_float4(_SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0, (_Add_bca95b13bbde4c8b8a8d5c3c5edbfeab_Out_2.xxxx), _Multiply_9c21544a05f2400b8735e3a7f02361f4_Out_2);
            float4 _Multiply_2528287665c24e6d81e3369b4ca73a11_Out_2;
            Unity_Multiply_float4_float4((_Property_dd9b23bb0e8d4e23828a2903b0e8f1bd_Out_0.xxxx), _Multiply_9c21544a05f2400b8735e3a7f02361f4_Out_2, _Multiply_2528287665c24e6d81e3369b4ca73a11_Out_2);
            float3 _Hue_e41f1877edfd4476b9a0d85c9616cac1_Out_2;
            Unity_Hue_Degrees_float((_Multiply_2528287665c24e6d81e3369b4ca73a11_Out_2.xyz), 5, _Hue_e41f1877edfd4476b9a0d85c9616cac1_Out_2);
            UnityTexture2D _Property_b48e111d2b6a488c93d88bd4f120c5f9_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_E933B04B);
            float4 _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0 = SAMPLE_TEXTURE2D(_Property_b48e111d2b6a488c93d88bd4f120c5f9_Out_0.tex, _Property_b48e111d2b6a488c93d88bd4f120c5f9_Out_0.samplerstate, _Property_b48e111d2b6a488c93d88bd4f120c5f9_Out_0.GetTransformedUV(IN.uv0.xy));
            _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.rgb = UnpackNormal(_SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0);
            float _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_R_4 = _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.r;
            float _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_G_5 = _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.g;
            float _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_B_6 = _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.b;
            float _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_A_7 = _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.a;
            float _Property_032c33beaac840d58b4e508ae522a70c_Out_0 = _Normal_Strength;
            float3 _NormalStrength_9b9e9bf8069a42888b650f5735fe4abd_Out_2;
            Unity_NormalStrength_float((_SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.xyz), _Property_032c33beaac840d58b4e508ae522a70c_Out_0, _NormalStrength_9b9e9bf8069a42888b650f5735fe4abd_Out_2);
            float _Property_258b7abc9b6247baa769a139aaecb429_Out_0 = Alpha_Threshold;
            surface.BaseColor = _Hue_e41f1877edfd4476b9a0d85c9616cac1_Out_2;
            surface.NormalTS = _NormalStrength_9b9e9bf8069a42888b650f5735fe4abd_Out_2;
            surface.Emission = float3(0, 0, 0);
            surface.Metallic = 0;
            surface.Smoothness = 0;
            surface.Occlusion = 1;
            surface.Alpha = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7;
            surface.AlphaClipThreshold = _Property_258b7abc9b6247baa769a139aaecb429_Out_0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.WorldSpaceTangent =                          TransformObjectToWorldDir(input.tangentOS.xyz);
            output.ObjectSpaceBiTangent =                       normalize(cross(input.normalOS, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
            output.WorldSpaceBiTangent =                        TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
            output.ObjectSpacePosition =                        input.positionOS;
            output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
            output.uv0 =                                        input.uv0;
            output.TimeParameters =                             _TimeParameters.xyz;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
            // FragInputs from VFX come from two places: Interpolator or CBuffer.
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
            output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
        
        
            output.WorldSpacePosition = input.positionWS;
            output.uv0 = input.texCoord0;
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRGBufferPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
        
        // Render State
        Cull Off
        ZTest LEqual
        ZWrite On
        ColorMask 0
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 4.5
        #pragma exclude_renderers gles gles3 glcore
        #pragma multi_compile_instancing
        #pragma multi_compile _ DOTS_INSTANCING_ON
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_SHADOWCASTER
        #define _ALPHATEST_ON 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 normalWS;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 WorldSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 WorldSpaceTangent;
             float3 ObjectSpaceBiTangent;
             float3 WorldSpaceBiTangent;
             float3 ObjectSpacePosition;
             float3 WorldSpacePosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float3 interp0 : INTERP0;
             float4 interp1 : INTERP1;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.interp0.xyz =  input.normalWS;
            output.interp1.xyzw =  input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.normalWS = input.interp0.xyz;
            output.texCoord0 = input.interp1.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 Texture2D_B18C2857_TexelSize;
        float4 Texture2D_E933B04B_TexelSize;
        float2 Vector2_EC93FC28;
        float Vector1_B7976139;
        float Vector1_C39A3E90;
        float4 Texture2D_C3D58803_TexelSize;
        float Alpha_Threshold;
        float _Normal_Strength;
        float _Luminosity;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(Texture2D_B18C2857);
        SAMPLER(samplerTexture2D_B18C2857);
        TEXTURE2D(Texture2D_E933B04B);
        SAMPLER(samplerTexture2D_E933B04B);
        TEXTURE2D(Texture2D_C3D58803);
        SAMPLER(samplerTexture2D_C3D58803);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        
        float2 Unity_GradientNoise_Dir_float(float2 p)
        {
            // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
            p = p % 289;
            // need full precision, otherwise half overflows when p > 1
            float x = float(34 * p.x + 1) * p.x % 289 + p.y;
            x = (34 * x + 1) * x % 289;
            x = frac(x / 41) * 2 - 1;
            return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
        }
        
        void Unity_GradientNoise_float(float2 UV, float Scale, out float Out)
        {
            float2 p = UV * Scale;
            float2 ip = floor(p);
            float2 fp = frac(p);
            float d00 = dot(Unity_GradientNoise_Dir_float(ip), fp);
            float d01 = dot(Unity_GradientNoise_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
            float d10 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
            float d11 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
            fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
            Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_DotProduct_float4(float4 A, float4 B, out float Out)
        {
            Out = dot(A, B);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float _Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0 = Vector1_C39A3E90;
            float2 _Property_51db9fe5f8707d8fa4507c481749556e_Out_0 = Vector2_EC93FC28;
            float2 _Multiply_d93244631121ab899bc52aade4ded114_Out_2;
            Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Property_51db9fe5f8707d8fa4507c481749556e_Out_0, _Multiply_d93244631121ab899bc52aade4ded114_Out_2);
            float2 _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3;
            Unity_TilingAndOffset_float((IN.WorldSpacePosition.xy), float2 (1, 1), _Multiply_d93244631121ab899bc52aade4ded114_Out_2, _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3);
            float _Property_a99d72da27ad61808450823e32ff058d_Out_0 = Vector1_B7976139;
            float _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2;
            Unity_GradientNoise_float(_TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3, _Property_a99d72da27ad61808450823e32ff058d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2);
            float _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2;
            Unity_Multiply_float_float(_Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2, _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2);
            UnityTexture2D _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_C3D58803);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.tex, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.samplerstate, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.GetTransformedUV(IN.uv0.xy), 0);
            #endif
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_R_5 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.r;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_G_6 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.g;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_B_7 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.b;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_A_8 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.a;
            float _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2;
            Unity_DotProduct_float4(_SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0, float4(0, 0.5, 0, 0), _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2);
            float _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2;
            Unity_Multiply_float_float(_Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2, _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2, _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2);
            float _Split_00fd5896a472d482b73269374a947f07_R_1 = IN.WorldSpacePosition[0];
            float _Split_00fd5896a472d482b73269374a947f07_G_2 = IN.WorldSpacePosition[1];
            float _Split_00fd5896a472d482b73269374a947f07_B_3 = IN.WorldSpacePosition[2];
            float _Split_00fd5896a472d482b73269374a947f07_A_4 = 0;
            float _Add_811329329cfbd18fb3e807f2cd027976_Out_2;
            Unity_Add_float(_Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2, _Split_00fd5896a472d482b73269374a947f07_R_1, _Add_811329329cfbd18fb3e807f2cd027976_Out_2);
            float4 _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4;
            float3 _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5;
            float2 _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6;
            Unity_Combine_float(_Add_811329329cfbd18fb3e807f2cd027976_Out_2, _Split_00fd5896a472d482b73269374a947f07_G_2, _Split_00fd5896a472d482b73269374a947f07_B_3, 0, _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4, _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5, _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6);
            float4 _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0 = IN.uv0;
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_R_1 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[0];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[1];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_B_3 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[2];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_A_4 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[3];
            float3 _Lerp_671d23c02bbda885b7de9901db258bec_Out_3;
            Unity_Lerp_float3(IN.WorldSpacePosition, (_Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4.xyz), (_Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2.xxx), _Lerp_671d23c02bbda885b7de9901db258bec_Out_3);
            float3 _Transform_5289292aa2111a8493f276a8d330172a_Out_1 = TransformWorldToObject(_Lerp_671d23c02bbda885b7de9901db258bec_Out_3.xyz);
            description.Position = _Transform_5289292aa2111a8493f276a8d330172a_Out_1;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_B18C2857);
            float4 _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0 = SAMPLE_TEXTURE2D(_Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.tex, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.samplerstate, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.GetTransformedUV(IN.uv0.xy));
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_R_4 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.r;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_G_5 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.g;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_B_6 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.b;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.a;
            float _Property_258b7abc9b6247baa769a139aaecb429_Out_0 = Alpha_Threshold;
            surface.Alpha = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7;
            surface.AlphaClipThreshold = _Property_258b7abc9b6247baa769a139aaecb429_Out_0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.WorldSpaceTangent =                          TransformObjectToWorldDir(input.tangentOS.xyz);
            output.ObjectSpaceBiTangent =                       normalize(cross(input.normalOS, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
            output.WorldSpaceBiTangent =                        TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
            output.ObjectSpacePosition =                        input.positionOS;
            output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
            output.uv0 =                                        input.uv0;
            output.TimeParameters =                             _TimeParameters.xyz;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
            // FragInputs from VFX come from two places: Interpolator or CBuffer.
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
            output.uv0 = input.texCoord0;
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }
        
        // Render State
        Cull Off
        ZTest LEqual
        ZWrite On
        ColorMask 0
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 4.5
        #pragma exclude_renderers gles gles3 glcore
        #pragma multi_compile_instancing
        #pragma multi_compile _ DOTS_INSTANCING_ON
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHONLY
        #define _ALPHATEST_ON 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 WorldSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 WorldSpaceTangent;
             float3 ObjectSpaceBiTangent;
             float3 WorldSpaceBiTangent;
             float3 ObjectSpacePosition;
             float3 WorldSpacePosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 interp0 : INTERP0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.interp0.xyzw =  input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.interp0.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 Texture2D_B18C2857_TexelSize;
        float4 Texture2D_E933B04B_TexelSize;
        float2 Vector2_EC93FC28;
        float Vector1_B7976139;
        float Vector1_C39A3E90;
        float4 Texture2D_C3D58803_TexelSize;
        float Alpha_Threshold;
        float _Normal_Strength;
        float _Luminosity;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(Texture2D_B18C2857);
        SAMPLER(samplerTexture2D_B18C2857);
        TEXTURE2D(Texture2D_E933B04B);
        SAMPLER(samplerTexture2D_E933B04B);
        TEXTURE2D(Texture2D_C3D58803);
        SAMPLER(samplerTexture2D_C3D58803);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        
        float2 Unity_GradientNoise_Dir_float(float2 p)
        {
            // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
            p = p % 289;
            // need full precision, otherwise half overflows when p > 1
            float x = float(34 * p.x + 1) * p.x % 289 + p.y;
            x = (34 * x + 1) * x % 289;
            x = frac(x / 41) * 2 - 1;
            return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
        }
        
        void Unity_GradientNoise_float(float2 UV, float Scale, out float Out)
        {
            float2 p = UV * Scale;
            float2 ip = floor(p);
            float2 fp = frac(p);
            float d00 = dot(Unity_GradientNoise_Dir_float(ip), fp);
            float d01 = dot(Unity_GradientNoise_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
            float d10 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
            float d11 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
            fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
            Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_DotProduct_float4(float4 A, float4 B, out float Out)
        {
            Out = dot(A, B);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float _Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0 = Vector1_C39A3E90;
            float2 _Property_51db9fe5f8707d8fa4507c481749556e_Out_0 = Vector2_EC93FC28;
            float2 _Multiply_d93244631121ab899bc52aade4ded114_Out_2;
            Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Property_51db9fe5f8707d8fa4507c481749556e_Out_0, _Multiply_d93244631121ab899bc52aade4ded114_Out_2);
            float2 _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3;
            Unity_TilingAndOffset_float((IN.WorldSpacePosition.xy), float2 (1, 1), _Multiply_d93244631121ab899bc52aade4ded114_Out_2, _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3);
            float _Property_a99d72da27ad61808450823e32ff058d_Out_0 = Vector1_B7976139;
            float _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2;
            Unity_GradientNoise_float(_TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3, _Property_a99d72da27ad61808450823e32ff058d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2);
            float _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2;
            Unity_Multiply_float_float(_Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2, _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2);
            UnityTexture2D _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_C3D58803);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.tex, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.samplerstate, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.GetTransformedUV(IN.uv0.xy), 0);
            #endif
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_R_5 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.r;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_G_6 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.g;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_B_7 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.b;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_A_8 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.a;
            float _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2;
            Unity_DotProduct_float4(_SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0, float4(0, 0.5, 0, 0), _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2);
            float _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2;
            Unity_Multiply_float_float(_Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2, _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2, _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2);
            float _Split_00fd5896a472d482b73269374a947f07_R_1 = IN.WorldSpacePosition[0];
            float _Split_00fd5896a472d482b73269374a947f07_G_2 = IN.WorldSpacePosition[1];
            float _Split_00fd5896a472d482b73269374a947f07_B_3 = IN.WorldSpacePosition[2];
            float _Split_00fd5896a472d482b73269374a947f07_A_4 = 0;
            float _Add_811329329cfbd18fb3e807f2cd027976_Out_2;
            Unity_Add_float(_Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2, _Split_00fd5896a472d482b73269374a947f07_R_1, _Add_811329329cfbd18fb3e807f2cd027976_Out_2);
            float4 _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4;
            float3 _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5;
            float2 _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6;
            Unity_Combine_float(_Add_811329329cfbd18fb3e807f2cd027976_Out_2, _Split_00fd5896a472d482b73269374a947f07_G_2, _Split_00fd5896a472d482b73269374a947f07_B_3, 0, _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4, _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5, _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6);
            float4 _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0 = IN.uv0;
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_R_1 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[0];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[1];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_B_3 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[2];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_A_4 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[3];
            float3 _Lerp_671d23c02bbda885b7de9901db258bec_Out_3;
            Unity_Lerp_float3(IN.WorldSpacePosition, (_Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4.xyz), (_Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2.xxx), _Lerp_671d23c02bbda885b7de9901db258bec_Out_3);
            float3 _Transform_5289292aa2111a8493f276a8d330172a_Out_1 = TransformWorldToObject(_Lerp_671d23c02bbda885b7de9901db258bec_Out_3.xyz);
            description.Position = _Transform_5289292aa2111a8493f276a8d330172a_Out_1;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_B18C2857);
            float4 _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0 = SAMPLE_TEXTURE2D(_Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.tex, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.samplerstate, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.GetTransformedUV(IN.uv0.xy));
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_R_4 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.r;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_G_5 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.g;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_B_6 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.b;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.a;
            float _Property_258b7abc9b6247baa769a139aaecb429_Out_0 = Alpha_Threshold;
            surface.Alpha = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7;
            surface.AlphaClipThreshold = _Property_258b7abc9b6247baa769a139aaecb429_Out_0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.WorldSpaceTangent =                          TransformObjectToWorldDir(input.tangentOS.xyz);
            output.ObjectSpaceBiTangent =                       normalize(cross(input.normalOS, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
            output.WorldSpaceBiTangent =                        TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
            output.ObjectSpacePosition =                        input.positionOS;
            output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
            output.uv0 =                                        input.uv0;
            output.TimeParameters =                             _TimeParameters.xyz;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
            // FragInputs from VFX come from two places: Interpolator or CBuffer.
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
            output.uv0 = input.texCoord0;
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }
        
        // Render State
        Cull Off
        ZTest LEqual
        ZWrite On
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 4.5
        #pragma exclude_renderers gles gles3 glcore
        #pragma multi_compile_instancing
        #pragma multi_compile _ DOTS_INSTANCING_ON
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TANGENT_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHNORMALS
        #define _ALPHATEST_ON 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
             float4 uv1 : TEXCOORD1;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 normalWS;
             float4 tangentWS;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 TangentSpaceNormal;
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 WorldSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 WorldSpaceTangent;
             float3 ObjectSpaceBiTangent;
             float3 WorldSpaceBiTangent;
             float3 ObjectSpacePosition;
             float3 WorldSpacePosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float3 interp0 : INTERP0;
             float4 interp1 : INTERP1;
             float4 interp2 : INTERP2;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.interp0.xyz =  input.normalWS;
            output.interp1.xyzw =  input.tangentWS;
            output.interp2.xyzw =  input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.normalWS = input.interp0.xyz;
            output.tangentWS = input.interp1.xyzw;
            output.texCoord0 = input.interp2.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 Texture2D_B18C2857_TexelSize;
        float4 Texture2D_E933B04B_TexelSize;
        float2 Vector2_EC93FC28;
        float Vector1_B7976139;
        float Vector1_C39A3E90;
        float4 Texture2D_C3D58803_TexelSize;
        float Alpha_Threshold;
        float _Normal_Strength;
        float _Luminosity;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(Texture2D_B18C2857);
        SAMPLER(samplerTexture2D_B18C2857);
        TEXTURE2D(Texture2D_E933B04B);
        SAMPLER(samplerTexture2D_E933B04B);
        TEXTURE2D(Texture2D_C3D58803);
        SAMPLER(samplerTexture2D_C3D58803);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        
        float2 Unity_GradientNoise_Dir_float(float2 p)
        {
            // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
            p = p % 289;
            // need full precision, otherwise half overflows when p > 1
            float x = float(34 * p.x + 1) * p.x % 289 + p.y;
            x = (34 * x + 1) * x % 289;
            x = frac(x / 41) * 2 - 1;
            return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
        }
        
        void Unity_GradientNoise_float(float2 UV, float Scale, out float Out)
        {
            float2 p = UV * Scale;
            float2 ip = floor(p);
            float2 fp = frac(p);
            float d00 = dot(Unity_GradientNoise_Dir_float(ip), fp);
            float d01 = dot(Unity_GradientNoise_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
            float d10 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
            float d11 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
            fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
            Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_DotProduct_float4(float4 A, float4 B, out float Out)
        {
            Out = dot(A, B);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
        {
            Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float _Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0 = Vector1_C39A3E90;
            float2 _Property_51db9fe5f8707d8fa4507c481749556e_Out_0 = Vector2_EC93FC28;
            float2 _Multiply_d93244631121ab899bc52aade4ded114_Out_2;
            Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Property_51db9fe5f8707d8fa4507c481749556e_Out_0, _Multiply_d93244631121ab899bc52aade4ded114_Out_2);
            float2 _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3;
            Unity_TilingAndOffset_float((IN.WorldSpacePosition.xy), float2 (1, 1), _Multiply_d93244631121ab899bc52aade4ded114_Out_2, _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3);
            float _Property_a99d72da27ad61808450823e32ff058d_Out_0 = Vector1_B7976139;
            float _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2;
            Unity_GradientNoise_float(_TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3, _Property_a99d72da27ad61808450823e32ff058d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2);
            float _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2;
            Unity_Multiply_float_float(_Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2, _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2);
            UnityTexture2D _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_C3D58803);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.tex, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.samplerstate, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.GetTransformedUV(IN.uv0.xy), 0);
            #endif
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_R_5 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.r;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_G_6 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.g;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_B_7 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.b;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_A_8 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.a;
            float _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2;
            Unity_DotProduct_float4(_SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0, float4(0, 0.5, 0, 0), _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2);
            float _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2;
            Unity_Multiply_float_float(_Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2, _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2, _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2);
            float _Split_00fd5896a472d482b73269374a947f07_R_1 = IN.WorldSpacePosition[0];
            float _Split_00fd5896a472d482b73269374a947f07_G_2 = IN.WorldSpacePosition[1];
            float _Split_00fd5896a472d482b73269374a947f07_B_3 = IN.WorldSpacePosition[2];
            float _Split_00fd5896a472d482b73269374a947f07_A_4 = 0;
            float _Add_811329329cfbd18fb3e807f2cd027976_Out_2;
            Unity_Add_float(_Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2, _Split_00fd5896a472d482b73269374a947f07_R_1, _Add_811329329cfbd18fb3e807f2cd027976_Out_2);
            float4 _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4;
            float3 _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5;
            float2 _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6;
            Unity_Combine_float(_Add_811329329cfbd18fb3e807f2cd027976_Out_2, _Split_00fd5896a472d482b73269374a947f07_G_2, _Split_00fd5896a472d482b73269374a947f07_B_3, 0, _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4, _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5, _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6);
            float4 _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0 = IN.uv0;
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_R_1 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[0];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[1];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_B_3 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[2];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_A_4 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[3];
            float3 _Lerp_671d23c02bbda885b7de9901db258bec_Out_3;
            Unity_Lerp_float3(IN.WorldSpacePosition, (_Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4.xyz), (_Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2.xxx), _Lerp_671d23c02bbda885b7de9901db258bec_Out_3);
            float3 _Transform_5289292aa2111a8493f276a8d330172a_Out_1 = TransformWorldToObject(_Lerp_671d23c02bbda885b7de9901db258bec_Out_3.xyz);
            description.Position = _Transform_5289292aa2111a8493f276a8d330172a_Out_1;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 NormalTS;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_b48e111d2b6a488c93d88bd4f120c5f9_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_E933B04B);
            float4 _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0 = SAMPLE_TEXTURE2D(_Property_b48e111d2b6a488c93d88bd4f120c5f9_Out_0.tex, _Property_b48e111d2b6a488c93d88bd4f120c5f9_Out_0.samplerstate, _Property_b48e111d2b6a488c93d88bd4f120c5f9_Out_0.GetTransformedUV(IN.uv0.xy));
            _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.rgb = UnpackNormal(_SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0);
            float _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_R_4 = _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.r;
            float _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_G_5 = _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.g;
            float _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_B_6 = _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.b;
            float _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_A_7 = _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.a;
            float _Property_032c33beaac840d58b4e508ae522a70c_Out_0 = _Normal_Strength;
            float3 _NormalStrength_9b9e9bf8069a42888b650f5735fe4abd_Out_2;
            Unity_NormalStrength_float((_SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.xyz), _Property_032c33beaac840d58b4e508ae522a70c_Out_0, _NormalStrength_9b9e9bf8069a42888b650f5735fe4abd_Out_2);
            UnityTexture2D _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_B18C2857);
            float4 _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0 = SAMPLE_TEXTURE2D(_Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.tex, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.samplerstate, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.GetTransformedUV(IN.uv0.xy));
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_R_4 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.r;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_G_5 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.g;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_B_6 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.b;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.a;
            float _Property_258b7abc9b6247baa769a139aaecb429_Out_0 = Alpha_Threshold;
            surface.NormalTS = _NormalStrength_9b9e9bf8069a42888b650f5735fe4abd_Out_2;
            surface.Alpha = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7;
            surface.AlphaClipThreshold = _Property_258b7abc9b6247baa769a139aaecb429_Out_0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.WorldSpaceTangent =                          TransformObjectToWorldDir(input.tangentOS.xyz);
            output.ObjectSpaceBiTangent =                       normalize(cross(input.normalOS, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
            output.WorldSpaceBiTangent =                        TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
            output.ObjectSpacePosition =                        input.positionOS;
            output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
            output.uv0 =                                        input.uv0;
            output.TimeParameters =                             _TimeParameters.xyz;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
            // FragInputs from VFX come from two places: Interpolator or CBuffer.
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
            output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
        
        
            output.uv0 = input.texCoord0;
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthNormalsOnlyPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "Meta"
            Tags
            {
                "LightMode" = "Meta"
            }
        
        // Render State
        Cull Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 4.5
        #pragma exclude_renderers gles gles3 glcore
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        #pragma shader_feature _ EDITOR_VISUALIZATION
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define ATTRIBUTES_NEED_TEXCOORD2
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define VARYINGS_NEED_TEXCOORD1
        #define VARYINGS_NEED_TEXCOORD2
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_META
        #define _FOG_FRAGMENT 1
        #define _ALPHATEST_ON 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
             float4 uv1 : TEXCOORD1;
             float4 uv2 : TEXCOORD2;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float4 texCoord0;
             float4 texCoord1;
             float4 texCoord2;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 WorldSpacePosition;
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 WorldSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 WorldSpaceTangent;
             float3 ObjectSpaceBiTangent;
             float3 WorldSpaceBiTangent;
             float3 ObjectSpacePosition;
             float3 WorldSpacePosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float3 interp0 : INTERP0;
             float4 interp1 : INTERP1;
             float4 interp2 : INTERP2;
             float4 interp3 : INTERP3;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.interp0.xyz =  input.positionWS;
            output.interp1.xyzw =  input.texCoord0;
            output.interp2.xyzw =  input.texCoord1;
            output.interp3.xyzw =  input.texCoord2;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.positionWS = input.interp0.xyz;
            output.texCoord0 = input.interp1.xyzw;
            output.texCoord1 = input.interp2.xyzw;
            output.texCoord2 = input.interp3.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 Texture2D_B18C2857_TexelSize;
        float4 Texture2D_E933B04B_TexelSize;
        float2 Vector2_EC93FC28;
        float Vector1_B7976139;
        float Vector1_C39A3E90;
        float4 Texture2D_C3D58803_TexelSize;
        float Alpha_Threshold;
        float _Normal_Strength;
        float _Luminosity;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(Texture2D_B18C2857);
        SAMPLER(samplerTexture2D_B18C2857);
        TEXTURE2D(Texture2D_E933B04B);
        SAMPLER(samplerTexture2D_E933B04B);
        TEXTURE2D(Texture2D_C3D58803);
        SAMPLER(samplerTexture2D_C3D58803);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        
        float2 Unity_GradientNoise_Dir_float(float2 p)
        {
            // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
            p = p % 289;
            // need full precision, otherwise half overflows when p > 1
            float x = float(34 * p.x + 1) * p.x % 289 + p.y;
            x = (34 * x + 1) * x % 289;
            x = frac(x / 41) * 2 - 1;
            return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
        }
        
        void Unity_GradientNoise_float(float2 UV, float Scale, out float Out)
        {
            float2 p = UV * Scale;
            float2 ip = floor(p);
            float2 fp = frac(p);
            float d00 = dot(Unity_GradientNoise_Dir_float(ip), fp);
            float d01 = dot(Unity_GradientNoise_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
            float d10 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
            float d11 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
            fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
            Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_DotProduct_float4(float4 A, float4 B, out float Out)
        {
            Out = dot(A, B);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        void Unity_Hue_Degrees_float(float3 In, float Offset, out float3 Out)
        {
            // RGB to HSV
            float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
            float4 P = lerp(float4(In.bg, K.wz), float4(In.gb, K.xy), step(In.b, In.g));
            float4 Q = lerp(float4(P.xyw, In.r), float4(In.r, P.yzx), step(P.x, In.r));
            float D = Q.x - min(Q.w, Q.y);
            float E = 1e-10;
            float V = (D == 0) ? Q.x : (Q.x + E);
            float3 hsv = float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), V);
        
            float hue = hsv.x + Offset / 360;
            hsv.x = (hue < 0)
                    ? hue + 1
                    : (hue > 1)
                        ? hue - 1
                        : hue;
        
            // HSV to RGB
            float4 K2 = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
            float3 P2 = abs(frac(hsv.xxx + K2.xyz) * 6.0 - K2.www);
            Out = hsv.z * lerp(K2.xxx, saturate(P2 - K2.xxx), hsv.y);
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float _Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0 = Vector1_C39A3E90;
            float2 _Property_51db9fe5f8707d8fa4507c481749556e_Out_0 = Vector2_EC93FC28;
            float2 _Multiply_d93244631121ab899bc52aade4ded114_Out_2;
            Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Property_51db9fe5f8707d8fa4507c481749556e_Out_0, _Multiply_d93244631121ab899bc52aade4ded114_Out_2);
            float2 _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3;
            Unity_TilingAndOffset_float((IN.WorldSpacePosition.xy), float2 (1, 1), _Multiply_d93244631121ab899bc52aade4ded114_Out_2, _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3);
            float _Property_a99d72da27ad61808450823e32ff058d_Out_0 = Vector1_B7976139;
            float _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2;
            Unity_GradientNoise_float(_TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3, _Property_a99d72da27ad61808450823e32ff058d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2);
            float _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2;
            Unity_Multiply_float_float(_Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2, _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2);
            UnityTexture2D _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_C3D58803);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.tex, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.samplerstate, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.GetTransformedUV(IN.uv0.xy), 0);
            #endif
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_R_5 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.r;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_G_6 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.g;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_B_7 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.b;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_A_8 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.a;
            float _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2;
            Unity_DotProduct_float4(_SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0, float4(0, 0.5, 0, 0), _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2);
            float _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2;
            Unity_Multiply_float_float(_Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2, _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2, _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2);
            float _Split_00fd5896a472d482b73269374a947f07_R_1 = IN.WorldSpacePosition[0];
            float _Split_00fd5896a472d482b73269374a947f07_G_2 = IN.WorldSpacePosition[1];
            float _Split_00fd5896a472d482b73269374a947f07_B_3 = IN.WorldSpacePosition[2];
            float _Split_00fd5896a472d482b73269374a947f07_A_4 = 0;
            float _Add_811329329cfbd18fb3e807f2cd027976_Out_2;
            Unity_Add_float(_Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2, _Split_00fd5896a472d482b73269374a947f07_R_1, _Add_811329329cfbd18fb3e807f2cd027976_Out_2);
            float4 _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4;
            float3 _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5;
            float2 _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6;
            Unity_Combine_float(_Add_811329329cfbd18fb3e807f2cd027976_Out_2, _Split_00fd5896a472d482b73269374a947f07_G_2, _Split_00fd5896a472d482b73269374a947f07_B_3, 0, _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4, _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5, _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6);
            float4 _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0 = IN.uv0;
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_R_1 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[0];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[1];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_B_3 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[2];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_A_4 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[3];
            float3 _Lerp_671d23c02bbda885b7de9901db258bec_Out_3;
            Unity_Lerp_float3(IN.WorldSpacePosition, (_Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4.xyz), (_Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2.xxx), _Lerp_671d23c02bbda885b7de9901db258bec_Out_3);
            float3 _Transform_5289292aa2111a8493f276a8d330172a_Out_1 = TransformWorldToObject(_Lerp_671d23c02bbda885b7de9901db258bec_Out_3.xyz);
            description.Position = _Transform_5289292aa2111a8493f276a8d330172a_Out_1;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float3 Emission;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float _Property_dd9b23bb0e8d4e23828a2903b0e8f1bd_Out_0 = _Luminosity;
            UnityTexture2D _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_B18C2857);
            float4 _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0 = SAMPLE_TEXTURE2D(_Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.tex, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.samplerstate, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.GetTransformedUV(IN.uv0.xy));
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_R_4 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.r;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_G_5 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.g;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_B_6 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.b;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.a;
            float _GradientNoise_668f1ac4661940099bbb63949e9cf082_Out_2;
            Unity_GradientNoise_float((IN.WorldSpacePosition.xy), 3, _GradientNoise_668f1ac4661940099bbb63949e9cf082_Out_2);
            float _Add_bca95b13bbde4c8b8a8d5c3c5edbfeab_Out_2;
            Unity_Add_float(_GradientNoise_668f1ac4661940099bbb63949e9cf082_Out_2, 0.6, _Add_bca95b13bbde4c8b8a8d5c3c5edbfeab_Out_2);
            float4 _Multiply_9c21544a05f2400b8735e3a7f02361f4_Out_2;
            Unity_Multiply_float4_float4(_SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0, (_Add_bca95b13bbde4c8b8a8d5c3c5edbfeab_Out_2.xxxx), _Multiply_9c21544a05f2400b8735e3a7f02361f4_Out_2);
            float4 _Multiply_2528287665c24e6d81e3369b4ca73a11_Out_2;
            Unity_Multiply_float4_float4((_Property_dd9b23bb0e8d4e23828a2903b0e8f1bd_Out_0.xxxx), _Multiply_9c21544a05f2400b8735e3a7f02361f4_Out_2, _Multiply_2528287665c24e6d81e3369b4ca73a11_Out_2);
            float3 _Hue_e41f1877edfd4476b9a0d85c9616cac1_Out_2;
            Unity_Hue_Degrees_float((_Multiply_2528287665c24e6d81e3369b4ca73a11_Out_2.xyz), 5, _Hue_e41f1877edfd4476b9a0d85c9616cac1_Out_2);
            float _Property_258b7abc9b6247baa769a139aaecb429_Out_0 = Alpha_Threshold;
            surface.BaseColor = _Hue_e41f1877edfd4476b9a0d85c9616cac1_Out_2;
            surface.Emission = float3(0, 0, 0);
            surface.Alpha = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7;
            surface.AlphaClipThreshold = _Property_258b7abc9b6247baa769a139aaecb429_Out_0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.WorldSpaceTangent =                          TransformObjectToWorldDir(input.tangentOS.xyz);
            output.ObjectSpaceBiTangent =                       normalize(cross(input.normalOS, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
            output.WorldSpaceBiTangent =                        TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
            output.ObjectSpacePosition =                        input.positionOS;
            output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
            output.uv0 =                                        input.uv0;
            output.TimeParameters =                             _TimeParameters.xyz;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
            // FragInputs from VFX come from two places: Interpolator or CBuffer.
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
            output.WorldSpacePosition = input.positionWS;
            output.uv0 = input.texCoord0;
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/LightingMetaPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "SceneSelectionPass"
            Tags
            {
                "LightMode" = "SceneSelectionPass"
            }
        
        // Render State
        Cull Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 4.5
        #pragma exclude_renderers gles gles3 glcore
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHONLY
        #define SCENESELECTIONPASS 1
        #define ALPHA_CLIP_THRESHOLD 1
        #define _ALPHATEST_ON 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 WorldSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 WorldSpaceTangent;
             float3 ObjectSpaceBiTangent;
             float3 WorldSpaceBiTangent;
             float3 ObjectSpacePosition;
             float3 WorldSpacePosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 interp0 : INTERP0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.interp0.xyzw =  input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.interp0.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 Texture2D_B18C2857_TexelSize;
        float4 Texture2D_E933B04B_TexelSize;
        float2 Vector2_EC93FC28;
        float Vector1_B7976139;
        float Vector1_C39A3E90;
        float4 Texture2D_C3D58803_TexelSize;
        float Alpha_Threshold;
        float _Normal_Strength;
        float _Luminosity;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(Texture2D_B18C2857);
        SAMPLER(samplerTexture2D_B18C2857);
        TEXTURE2D(Texture2D_E933B04B);
        SAMPLER(samplerTexture2D_E933B04B);
        TEXTURE2D(Texture2D_C3D58803);
        SAMPLER(samplerTexture2D_C3D58803);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        
        float2 Unity_GradientNoise_Dir_float(float2 p)
        {
            // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
            p = p % 289;
            // need full precision, otherwise half overflows when p > 1
            float x = float(34 * p.x + 1) * p.x % 289 + p.y;
            x = (34 * x + 1) * x % 289;
            x = frac(x / 41) * 2 - 1;
            return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
        }
        
        void Unity_GradientNoise_float(float2 UV, float Scale, out float Out)
        {
            float2 p = UV * Scale;
            float2 ip = floor(p);
            float2 fp = frac(p);
            float d00 = dot(Unity_GradientNoise_Dir_float(ip), fp);
            float d01 = dot(Unity_GradientNoise_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
            float d10 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
            float d11 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
            fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
            Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_DotProduct_float4(float4 A, float4 B, out float Out)
        {
            Out = dot(A, B);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float _Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0 = Vector1_C39A3E90;
            float2 _Property_51db9fe5f8707d8fa4507c481749556e_Out_0 = Vector2_EC93FC28;
            float2 _Multiply_d93244631121ab899bc52aade4ded114_Out_2;
            Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Property_51db9fe5f8707d8fa4507c481749556e_Out_0, _Multiply_d93244631121ab899bc52aade4ded114_Out_2);
            float2 _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3;
            Unity_TilingAndOffset_float((IN.WorldSpacePosition.xy), float2 (1, 1), _Multiply_d93244631121ab899bc52aade4ded114_Out_2, _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3);
            float _Property_a99d72da27ad61808450823e32ff058d_Out_0 = Vector1_B7976139;
            float _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2;
            Unity_GradientNoise_float(_TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3, _Property_a99d72da27ad61808450823e32ff058d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2);
            float _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2;
            Unity_Multiply_float_float(_Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2, _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2);
            UnityTexture2D _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_C3D58803);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.tex, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.samplerstate, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.GetTransformedUV(IN.uv0.xy), 0);
            #endif
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_R_5 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.r;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_G_6 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.g;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_B_7 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.b;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_A_8 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.a;
            float _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2;
            Unity_DotProduct_float4(_SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0, float4(0, 0.5, 0, 0), _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2);
            float _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2;
            Unity_Multiply_float_float(_Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2, _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2, _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2);
            float _Split_00fd5896a472d482b73269374a947f07_R_1 = IN.WorldSpacePosition[0];
            float _Split_00fd5896a472d482b73269374a947f07_G_2 = IN.WorldSpacePosition[1];
            float _Split_00fd5896a472d482b73269374a947f07_B_3 = IN.WorldSpacePosition[2];
            float _Split_00fd5896a472d482b73269374a947f07_A_4 = 0;
            float _Add_811329329cfbd18fb3e807f2cd027976_Out_2;
            Unity_Add_float(_Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2, _Split_00fd5896a472d482b73269374a947f07_R_1, _Add_811329329cfbd18fb3e807f2cd027976_Out_2);
            float4 _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4;
            float3 _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5;
            float2 _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6;
            Unity_Combine_float(_Add_811329329cfbd18fb3e807f2cd027976_Out_2, _Split_00fd5896a472d482b73269374a947f07_G_2, _Split_00fd5896a472d482b73269374a947f07_B_3, 0, _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4, _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5, _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6);
            float4 _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0 = IN.uv0;
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_R_1 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[0];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[1];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_B_3 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[2];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_A_4 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[3];
            float3 _Lerp_671d23c02bbda885b7de9901db258bec_Out_3;
            Unity_Lerp_float3(IN.WorldSpacePosition, (_Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4.xyz), (_Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2.xxx), _Lerp_671d23c02bbda885b7de9901db258bec_Out_3);
            float3 _Transform_5289292aa2111a8493f276a8d330172a_Out_1 = TransformWorldToObject(_Lerp_671d23c02bbda885b7de9901db258bec_Out_3.xyz);
            description.Position = _Transform_5289292aa2111a8493f276a8d330172a_Out_1;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_B18C2857);
            float4 _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0 = SAMPLE_TEXTURE2D(_Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.tex, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.samplerstate, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.GetTransformedUV(IN.uv0.xy));
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_R_4 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.r;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_G_5 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.g;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_B_6 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.b;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.a;
            float _Property_258b7abc9b6247baa769a139aaecb429_Out_0 = Alpha_Threshold;
            surface.Alpha = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7;
            surface.AlphaClipThreshold = _Property_258b7abc9b6247baa769a139aaecb429_Out_0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.WorldSpaceTangent =                          TransformObjectToWorldDir(input.tangentOS.xyz);
            output.ObjectSpaceBiTangent =                       normalize(cross(input.normalOS, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
            output.WorldSpaceBiTangent =                        TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
            output.ObjectSpacePosition =                        input.positionOS;
            output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
            output.uv0 =                                        input.uv0;
            output.TimeParameters =                             _TimeParameters.xyz;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
            // FragInputs from VFX come from two places: Interpolator or CBuffer.
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
            output.uv0 = input.texCoord0;
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "ScenePickingPass"
            Tags
            {
                "LightMode" = "Picking"
            }
        
        // Render State
        Cull Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 4.5
        #pragma exclude_renderers gles gles3 glcore
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHONLY
        #define SCENEPICKINGPASS 1
        #define ALPHA_CLIP_THRESHOLD 1
        #define _ALPHATEST_ON 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 WorldSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 WorldSpaceTangent;
             float3 ObjectSpaceBiTangent;
             float3 WorldSpaceBiTangent;
             float3 ObjectSpacePosition;
             float3 WorldSpacePosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 interp0 : INTERP0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.interp0.xyzw =  input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.interp0.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 Texture2D_B18C2857_TexelSize;
        float4 Texture2D_E933B04B_TexelSize;
        float2 Vector2_EC93FC28;
        float Vector1_B7976139;
        float Vector1_C39A3E90;
        float4 Texture2D_C3D58803_TexelSize;
        float Alpha_Threshold;
        float _Normal_Strength;
        float _Luminosity;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(Texture2D_B18C2857);
        SAMPLER(samplerTexture2D_B18C2857);
        TEXTURE2D(Texture2D_E933B04B);
        SAMPLER(samplerTexture2D_E933B04B);
        TEXTURE2D(Texture2D_C3D58803);
        SAMPLER(samplerTexture2D_C3D58803);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        
        float2 Unity_GradientNoise_Dir_float(float2 p)
        {
            // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
            p = p % 289;
            // need full precision, otherwise half overflows when p > 1
            float x = float(34 * p.x + 1) * p.x % 289 + p.y;
            x = (34 * x + 1) * x % 289;
            x = frac(x / 41) * 2 - 1;
            return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
        }
        
        void Unity_GradientNoise_float(float2 UV, float Scale, out float Out)
        {
            float2 p = UV * Scale;
            float2 ip = floor(p);
            float2 fp = frac(p);
            float d00 = dot(Unity_GradientNoise_Dir_float(ip), fp);
            float d01 = dot(Unity_GradientNoise_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
            float d10 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
            float d11 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
            fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
            Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_DotProduct_float4(float4 A, float4 B, out float Out)
        {
            Out = dot(A, B);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float _Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0 = Vector1_C39A3E90;
            float2 _Property_51db9fe5f8707d8fa4507c481749556e_Out_0 = Vector2_EC93FC28;
            float2 _Multiply_d93244631121ab899bc52aade4ded114_Out_2;
            Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Property_51db9fe5f8707d8fa4507c481749556e_Out_0, _Multiply_d93244631121ab899bc52aade4ded114_Out_2);
            float2 _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3;
            Unity_TilingAndOffset_float((IN.WorldSpacePosition.xy), float2 (1, 1), _Multiply_d93244631121ab899bc52aade4ded114_Out_2, _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3);
            float _Property_a99d72da27ad61808450823e32ff058d_Out_0 = Vector1_B7976139;
            float _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2;
            Unity_GradientNoise_float(_TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3, _Property_a99d72da27ad61808450823e32ff058d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2);
            float _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2;
            Unity_Multiply_float_float(_Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2, _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2);
            UnityTexture2D _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_C3D58803);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.tex, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.samplerstate, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.GetTransformedUV(IN.uv0.xy), 0);
            #endif
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_R_5 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.r;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_G_6 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.g;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_B_7 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.b;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_A_8 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.a;
            float _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2;
            Unity_DotProduct_float4(_SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0, float4(0, 0.5, 0, 0), _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2);
            float _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2;
            Unity_Multiply_float_float(_Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2, _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2, _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2);
            float _Split_00fd5896a472d482b73269374a947f07_R_1 = IN.WorldSpacePosition[0];
            float _Split_00fd5896a472d482b73269374a947f07_G_2 = IN.WorldSpacePosition[1];
            float _Split_00fd5896a472d482b73269374a947f07_B_3 = IN.WorldSpacePosition[2];
            float _Split_00fd5896a472d482b73269374a947f07_A_4 = 0;
            float _Add_811329329cfbd18fb3e807f2cd027976_Out_2;
            Unity_Add_float(_Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2, _Split_00fd5896a472d482b73269374a947f07_R_1, _Add_811329329cfbd18fb3e807f2cd027976_Out_2);
            float4 _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4;
            float3 _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5;
            float2 _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6;
            Unity_Combine_float(_Add_811329329cfbd18fb3e807f2cd027976_Out_2, _Split_00fd5896a472d482b73269374a947f07_G_2, _Split_00fd5896a472d482b73269374a947f07_B_3, 0, _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4, _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5, _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6);
            float4 _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0 = IN.uv0;
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_R_1 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[0];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[1];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_B_3 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[2];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_A_4 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[3];
            float3 _Lerp_671d23c02bbda885b7de9901db258bec_Out_3;
            Unity_Lerp_float3(IN.WorldSpacePosition, (_Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4.xyz), (_Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2.xxx), _Lerp_671d23c02bbda885b7de9901db258bec_Out_3);
            float3 _Transform_5289292aa2111a8493f276a8d330172a_Out_1 = TransformWorldToObject(_Lerp_671d23c02bbda885b7de9901db258bec_Out_3.xyz);
            description.Position = _Transform_5289292aa2111a8493f276a8d330172a_Out_1;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_B18C2857);
            float4 _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0 = SAMPLE_TEXTURE2D(_Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.tex, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.samplerstate, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.GetTransformedUV(IN.uv0.xy));
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_R_4 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.r;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_G_5 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.g;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_B_6 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.b;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.a;
            float _Property_258b7abc9b6247baa769a139aaecb429_Out_0 = Alpha_Threshold;
            surface.Alpha = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7;
            surface.AlphaClipThreshold = _Property_258b7abc9b6247baa769a139aaecb429_Out_0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.WorldSpaceTangent =                          TransformObjectToWorldDir(input.tangentOS.xyz);
            output.ObjectSpaceBiTangent =                       normalize(cross(input.normalOS, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
            output.WorldSpaceBiTangent =                        TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
            output.ObjectSpacePosition =                        input.positionOS;
            output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
            output.uv0 =                                        input.uv0;
            output.TimeParameters =                             _TimeParameters.xyz;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
            // FragInputs from VFX come from two places: Interpolator or CBuffer.
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
            output.uv0 = input.texCoord0;
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            // Name: <None>
            Tags
            {
                "LightMode" = "Universal2D"
            }
        
        // Render State
        Cull Off
        Blend One Zero
        ZTest LEqual
        ZWrite On
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 4.5
        #pragma exclude_renderers gles gles3 glcore
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_2D
        #define _ALPHATEST_ON 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 WorldSpacePosition;
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 WorldSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 WorldSpaceTangent;
             float3 ObjectSpaceBiTangent;
             float3 WorldSpaceBiTangent;
             float3 ObjectSpacePosition;
             float3 WorldSpacePosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float3 interp0 : INTERP0;
             float4 interp1 : INTERP1;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.interp0.xyz =  input.positionWS;
            output.interp1.xyzw =  input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.positionWS = input.interp0.xyz;
            output.texCoord0 = input.interp1.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 Texture2D_B18C2857_TexelSize;
        float4 Texture2D_E933B04B_TexelSize;
        float2 Vector2_EC93FC28;
        float Vector1_B7976139;
        float Vector1_C39A3E90;
        float4 Texture2D_C3D58803_TexelSize;
        float Alpha_Threshold;
        float _Normal_Strength;
        float _Luminosity;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(Texture2D_B18C2857);
        SAMPLER(samplerTexture2D_B18C2857);
        TEXTURE2D(Texture2D_E933B04B);
        SAMPLER(samplerTexture2D_E933B04B);
        TEXTURE2D(Texture2D_C3D58803);
        SAMPLER(samplerTexture2D_C3D58803);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        
        float2 Unity_GradientNoise_Dir_float(float2 p)
        {
            // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
            p = p % 289;
            // need full precision, otherwise half overflows when p > 1
            float x = float(34 * p.x + 1) * p.x % 289 + p.y;
            x = (34 * x + 1) * x % 289;
            x = frac(x / 41) * 2 - 1;
            return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
        }
        
        void Unity_GradientNoise_float(float2 UV, float Scale, out float Out)
        {
            float2 p = UV * Scale;
            float2 ip = floor(p);
            float2 fp = frac(p);
            float d00 = dot(Unity_GradientNoise_Dir_float(ip), fp);
            float d01 = dot(Unity_GradientNoise_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
            float d10 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
            float d11 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
            fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
            Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_DotProduct_float4(float4 A, float4 B, out float Out)
        {
            Out = dot(A, B);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        void Unity_Hue_Degrees_float(float3 In, float Offset, out float3 Out)
        {
            // RGB to HSV
            float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
            float4 P = lerp(float4(In.bg, K.wz), float4(In.gb, K.xy), step(In.b, In.g));
            float4 Q = lerp(float4(P.xyw, In.r), float4(In.r, P.yzx), step(P.x, In.r));
            float D = Q.x - min(Q.w, Q.y);
            float E = 1e-10;
            float V = (D == 0) ? Q.x : (Q.x + E);
            float3 hsv = float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), V);
        
            float hue = hsv.x + Offset / 360;
            hsv.x = (hue < 0)
                    ? hue + 1
                    : (hue > 1)
                        ? hue - 1
                        : hue;
        
            // HSV to RGB
            float4 K2 = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
            float3 P2 = abs(frac(hsv.xxx + K2.xyz) * 6.0 - K2.www);
            Out = hsv.z * lerp(K2.xxx, saturate(P2 - K2.xxx), hsv.y);
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float _Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0 = Vector1_C39A3E90;
            float2 _Property_51db9fe5f8707d8fa4507c481749556e_Out_0 = Vector2_EC93FC28;
            float2 _Multiply_d93244631121ab899bc52aade4ded114_Out_2;
            Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Property_51db9fe5f8707d8fa4507c481749556e_Out_0, _Multiply_d93244631121ab899bc52aade4ded114_Out_2);
            float2 _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3;
            Unity_TilingAndOffset_float((IN.WorldSpacePosition.xy), float2 (1, 1), _Multiply_d93244631121ab899bc52aade4ded114_Out_2, _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3);
            float _Property_a99d72da27ad61808450823e32ff058d_Out_0 = Vector1_B7976139;
            float _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2;
            Unity_GradientNoise_float(_TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3, _Property_a99d72da27ad61808450823e32ff058d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2);
            float _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2;
            Unity_Multiply_float_float(_Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2, _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2);
            UnityTexture2D _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_C3D58803);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.tex, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.samplerstate, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.GetTransformedUV(IN.uv0.xy), 0);
            #endif
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_R_5 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.r;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_G_6 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.g;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_B_7 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.b;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_A_8 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.a;
            float _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2;
            Unity_DotProduct_float4(_SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0, float4(0, 0.5, 0, 0), _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2);
            float _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2;
            Unity_Multiply_float_float(_Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2, _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2, _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2);
            float _Split_00fd5896a472d482b73269374a947f07_R_1 = IN.WorldSpacePosition[0];
            float _Split_00fd5896a472d482b73269374a947f07_G_2 = IN.WorldSpacePosition[1];
            float _Split_00fd5896a472d482b73269374a947f07_B_3 = IN.WorldSpacePosition[2];
            float _Split_00fd5896a472d482b73269374a947f07_A_4 = 0;
            float _Add_811329329cfbd18fb3e807f2cd027976_Out_2;
            Unity_Add_float(_Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2, _Split_00fd5896a472d482b73269374a947f07_R_1, _Add_811329329cfbd18fb3e807f2cd027976_Out_2);
            float4 _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4;
            float3 _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5;
            float2 _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6;
            Unity_Combine_float(_Add_811329329cfbd18fb3e807f2cd027976_Out_2, _Split_00fd5896a472d482b73269374a947f07_G_2, _Split_00fd5896a472d482b73269374a947f07_B_3, 0, _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4, _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5, _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6);
            float4 _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0 = IN.uv0;
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_R_1 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[0];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[1];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_B_3 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[2];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_A_4 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[3];
            float3 _Lerp_671d23c02bbda885b7de9901db258bec_Out_3;
            Unity_Lerp_float3(IN.WorldSpacePosition, (_Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4.xyz), (_Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2.xxx), _Lerp_671d23c02bbda885b7de9901db258bec_Out_3);
            float3 _Transform_5289292aa2111a8493f276a8d330172a_Out_1 = TransformWorldToObject(_Lerp_671d23c02bbda885b7de9901db258bec_Out_3.xyz);
            description.Position = _Transform_5289292aa2111a8493f276a8d330172a_Out_1;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float _Property_dd9b23bb0e8d4e23828a2903b0e8f1bd_Out_0 = _Luminosity;
            UnityTexture2D _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_B18C2857);
            float4 _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0 = SAMPLE_TEXTURE2D(_Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.tex, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.samplerstate, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.GetTransformedUV(IN.uv0.xy));
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_R_4 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.r;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_G_5 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.g;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_B_6 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.b;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.a;
            float _GradientNoise_668f1ac4661940099bbb63949e9cf082_Out_2;
            Unity_GradientNoise_float((IN.WorldSpacePosition.xy), 3, _GradientNoise_668f1ac4661940099bbb63949e9cf082_Out_2);
            float _Add_bca95b13bbde4c8b8a8d5c3c5edbfeab_Out_2;
            Unity_Add_float(_GradientNoise_668f1ac4661940099bbb63949e9cf082_Out_2, 0.6, _Add_bca95b13bbde4c8b8a8d5c3c5edbfeab_Out_2);
            float4 _Multiply_9c21544a05f2400b8735e3a7f02361f4_Out_2;
            Unity_Multiply_float4_float4(_SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0, (_Add_bca95b13bbde4c8b8a8d5c3c5edbfeab_Out_2.xxxx), _Multiply_9c21544a05f2400b8735e3a7f02361f4_Out_2);
            float4 _Multiply_2528287665c24e6d81e3369b4ca73a11_Out_2;
            Unity_Multiply_float4_float4((_Property_dd9b23bb0e8d4e23828a2903b0e8f1bd_Out_0.xxxx), _Multiply_9c21544a05f2400b8735e3a7f02361f4_Out_2, _Multiply_2528287665c24e6d81e3369b4ca73a11_Out_2);
            float3 _Hue_e41f1877edfd4476b9a0d85c9616cac1_Out_2;
            Unity_Hue_Degrees_float((_Multiply_2528287665c24e6d81e3369b4ca73a11_Out_2.xyz), 5, _Hue_e41f1877edfd4476b9a0d85c9616cac1_Out_2);
            float _Property_258b7abc9b6247baa769a139aaecb429_Out_0 = Alpha_Threshold;
            surface.BaseColor = _Hue_e41f1877edfd4476b9a0d85c9616cac1_Out_2;
            surface.Alpha = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7;
            surface.AlphaClipThreshold = _Property_258b7abc9b6247baa769a139aaecb429_Out_0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.WorldSpaceTangent =                          TransformObjectToWorldDir(input.tangentOS.xyz);
            output.ObjectSpaceBiTangent =                       normalize(cross(input.normalOS, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
            output.WorldSpaceBiTangent =                        TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
            output.ObjectSpacePosition =                        input.positionOS;
            output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
            output.uv0 =                                        input.uv0;
            output.TimeParameters =                             _TimeParameters.xyz;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
            // FragInputs from VFX come from two places: Interpolator or CBuffer.
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
            output.WorldSpacePosition = input.positionWS;
            output.uv0 = input.texCoord0;
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBR2DPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "UniversalMaterialType" = "Lit"
            "Queue"="AlphaTest"
            "ShaderGraphShader"="true"
            "ShaderGraphTargetId"="UniversalLitSubTarget"
        }
        Pass
        {
            Name "Universal Forward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
        
        // Render State
        Cull Off
        Blend One Zero
        ZTest LEqual
        ZWrite On
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma only_renderers gles gles3 glcore d3d11
        #pragma multi_compile_instancing
        #pragma multi_compile_fog
        #pragma instancing_options renderinglayer
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
        #pragma multi_compile _ LIGHTMAP_ON
        #pragma multi_compile _ DYNAMICLIGHTMAP_ON
        #pragma multi_compile _ DIRLIGHTMAP_COMBINED
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
        #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
        #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
        #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
        #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
        #pragma multi_compile_fragment _ _SHADOWS_SOFT
        #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
        #pragma multi_compile _ SHADOWS_SHADOWMASK
        #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
        #pragma multi_compile_fragment _ _LIGHT_LAYERS
        #pragma multi_compile_fragment _ DEBUG_DISPLAY
        #pragma multi_compile_fragment _ _LIGHT_COOKIES
        #pragma multi_compile _ _CLUSTERED_RENDERING
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define ATTRIBUTES_NEED_TEXCOORD2
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TANGENT_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define VARYINGS_NEED_VIEWDIRECTION_WS
        #define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
        #define VARYINGS_NEED_SHADOW_COORD
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_FORWARD
        #define _FOG_FRAGMENT 1
        #define _ALPHATEST_ON 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
             float4 uv1 : TEXCOORD1;
             float4 uv2 : TEXCOORD2;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float3 normalWS;
             float4 tangentWS;
             float4 texCoord0;
             float3 viewDirectionWS;
            #if defined(LIGHTMAP_ON)
             float2 staticLightmapUV;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
             float2 dynamicLightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
             float3 sh;
            #endif
             float4 fogFactorAndVertexLight;
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
             float4 shadowCoord;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 TangentSpaceNormal;
             float3 WorldSpacePosition;
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 WorldSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 WorldSpaceTangent;
             float3 ObjectSpaceBiTangent;
             float3 WorldSpaceBiTangent;
             float3 ObjectSpacePosition;
             float3 WorldSpacePosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float3 interp0 : INTERP0;
             float3 interp1 : INTERP1;
             float4 interp2 : INTERP2;
             float4 interp3 : INTERP3;
             float3 interp4 : INTERP4;
             float2 interp5 : INTERP5;
             float2 interp6 : INTERP6;
             float3 interp7 : INTERP7;
             float4 interp8 : INTERP8;
             float4 interp9 : INTERP9;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.interp0.xyz =  input.positionWS;
            output.interp1.xyz =  input.normalWS;
            output.interp2.xyzw =  input.tangentWS;
            output.interp3.xyzw =  input.texCoord0;
            output.interp4.xyz =  input.viewDirectionWS;
            #if defined(LIGHTMAP_ON)
            output.interp5.xy =  input.staticLightmapUV;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
            output.interp6.xy =  input.dynamicLightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
            output.interp7.xyz =  input.sh;
            #endif
            output.interp8.xyzw =  input.fogFactorAndVertexLight;
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
            output.interp9.xyzw =  input.shadowCoord;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.positionWS = input.interp0.xyz;
            output.normalWS = input.interp1.xyz;
            output.tangentWS = input.interp2.xyzw;
            output.texCoord0 = input.interp3.xyzw;
            output.viewDirectionWS = input.interp4.xyz;
            #if defined(LIGHTMAP_ON)
            output.staticLightmapUV = input.interp5.xy;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
            output.dynamicLightmapUV = input.interp6.xy;
            #endif
            #if !defined(LIGHTMAP_ON)
            output.sh = input.interp7.xyz;
            #endif
            output.fogFactorAndVertexLight = input.interp8.xyzw;
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
            output.shadowCoord = input.interp9.xyzw;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 Texture2D_B18C2857_TexelSize;
        float4 Texture2D_E933B04B_TexelSize;
        float2 Vector2_EC93FC28;
        float Vector1_B7976139;
        float Vector1_C39A3E90;
        float4 Texture2D_C3D58803_TexelSize;
        float Alpha_Threshold;
        float _Normal_Strength;
        float _Luminosity;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(Texture2D_B18C2857);
        SAMPLER(samplerTexture2D_B18C2857);
        TEXTURE2D(Texture2D_E933B04B);
        SAMPLER(samplerTexture2D_E933B04B);
        TEXTURE2D(Texture2D_C3D58803);
        SAMPLER(samplerTexture2D_C3D58803);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        
        float2 Unity_GradientNoise_Dir_float(float2 p)
        {
            // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
            p = p % 289;
            // need full precision, otherwise half overflows when p > 1
            float x = float(34 * p.x + 1) * p.x % 289 + p.y;
            x = (34 * x + 1) * x % 289;
            x = frac(x / 41) * 2 - 1;
            return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
        }
        
        void Unity_GradientNoise_float(float2 UV, float Scale, out float Out)
        {
            float2 p = UV * Scale;
            float2 ip = floor(p);
            float2 fp = frac(p);
            float d00 = dot(Unity_GradientNoise_Dir_float(ip), fp);
            float d01 = dot(Unity_GradientNoise_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
            float d10 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
            float d11 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
            fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
            Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_DotProduct_float4(float4 A, float4 B, out float Out)
        {
            Out = dot(A, B);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        void Unity_Hue_Degrees_float(float3 In, float Offset, out float3 Out)
        {
            // RGB to HSV
            float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
            float4 P = lerp(float4(In.bg, K.wz), float4(In.gb, K.xy), step(In.b, In.g));
            float4 Q = lerp(float4(P.xyw, In.r), float4(In.r, P.yzx), step(P.x, In.r));
            float D = Q.x - min(Q.w, Q.y);
            float E = 1e-10;
            float V = (D == 0) ? Q.x : (Q.x + E);
            float3 hsv = float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), V);
        
            float hue = hsv.x + Offset / 360;
            hsv.x = (hue < 0)
                    ? hue + 1
                    : (hue > 1)
                        ? hue - 1
                        : hue;
        
            // HSV to RGB
            float4 K2 = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
            float3 P2 = abs(frac(hsv.xxx + K2.xyz) * 6.0 - K2.www);
            Out = hsv.z * lerp(K2.xxx, saturate(P2 - K2.xxx), hsv.y);
        }
        
        void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
        {
            Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float _Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0 = Vector1_C39A3E90;
            float2 _Property_51db9fe5f8707d8fa4507c481749556e_Out_0 = Vector2_EC93FC28;
            float2 _Multiply_d93244631121ab899bc52aade4ded114_Out_2;
            Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Property_51db9fe5f8707d8fa4507c481749556e_Out_0, _Multiply_d93244631121ab899bc52aade4ded114_Out_2);
            float2 _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3;
            Unity_TilingAndOffset_float((IN.WorldSpacePosition.xy), float2 (1, 1), _Multiply_d93244631121ab899bc52aade4ded114_Out_2, _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3);
            float _Property_a99d72da27ad61808450823e32ff058d_Out_0 = Vector1_B7976139;
            float _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2;
            Unity_GradientNoise_float(_TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3, _Property_a99d72da27ad61808450823e32ff058d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2);
            float _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2;
            Unity_Multiply_float_float(_Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2, _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2);
            UnityTexture2D _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_C3D58803);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.tex, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.samplerstate, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.GetTransformedUV(IN.uv0.xy), 0);
            #endif
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_R_5 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.r;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_G_6 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.g;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_B_7 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.b;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_A_8 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.a;
            float _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2;
            Unity_DotProduct_float4(_SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0, float4(0, 0.5, 0, 0), _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2);
            float _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2;
            Unity_Multiply_float_float(_Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2, _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2, _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2);
            float _Split_00fd5896a472d482b73269374a947f07_R_1 = IN.WorldSpacePosition[0];
            float _Split_00fd5896a472d482b73269374a947f07_G_2 = IN.WorldSpacePosition[1];
            float _Split_00fd5896a472d482b73269374a947f07_B_3 = IN.WorldSpacePosition[2];
            float _Split_00fd5896a472d482b73269374a947f07_A_4 = 0;
            float _Add_811329329cfbd18fb3e807f2cd027976_Out_2;
            Unity_Add_float(_Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2, _Split_00fd5896a472d482b73269374a947f07_R_1, _Add_811329329cfbd18fb3e807f2cd027976_Out_2);
            float4 _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4;
            float3 _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5;
            float2 _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6;
            Unity_Combine_float(_Add_811329329cfbd18fb3e807f2cd027976_Out_2, _Split_00fd5896a472d482b73269374a947f07_G_2, _Split_00fd5896a472d482b73269374a947f07_B_3, 0, _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4, _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5, _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6);
            float4 _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0 = IN.uv0;
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_R_1 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[0];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[1];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_B_3 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[2];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_A_4 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[3];
            float3 _Lerp_671d23c02bbda885b7de9901db258bec_Out_3;
            Unity_Lerp_float3(IN.WorldSpacePosition, (_Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4.xyz), (_Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2.xxx), _Lerp_671d23c02bbda885b7de9901db258bec_Out_3);
            float3 _Transform_5289292aa2111a8493f276a8d330172a_Out_1 = TransformWorldToObject(_Lerp_671d23c02bbda885b7de9901db258bec_Out_3.xyz);
            description.Position = _Transform_5289292aa2111a8493f276a8d330172a_Out_1;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float3 NormalTS;
            float3 Emission;
            float Metallic;
            float Smoothness;
            float Occlusion;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float _Property_dd9b23bb0e8d4e23828a2903b0e8f1bd_Out_0 = _Luminosity;
            UnityTexture2D _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_B18C2857);
            float4 _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0 = SAMPLE_TEXTURE2D(_Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.tex, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.samplerstate, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.GetTransformedUV(IN.uv0.xy));
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_R_4 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.r;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_G_5 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.g;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_B_6 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.b;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.a;
            float _GradientNoise_668f1ac4661940099bbb63949e9cf082_Out_2;
            Unity_GradientNoise_float((IN.WorldSpacePosition.xy), 3, _GradientNoise_668f1ac4661940099bbb63949e9cf082_Out_2);
            float _Add_bca95b13bbde4c8b8a8d5c3c5edbfeab_Out_2;
            Unity_Add_float(_GradientNoise_668f1ac4661940099bbb63949e9cf082_Out_2, 0.6, _Add_bca95b13bbde4c8b8a8d5c3c5edbfeab_Out_2);
            float4 _Multiply_9c21544a05f2400b8735e3a7f02361f4_Out_2;
            Unity_Multiply_float4_float4(_SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0, (_Add_bca95b13bbde4c8b8a8d5c3c5edbfeab_Out_2.xxxx), _Multiply_9c21544a05f2400b8735e3a7f02361f4_Out_2);
            float4 _Multiply_2528287665c24e6d81e3369b4ca73a11_Out_2;
            Unity_Multiply_float4_float4((_Property_dd9b23bb0e8d4e23828a2903b0e8f1bd_Out_0.xxxx), _Multiply_9c21544a05f2400b8735e3a7f02361f4_Out_2, _Multiply_2528287665c24e6d81e3369b4ca73a11_Out_2);
            float3 _Hue_e41f1877edfd4476b9a0d85c9616cac1_Out_2;
            Unity_Hue_Degrees_float((_Multiply_2528287665c24e6d81e3369b4ca73a11_Out_2.xyz), 5, _Hue_e41f1877edfd4476b9a0d85c9616cac1_Out_2);
            UnityTexture2D _Property_b48e111d2b6a488c93d88bd4f120c5f9_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_E933B04B);
            float4 _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0 = SAMPLE_TEXTURE2D(_Property_b48e111d2b6a488c93d88bd4f120c5f9_Out_0.tex, _Property_b48e111d2b6a488c93d88bd4f120c5f9_Out_0.samplerstate, _Property_b48e111d2b6a488c93d88bd4f120c5f9_Out_0.GetTransformedUV(IN.uv0.xy));
            _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.rgb = UnpackNormal(_SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0);
            float _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_R_4 = _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.r;
            float _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_G_5 = _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.g;
            float _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_B_6 = _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.b;
            float _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_A_7 = _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.a;
            float _Property_032c33beaac840d58b4e508ae522a70c_Out_0 = _Normal_Strength;
            float3 _NormalStrength_9b9e9bf8069a42888b650f5735fe4abd_Out_2;
            Unity_NormalStrength_float((_SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.xyz), _Property_032c33beaac840d58b4e508ae522a70c_Out_0, _NormalStrength_9b9e9bf8069a42888b650f5735fe4abd_Out_2);
            float _Property_258b7abc9b6247baa769a139aaecb429_Out_0 = Alpha_Threshold;
            surface.BaseColor = _Hue_e41f1877edfd4476b9a0d85c9616cac1_Out_2;
            surface.NormalTS = _NormalStrength_9b9e9bf8069a42888b650f5735fe4abd_Out_2;
            surface.Emission = float3(0, 0, 0);
            surface.Metallic = 0;
            surface.Smoothness = 0;
            surface.Occlusion = 1;
            surface.Alpha = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7;
            surface.AlphaClipThreshold = _Property_258b7abc9b6247baa769a139aaecb429_Out_0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.WorldSpaceTangent =                          TransformObjectToWorldDir(input.tangentOS.xyz);
            output.ObjectSpaceBiTangent =                       normalize(cross(input.normalOS, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
            output.WorldSpaceBiTangent =                        TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
            output.ObjectSpacePosition =                        input.positionOS;
            output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
            output.uv0 =                                        input.uv0;
            output.TimeParameters =                             _TimeParameters.xyz;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
            // FragInputs from VFX come from two places: Interpolator or CBuffer.
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
            output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
        
        
            output.WorldSpacePosition = input.positionWS;
            output.uv0 = input.texCoord0;
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
        
        // Render State
        Cull Off
        ZTest LEqual
        ZWrite On
        ColorMask 0
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma only_renderers gles gles3 glcore d3d11
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_SHADOWCASTER
        #define _ALPHATEST_ON 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 normalWS;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 WorldSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 WorldSpaceTangent;
             float3 ObjectSpaceBiTangent;
             float3 WorldSpaceBiTangent;
             float3 ObjectSpacePosition;
             float3 WorldSpacePosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float3 interp0 : INTERP0;
             float4 interp1 : INTERP1;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.interp0.xyz =  input.normalWS;
            output.interp1.xyzw =  input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.normalWS = input.interp0.xyz;
            output.texCoord0 = input.interp1.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 Texture2D_B18C2857_TexelSize;
        float4 Texture2D_E933B04B_TexelSize;
        float2 Vector2_EC93FC28;
        float Vector1_B7976139;
        float Vector1_C39A3E90;
        float4 Texture2D_C3D58803_TexelSize;
        float Alpha_Threshold;
        float _Normal_Strength;
        float _Luminosity;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(Texture2D_B18C2857);
        SAMPLER(samplerTexture2D_B18C2857);
        TEXTURE2D(Texture2D_E933B04B);
        SAMPLER(samplerTexture2D_E933B04B);
        TEXTURE2D(Texture2D_C3D58803);
        SAMPLER(samplerTexture2D_C3D58803);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        
        float2 Unity_GradientNoise_Dir_float(float2 p)
        {
            // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
            p = p % 289;
            // need full precision, otherwise half overflows when p > 1
            float x = float(34 * p.x + 1) * p.x % 289 + p.y;
            x = (34 * x + 1) * x % 289;
            x = frac(x / 41) * 2 - 1;
            return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
        }
        
        void Unity_GradientNoise_float(float2 UV, float Scale, out float Out)
        {
            float2 p = UV * Scale;
            float2 ip = floor(p);
            float2 fp = frac(p);
            float d00 = dot(Unity_GradientNoise_Dir_float(ip), fp);
            float d01 = dot(Unity_GradientNoise_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
            float d10 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
            float d11 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
            fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
            Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_DotProduct_float4(float4 A, float4 B, out float Out)
        {
            Out = dot(A, B);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float _Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0 = Vector1_C39A3E90;
            float2 _Property_51db9fe5f8707d8fa4507c481749556e_Out_0 = Vector2_EC93FC28;
            float2 _Multiply_d93244631121ab899bc52aade4ded114_Out_2;
            Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Property_51db9fe5f8707d8fa4507c481749556e_Out_0, _Multiply_d93244631121ab899bc52aade4ded114_Out_2);
            float2 _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3;
            Unity_TilingAndOffset_float((IN.WorldSpacePosition.xy), float2 (1, 1), _Multiply_d93244631121ab899bc52aade4ded114_Out_2, _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3);
            float _Property_a99d72da27ad61808450823e32ff058d_Out_0 = Vector1_B7976139;
            float _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2;
            Unity_GradientNoise_float(_TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3, _Property_a99d72da27ad61808450823e32ff058d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2);
            float _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2;
            Unity_Multiply_float_float(_Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2, _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2);
            UnityTexture2D _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_C3D58803);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.tex, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.samplerstate, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.GetTransformedUV(IN.uv0.xy), 0);
            #endif
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_R_5 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.r;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_G_6 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.g;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_B_7 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.b;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_A_8 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.a;
            float _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2;
            Unity_DotProduct_float4(_SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0, float4(0, 0.5, 0, 0), _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2);
            float _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2;
            Unity_Multiply_float_float(_Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2, _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2, _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2);
            float _Split_00fd5896a472d482b73269374a947f07_R_1 = IN.WorldSpacePosition[0];
            float _Split_00fd5896a472d482b73269374a947f07_G_2 = IN.WorldSpacePosition[1];
            float _Split_00fd5896a472d482b73269374a947f07_B_3 = IN.WorldSpacePosition[2];
            float _Split_00fd5896a472d482b73269374a947f07_A_4 = 0;
            float _Add_811329329cfbd18fb3e807f2cd027976_Out_2;
            Unity_Add_float(_Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2, _Split_00fd5896a472d482b73269374a947f07_R_1, _Add_811329329cfbd18fb3e807f2cd027976_Out_2);
            float4 _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4;
            float3 _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5;
            float2 _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6;
            Unity_Combine_float(_Add_811329329cfbd18fb3e807f2cd027976_Out_2, _Split_00fd5896a472d482b73269374a947f07_G_2, _Split_00fd5896a472d482b73269374a947f07_B_3, 0, _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4, _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5, _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6);
            float4 _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0 = IN.uv0;
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_R_1 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[0];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[1];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_B_3 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[2];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_A_4 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[3];
            float3 _Lerp_671d23c02bbda885b7de9901db258bec_Out_3;
            Unity_Lerp_float3(IN.WorldSpacePosition, (_Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4.xyz), (_Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2.xxx), _Lerp_671d23c02bbda885b7de9901db258bec_Out_3);
            float3 _Transform_5289292aa2111a8493f276a8d330172a_Out_1 = TransformWorldToObject(_Lerp_671d23c02bbda885b7de9901db258bec_Out_3.xyz);
            description.Position = _Transform_5289292aa2111a8493f276a8d330172a_Out_1;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_B18C2857);
            float4 _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0 = SAMPLE_TEXTURE2D(_Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.tex, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.samplerstate, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.GetTransformedUV(IN.uv0.xy));
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_R_4 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.r;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_G_5 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.g;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_B_6 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.b;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.a;
            float _Property_258b7abc9b6247baa769a139aaecb429_Out_0 = Alpha_Threshold;
            surface.Alpha = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7;
            surface.AlphaClipThreshold = _Property_258b7abc9b6247baa769a139aaecb429_Out_0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.WorldSpaceTangent =                          TransformObjectToWorldDir(input.tangentOS.xyz);
            output.ObjectSpaceBiTangent =                       normalize(cross(input.normalOS, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
            output.WorldSpaceBiTangent =                        TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
            output.ObjectSpacePosition =                        input.positionOS;
            output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
            output.uv0 =                                        input.uv0;
            output.TimeParameters =                             _TimeParameters.xyz;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
            // FragInputs from VFX come from two places: Interpolator or CBuffer.
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
            output.uv0 = input.texCoord0;
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }
        
        // Render State
        Cull Off
        ZTest LEqual
        ZWrite On
        ColorMask 0
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma only_renderers gles gles3 glcore d3d11
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHONLY
        #define _ALPHATEST_ON 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 WorldSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 WorldSpaceTangent;
             float3 ObjectSpaceBiTangent;
             float3 WorldSpaceBiTangent;
             float3 ObjectSpacePosition;
             float3 WorldSpacePosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 interp0 : INTERP0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.interp0.xyzw =  input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.interp0.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 Texture2D_B18C2857_TexelSize;
        float4 Texture2D_E933B04B_TexelSize;
        float2 Vector2_EC93FC28;
        float Vector1_B7976139;
        float Vector1_C39A3E90;
        float4 Texture2D_C3D58803_TexelSize;
        float Alpha_Threshold;
        float _Normal_Strength;
        float _Luminosity;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(Texture2D_B18C2857);
        SAMPLER(samplerTexture2D_B18C2857);
        TEXTURE2D(Texture2D_E933B04B);
        SAMPLER(samplerTexture2D_E933B04B);
        TEXTURE2D(Texture2D_C3D58803);
        SAMPLER(samplerTexture2D_C3D58803);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        
        float2 Unity_GradientNoise_Dir_float(float2 p)
        {
            // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
            p = p % 289;
            // need full precision, otherwise half overflows when p > 1
            float x = float(34 * p.x + 1) * p.x % 289 + p.y;
            x = (34 * x + 1) * x % 289;
            x = frac(x / 41) * 2 - 1;
            return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
        }
        
        void Unity_GradientNoise_float(float2 UV, float Scale, out float Out)
        {
            float2 p = UV * Scale;
            float2 ip = floor(p);
            float2 fp = frac(p);
            float d00 = dot(Unity_GradientNoise_Dir_float(ip), fp);
            float d01 = dot(Unity_GradientNoise_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
            float d10 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
            float d11 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
            fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
            Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_DotProduct_float4(float4 A, float4 B, out float Out)
        {
            Out = dot(A, B);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float _Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0 = Vector1_C39A3E90;
            float2 _Property_51db9fe5f8707d8fa4507c481749556e_Out_0 = Vector2_EC93FC28;
            float2 _Multiply_d93244631121ab899bc52aade4ded114_Out_2;
            Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Property_51db9fe5f8707d8fa4507c481749556e_Out_0, _Multiply_d93244631121ab899bc52aade4ded114_Out_2);
            float2 _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3;
            Unity_TilingAndOffset_float((IN.WorldSpacePosition.xy), float2 (1, 1), _Multiply_d93244631121ab899bc52aade4ded114_Out_2, _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3);
            float _Property_a99d72da27ad61808450823e32ff058d_Out_0 = Vector1_B7976139;
            float _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2;
            Unity_GradientNoise_float(_TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3, _Property_a99d72da27ad61808450823e32ff058d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2);
            float _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2;
            Unity_Multiply_float_float(_Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2, _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2);
            UnityTexture2D _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_C3D58803);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.tex, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.samplerstate, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.GetTransformedUV(IN.uv0.xy), 0);
            #endif
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_R_5 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.r;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_G_6 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.g;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_B_7 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.b;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_A_8 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.a;
            float _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2;
            Unity_DotProduct_float4(_SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0, float4(0, 0.5, 0, 0), _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2);
            float _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2;
            Unity_Multiply_float_float(_Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2, _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2, _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2);
            float _Split_00fd5896a472d482b73269374a947f07_R_1 = IN.WorldSpacePosition[0];
            float _Split_00fd5896a472d482b73269374a947f07_G_2 = IN.WorldSpacePosition[1];
            float _Split_00fd5896a472d482b73269374a947f07_B_3 = IN.WorldSpacePosition[2];
            float _Split_00fd5896a472d482b73269374a947f07_A_4 = 0;
            float _Add_811329329cfbd18fb3e807f2cd027976_Out_2;
            Unity_Add_float(_Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2, _Split_00fd5896a472d482b73269374a947f07_R_1, _Add_811329329cfbd18fb3e807f2cd027976_Out_2);
            float4 _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4;
            float3 _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5;
            float2 _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6;
            Unity_Combine_float(_Add_811329329cfbd18fb3e807f2cd027976_Out_2, _Split_00fd5896a472d482b73269374a947f07_G_2, _Split_00fd5896a472d482b73269374a947f07_B_3, 0, _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4, _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5, _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6);
            float4 _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0 = IN.uv0;
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_R_1 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[0];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[1];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_B_3 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[2];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_A_4 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[3];
            float3 _Lerp_671d23c02bbda885b7de9901db258bec_Out_3;
            Unity_Lerp_float3(IN.WorldSpacePosition, (_Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4.xyz), (_Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2.xxx), _Lerp_671d23c02bbda885b7de9901db258bec_Out_3);
            float3 _Transform_5289292aa2111a8493f276a8d330172a_Out_1 = TransformWorldToObject(_Lerp_671d23c02bbda885b7de9901db258bec_Out_3.xyz);
            description.Position = _Transform_5289292aa2111a8493f276a8d330172a_Out_1;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_B18C2857);
            float4 _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0 = SAMPLE_TEXTURE2D(_Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.tex, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.samplerstate, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.GetTransformedUV(IN.uv0.xy));
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_R_4 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.r;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_G_5 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.g;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_B_6 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.b;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.a;
            float _Property_258b7abc9b6247baa769a139aaecb429_Out_0 = Alpha_Threshold;
            surface.Alpha = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7;
            surface.AlphaClipThreshold = _Property_258b7abc9b6247baa769a139aaecb429_Out_0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.WorldSpaceTangent =                          TransformObjectToWorldDir(input.tangentOS.xyz);
            output.ObjectSpaceBiTangent =                       normalize(cross(input.normalOS, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
            output.WorldSpaceBiTangent =                        TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
            output.ObjectSpacePosition =                        input.positionOS;
            output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
            output.uv0 =                                        input.uv0;
            output.TimeParameters =                             _TimeParameters.xyz;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
            // FragInputs from VFX come from two places: Interpolator or CBuffer.
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
            output.uv0 = input.texCoord0;
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }
        
        // Render State
        Cull Off
        ZTest LEqual
        ZWrite On
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma only_renderers gles gles3 glcore d3d11
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TANGENT_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHNORMALS
        #define _ALPHATEST_ON 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
             float4 uv1 : TEXCOORD1;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 normalWS;
             float4 tangentWS;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 TangentSpaceNormal;
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 WorldSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 WorldSpaceTangent;
             float3 ObjectSpaceBiTangent;
             float3 WorldSpaceBiTangent;
             float3 ObjectSpacePosition;
             float3 WorldSpacePosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float3 interp0 : INTERP0;
             float4 interp1 : INTERP1;
             float4 interp2 : INTERP2;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.interp0.xyz =  input.normalWS;
            output.interp1.xyzw =  input.tangentWS;
            output.interp2.xyzw =  input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.normalWS = input.interp0.xyz;
            output.tangentWS = input.interp1.xyzw;
            output.texCoord0 = input.interp2.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 Texture2D_B18C2857_TexelSize;
        float4 Texture2D_E933B04B_TexelSize;
        float2 Vector2_EC93FC28;
        float Vector1_B7976139;
        float Vector1_C39A3E90;
        float4 Texture2D_C3D58803_TexelSize;
        float Alpha_Threshold;
        float _Normal_Strength;
        float _Luminosity;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(Texture2D_B18C2857);
        SAMPLER(samplerTexture2D_B18C2857);
        TEXTURE2D(Texture2D_E933B04B);
        SAMPLER(samplerTexture2D_E933B04B);
        TEXTURE2D(Texture2D_C3D58803);
        SAMPLER(samplerTexture2D_C3D58803);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        
        float2 Unity_GradientNoise_Dir_float(float2 p)
        {
            // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
            p = p % 289;
            // need full precision, otherwise half overflows when p > 1
            float x = float(34 * p.x + 1) * p.x % 289 + p.y;
            x = (34 * x + 1) * x % 289;
            x = frac(x / 41) * 2 - 1;
            return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
        }
        
        void Unity_GradientNoise_float(float2 UV, float Scale, out float Out)
        {
            float2 p = UV * Scale;
            float2 ip = floor(p);
            float2 fp = frac(p);
            float d00 = dot(Unity_GradientNoise_Dir_float(ip), fp);
            float d01 = dot(Unity_GradientNoise_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
            float d10 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
            float d11 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
            fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
            Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_DotProduct_float4(float4 A, float4 B, out float Out)
        {
            Out = dot(A, B);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
        {
            Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float _Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0 = Vector1_C39A3E90;
            float2 _Property_51db9fe5f8707d8fa4507c481749556e_Out_0 = Vector2_EC93FC28;
            float2 _Multiply_d93244631121ab899bc52aade4ded114_Out_2;
            Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Property_51db9fe5f8707d8fa4507c481749556e_Out_0, _Multiply_d93244631121ab899bc52aade4ded114_Out_2);
            float2 _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3;
            Unity_TilingAndOffset_float((IN.WorldSpacePosition.xy), float2 (1, 1), _Multiply_d93244631121ab899bc52aade4ded114_Out_2, _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3);
            float _Property_a99d72da27ad61808450823e32ff058d_Out_0 = Vector1_B7976139;
            float _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2;
            Unity_GradientNoise_float(_TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3, _Property_a99d72da27ad61808450823e32ff058d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2);
            float _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2;
            Unity_Multiply_float_float(_Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2, _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2);
            UnityTexture2D _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_C3D58803);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.tex, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.samplerstate, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.GetTransformedUV(IN.uv0.xy), 0);
            #endif
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_R_5 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.r;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_G_6 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.g;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_B_7 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.b;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_A_8 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.a;
            float _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2;
            Unity_DotProduct_float4(_SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0, float4(0, 0.5, 0, 0), _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2);
            float _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2;
            Unity_Multiply_float_float(_Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2, _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2, _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2);
            float _Split_00fd5896a472d482b73269374a947f07_R_1 = IN.WorldSpacePosition[0];
            float _Split_00fd5896a472d482b73269374a947f07_G_2 = IN.WorldSpacePosition[1];
            float _Split_00fd5896a472d482b73269374a947f07_B_3 = IN.WorldSpacePosition[2];
            float _Split_00fd5896a472d482b73269374a947f07_A_4 = 0;
            float _Add_811329329cfbd18fb3e807f2cd027976_Out_2;
            Unity_Add_float(_Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2, _Split_00fd5896a472d482b73269374a947f07_R_1, _Add_811329329cfbd18fb3e807f2cd027976_Out_2);
            float4 _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4;
            float3 _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5;
            float2 _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6;
            Unity_Combine_float(_Add_811329329cfbd18fb3e807f2cd027976_Out_2, _Split_00fd5896a472d482b73269374a947f07_G_2, _Split_00fd5896a472d482b73269374a947f07_B_3, 0, _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4, _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5, _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6);
            float4 _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0 = IN.uv0;
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_R_1 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[0];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[1];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_B_3 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[2];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_A_4 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[3];
            float3 _Lerp_671d23c02bbda885b7de9901db258bec_Out_3;
            Unity_Lerp_float3(IN.WorldSpacePosition, (_Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4.xyz), (_Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2.xxx), _Lerp_671d23c02bbda885b7de9901db258bec_Out_3);
            float3 _Transform_5289292aa2111a8493f276a8d330172a_Out_1 = TransformWorldToObject(_Lerp_671d23c02bbda885b7de9901db258bec_Out_3.xyz);
            description.Position = _Transform_5289292aa2111a8493f276a8d330172a_Out_1;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 NormalTS;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_b48e111d2b6a488c93d88bd4f120c5f9_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_E933B04B);
            float4 _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0 = SAMPLE_TEXTURE2D(_Property_b48e111d2b6a488c93d88bd4f120c5f9_Out_0.tex, _Property_b48e111d2b6a488c93d88bd4f120c5f9_Out_0.samplerstate, _Property_b48e111d2b6a488c93d88bd4f120c5f9_Out_0.GetTransformedUV(IN.uv0.xy));
            _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.rgb = UnpackNormal(_SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0);
            float _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_R_4 = _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.r;
            float _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_G_5 = _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.g;
            float _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_B_6 = _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.b;
            float _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_A_7 = _SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.a;
            float _Property_032c33beaac840d58b4e508ae522a70c_Out_0 = _Normal_Strength;
            float3 _NormalStrength_9b9e9bf8069a42888b650f5735fe4abd_Out_2;
            Unity_NormalStrength_float((_SampleTexture2D_efe8f8fc91d40c88ae919e88dfd1758e_RGBA_0.xyz), _Property_032c33beaac840d58b4e508ae522a70c_Out_0, _NormalStrength_9b9e9bf8069a42888b650f5735fe4abd_Out_2);
            UnityTexture2D _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_B18C2857);
            float4 _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0 = SAMPLE_TEXTURE2D(_Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.tex, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.samplerstate, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.GetTransformedUV(IN.uv0.xy));
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_R_4 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.r;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_G_5 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.g;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_B_6 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.b;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.a;
            float _Property_258b7abc9b6247baa769a139aaecb429_Out_0 = Alpha_Threshold;
            surface.NormalTS = _NormalStrength_9b9e9bf8069a42888b650f5735fe4abd_Out_2;
            surface.Alpha = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7;
            surface.AlphaClipThreshold = _Property_258b7abc9b6247baa769a139aaecb429_Out_0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.WorldSpaceTangent =                          TransformObjectToWorldDir(input.tangentOS.xyz);
            output.ObjectSpaceBiTangent =                       normalize(cross(input.normalOS, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
            output.WorldSpaceBiTangent =                        TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
            output.ObjectSpacePosition =                        input.positionOS;
            output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
            output.uv0 =                                        input.uv0;
            output.TimeParameters =                             _TimeParameters.xyz;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
            // FragInputs from VFX come from two places: Interpolator or CBuffer.
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
            output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
        
        
            output.uv0 = input.texCoord0;
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthNormalsOnlyPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "Meta"
            Tags
            {
                "LightMode" = "Meta"
            }
        
        // Render State
        Cull Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma only_renderers gles gles3 glcore d3d11
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        #pragma shader_feature _ EDITOR_VISUALIZATION
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define ATTRIBUTES_NEED_TEXCOORD2
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define VARYINGS_NEED_TEXCOORD1
        #define VARYINGS_NEED_TEXCOORD2
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_META
        #define _FOG_FRAGMENT 1
        #define _ALPHATEST_ON 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
             float4 uv1 : TEXCOORD1;
             float4 uv2 : TEXCOORD2;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float4 texCoord0;
             float4 texCoord1;
             float4 texCoord2;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 WorldSpacePosition;
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 WorldSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 WorldSpaceTangent;
             float3 ObjectSpaceBiTangent;
             float3 WorldSpaceBiTangent;
             float3 ObjectSpacePosition;
             float3 WorldSpacePosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float3 interp0 : INTERP0;
             float4 interp1 : INTERP1;
             float4 interp2 : INTERP2;
             float4 interp3 : INTERP3;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.interp0.xyz =  input.positionWS;
            output.interp1.xyzw =  input.texCoord0;
            output.interp2.xyzw =  input.texCoord1;
            output.interp3.xyzw =  input.texCoord2;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.positionWS = input.interp0.xyz;
            output.texCoord0 = input.interp1.xyzw;
            output.texCoord1 = input.interp2.xyzw;
            output.texCoord2 = input.interp3.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 Texture2D_B18C2857_TexelSize;
        float4 Texture2D_E933B04B_TexelSize;
        float2 Vector2_EC93FC28;
        float Vector1_B7976139;
        float Vector1_C39A3E90;
        float4 Texture2D_C3D58803_TexelSize;
        float Alpha_Threshold;
        float _Normal_Strength;
        float _Luminosity;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(Texture2D_B18C2857);
        SAMPLER(samplerTexture2D_B18C2857);
        TEXTURE2D(Texture2D_E933B04B);
        SAMPLER(samplerTexture2D_E933B04B);
        TEXTURE2D(Texture2D_C3D58803);
        SAMPLER(samplerTexture2D_C3D58803);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        
        float2 Unity_GradientNoise_Dir_float(float2 p)
        {
            // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
            p = p % 289;
            // need full precision, otherwise half overflows when p > 1
            float x = float(34 * p.x + 1) * p.x % 289 + p.y;
            x = (34 * x + 1) * x % 289;
            x = frac(x / 41) * 2 - 1;
            return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
        }
        
        void Unity_GradientNoise_float(float2 UV, float Scale, out float Out)
        {
            float2 p = UV * Scale;
            float2 ip = floor(p);
            float2 fp = frac(p);
            float d00 = dot(Unity_GradientNoise_Dir_float(ip), fp);
            float d01 = dot(Unity_GradientNoise_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
            float d10 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
            float d11 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
            fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
            Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_DotProduct_float4(float4 A, float4 B, out float Out)
        {
            Out = dot(A, B);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        void Unity_Hue_Degrees_float(float3 In, float Offset, out float3 Out)
        {
            // RGB to HSV
            float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
            float4 P = lerp(float4(In.bg, K.wz), float4(In.gb, K.xy), step(In.b, In.g));
            float4 Q = lerp(float4(P.xyw, In.r), float4(In.r, P.yzx), step(P.x, In.r));
            float D = Q.x - min(Q.w, Q.y);
            float E = 1e-10;
            float V = (D == 0) ? Q.x : (Q.x + E);
            float3 hsv = float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), V);
        
            float hue = hsv.x + Offset / 360;
            hsv.x = (hue < 0)
                    ? hue + 1
                    : (hue > 1)
                        ? hue - 1
                        : hue;
        
            // HSV to RGB
            float4 K2 = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
            float3 P2 = abs(frac(hsv.xxx + K2.xyz) * 6.0 - K2.www);
            Out = hsv.z * lerp(K2.xxx, saturate(P2 - K2.xxx), hsv.y);
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float _Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0 = Vector1_C39A3E90;
            float2 _Property_51db9fe5f8707d8fa4507c481749556e_Out_0 = Vector2_EC93FC28;
            float2 _Multiply_d93244631121ab899bc52aade4ded114_Out_2;
            Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Property_51db9fe5f8707d8fa4507c481749556e_Out_0, _Multiply_d93244631121ab899bc52aade4ded114_Out_2);
            float2 _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3;
            Unity_TilingAndOffset_float((IN.WorldSpacePosition.xy), float2 (1, 1), _Multiply_d93244631121ab899bc52aade4ded114_Out_2, _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3);
            float _Property_a99d72da27ad61808450823e32ff058d_Out_0 = Vector1_B7976139;
            float _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2;
            Unity_GradientNoise_float(_TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3, _Property_a99d72da27ad61808450823e32ff058d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2);
            float _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2;
            Unity_Multiply_float_float(_Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2, _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2);
            UnityTexture2D _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_C3D58803);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.tex, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.samplerstate, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.GetTransformedUV(IN.uv0.xy), 0);
            #endif
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_R_5 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.r;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_G_6 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.g;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_B_7 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.b;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_A_8 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.a;
            float _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2;
            Unity_DotProduct_float4(_SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0, float4(0, 0.5, 0, 0), _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2);
            float _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2;
            Unity_Multiply_float_float(_Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2, _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2, _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2);
            float _Split_00fd5896a472d482b73269374a947f07_R_1 = IN.WorldSpacePosition[0];
            float _Split_00fd5896a472d482b73269374a947f07_G_2 = IN.WorldSpacePosition[1];
            float _Split_00fd5896a472d482b73269374a947f07_B_3 = IN.WorldSpacePosition[2];
            float _Split_00fd5896a472d482b73269374a947f07_A_4 = 0;
            float _Add_811329329cfbd18fb3e807f2cd027976_Out_2;
            Unity_Add_float(_Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2, _Split_00fd5896a472d482b73269374a947f07_R_1, _Add_811329329cfbd18fb3e807f2cd027976_Out_2);
            float4 _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4;
            float3 _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5;
            float2 _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6;
            Unity_Combine_float(_Add_811329329cfbd18fb3e807f2cd027976_Out_2, _Split_00fd5896a472d482b73269374a947f07_G_2, _Split_00fd5896a472d482b73269374a947f07_B_3, 0, _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4, _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5, _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6);
            float4 _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0 = IN.uv0;
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_R_1 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[0];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[1];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_B_3 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[2];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_A_4 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[3];
            float3 _Lerp_671d23c02bbda885b7de9901db258bec_Out_3;
            Unity_Lerp_float3(IN.WorldSpacePosition, (_Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4.xyz), (_Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2.xxx), _Lerp_671d23c02bbda885b7de9901db258bec_Out_3);
            float3 _Transform_5289292aa2111a8493f276a8d330172a_Out_1 = TransformWorldToObject(_Lerp_671d23c02bbda885b7de9901db258bec_Out_3.xyz);
            description.Position = _Transform_5289292aa2111a8493f276a8d330172a_Out_1;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float3 Emission;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float _Property_dd9b23bb0e8d4e23828a2903b0e8f1bd_Out_0 = _Luminosity;
            UnityTexture2D _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_B18C2857);
            float4 _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0 = SAMPLE_TEXTURE2D(_Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.tex, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.samplerstate, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.GetTransformedUV(IN.uv0.xy));
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_R_4 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.r;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_G_5 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.g;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_B_6 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.b;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.a;
            float _GradientNoise_668f1ac4661940099bbb63949e9cf082_Out_2;
            Unity_GradientNoise_float((IN.WorldSpacePosition.xy), 3, _GradientNoise_668f1ac4661940099bbb63949e9cf082_Out_2);
            float _Add_bca95b13bbde4c8b8a8d5c3c5edbfeab_Out_2;
            Unity_Add_float(_GradientNoise_668f1ac4661940099bbb63949e9cf082_Out_2, 0.6, _Add_bca95b13bbde4c8b8a8d5c3c5edbfeab_Out_2);
            float4 _Multiply_9c21544a05f2400b8735e3a7f02361f4_Out_2;
            Unity_Multiply_float4_float4(_SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0, (_Add_bca95b13bbde4c8b8a8d5c3c5edbfeab_Out_2.xxxx), _Multiply_9c21544a05f2400b8735e3a7f02361f4_Out_2);
            float4 _Multiply_2528287665c24e6d81e3369b4ca73a11_Out_2;
            Unity_Multiply_float4_float4((_Property_dd9b23bb0e8d4e23828a2903b0e8f1bd_Out_0.xxxx), _Multiply_9c21544a05f2400b8735e3a7f02361f4_Out_2, _Multiply_2528287665c24e6d81e3369b4ca73a11_Out_2);
            float3 _Hue_e41f1877edfd4476b9a0d85c9616cac1_Out_2;
            Unity_Hue_Degrees_float((_Multiply_2528287665c24e6d81e3369b4ca73a11_Out_2.xyz), 5, _Hue_e41f1877edfd4476b9a0d85c9616cac1_Out_2);
            float _Property_258b7abc9b6247baa769a139aaecb429_Out_0 = Alpha_Threshold;
            surface.BaseColor = _Hue_e41f1877edfd4476b9a0d85c9616cac1_Out_2;
            surface.Emission = float3(0, 0, 0);
            surface.Alpha = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7;
            surface.AlphaClipThreshold = _Property_258b7abc9b6247baa769a139aaecb429_Out_0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.WorldSpaceTangent =                          TransformObjectToWorldDir(input.tangentOS.xyz);
            output.ObjectSpaceBiTangent =                       normalize(cross(input.normalOS, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
            output.WorldSpaceBiTangent =                        TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
            output.ObjectSpacePosition =                        input.positionOS;
            output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
            output.uv0 =                                        input.uv0;
            output.TimeParameters =                             _TimeParameters.xyz;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
            // FragInputs from VFX come from two places: Interpolator or CBuffer.
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
            output.WorldSpacePosition = input.positionWS;
            output.uv0 = input.texCoord0;
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/LightingMetaPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "SceneSelectionPass"
            Tags
            {
                "LightMode" = "SceneSelectionPass"
            }
        
        // Render State
        Cull Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma only_renderers gles gles3 glcore d3d11
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHONLY
        #define SCENESELECTIONPASS 1
        #define ALPHA_CLIP_THRESHOLD 1
        #define _ALPHATEST_ON 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 WorldSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 WorldSpaceTangent;
             float3 ObjectSpaceBiTangent;
             float3 WorldSpaceBiTangent;
             float3 ObjectSpacePosition;
             float3 WorldSpacePosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 interp0 : INTERP0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.interp0.xyzw =  input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.interp0.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 Texture2D_B18C2857_TexelSize;
        float4 Texture2D_E933B04B_TexelSize;
        float2 Vector2_EC93FC28;
        float Vector1_B7976139;
        float Vector1_C39A3E90;
        float4 Texture2D_C3D58803_TexelSize;
        float Alpha_Threshold;
        float _Normal_Strength;
        float _Luminosity;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(Texture2D_B18C2857);
        SAMPLER(samplerTexture2D_B18C2857);
        TEXTURE2D(Texture2D_E933B04B);
        SAMPLER(samplerTexture2D_E933B04B);
        TEXTURE2D(Texture2D_C3D58803);
        SAMPLER(samplerTexture2D_C3D58803);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        
        float2 Unity_GradientNoise_Dir_float(float2 p)
        {
            // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
            p = p % 289;
            // need full precision, otherwise half overflows when p > 1
            float x = float(34 * p.x + 1) * p.x % 289 + p.y;
            x = (34 * x + 1) * x % 289;
            x = frac(x / 41) * 2 - 1;
            return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
        }
        
        void Unity_GradientNoise_float(float2 UV, float Scale, out float Out)
        {
            float2 p = UV * Scale;
            float2 ip = floor(p);
            float2 fp = frac(p);
            float d00 = dot(Unity_GradientNoise_Dir_float(ip), fp);
            float d01 = dot(Unity_GradientNoise_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
            float d10 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
            float d11 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
            fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
            Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_DotProduct_float4(float4 A, float4 B, out float Out)
        {
            Out = dot(A, B);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float _Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0 = Vector1_C39A3E90;
            float2 _Property_51db9fe5f8707d8fa4507c481749556e_Out_0 = Vector2_EC93FC28;
            float2 _Multiply_d93244631121ab899bc52aade4ded114_Out_2;
            Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Property_51db9fe5f8707d8fa4507c481749556e_Out_0, _Multiply_d93244631121ab899bc52aade4ded114_Out_2);
            float2 _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3;
            Unity_TilingAndOffset_float((IN.WorldSpacePosition.xy), float2 (1, 1), _Multiply_d93244631121ab899bc52aade4ded114_Out_2, _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3);
            float _Property_a99d72da27ad61808450823e32ff058d_Out_0 = Vector1_B7976139;
            float _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2;
            Unity_GradientNoise_float(_TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3, _Property_a99d72da27ad61808450823e32ff058d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2);
            float _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2;
            Unity_Multiply_float_float(_Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2, _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2);
            UnityTexture2D _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_C3D58803);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.tex, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.samplerstate, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.GetTransformedUV(IN.uv0.xy), 0);
            #endif
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_R_5 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.r;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_G_6 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.g;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_B_7 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.b;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_A_8 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.a;
            float _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2;
            Unity_DotProduct_float4(_SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0, float4(0, 0.5, 0, 0), _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2);
            float _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2;
            Unity_Multiply_float_float(_Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2, _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2, _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2);
            float _Split_00fd5896a472d482b73269374a947f07_R_1 = IN.WorldSpacePosition[0];
            float _Split_00fd5896a472d482b73269374a947f07_G_2 = IN.WorldSpacePosition[1];
            float _Split_00fd5896a472d482b73269374a947f07_B_3 = IN.WorldSpacePosition[2];
            float _Split_00fd5896a472d482b73269374a947f07_A_4 = 0;
            float _Add_811329329cfbd18fb3e807f2cd027976_Out_2;
            Unity_Add_float(_Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2, _Split_00fd5896a472d482b73269374a947f07_R_1, _Add_811329329cfbd18fb3e807f2cd027976_Out_2);
            float4 _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4;
            float3 _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5;
            float2 _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6;
            Unity_Combine_float(_Add_811329329cfbd18fb3e807f2cd027976_Out_2, _Split_00fd5896a472d482b73269374a947f07_G_2, _Split_00fd5896a472d482b73269374a947f07_B_3, 0, _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4, _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5, _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6);
            float4 _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0 = IN.uv0;
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_R_1 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[0];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[1];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_B_3 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[2];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_A_4 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[3];
            float3 _Lerp_671d23c02bbda885b7de9901db258bec_Out_3;
            Unity_Lerp_float3(IN.WorldSpacePosition, (_Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4.xyz), (_Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2.xxx), _Lerp_671d23c02bbda885b7de9901db258bec_Out_3);
            float3 _Transform_5289292aa2111a8493f276a8d330172a_Out_1 = TransformWorldToObject(_Lerp_671d23c02bbda885b7de9901db258bec_Out_3.xyz);
            description.Position = _Transform_5289292aa2111a8493f276a8d330172a_Out_1;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_B18C2857);
            float4 _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0 = SAMPLE_TEXTURE2D(_Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.tex, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.samplerstate, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.GetTransformedUV(IN.uv0.xy));
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_R_4 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.r;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_G_5 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.g;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_B_6 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.b;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.a;
            float _Property_258b7abc9b6247baa769a139aaecb429_Out_0 = Alpha_Threshold;
            surface.Alpha = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7;
            surface.AlphaClipThreshold = _Property_258b7abc9b6247baa769a139aaecb429_Out_0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.WorldSpaceTangent =                          TransformObjectToWorldDir(input.tangentOS.xyz);
            output.ObjectSpaceBiTangent =                       normalize(cross(input.normalOS, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
            output.WorldSpaceBiTangent =                        TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
            output.ObjectSpacePosition =                        input.positionOS;
            output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
            output.uv0 =                                        input.uv0;
            output.TimeParameters =                             _TimeParameters.xyz;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
            // FragInputs from VFX come from two places: Interpolator or CBuffer.
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
            output.uv0 = input.texCoord0;
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "ScenePickingPass"
            Tags
            {
                "LightMode" = "Picking"
            }
        
        // Render State
        Cull Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma only_renderers gles gles3 glcore d3d11
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHONLY
        #define SCENEPICKINGPASS 1
        #define ALPHA_CLIP_THRESHOLD 1
        #define _ALPHATEST_ON 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 WorldSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 WorldSpaceTangent;
             float3 ObjectSpaceBiTangent;
             float3 WorldSpaceBiTangent;
             float3 ObjectSpacePosition;
             float3 WorldSpacePosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 interp0 : INTERP0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.interp0.xyzw =  input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.interp0.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 Texture2D_B18C2857_TexelSize;
        float4 Texture2D_E933B04B_TexelSize;
        float2 Vector2_EC93FC28;
        float Vector1_B7976139;
        float Vector1_C39A3E90;
        float4 Texture2D_C3D58803_TexelSize;
        float Alpha_Threshold;
        float _Normal_Strength;
        float _Luminosity;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(Texture2D_B18C2857);
        SAMPLER(samplerTexture2D_B18C2857);
        TEXTURE2D(Texture2D_E933B04B);
        SAMPLER(samplerTexture2D_E933B04B);
        TEXTURE2D(Texture2D_C3D58803);
        SAMPLER(samplerTexture2D_C3D58803);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        
        float2 Unity_GradientNoise_Dir_float(float2 p)
        {
            // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
            p = p % 289;
            // need full precision, otherwise half overflows when p > 1
            float x = float(34 * p.x + 1) * p.x % 289 + p.y;
            x = (34 * x + 1) * x % 289;
            x = frac(x / 41) * 2 - 1;
            return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
        }
        
        void Unity_GradientNoise_float(float2 UV, float Scale, out float Out)
        {
            float2 p = UV * Scale;
            float2 ip = floor(p);
            float2 fp = frac(p);
            float d00 = dot(Unity_GradientNoise_Dir_float(ip), fp);
            float d01 = dot(Unity_GradientNoise_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
            float d10 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
            float d11 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
            fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
            Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_DotProduct_float4(float4 A, float4 B, out float Out)
        {
            Out = dot(A, B);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float _Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0 = Vector1_C39A3E90;
            float2 _Property_51db9fe5f8707d8fa4507c481749556e_Out_0 = Vector2_EC93FC28;
            float2 _Multiply_d93244631121ab899bc52aade4ded114_Out_2;
            Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Property_51db9fe5f8707d8fa4507c481749556e_Out_0, _Multiply_d93244631121ab899bc52aade4ded114_Out_2);
            float2 _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3;
            Unity_TilingAndOffset_float((IN.WorldSpacePosition.xy), float2 (1, 1), _Multiply_d93244631121ab899bc52aade4ded114_Out_2, _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3);
            float _Property_a99d72da27ad61808450823e32ff058d_Out_0 = Vector1_B7976139;
            float _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2;
            Unity_GradientNoise_float(_TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3, _Property_a99d72da27ad61808450823e32ff058d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2);
            float _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2;
            Unity_Multiply_float_float(_Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2, _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2);
            UnityTexture2D _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_C3D58803);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.tex, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.samplerstate, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.GetTransformedUV(IN.uv0.xy), 0);
            #endif
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_R_5 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.r;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_G_6 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.g;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_B_7 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.b;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_A_8 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.a;
            float _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2;
            Unity_DotProduct_float4(_SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0, float4(0, 0.5, 0, 0), _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2);
            float _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2;
            Unity_Multiply_float_float(_Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2, _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2, _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2);
            float _Split_00fd5896a472d482b73269374a947f07_R_1 = IN.WorldSpacePosition[0];
            float _Split_00fd5896a472d482b73269374a947f07_G_2 = IN.WorldSpacePosition[1];
            float _Split_00fd5896a472d482b73269374a947f07_B_3 = IN.WorldSpacePosition[2];
            float _Split_00fd5896a472d482b73269374a947f07_A_4 = 0;
            float _Add_811329329cfbd18fb3e807f2cd027976_Out_2;
            Unity_Add_float(_Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2, _Split_00fd5896a472d482b73269374a947f07_R_1, _Add_811329329cfbd18fb3e807f2cd027976_Out_2);
            float4 _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4;
            float3 _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5;
            float2 _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6;
            Unity_Combine_float(_Add_811329329cfbd18fb3e807f2cd027976_Out_2, _Split_00fd5896a472d482b73269374a947f07_G_2, _Split_00fd5896a472d482b73269374a947f07_B_3, 0, _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4, _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5, _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6);
            float4 _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0 = IN.uv0;
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_R_1 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[0];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[1];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_B_3 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[2];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_A_4 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[3];
            float3 _Lerp_671d23c02bbda885b7de9901db258bec_Out_3;
            Unity_Lerp_float3(IN.WorldSpacePosition, (_Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4.xyz), (_Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2.xxx), _Lerp_671d23c02bbda885b7de9901db258bec_Out_3);
            float3 _Transform_5289292aa2111a8493f276a8d330172a_Out_1 = TransformWorldToObject(_Lerp_671d23c02bbda885b7de9901db258bec_Out_3.xyz);
            description.Position = _Transform_5289292aa2111a8493f276a8d330172a_Out_1;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_B18C2857);
            float4 _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0 = SAMPLE_TEXTURE2D(_Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.tex, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.samplerstate, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.GetTransformedUV(IN.uv0.xy));
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_R_4 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.r;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_G_5 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.g;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_B_6 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.b;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.a;
            float _Property_258b7abc9b6247baa769a139aaecb429_Out_0 = Alpha_Threshold;
            surface.Alpha = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7;
            surface.AlphaClipThreshold = _Property_258b7abc9b6247baa769a139aaecb429_Out_0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.WorldSpaceTangent =                          TransformObjectToWorldDir(input.tangentOS.xyz);
            output.ObjectSpaceBiTangent =                       normalize(cross(input.normalOS, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
            output.WorldSpaceBiTangent =                        TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
            output.ObjectSpacePosition =                        input.positionOS;
            output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
            output.uv0 =                                        input.uv0;
            output.TimeParameters =                             _TimeParameters.xyz;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
            // FragInputs from VFX come from two places: Interpolator or CBuffer.
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
            output.uv0 = input.texCoord0;
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            // Name: <None>
            Tags
            {
                "LightMode" = "Universal2D"
            }
        
        // Render State
        Cull Off
        Blend One Zero
        ZTest LEqual
        ZWrite On
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma only_renderers gles gles3 glcore d3d11
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_2D
        #define _ALPHATEST_ON 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 WorldSpacePosition;
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 WorldSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 WorldSpaceTangent;
             float3 ObjectSpaceBiTangent;
             float3 WorldSpaceBiTangent;
             float3 ObjectSpacePosition;
             float3 WorldSpacePosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float3 interp0 : INTERP0;
             float4 interp1 : INTERP1;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.interp0.xyz =  input.positionWS;
            output.interp1.xyzw =  input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.positionWS = input.interp0.xyz;
            output.texCoord0 = input.interp1.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 Texture2D_B18C2857_TexelSize;
        float4 Texture2D_E933B04B_TexelSize;
        float2 Vector2_EC93FC28;
        float Vector1_B7976139;
        float Vector1_C39A3E90;
        float4 Texture2D_C3D58803_TexelSize;
        float Alpha_Threshold;
        float _Normal_Strength;
        float _Luminosity;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(Texture2D_B18C2857);
        SAMPLER(samplerTexture2D_B18C2857);
        TEXTURE2D(Texture2D_E933B04B);
        SAMPLER(samplerTexture2D_E933B04B);
        TEXTURE2D(Texture2D_C3D58803);
        SAMPLER(samplerTexture2D_C3D58803);
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        
        float2 Unity_GradientNoise_Dir_float(float2 p)
        {
            // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
            p = p % 289;
            // need full precision, otherwise half overflows when p > 1
            float x = float(34 * p.x + 1) * p.x % 289 + p.y;
            x = (34 * x + 1) * x % 289;
            x = frac(x / 41) * 2 - 1;
            return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
        }
        
        void Unity_GradientNoise_float(float2 UV, float Scale, out float Out)
        {
            float2 p = UV * Scale;
            float2 ip = floor(p);
            float2 fp = frac(p);
            float d00 = dot(Unity_GradientNoise_Dir_float(ip), fp);
            float d01 = dot(Unity_GradientNoise_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
            float d10 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
            float d11 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
            fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
            Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_DotProduct_float4(float4 A, float4 B, out float Out)
        {
            Out = dot(A, B);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        void Unity_Hue_Degrees_float(float3 In, float Offset, out float3 Out)
        {
            // RGB to HSV
            float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
            float4 P = lerp(float4(In.bg, K.wz), float4(In.gb, K.xy), step(In.b, In.g));
            float4 Q = lerp(float4(P.xyw, In.r), float4(In.r, P.yzx), step(P.x, In.r));
            float D = Q.x - min(Q.w, Q.y);
            float E = 1e-10;
            float V = (D == 0) ? Q.x : (Q.x + E);
            float3 hsv = float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), V);
        
            float hue = hsv.x + Offset / 360;
            hsv.x = (hue < 0)
                    ? hue + 1
                    : (hue > 1)
                        ? hue - 1
                        : hue;
        
            // HSV to RGB
            float4 K2 = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
            float3 P2 = abs(frac(hsv.xxx + K2.xyz) * 6.0 - K2.www);
            Out = hsv.z * lerp(K2.xxx, saturate(P2 - K2.xxx), hsv.y);
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float _Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0 = Vector1_C39A3E90;
            float2 _Property_51db9fe5f8707d8fa4507c481749556e_Out_0 = Vector2_EC93FC28;
            float2 _Multiply_d93244631121ab899bc52aade4ded114_Out_2;
            Unity_Multiply_float2_float2((IN.TimeParameters.x.xx), _Property_51db9fe5f8707d8fa4507c481749556e_Out_0, _Multiply_d93244631121ab899bc52aade4ded114_Out_2);
            float2 _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3;
            Unity_TilingAndOffset_float((IN.WorldSpacePosition.xy), float2 (1, 1), _Multiply_d93244631121ab899bc52aade4ded114_Out_2, _TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3);
            float _Property_a99d72da27ad61808450823e32ff058d_Out_0 = Vector1_B7976139;
            float _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2;
            Unity_GradientNoise_float(_TilingAndOffset_3530a825f1ab538c83081b0cd6bf5c56_Out_3, _Property_a99d72da27ad61808450823e32ff058d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2);
            float _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2;
            Unity_Multiply_float_float(_Property_76ac5abcc34fd283a4f4bb1d20eeef0d_Out_0, _GradientNoise_db66c0952848e8849986524e24bc213d_Out_2, _Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2);
            UnityTexture2D _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_C3D58803);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.tex, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.samplerstate, _Property_10032359bb7d4b8c96b9f4726b1cf6c5_Out_0.GetTransformedUV(IN.uv0.xy), 0);
            #endif
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_R_5 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.r;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_G_6 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.g;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_B_7 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.b;
            float _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_A_8 = _SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0.a;
            float _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2;
            Unity_DotProduct_float4(_SampleTexture2DLOD_79c57572601e4fb19d609798681e84af_RGBA_0, float4(0, 0.5, 0, 0), _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2);
            float _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2;
            Unity_Multiply_float_float(_Multiply_2de06a712ae58584b3aa5519bd3071cf_Out_2, _DotProduct_a2b4edeb9c9344d69e54a83053a9c111_Out_2, _Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2);
            float _Split_00fd5896a472d482b73269374a947f07_R_1 = IN.WorldSpacePosition[0];
            float _Split_00fd5896a472d482b73269374a947f07_G_2 = IN.WorldSpacePosition[1];
            float _Split_00fd5896a472d482b73269374a947f07_B_3 = IN.WorldSpacePosition[2];
            float _Split_00fd5896a472d482b73269374a947f07_A_4 = 0;
            float _Add_811329329cfbd18fb3e807f2cd027976_Out_2;
            Unity_Add_float(_Multiply_e8f5063f5242f48fbb4188ca9a3d2e86_Out_2, _Split_00fd5896a472d482b73269374a947f07_R_1, _Add_811329329cfbd18fb3e807f2cd027976_Out_2);
            float4 _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4;
            float3 _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5;
            float2 _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6;
            Unity_Combine_float(_Add_811329329cfbd18fb3e807f2cd027976_Out_2, _Split_00fd5896a472d482b73269374a947f07_G_2, _Split_00fd5896a472d482b73269374a947f07_B_3, 0, _Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4, _Combine_996911cbfbe4c48695bf9a06762190cb_RGB_5, _Combine_996911cbfbe4c48695bf9a06762190cb_RG_6);
            float4 _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0 = IN.uv0;
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_R_1 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[0];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[1];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_B_3 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[2];
            float _Split_ea7d7bb5df72d187b5c28e6d812f5283_A_4 = _UV_b8b1d1dc8b5f8084a0b032acb7a1487a_Out_0[3];
            float3 _Lerp_671d23c02bbda885b7de9901db258bec_Out_3;
            Unity_Lerp_float3(IN.WorldSpacePosition, (_Combine_996911cbfbe4c48695bf9a06762190cb_RGBA_4.xyz), (_Split_ea7d7bb5df72d187b5c28e6d812f5283_G_2.xxx), _Lerp_671d23c02bbda885b7de9901db258bec_Out_3);
            float3 _Transform_5289292aa2111a8493f276a8d330172a_Out_1 = TransformWorldToObject(_Lerp_671d23c02bbda885b7de9901db258bec_Out_3.xyz);
            description.Position = _Transform_5289292aa2111a8493f276a8d330172a_Out_1;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float _Property_dd9b23bb0e8d4e23828a2903b0e8f1bd_Out_0 = _Luminosity;
            UnityTexture2D _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_B18C2857);
            float4 _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0 = SAMPLE_TEXTURE2D(_Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.tex, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.samplerstate, _Property_bf53f3d1be03348da61e6edd24adbfc7_Out_0.GetTransformedUV(IN.uv0.xy));
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_R_4 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.r;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_G_5 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.g;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_B_6 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.b;
            float _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7 = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0.a;
            float _GradientNoise_668f1ac4661940099bbb63949e9cf082_Out_2;
            Unity_GradientNoise_float((IN.WorldSpacePosition.xy), 3, _GradientNoise_668f1ac4661940099bbb63949e9cf082_Out_2);
            float _Add_bca95b13bbde4c8b8a8d5c3c5edbfeab_Out_2;
            Unity_Add_float(_GradientNoise_668f1ac4661940099bbb63949e9cf082_Out_2, 0.6, _Add_bca95b13bbde4c8b8a8d5c3c5edbfeab_Out_2);
            float4 _Multiply_9c21544a05f2400b8735e3a7f02361f4_Out_2;
            Unity_Multiply_float4_float4(_SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_RGBA_0, (_Add_bca95b13bbde4c8b8a8d5c3c5edbfeab_Out_2.xxxx), _Multiply_9c21544a05f2400b8735e3a7f02361f4_Out_2);
            float4 _Multiply_2528287665c24e6d81e3369b4ca73a11_Out_2;
            Unity_Multiply_float4_float4((_Property_dd9b23bb0e8d4e23828a2903b0e8f1bd_Out_0.xxxx), _Multiply_9c21544a05f2400b8735e3a7f02361f4_Out_2, _Multiply_2528287665c24e6d81e3369b4ca73a11_Out_2);
            float3 _Hue_e41f1877edfd4476b9a0d85c9616cac1_Out_2;
            Unity_Hue_Degrees_float((_Multiply_2528287665c24e6d81e3369b4ca73a11_Out_2.xyz), 5, _Hue_e41f1877edfd4476b9a0d85c9616cac1_Out_2);
            float _Property_258b7abc9b6247baa769a139aaecb429_Out_0 = Alpha_Threshold;
            surface.BaseColor = _Hue_e41f1877edfd4476b9a0d85c9616cac1_Out_2;
            surface.Alpha = _SampleTexture2D_5373afedb216a581b7c0a045f8e2abad_A_7;
            surface.AlphaClipThreshold = _Property_258b7abc9b6247baa769a139aaecb429_Out_0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.WorldSpaceTangent =                          TransformObjectToWorldDir(input.tangentOS.xyz);
            output.ObjectSpaceBiTangent =                       normalize(cross(input.normalOS, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
            output.WorldSpaceBiTangent =                        TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
            output.ObjectSpacePosition =                        input.positionOS;
            output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
            output.uv0 =                                        input.uv0;
            output.TimeParameters =                             _TimeParameters.xyz;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
            // FragInputs from VFX come from two places: Interpolator or CBuffer.
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
            output.WorldSpacePosition = input.positionWS;
            output.uv0 = input.texCoord0;
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBR2DPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
    }
    CustomEditorForRenderPipeline "UnityEditor.ShaderGraphLitGUI" "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"
    CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
        Dependency "BillboardShader" = "Hidden/Nature/Tree Soft Occlusion Bark Rendertex"
    FallBack "Hidden/Shader Graph/FallbackError"

}