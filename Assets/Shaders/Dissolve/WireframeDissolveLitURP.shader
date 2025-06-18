Shader "Universal Render Pipeline/Wireframe/DisplacementFade-SimpleLit"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" { }
        _Normal ("Normal Map", 2D) = "bump" { }
        _WireThickness ("Wire Thickness", RANGE(0, 800)) = 100
        [HDR] _WireColor ("Wire Color", Color) = (0,1,1,1)
        [Toggle(INVERT)] _INVERT("Invert", Float) = 1
        _MovingSlider ("Moving Slider", RANGE(-10, 10)) = 10
        _Extrude("Extrude Amount", RANGE(-10, 10)) = 10
        _WireFrameStay ("Wire Stay", RANGE(-1, 1)) = 0
        
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
         float _MovingSlider;
         float _Extrude;
         float _WireFrameStay;
         CBUFFER_END

         TEXTURE2D(_MainTex);
         SAMPLER(sampler_MainTex);
         TEXTURE2D(_Normal);
         SAMPLER(sampler_Normal);

         struct Attributes
         {
             float4 positionOS       : POSITION;
             UNITY_VERTEX_INPUT_INSTANCE_ID
             float4 uv : TEXCOORD0;
             
             float3 normal :NORMAL;
         };

         struct v2g
         {
             float4 projectionSpaceVertex : SV_POSITION;
             float4 uv :TEXCOORD0;
             float4 worldSpacePosition : TEXCOORD2;
             float movingPos : TEXCOORD1;
             float3 normal:NORMAL;
             float3 worldNormal:TEXCOORD3;
             UNITY_VERTEX_OUTPUT_STEREO
         };

         v2g vert(Attributes input)
         {
             v2g output = (v2g)0;

             UNITY_SETUP_INSTANCE_ID(input);
             UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

             // move over the mesh y axis using a slider
             // with option to invert
             #if INVERT
             float movingPos = input.positionOS.y + _MovingSlider;
             #else
             float movingPos = 1- input.positionOS.y + _MovingSlider;
             #endif
             input.positionOS.xyz -= saturate(1-movingPos) * input.normal * _Extrude;
             VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
             output.projectionSpaceVertex = vertexInput.positionCS;
             output.worldSpacePosition = mul(UNITY_MATRIX_M, input.positionOS);
             output.uv = input.uv;
             output.worldNormal = TransformObjectToWorldNormal(input.normal);
             output.normal = input.normal;
             output.movingPos = movingPos;
             return output;
         }

         struct g2f
         {
             float4 projectionSpaceVertex : SV_POSITION;
             float4 dist : TEXCOORD1;
             float4 uv : TEXCOORD0;
             float4 worldSpacePosition : TEXCOORD3;
             float movingPos : TEXCOORD2;
             float3 normal:NORMAL;
             float3 worldNormal:TEXCOORD4;
             UNITY_VERTEX_OUTPUT_STEREO
         };

         [maxvertexcount(3)]
         void geom(triangle v2g i[3], inout TriangleStream<g2f> triangleStream)
         {
             UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i[0]);

             float2 p0 = i[0].projectionSpaceVertex.xy / i[0].projectionSpaceVertex.w;
             float2 p1 = i[1].projectionSpaceVertex.xy / i[1].projectionSpaceVertex.w;
             float2 p2 = i[2].projectionSpaceVertex.xy / i[2].projectionSpaceVertex.w;

             float2 edge0 = p2 - p1;
             float2 edge1 = p2 - p0;
             float2 edge2 = p1 - p0;

             // To find the distance to the opposite edge, we take the
             // formula for finding the area of a triangle Area = Base/2 * Height,
             // and solve for the Height = (Area * 2)/Base.
             // We can get the area of a triangle by taking its cross product
             // divided by 2.  However we can avoid dividing our area/base by 2
             // since our cross product will already be double our area.
             float area = abs(edge1.x * edge2.y - edge1.y * edge2.x);
             float wireThickness = 800 - _WireThickness;

             g2f o;
             UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
             o.projectionSpaceVertex = i[0].projectionSpaceVertex;
             o.worldSpacePosition = i[0].worldSpacePosition;
             o.dist.xyz = float3( (area / length(edge0)), 0.0, 0.0) * o.projectionSpaceVertex.w * wireThickness;
             o.dist.w = 1.0 / o.projectionSpaceVertex.w;
             o.uv = i[0].uv;
             o.normal = i[0].normal;
             o.worldNormal = i[0].worldNormal;
             o.movingPos = i[0].movingPos;
             triangleStream.Append(o);

             o.projectionSpaceVertex = i[1].projectionSpaceVertex;
             o.worldSpacePosition = i[1].worldSpacePosition;
             o.dist.xyz = float3(0.0, (area / length(edge1)), 0.0) * o.projectionSpaceVertex.w * wireThickness;
             o.dist.w = 1.0 / o.projectionSpaceVertex.w;
             o.uv = i[1].uv;
             o.normal = i[1].normal;
             o.worldNormal = i[1].worldNormal;
             o.movingPos = i[1].movingPos;
             triangleStream.Append(o);

             o.projectionSpaceVertex = i[2].projectionSpaceVertex;
             o.worldSpacePosition = i[2].worldSpacePosition;
             o.dist.xyz = float3(0.0, 0.0, (area / length(edge2))) * o.projectionSpaceVertex.w * wireThickness;
             o.dist.w = 1.0 / o.projectionSpaceVertex.w;
             o.uv = i[2].uv;
             o.normal = i[2].normal;
             o.worldNormal = i[2].worldNormal;
             o.movingPos = i[2].movingPos;
             triangleStream.Append(o);
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


         half4 frag(g2f i) : SV_Target
        {
            // Lấy khoảng cách đến các cạnh từ geometry shader
            float minDistanceToEdge = min(min(i.dist.x, i.dist.y), i.dist.z);

            // Xử lý normal map và ánh sáng
            float3 normal = UnpackNormal(SAMPLE_TEXTURE2D(_Normal, sampler_Normal, i.uv.xy));
            #if SHADOWS_SCREEN
                half4 shadowCoord = ComputeScreenPos(i.projectionSpaceVertex);
            #else
                half4 shadowCoord = TransformWorldToShadowCoord(i.worldSpacePosition.xyz);
            #endif 
            #if _MAIN_LIGHT_SHADOWS_CASCADE || _MAIN_LIGHT_SHADOWS
                Light light = GetMainLight(shadowCoord);
            #else
                Light light = GetMainLight();
            #endif

            float3 lightCol = Lambert(light.color.rgb, light.direction, Unity_NormalBlend(normal, i.worldNormal)) * light.shadowAttenuation;

            int lightsCount = GetAdditionalLightsCount();
            for (int j = 0; j < lightsCount; j++)
            {
                Light lightAdd = GetAdditionalLight(j, i.worldSpacePosition.xyz);
                lightCol += Lambert(lightAdd.color * (lightAdd.distanceAttenuation * lightAdd.shadowAttenuation), lightAdd.direction, Unity_NormalBlend(normal, i.normal));
            }

            float4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy);
            float3 combinedAmbient = SampleSH(i.worldNormal);
            mainTex.rgb *= lightCol + combinedAmbient;

            // Bỏ qua logic clip để debug
            if (minDistanceToEdge > 0.9 * (1 - saturate(i.movingPos - _WireFrameStay)))
            {
                return mainTex; // Trả về texture mà không clip
            }
            return _WireColor;
        }
        ENDHLSL
      

        Pass
        {
            // Wireframe shader based on the the following
            // http://developer.download.nvidia.com/SDK/10/direct3d/Source/SolidWireframe/Doc/SolidWireframe.pdf

            HLSLPROGRAM

            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
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


          

            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
          
            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_instancing

        
      
         
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma require geometry
            // -------------------------------------
            // Shader Stages
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // -------------------------------------
            // Universal Pipeline keywords

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            // -------------------------------------
            // Includes
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            ColorMask R
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma require geometry
            // -------------------------------------
            // Shader Stages
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // -------------------------------------
            // Includes
            ENDHLSL
        }

        // This pass is used when drawing to a _CameraNormalsTexture texture
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma require geometry
            // -------------------------------------
            // Shader Stages
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // -------------------------------------
            // Universal Pipeline keywords
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // -------------------------------------
            // Includes
            ENDHLSL
        }

    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
