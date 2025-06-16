
Shader "Custom/URPDecalOrtho"
{
    Properties
    {
    _Intensity("Intensity", Range(0,10)) = 5.0
    [Header(Movement)]
    _SpeedX("Speed X", Range(-5,5)) = 1.0
    _SpeedY("Speed Y", Range(-5,5)) = 1.0
    _RadialScale("Radial Scale", Range(0,10)) = 1.0
    _LengthScale("Length Scale", Range(0,10)) = 1.0
    _MovingTex ("MovingTex", 2D) = "white" {}
    _Multiply("Multiply Moving", Range(0,10)) = 1.0

    [Header(Shape)]
    _ShapeTex("Shape Texture", 2D) = "white" {}
    _ShapeTexIntensity("Shape tex intensity", Range(0,6)) = 0.5

    [Header(Gradient Coloring)]
    _Gradient("Gradient Texture", 2D) = "white" {}
    _Stretch("Gradient Stretch", Range(-2,10)) = 1.0
    _Offset("Gradient Offset", Range(-2,10)) = 1.0

    [Header(Cutoff)]    
    _Cutoff("Outside Cutoff", Range(0,1)) = 1.0
    _Smoothness("Outside Smoothness", Range(0,1)) = 1.0 }

    // The SubShader block containing the Shader code.
    SubShader
    {
        // SubShader Tags define when and under which conditions a SubShader block or
        // a pass is executed.
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry +1"}

       

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        // The DeclareDepthTexture.hlsl file contains utilities for sampling the
        // Camera depth texture.
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"   
      
        float _Cutoff, _Smoothness;
        sampler2D _MovingTex;
        float _SpeedX, _SpeedY;
        sampler2D _ShapeTex;
        float _ShapeTexIntensity;
        sampler2D _Gradient;
        float _Stretch, _Multiply;
        float _Intensity, _Offset;
        float _RadialScale, _LengthScale;
        float4 _Tint;
       

       
       
      

         // helper functions
         float2 Unity_PolarCoordinates(float2 UV, float2 Center, float RadialScale, float LengthScale)
         {
             float2 delta = UV - Center;
             float radius = length(delta) * 2.0 * RadialScale;
             float angle = atan2(delta.y, delta.x) * 1.0 / 6.28318 * LengthScale;
             return float2(radius, angle);
         }

         
        float GetFinalDistortion(float2 uvProj, float shapeTex)
        {
            float2 polarUV = Unity_PolarCoordinates(uvProj, float2(0.5, 0.5), _RadialScale, _LengthScale);

            // Move UV
            float2 movingUV = float2(polarUV.x + (_Time.x * _SpeedX), polarUV.y + (_Time.x * _SpeedY));

            // Final moving texture with the distortion
            float final = tex2D(_MovingTex, movingUV).r;

            shapeTex *= _ShapeTexIntensity;
            final *= shapeTex;
            return final;
        }

            // This example uses the Attributes structure as an input structure in
            // the vertex shader.
            struct Attributes
            {
                // The positionOS variable contains the vertex positions in object
                // space.
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                // The positions in this struct must have the SV_POSITION semantic.
                float4 positionHCS  : SV_POSITION;
            };

            // The vertex shader definition with properties defined in the Varyings
            // structure. The type of the vert function must match the type (struct)
            // that it returns.
            Varyings vert(Attributes IN)
            {
                // Declaring the output object (OUT) with the Varyings struct.
                Varyings OUT;
                // The TransformObjectToHClip function transforms vertex positions
                // from object space to homogenous clip space.
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                // Returning the output.
                return OUT;
            }

            half4 fragStencilMask(Varyings IN) : SV_Target{
// To calculate the UV coordinates for sampling the depth buffer,
                // divide the pixel location by the render target resolution
                // _ScaledScreenParams.
                float2 UV = IN.positionHCS.xy / _ScaledScreenParams.xy;

                // Sample the depth from the Camera depth texture.
                #if UNITY_REVERSED_Z
                    real depth = SampleSceneDepth(UV);
                #else
                    // Adjust Z to match NDC for OpenGL ([-1, 1])
                    real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(UV));
                #endif

                // Reconstruct the world space positions.
                float3 worldPos = ComputeWorldSpacePosition(UV, depth, UNITY_MATRIX_I_VP);

                float3 opos = mul(unity_WorldToObject, float4(worldPos,1)).xyz;

                clip(float3(0.5,0.5,0.5) - abs(opos.xyz));

                float2 uvProj = opos.xz + 0.5;	
			// get the main shape texture for the alpha
			float shapeTex = tex2D(_ShapeTex, uvProj).r;	

			float vortexEffect = GetFinalDistortion(uvProj, shapeTex);				
					
			// discard outside of texture alpha
			clip(vortexEffect- 0.1);
			return float4(1,1,1,1);

            }

            // The fragment shader definition.
            // The Varyings input structure contains interpolated values from the
            // vertex shader. The fragment shader uses the `positionHCS` property
            // from the `Varyings` struct to get locations of pixels.
            half4 frag(Varyings IN) : SV_Target
            {
                // To calculate the UV coordinates for sampling the depth buffer,
                // divide the pixel location by the render target resolution
                // _ScaledScreenParams.
                float2 UV = IN.positionHCS.xy / _ScaledScreenParams.xy;

                // Sample the depth from the Camera depth texture.
                #if UNITY_REVERSED_Z
                    real depth = SampleSceneDepth(UV);
                #else
                    // Adjust Z to match NDC for OpenGL ([-1, 1])
                    real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(UV));
                #endif

                // Reconstruct the world space positions.
                float3 worldPos = ComputeWorldSpacePosition(UV, depth, UNITY_MATRIX_I_VP);

                float3 opos = mul(unity_WorldToObject, float4(worldPos,1)).xyz;

                clip(float3(0.5,0.5,0.5) - abs(opos.xyz));

                float2 uvProj = opos.xz + 0.5;
                // Get the main shape texture for the alpha
                float shapeTex = tex2D(_ShapeTex, uvProj).r;

                float vortexEffect = GetFinalDistortion(uvProj, shapeTex);

                // Add the coloring from the gradient map
                float4 gradientmap = tex2D(_Gradient, (vortexEffect * _Stretch) + _Offset) * _Intensity;
                gradientmap *= vortexEffect;
               gradientmap *= _Tint;
    
                // Add tinting and transparency
               gradientmap.rgb *= _Tint.rgb;
               gradientmap *= _Tint.a;
                gradientmap *= shapeTex;
    
                // Create a cutoff point for the outside of the portal effect
                gradientmap *= smoothstep(_Cutoff - _Smoothness, _Cutoff, vortexEffect * _Multiply);
                // Increase intensity
                gradientmap = saturate(gradientmap * 10) * _Intensity;
                return gradientmap;
            }
        ENDHLSL
        Pass
        {
            Name "Decal Mask"
            Ztest Greater
            Zwrite off
            Cull Off
            Colormask 0
            Lighting Off

            Tags
            {
                "RenderType" = "Transparent"             
                "RenderPipeline" = "UniversalPipeline"
            }
            
            Stencil
            {
                comp Always
                ref 1
                pass replace
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragStencilMask

            ENDHLSL
        }

        Pass {
            Name "Decal Outside"
            Zwrite off
            ZTest off
            Cull Back
            Lighting Off
           	Blend OneMinusDstColor One
			
            Tags
            {
                "RenderType" = "Transparent"              
                "RenderPipeline" = "UniversalPipeline"           
            }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag         
            ENDHLSL
        }
		
		Pass
        {
            Name "Decal Inside"
            Zwrite off
            Ztest Off
            Cull Front
            Lighting Off
          	Blend OneMinusDstColor One

			Tags
            {
                "RenderType" = "Transparent"
                "Queue" = "Transparent"
                "RenderPipeline" = "UniversalPipeline"
                "LightMode" = "UniversalForward"
            }
           
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag  
            ENDHLSL
        }       
    }
}