Shader "Universal Render Pipeline/Wireframe/DisplacementFadeWire"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" { }
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

        Pass
        {
            // Wireframe shader based on the the following
            // http://developer.download.nvidia.com/SDK/10/direct3d/Source/SolidWireframe/Doc/SolidWireframe.pdf

            HLSLPROGRAM
            #pragma require geometry

            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma shader_feature INVERT
            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
        //    #include "UnlitInput.hlsl"

            float _WireThickness;
            float4 _WireColor;
            sampler2D _MainTex;
            float _MovingSlider;
            float _Extrude;
            float _WireFrameStay;

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
                float movingPos : TEXCOORD1;
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
                output.uv = input.uv;
                output.movingPos = movingPos;
                return output;
            }

            struct g2f
            {
                float4 projectionSpaceVertex : SV_POSITION;
                float4 dist : TEXCOORD1;
                float4 uv : TEXCOORD0;
                float movingPos : TEXCOORD2;
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
                o.dist.xyz = float3( (area / length(edge0)), 0.0, 0.0) * o.projectionSpaceVertex.w * wireThickness;
                o.dist.w = 1.0 / o.projectionSpaceVertex.w;
                o.uv = i[0].uv;
                o.movingPos = i[0].movingPos;
                triangleStream.Append(o);

                o.projectionSpaceVertex = i[1].projectionSpaceVertex;
                o.dist.xyz = float3(0.0, (area / length(edge1)), 0.0) * o.projectionSpaceVertex.w * wireThickness;
                o.dist.w = 1.0 / o.projectionSpaceVertex.w;
                o.uv = i[1].uv;
                o.movingPos = i[1].movingPos;
                triangleStream.Append(o);

                o.projectionSpaceVertex = i[2].projectionSpaceVertex;
                o.dist.xyz = float3(0.0, 0.0, (area / length(edge2))) * o.projectionSpaceVertex.w * wireThickness;
                o.dist.w = 1.0 / o.projectionSpaceVertex.w;
                o.uv = i[2].uv;
                o.movingPos = i[2].movingPos;
                triangleStream.Append(o);
            }

            half4 frag(g2f i) : SV_Target
            {
                float minDistanceToEdge = min(i.dist[0], min(i.dist[1], i.dist[2])) * i.dist[3];
                float4 mainTex = tex2D(_MainTex, i.uv.xy);
                
                // show texture where there is no wireframe
                // also fades the wireframe over the moving position
                if(minDistanceToEdge > 0.9* 1- saturate(i.movingPos - _WireFrameStay))
                {
                    // discard pixels where there is no wireframe earlier
                    clip(i.movingPos - 0.2);
                    return mainTex;
                }
                // discard pixels over the moving position
                clip(i.movingPos);
                return _WireColor;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
