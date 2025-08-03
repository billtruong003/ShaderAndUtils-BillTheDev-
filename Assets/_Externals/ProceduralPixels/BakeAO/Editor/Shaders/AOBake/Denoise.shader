Shader "Hidden/BakeAO/Denoise"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags {}
        Cull Off 
        ZWrite Off 
        ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "./ShaderLibrary/AOBakeCore.hlsl"

            Texture2D _MainTex;
            Texture2D _PositionWSTexture;
            Texture2D _NormalWSTexture;

            SamplerState pointClampSampler;
            SamplerState linearClampSampler;

            struct vertexData
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct fragmentData
            {
                float4 clipPos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //  Copyright (c) 2018-2019 Michele Morrone
            //  All rights reserved.
            //
            //  https://michelemorrone.eu - https://BrutPitt.com
            //
            //  me@michelemorrone.eu - brutpitt@gmail.com
            //  twitter: @BrutPitt - github: BrutPitt
            //  
            //  https://github.com/BrutPitt/glslSmartDeNoise/
            //
            //  This software is distributed under the terms of the BSD 2-Clause license
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            #define INV_SQRT_OF_2PI 0.398942280401
            #define INV_PI 0.3183098861

            //denose parameters
            #define SIGMA 1.5
            #define KSIGMA 0.75
            #define THRESHOLD (128.0 / 256.0)

            float _TextureSize;

            float4 smartDeNoise(Texture2D tex, float2 uv, float sigma, float kSigma, float threshold)
            {
                float radius = round(kSigma*sigma);
                float radQ = radius * radius;
                
                float invSigmaQx2 = .5 / (sigma * sigma);      // 1.0 / (sigma^2 * 2.0)
                float invSigmaQx2PI = INV_PI * invSigmaQx2;    // 1.0 / (sqrt(PI) * sigma)
                
                float invThresholdSqx2 = 0.5 / (threshold * threshold);     // 1.0 / (sigma^2 * 2.0)
                float invThresholdSqrt2PI = INV_SQRT_OF_2PI / threshold;   // 1.0 / (sqrt(2*PI) * sigma)
                
                float4 centrPx = tex.SampleLevel(linearClampSampler, uv, 0.0);
                
                float zBuff = 0.0;
                float4 aBuff = 0.0;
                float2 size = float2(_TextureSize, _TextureSize);

                float4 pixelColor = tex.SampleLevel(linearClampSampler, uv, 0.0);
                float4 pixelPositionWS = _PositionWSTexture.SampleLevel(linearClampSampler, uv, 0.0);
                float3 pixelNormalWS = normalize(_NormalWSTexture.SampleLevel(linearClampSampler, uv, 0.0).xyz);
                
                for(float x=-radius; x <= radius; x++) 
                {
                    float pt = sqrt(radQ-x*x);  // pt = yRadius: have circular trend
                    for(float y=-pt; y <= pt; y++) 
                    {
                        float2 d = float2(x,y);

                        float blurFactor = exp( -dot(d , d) * invSigmaQx2 ) * invSigmaQx2PI; 
                        
                        float2 walkUV = uv+d/size;
                        float4 walkPx = tex.SampleLevel(linearClampSampler, walkUV, 0.0);
                        float4 walkPositionWS = _PositionWSTexture.SampleLevel(linearClampSampler, walkUV, 0.0);
                        float3 walkNormalWS = normalize(_NormalWSTexture.SampleLevel(linearClampSampler, walkUV, 0.0).xyz);

                        // if(dot(walkNormalWS.xyz, pixelNormalWS.xyz) < 0.85)
                        //    continue;

                        float4 dC = walkPx-centrPx;
                        float deltaFactor = exp( -dot(dC, dC) * invThresholdSqx2) * invThresholdSqrt2PI * blurFactor;
                                            
                        zBuff += deltaFactor;
                        aBuff += deltaFactor*walkPx;
                    }
                }
                return aBuff/zBuff;
            }


            fragmentData vert(vertexData input)
            {
                fragmentData output;
                output.clipPos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, float4(input.positionOS.xyz, 1.0)));
                output.uv = input.uv;
                return output;
            }

            float4 frag(fragmentData input) : SV_Target
            { 
                float4 color = smartDeNoise(_MainTex, input.uv, SIGMA, KSIGMA, THRESHOLD);
                return color;
            }

            ENDHLSL
        }
    }
}
