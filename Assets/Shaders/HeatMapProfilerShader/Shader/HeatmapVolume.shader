Shader "PerfHeatMap/Volume"
{
    Properties
    {
        _VolumeTex ("Volume Data (RGBA)", 3D) = "" {}
        
        _Color1 ("Stat 1 Color (DrawCalls)", Color) = (1,0,0,0.1)
        _Color2 ("Stat 2 Color (Tris)", Color) = (0,1,0,0.1)
        _Color3 ("Stat 3 Color (GpuTime)", Color) = (0,0,1,0.1)
        _Color4 ("Stat 4 Color (FrameTime)", Color) = (1,1,0,0.1)
        
        _Range1 ("Stat 1 Range (Min, Max)", Vector) = (0, 1000, 0, 0)
        _Range2 ("Stat 2 Range (Min, Max)", Vector) = (0, 100000, 0, 0)
        _Range3 ("Stat 3 Range (Min, Max)", Vector) = (0, 33, 0, 0)
        _Range4 ("Stat 4 Range (Min, Max)", Vector) = (0, 33, 0, 0)
        
        _StepSize("Ray Marching Step Size", Range(0.01, 0.2)) = 0.05
        _Intensity("Intensity Multiplier", Range(1, 20)) = 5.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Front // Render inside of the cube
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #include "UnityCG.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 localPos : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            sampler3D _VolumeTex;
            float4x4 _VolumeTransform;

            float4 _Color1, _Color2, _Color3, _Color4;
            float2 _Range1, _Range2, _Range3, _Range4;
            float _StepSize;
            float _Intensity;

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.localPos = v.vertex.xyz + 0.5;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            
            // remap value from [minVal, maxVal] to [0, 1]
            float remap(float val, float2 range)
            {
                return saturate((val - range.x) / (range.y - range.x + 0.0001));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 rayDir = normalize(i.worldPos - _WorldSpaceCameraPos);
                float3 invRayDir = 1.0 / rayDir;
                float3 t0 = (mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz - (-0.5)) * invRayDir;
                float3 t1 = (mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz - (0.5)) * invRayDir;

                float tmin = max(max(min(t0.x, t1.x), min(t0.y, t1.y)), min(t0.z, t1.z));
                float tmax = min(min(max(t0.x, t1.x), max(t0.y, t1.y)), max(t0.z, t1.z));

                if (tmin >= tmax)
                {
                    discard;
                }
                
                tmin = max(0, tmin);

                float3 localRayOrigin = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;
                float3 currentPos = localRayOrigin + rayDir * tmin;
                
                fixed4 finalColor = fixed4(0, 0, 0, 0);
                
                int maxSteps = (int)(1.0 / _StepSize);
                for (int step = 0; step < maxSteps; ++step)
                {
                    float3 samplePos = currentPos + 0.5;
                    if (all(samplePos >= 0) && all(samplePos <= 1))
                    {
                        float4 rawData = tex3Dlod(_VolumeTex, float4(samplePos, 0));
                        
                        float4 normalizedData;
                        normalizedData.r = remap(rawData.r, _Range1);
                        normalizedData.g = remap(rawData.g, _Range2);
                        normalizedData.b = remap(rawData.b, _Range3);
                        normalizedData.a = remap(rawData.a, _Range4);

                        fixed4 sampleColor = 0;
                        sampleColor += _Color1 * normalizedData.r;
                        sampleColor += _Color2 * normalizedData.g;
                        sampleColor += _Color3 * normalizedData.b;
                        sampleColor += _Color4 * normalizedData.a;
                        
                        sampleColor.a *= _Intensity;
                        finalColor.rgb += (1.0 - finalColor.a) * sampleColor.rgb * sampleColor.a;
                        finalColor.a += (1.0 - finalColor.a) * sampleColor.a;
                    }

                    currentPos += rayDir * _StepSize;
                    if( (step * _StepSize) > (tmax-tmin) || finalColor.a > 0.99)
                    {
                        break;
                    }
                }

                return finalColor;
            }
            ENDCG
        }
    }
}