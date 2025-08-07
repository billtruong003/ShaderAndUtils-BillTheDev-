////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Martin Bustos @FronkonGames <fronkongames@gmail.com>. All rights reserved.
//
// THIS FILE CAN NOT BE HOSTED IN PUBLIC REPOSITORIES.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Shader "Hidden/Fronkon Games/Spice Up/Speedlines URP"
{
  Properties
  {
    _MainTex("Main Texture", 2D) = "white" {}
  }

  SubShader
  {
    Tags
    {
      "RenderType" = "Opaque"
      "RenderPipeline" = "UniversalPipeline"
    }
    LOD 100
    ZTest Always ZWrite Off Cull Off

    Pass
    {
      Name "Fronkon Games Spice Up Speedlines"

      HLSLPROGRAM
      #include "SpiceUp.hlsl"
      #include "ColorBlend.hlsl"
      #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

      float _Strength;
      int _ColorBlend;
      half3 _ColorBorder;
      half3 _ColorCenter;
      float _Radius;
      float _Length;
      float _Speed;
      float _Frequency;
      float _Softness;
      float _Noise;
      int _Aspect;
      float _ColorBrightness;
      float _ColorOffset;
      float _ColorDefinition;

      #pragma vertex SpiceUpVert
      #pragma fragment SpiceUpFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash
      #pragma multi_compile _ _USE_DRAW_PROCEDURAL

      #define LOFI(x, d) (floor((x) / (d)) * (d))

      inline float Line(const float2 v, const float2 r)
      {
        float4 h = float4(Rand(float2(floor(v * r + float2(0.0, 0.0)) / r)),
                          Rand(float2(floor(v * r + float2(0.0, 1.0)) / r)),
                          Rand(float2(floor(v * r + float2(1.0, 0.0)) / r)),
                          Rand(float2(floor(v * r + float2(1.0, 1.0)) / r)));

        float2 ip = float2(smoothstep((float2)0.0, (float2)1.0, mod(v * r, 1.0)));
        
        return lerp(lerp(h.x, h.y, ip.y), lerp(h.z, h.w, ip.y), ip.x);
      }

      inline float LineNoise(const float2 v)
      {
        float sum = 0.0;

        sum += Line(v + (float2)1.0, (float2)4.0)   / 2.0;
        sum += Line(v + (float2)2.0, (float2)8.0)   / 4.0;
        sum += Line(v + (float2)3.0, (float2)16.0)  / 8.0;
        sum += Line(v + (float2)4.0, (float2)32.0)  / 16.0;
        sum += Line(v + (float2)5.0, (float2)64.0)  / 32.0;
        sum += Line(v + (float2)6.0, (float2)128.0) / 64.0;

        return sum;
      }

      half4 SpiceUpFrag(const VertexOutput input) : SV_Target 
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.uv).xy;

        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        float2 uv2 = (uv - 0.5) * 2.0;

        UNITY_BRANCH
        if (_Aspect == 1)
          uv2.x *= _ScreenParams.x / _ScreenParams.y;

        const float2 puv = float2(_Noise * length(uv2) + _Speed * _Strength * LOFI(_Time.y, _Speed / 200.0), _Frequency * atan2(uv2.x, uv2.y));

        float value = LineNoise(puv);
        value = length(uv2) - _Radius - _Length * (value - 0.5) * _Strength;
        value = smoothstep(-_Softness, _Softness, value);

        const float3 tint = lerp(_ColorCenter, _ColorBorder, pow(abs(length(uv2) * _ColorOffset), _ColorDefinition)) * _ColorBrightness;
        pixel.rgb = lerp(pixel.rgb, tint, value);

        pixel.rgb = lerp(color.rgb, ColorBlend(_ColorBlend, color.rgb, pixel.rgb), value * _Strength);
        
        // Color adjust.
        pixel.rgb = ColorAdjust(pixel.rgb);

        return lerp(color, pixel, _Intensity);
      }

      ENDHLSL
    }
  }
  
  FallBack "Diffuse"
}
