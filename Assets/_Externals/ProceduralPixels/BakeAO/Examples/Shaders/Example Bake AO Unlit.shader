Shader "Procedural Pixels/Bake AO/Example Bake AO Unlit"
{
    Properties
    {
        _Color("Color", Color) = (1.0, 1.0, 1.0, 1.0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // Include BakeAO_CG.cginc library. If you are programming shaders in HLSL, include BakeAO_HLSL.hlsl instead.
            #include "../../Runtime/Shaders/ShaderLibrary/BakeAO_CG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;

                // Add uvs of the model into vertex data
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float2 uv3 : TEXCOORD3;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                
                // Include UV to sample AO texture in the fragment shader interpolator.
                float2 aoTextureUV : TEXCOORD0;
            };

            float4 _Color;

            // Unity Ambient Occlusion properties
            sampler2D _OcclusionMap;
            float _OcclusionStrength;

            // Bake AO ambient occlusion properties. If your shader supports instancing, place those in correct CBuffer.
            float _AOTextureUV;
            float _MultiplyAlbedoAndOcclusion;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                // Let BakeAO method to pick up uv that will be used for sampling AO, pass this UV into interpolator.
                o.aoTextureUV = BakeAO_FilterAOTextureUV(v.uv0, v.uv1, v.uv2, v.uv3, _AOTextureUV);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 objectColor = _Color;

                // sample the ambient occlusion texture, using uv prepared in vertex shader.
                float occlusion = tex2D(_OcclusionMap, i.aoTextureUV).r;

                // Apply the occlusion strength parameter.
                occlusion = BakeAO_ApplyOcclusionStrength(occlusion, _OcclusionStrength);

                // Apply the occlusion into diffuse lighting, if relevant, or just use it according to your needs.
                objectColor = BakeAO_ModifyBaseColor(objectColor, occlusion, _MultiplyAlbedoAndOcclusion);

                // You can also just multiply the color with occlusion value to apply AO.
                return objectColor * occlusion; 
            }
            ENDCG
        }
    }
}
