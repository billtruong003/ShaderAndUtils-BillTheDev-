Shader "Custom/ProcEyeTexturedURP"
{
    Properties
    {
        [Header(Main)]
        _Color ("Color", Color) = (1,1,1,1)
      
        [Header(Iris)]
        _IrisTex("Iris Texture (RGB)", 2D) = "black" {}
        _IrisTexColor("Iris Texture Tint", Color) = (1,0,0,1)
        _Radius("Iris Radius", Range(0,1)) = 0.4
        _IrisColor("Iris Color", Color) = (0,1,1,1)
        _IrisColorOut("Iris Color Out", Color) = (0,1,0,1)
        _IrisScaleX("Iris Scale X", Range(0,2)) = 1
        _IrisScaleY("Iris Scale Y", Range(0,2)) = 1
        _Speed("Iris Scroll Speed", Range(-10,10)) = 0
        _RotationDegree("Iris Rotation Degree", Range(0,360)) = 0
        _Scale("Iris Texture Scale", Range(0.1,10)) = 10
        [Toggle(TEXTURE)] _TEXTURE("Circular Texture", Float) = 0
        [Toggle(DISTORT)] _DISTORT("Enable Distortion", Float) = 0
        _MaxDistort("Max Iris Texture Distortion", Range(0,1)) = 0.5
        _Brightness("Iris Texture Brightness", Range(0,5)) = 1
 
        [Header(Pupil)]     
        _PupilTex("Pupil Texture (RGB)", 2D) = "white" {}
        _PupilScale("Pupil Tex Radius", Range(0,1)) = 0.3
        _RadiusPupil("Pupil Radius", Range(0,0.5)) = 0.1
        _PupilColor("Pupil Color", Color) = (0,0,0,1)
        _PupilColorOut("Pupil Color Out", Color) = (0,0,1,1)
        _PupilScaleX("Pupil Scale X", Range(0,1)) = 0.5
        _PupilScaleY("Pupil Scale Y", Range(0,1)) = 0.5
 
        [Header(Highlight and Iris Edge)]
        _GlintTex("Glint Texture (RGB)", 2D) = "black" {}
        _GlintScale("Glint Scale", Range(0,1)) = 0.3
        _Edgewidth("Iris Edge Width", Range(0,2)) = 0.1
        _IrisEdgeColor("Iris Edge Color", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ TEXTURE
            #pragma multi_compile _ DISTORT
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            sampler2D _IrisTex, _PupilTex, _GlintTex;
            CBUFFER_START(UnityPerMaterial)
                float4 _Color, _IrisColor, _PupilColor, _PupilColorOut, _IrisColorOut, _IrisTexColor, _IrisEdgeColor;
                float _Radius, _RadiusPupil, _PupilScaleX, _PupilScaleY, _Edgewidth, _IrisScaleX, _IrisScaleY;
                float _Scale, _Speed, _RotationDegree, _MaxDistort, _Brightness, _PupilScale, _GlintScale;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 objPos : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.objPos = IN.positionOS;
                OUT.viewDir = TransformWorldToViewDir(TransformObjectToWorldNormal(IN.normalOS));
                return OUT;
            }

            float2 ToPolarCoordinates(float2 uv)
            {
                float2 centered = uv - 0.5;
                float r = length(centered);
                float theta = atan2(centered.y, centered.x);
                return float2(r, theta / (2.0 * 3.14159265359));
            }

            float2 FromPolarCoordinates(float2 polar)
            {
                float x = polar.x * cos(polar.y * 2.0 * 3.14159265359);
                float y = polar.x * sin(polar.y * 2.0 * 3.14159265359);
                return float2(x, y) + 0.5;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float dis = distance(0, float3(IN.objPos.x * _IrisScaleX, IN.objPos.y * _IrisScaleY, IN.objPos.z - 0.5));
                float disPup = distance(0, float3(IN.objPos.x * _PupilScaleX, IN.objPos.y * _PupilScaleY, IN.objPos.z - 0.5));
                float irisRadius = 1 - saturate(dis / _Radius);
                float pupilRadius = 1 - saturate(disPup / _RadiusPupil);
                float irisEdge = 1 - saturate(dis / _Radius - _Edgewidth);

                float2 uv = float2(-IN.objPos.x, IN.objPos.y);
                float2 uvPup = uv / (_PupilScale * 2) + 0.5;
                float2 uvGlint = uv / (_GlintScale * 2) + 0.5;
                uvGlint.x -= 0.2;
                uvGlint += IN.viewDir * 0.2;

                float t = _RotationDegree / 360.0;
                float scaleAnim = lerp(0.1, 1.0, t) * _Scale;
                float distortAmount = lerp(_MaxDistort, 0.0, t);
                float rotation = _RotationDegree * 3.14159265359 / 180.0;

                float2 polar = ToPolarCoordinates(IN.uv);
                polar.y += rotation / (2.0 * 3.14159265359);
                polar.y = frac(polar.y);
                float2 rotatedUV = FromPolarCoordinates(polar);
                float speed = _Time.y * _Speed;

                float4 i;
                #if TEXTURE
                    float2 texUV = (disPup * rotatedUV + speed) * scaleAnim;
                    #if DISTORT
                        i = tex2D(_IrisTex, texUV + distortAmount * tex2D(_IrisTex, texUV).rg);
                    #else
                        i = tex2D(_IrisTex, texUV);
                    #endif
                #else
                    float2 texUV = float2(rotatedUV.x, rotatedUV.y + speed) * scaleAnim;
                    #if DISTORT
                        i = tex2D(_IrisTex, texUV + distortAmount * tex2D(_IrisTex, texUV).rg);
                    #else
                        i = tex2D(_IrisTex, texUV);
                    #endif
                #endif
                i *= _IrisTexColor * _Brightness;

                float4 glint = tex2D(_GlintTex, uvGlint);
                float4 pup = tex2D(_PupilTex, uvPup);

                float irisCircle = saturate(irisRadius * 20);
                float pupilCircle = saturate(pupilRadius * 20) * pup.r;
                float irisEdgeCircle = saturate(irisEdge * 10);

                float4 eyeWhite = _Color * (1 - irisEdgeCircle);
                glint *= irisCircle;
                irisEdgeCircle -= irisCircle;
                irisCircle -= pupilCircle;

                float4 irisLerp = lerp(_IrisColorOut, _IrisColor, irisRadius) + i;
                float4 irisColored = irisCircle * irisLerp;
                float4 pupilLerp = lerp(_PupilColorOut, _PupilColor, pupilRadius);
                float4 pupilColored = pupilCircle * pupilLerp;
                float4 irisEdgeColored = irisEdgeCircle * _IrisEdgeColor;

                half4 finalColor = eyeWhite + irisColored + pupilColored + irisEdgeColored;
                finalColor.rgb += glint.rgb;

                return half4(finalColor.rgb, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}