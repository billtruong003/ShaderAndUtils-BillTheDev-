// WaterStylizeCommon.hlsl

#ifndef WATER_STYLIZE_GRAPH_LOGIC_INCLUDED
#define WATER_STYLIZE_GRAPH_LOGIC_INCLUDED

// THÊM DÒNG NÀY VÀO ĐÂY
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

//-------------------------------------------------------------------------------------
// Properties and Textures
//-------------------------------------------------------------------------------------
CBUFFER_START(UnityPerMaterial)
    // Đã đổi tên thuộc tính để khớp với Shader Graph
    half4 _Shallow_Color, _Deep_Color, _Horizon_Color;
    half _Depth_Fade_Distance;
    half _Horizon_Distance;
    half _Wave_Steepness, _Wave_Length, _Wave_Speed;
    float4 _Wave_Directions;
    half _Normals_Strength, _Normals_Scale, _Normals_Speed;
    half _Refraction_Strength, _Refraction_Scale, _Refraction_Speed;
    half4 _Surface_Foam_Color;
    half _Surface_Foam_Tiling, _Surface_Foam_Direction, _Surface_Foam_Speed, _Surface_Foam_Distortion, _Surface_Foam_Cutoff;
    half _Surface_Foam_Color_Blend; // Mới
    half4 _Intersection_Foam_Color;
    half _Intersection_Foam_Depth, _Intersection_Foam_Fade, _Intersection_Foam_Tiling;
    half _Intersection_Foam_Direction, _Intersection_Foam_Speed, _Intersection_Foam_Cutoff;
    half _Intersection_Foam_Color_Blend; // Mới
    half4 _Specular_Color;
    half _Lighting_Smoothness, _Lighting_Hardness;
CBUFFER_END

TEXTURE2D(_Normals_Texture);
TEXTURE2D(_Surface_Foam_Texture);
TEXTURE2D(_Intersection_Foam_Texture);
TEXTURE2D(_CameraOpaqueTexture);

SAMPLER(sampler_linear_repeat);
SAMPLER(sampler_CameraOpaqueTexture);


// --- CÁC HÀM MỚI TỪ SHADER GRAPH ---

// Hàm băm dùng cho procedural noise
void Hash_LegacyMod_2_1_float(float2 p, out float r)
{
    float2 f = frac(p * float2(443.897, 441.423));
    f += dot(f.yx, f.xy + 19.19);
    r = frac((f.x + f.y) * f.x);
}

// Hàm tạo nhiễu Gradient theo thủ tục (tương đương node Gradient Noise)
float Unity_GradientNoise_LegacyMod_float(float2 uv, float scale)
{
    float2 p = uv * scale;
    float2 ip = floor(p);
    float2 fp = frac(p);
    float d00; Hash_LegacyMod_2_1_float(ip, d00);
    float d01; Hash_LegacyMod_2_1_float(ip + float2(0, 1), d01);
    float d10; Hash_LegacyMod_2_1_float(ip + float2(1, 0), d10);
    float d11; Hash_LegacyMod_2_1_float(ip + float2(1, 1), d11);
    fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
    return lerp(lerp(d00, d10, fp.x), lerp(d01, d11, fp.x), fp.y);
}

// Hàm tái tạo vị trí thế giới từ depth (tương đương subgraph ScenePosition)
float3 GetScenePositionWS(float2 screenUV)
{
    // Lỗi xảy ra ở dòng này, giờ sẽ hoạt động bình thường
    float rawDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV).r;
    return ComputeWorldSpacePosition(screenUV, rawDepth, UNITY_MATRIX_I_VP);
}

// Hàm hòa trộn Foam theo logic của Shader Graph
// Nó lerp giữa alpha-blend (overwrite) và additive-blend
half3 BlendFoam_GraphLogic(half3 base, half3 blend, half opacity, half blendMode)
{
    half3 alphaBlended = lerp(base, blend, opacity);
    half3 additiveBlended = lerp(base, base + blend, opacity);
    return lerp(alphaBlended, additiveBlended, blendMode);
}

// --- CÁC HÀM CŨ (giữ nguyên) ---

float3 GerstnerWave(float3 position, float steepness, float wavelength, float speed, float direction, inout float3 tangent, inout float3 binormal)
{
    direction = direction * 2 - 1;
    float2 d = normalize(float2(cos(PI * direction), sin(PI * direction)));
    float k = 2 * PI / wavelength;
    float f = k * (dot(d, position.xz) - speed * _Time.y);
    float a = steepness / k;

    tangent += float3( -d.x * d.x * (steepness * sin(f)), d.x * (steepness * cos(f)), -d.x * d.y * (steepness * sin(f)) );
    binormal += float3( -d.x * d.y * (steepness * sin(f)), d.y * (steepness * cos(f)), -d.y * d.y * (steepness * sin(f)) );

    return float3( d.x * (a * cos(f)), a * sin(f), d.y * (a * cos(f)) );
}

void GerstnerWaves_float(float3 position, float steepness, float wavelength, float speed, float4 directions, out float3 offset, out float3 normal)
{
    offset = 0;
    float3 tangent = float3(1, 0, 0);
    float3 binormal = float3(0, 0, 1);

    offset += GerstnerWave(position, steepness, wavelength, speed, directions.x, tangent, binormal);
    offset += GerstnerWave(position, steepness * 0.8, wavelength * 1.2, speed * 1.1, directions.y, tangent, binormal);
    offset += GerstnerWave(position, steepness * 1.2, wavelength * 0.9, speed * 0.8, directions.z, tangent, binormal);
    offset += GerstnerWave(position, steepness * 1.1, wavelength * 1.3, speed * 1.2, directions.w, tangent, binormal);

    normal = normalize(cross(binormal, tangent));
}

float2 GetPanningUV(float2 uv, float tiling, float direction, float speed)
{
    float angle = direction * 2.0 * PI;
    float2 dir = float2(sin(angle), cos(angle));
    return (uv * tiling) + (dir * speed * _Time.y);
}

float3 GetBlendedNormals(float2 uv)
{
    float2 uv1 = GetPanningUV(uv, _Normals_Scale, 0.25, _Normals_Speed);
    float2 uv2 = GetPanningUV(uv, _Normals_Scale * 2, 0.75, _Normals_Speed * 0.5);
    
    float3 normal1 = UnpackNormal(SAMPLE_TEXTURE2D(_Normals_Texture, sampler_linear_repeat, uv1));
    float3 normal2 = UnpackNormal(SAMPLE_TEXTURE2D(_Normals_Texture, sampler_linear_repeat, uv2));
    
    return normalize(float3(normal1.xy + normal2.xy, normal1.z * normal2.z));
}

float LightingSpecular(float3 L, float3 N, float3 V, float smoothness)
{
    float3 H = SafeNormalize(L + V);
    float NdotH = saturate(dot(N, H));
    return pow(NdotH, smoothness);
}

void MainLighting_float(float3 normalWS, float3 positionWS, float3 viewWS, float smoothness, out float specular)
{
    specular = 0.0;
    #ifndef SHADERGRAPH_PREVIEW
    smoothness = exp2(10 * smoothness + 1);
    Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
    specular = LightingSpecular(mainLight.direction, normalWS, viewWS, smoothness);
    #endif
}

void AdditionalLighting_float(float3 normalWS, float3 positionWS, float3 viewWS, float smoothness, float hardness, out float3 specular)
{
    specular = 0;
    #ifndef SHADERGRAPH_PREVIEW
    smoothness = exp2(10 * smoothness + 1);
    int pixelLightCount = GetAdditionalLightsCount();
    for (int i = 0; i < pixelLightCount; ++i)
    {
        Light light = GetAdditionalLight(i, positionWS);
        float3 attenuatedLight = light.color * light.distanceAttenuation * light.shadowAttenuation;

        float specular_soft = LightingSpecular(light.direction, normalWS, viewWS, smoothness);
        float specular_hard = smoothstep(0.005, 0.01, specular_soft);
        float specular_term = lerp(specular_soft, specular_hard, hardness);

        specular += specular_term * attenuatedLight;
    }
    #endif
}

#endif