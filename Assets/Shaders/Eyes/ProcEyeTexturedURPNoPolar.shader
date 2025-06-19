Shader "Custom/ProcEyeTexturedURPNoPolar"
{
    Properties
    {
        [Header(Main)]
        _Color ("Color", Color) = (1,1,1,1)
        [Toggle(WHITE_EYE)] _WhiteEye("Enable White Eye", Float) = 1

        [Header(Veins)]
        [Toggle(VEINS)] _Veins("Enable Veins", Float) = 0
        _VeinsTex("Veins Texture (RGB)", 2D) = "white" {}
        _VeinsColor("Veins Color", Color) = (1,0,0,1)

        [Header(Invisible White Eye)]
        [Toggle(INVISIBLE_WHITE_EYE)] _InvisibleWhiteEye("Enable Invisible White Eye", Float) = 0

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

        [Header(Pattern)]
        _PatternTex("Pattern Texture (RGBA)", 2D) = "black" {}
        _PatternColor("Pattern Color", Color) = (0,0,0,1)
        _PatternColorOut("Pattern Color Out", Color) = (0,0,1,1)
        _PatternRadius("Pattern Radius", Range(0,1)) = 0.15
        _PatternTexRadius("Pattern Texture Radius", Range(0,10)) = 0.15
        _PatternScaleX("Pattern Scale X", Range(0,2)) = 0.5
        _PatternScaleY("Pattern Scale Y", Range(0,2)) = 0.5
        _PatternCorner("Pattern Corner Radius", Range(0,1)) = 0
        _PatternRotation("Pattern Rotation Degree", Range(0,360)) = 0

        [Header(Pupil)]     
        _PupilTex("Pupil Texture (RGB)", 2D) = "white" {}
        _PupilScale("Pupil Tex Radius", Range(0,1)) = 0.3
        _RadiusPupil("Pupil Radius", Range(0,0.5)) = 0.1
        _PupilColor("Pupil Color", Color) = (0,0,0,1)
        _PupilColorOut("Pupil Color Out", Color) = (0,0,1,1)
        _PupilScaleX("Pupil Scale X", Range(0,2)) = 0.5
        _PupilScaleY("Pupil Scale Y", Range(0,2)) = 0.5
        _PupilCorner("Pupil Corner Radius", Range(0,1)) = 0

        [Header(Highlight and Iris Edge)]
        _GlintTex("Glint Texture (RGB)", 2D) = "black" {}
        _GlintScale("Glint Scale", Range(0,1)) = 0.3
        _GlintOffset("Glint Offset", Vector) = (0,0,0,0)
        _Edgewidth("Iris Edge Width", Range(0,2)) = 0.1
        _IrisEdgeColor("Iris Edge Color", Color) = (0,0,0,1)
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Back
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ TEXTURE
            #pragma multi_compile _ WHITE_EYE
            #pragma multi_compile _ VEINS
            #pragma multi_compile _ INVISIBLE_WHITE_EYE
            #pragma multi_compile _ DISTORT
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            sampler2D _IrisTex, _PupilTex, _GlintTex, _PatternTex, _VeinsTex;
            CBUFFER_START(UnityPerMaterial)
                float4 _Color, _IrisColor, _PupilColor, _PupilColorOut, _IrisColorOut, _IrisTexColor, _IrisEdgeColor, _PatternColor, _PatternColorOut, _VeinsColor;
                float _Radius, _RadiusPupil, _PupilScaleX, _PupilScaleY, _Edgewidth, _IrisScaleX, _IrisScaleY;
                float _Scale, _Speed, _RotationDegree, _MaxDistort, _Brightness, _PupilScale, _GlintScale;
                float _PupilCorner, _PatternRadius, _PatternTexRadius, _PatternScaleX, _PatternScaleY, _PatternCorner, _PatternRotation;
                float2 _GlintOffset;
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

            float2 Rotate(float2 v, float angle)
            {
                float c = cos(angle);
                float s = sin(angle);
                return float2(v.x * c - v.y * s, v.x * s + v.y * c);
            }

            float2 RotateUV(float2 uv, float rotation)
            {
                float c = cos(rotation);
                float s = sin(rotation);
                float2 centered = uv - 0.5;
                float2 rotated = float2(
                    centered.x * c - centered.y * s,
                    centered.x * s + centered.y * c
                );
                return rotated + 0.5;
            }

            float RoundedBox(float2 pos, float2 size, float cornerRadius)
            {
                float2 q = abs(pos) - size + cornerRadius;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - cornerRadius;
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
                // Tính khoảng cách từ tâm cho iris, pupil và pattern
                float dis = distance(0, float3(IN.objPos.x * _IrisScaleX, IN.objPos.y * _IrisScaleY, IN.objPos.z - 0.5));
                float2 pupPos = float2(IN.objPos.x * _PupilScaleX, IN.objPos.y * _PupilScaleY);
                float disPup = RoundedBox(pupPos, float2(max(_RadiusPupil, 0.0001), max(_RadiusPupil, 0.0001)), _PupilCorner * _RadiusPupil);
                disPup = 1 - saturate(disPup / max(_RadiusPupil, 0.0001));

                // Xoay vị trí cho pattern
                float2 pos = float2(IN.objPos.x, IN.objPos.y);
                float2 rotatedPos = Rotate(pos, radians(_PatternRotation));
                float2 patternPos = float2(rotatedPos.x * _PatternScaleX, rotatedPos.y * _PatternScaleY);
                float disPattern = RoundedBox(patternPos, float2(max(_PatternRadius, 0.0001), max(_PatternRadius, 0.0001)), _PatternCorner * _PatternRadius);
                disPattern = 1 - saturate(disPattern / max(_PatternRadius, 0.0001));

                // Tính bán kính cho iris, pupil và pattern
                float irisRadius = 1 - saturate(dis / max(_Radius, 0.0001));
                float pupilRadius = disPup;
                float patternRadius = disPattern;
                float irisEdge = 1 - saturate(dis / max(_Radius, 0.0001) - _Edgewidth);

                // Tính toán UV cơ bản từ tọa độ object space
                float2 uv = float2(-IN.objPos.x, IN.objPos.y);
                float2 uv01 = uv + 0.5; // Chuyển từ [-0.5, 0.5] sang [0, 1]
                float2 uvPup = uv / (_PupilScale * 2) + 0.5;

                // Xoay UV cho pattern texture
                float2 rotatedUV01 = RotateUV(uv01, radians(_PatternRotation));
                float2 uvPattern = (rotatedUV01 - 0.5) / (_PatternTexRadius * 2) + 0.5;

                float2 uvGlint = uv / (_GlintScale * 2) + 0.5 + _GlintOffset;
                uvGlint += IN.viewDir * 0.2;

                // Xử lý animation cho Iris
                float t = _RotationDegree / 360.0;
                float scaleAnim = lerp(0.1, 1.0, t) * _Scale;
                float distortAmount = lerp(_MaxDistort, 0.0, t);
                float rotation = radians(_RotationDegree);

                float2 polar = ToPolarCoordinates(uv01);
                polar.y += rotation / (2.0 * 3.14159265359);
                polar.y = frac(polar.y);
                float2 rotatedUV = FromPolarCoordinates(polar);
                float speed = _Time.y * _Speed;

                // Lấy mẫu texture Iris
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

                // Lấy mẫu texture pupil, pattern và glint
                float4 glint = tex2D(_GlintTex, uvGlint);
                float4 pup = tex2D(_PupilTex, uvPup);
                float4 patternTex = tex2D(_PatternTex, uvPattern);

                // Tính toán các vùng hình tròn với transition sắc nét hơn
                float irisCircle = saturate(irisRadius * 100);
                float pupilCircle = saturate(pupilRadius * 100);
                float patternCircle = saturate(patternRadius * 100);
                float irisEdgeCircle = saturate(irisEdge * 100);

                // Tính màu trắng của mắt (nếu có)
                float4 eyeWhite = float4(0,0,0,0);
                #if WHITE_EYE
                    eyeWhite = _Color * (1 - max(irisEdgeCircle, irisCircle));
                    #if VEINS
                        float2 uvVeins = uv01;
                        float4 veins = tex2D(_VeinsTex, uvVeins) * _VeinsColor;
                        eyeWhite.rgb += veins.rgb;
                    #endif
                #endif

                // Tính màu iris
                float4 irisLerp = lerp(_IrisColorOut, _IrisColor, irisRadius) + i;
                float4 irisColored = irisCircle * irisLerp;

                // Tính màu pattern
                float patternMask = patternCircle * patternTex.r;
                float4 patternLerp = lerp(_PatternColorOut, _PatternColor, patternRadius);
                float4 patternColored = patternMask * patternLerp;

                // Tính màu pupil
                float pupilMask = pupilCircle * pup.r;
                float4 pupilLerp = lerp(_PupilColorOut, _PupilColor, pupilRadius);
                float4 pupilColored = pupilMask * (pup.r > 0.01 ? pup : pupilLerp);

                // Tính màu viền iris
                float4 irisEdgeColored = irisEdgeCircle * _IrisEdgeColor;

                // Tính màu cơ bản và màu cuối cùng
                float3 baseColor = eyeWhite.rgb + irisColored.rgb + irisEdgeColored.rgb;
                float3 finalColor = lerp(baseColor, patternColored.rgb, patternMask);
                finalColor = lerp(finalColor, _PupilColor.rgb, pupilMask);
                finalColor += glint.rgb;

                // Xử lý alpha cho invisible white eye
                float alpha = 1.0;
                #if INVISIBLE_WHITE_EYE
                    alpha = max(irisCircle, max(pupilCircle, patternCircle));
                #endif

                return half4(finalColor, alpha);
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

            half4 frag(Varyings IN : SV_POSITION) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}