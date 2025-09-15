Shader "BillTheDev/VAT/Ultimate_VAT_Instanced"
{
    Properties
    {
        _MainTex("Albedo (RGB)", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200
        Cull Back

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 4.0

            #include "UnityCG.cginc"

            struct AppData
            {
                float4 vertex   : POSITION;
                float2 uv       : TEXCOORD0;
                float2 vertexIdUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VertexToFragment
            {
                float2 uv       : TEXCOORD0;
                float4 vertex   : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            sampler2D _PositionTexture;
            float4 _PositionMin;
            float4 _PositionMax;
            float _TextureHeight;
            sampler2D _MainTex;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _ClipStartFrame)
                UNITY_DEFINE_INSTANCED_PROP(float, _ClipFrameCount)
                UNITY_DEFINE_INSTANCED_PROP(float, _ClipDuration)
                UNITY_DEFINE_INSTANCED_PROP(float, _PlaybackSpeed)
                UNITY_DEFINE_INSTANCED_PROP(float, _AnimationStartTime)
                UNITY_DEFINE_INSTANCED_PROP(uint, _WrapMode)
            UNITY_INSTANCING_BUFFER_END(Props)

            float3 DecodeLocalPosition(float vertexU, float frame)
            {
                float v = (frame + 0.5h) / _TextureHeight;
                float4 encodedPosition = tex2Dlod(_PositionTexture, float4(vertexU, v, 0, 0));
                return lerp(_PositionMin.xyz, _PositionMax.xyz, encodedPosition.xyz);
            }

            float CalculateProgress(float currentTime, float startTime, float duration, float speed, uint wrapMode)
            {
                if (duration < 0.001h) return 0.0h;

                float elapsedTime = (currentTime - startTime) * speed;
                float progress = elapsedTime / duration;

                if (wrapMode == 1u) // Loop
                {
                    return frac(progress);
                }
                if (wrapMode == 2u) // PingPong
                {
                    return 1.0h - abs(fmod(progress, 2.0h) - 1.0h);
                }

                return saturate(progress); // Once
            }

            VertexToFragment vert(AppData v)
            {
                VertexToFragment o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                float clipStartFrame = UNITY_ACCESS_INSTANCED_PROP(Props, _ClipStartFrame);
                float clipFrameCount = UNITY_ACCESS_INSTANCED_PROP(Props, _ClipFrameCount);
                float clipDuration = UNITY_ACCESS_INSTANCED_PROP(Props, _ClipDuration);
                float playbackSpeed = UNITY_ACCESS_INSTANCED_PROP(Props, _PlaybackSpeed);
                float animationStartTime = UNITY_ACCESS_INSTANCED_PROP(Props, _AnimationStartTime);
                uint wrapMode = UNITY_ACCESS_INSTANCED_PROP(Props, _WrapMode);
                
                float progress = CalculateProgress(_Time.y, animationStartTime, clipDuration, playbackSpeed, wrapMode);

                float frameIndexInClip = progress * (clipFrameCount - 1.0h);
                float absoluteFrame = clipStartFrame + frameIndexInClip;

                float vertexU = v.vertexIdUV.x;
                float3 localPosition = DecodeLocalPosition(vertexU, absoluteFrame);

                float4 worldPosition = mul(unity_ObjectToWorld, float4(localPosition, 1.0h));
                o.vertex = mul(UNITY_MATRIX_VP, worldPosition);
                o.uv = v.uv;

                return o;
            }

            fixed4 frag(VertexToFragment i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Mobile/VertexLit"
}