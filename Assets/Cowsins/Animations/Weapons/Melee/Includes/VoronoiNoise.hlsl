// File: VoronoiNoise.hlsl
// Description: Provides 3D Worley/Voronoi noise functions for HLSL.
// Returns a float2 containing F1 (distance to closest point) and F2 (distance to second closest point).
// V1.1: Renamed 'point' variable to 'cellPoint' to avoid HLSL keyword conflict.

#ifndef VORONOI_NOISE_HLSL
#define VORONOI_NOISE_HLSL

// Hashing function to generate pseudo-random points
float3 hash3D(float3 p)
{
    p = float3(dot(p, float3(127.1, 311.7, 74.7)),
               dot(p, float3(269.5, 183.3, 246.1)),
               dot(p, float3(113.5, 271.9, 124.6)));
    return frac(sin(p) * 43758.5453123);
}

// 3D Worley Noise.
// p: input position
// jitter: randomness of points (1.0 is standard)
// manhattan: use manhattan distance instead of euclidean
float2 WorleyNoise(float3 p, float jitter, bool manhattan)
{
    float3 p_int = floor(p);
    float3 p_frac = frac(p);

    float F1 = 1.0;
    float F2 = 1.0;

    for (int k = -1; k <= 1; k++) {
        for (int j = -1; j <= 1; j++) {
            for (int i = -1; i <= 1; i++)
            {
                float3 neighbor = float3(i, j, k);
                
                // --- FIX START ---
                // The variable 'point' was renamed to 'cellPoint'
                float3 cellPoint = hash3D(p_int + neighbor);
                cellPoint = 0.5 + 0.5 * sin(6.2831 * cellPoint * jitter); // Remap to sine wave for better distribution
                
                float3 diff = neighbor + cellPoint - p_frac;
                // --- FIX END ---
                
                float dist;

                if (manhattan)
                    dist = abs(diff.x) + abs(diff.y) + abs(diff.z);
                else
                    dist = dot(diff, diff); // Use squared distance for performance

                if (dist < F1)
                {
                    F2 = F1;
                    F1 = dist;
                }
                else if (dist < F2)
                {
                    F2 = dist;
                }
            }
        }
    }

    if (!manhattan)
    {
        F1 = sqrt(F1);
        F2 = sqrt(F2);
    }

    return float2(F1, F2);
}

#endif // VORONOI_NOISE_HLSL