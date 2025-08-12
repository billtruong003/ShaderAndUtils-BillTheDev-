using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;

public class AdvancedNoiseGenerator : OdinEditorWindow
{
    // ENUMS for clear selection
    public enum NoiseType { Simplex, Perlin, Worley_Cellular }
    public enum WorleyFunction { F1, F2_minus_F1 }

    [MenuItem("Tools/Generators/Advanced 3D Noise Generator (Odin)")]
    private static void OpenWindow() => GetWindow<AdvancedNoiseGenerator>().Show();

    // --- SETTINGS ---
    [Title("GENERATOR SETTINGS", bold: true)]
    [BoxGroup("Settings")]
    [EnumToggleButtons, OnValueChanged("MarkPreviewDirty")]
    public NoiseType TypeOfNoise = NoiseType.Simplex;

    [BoxGroup("Settings")]
    [InfoBox("Generates 3 channels of noise for vector warping. Otherwise, generates grayscale noise for density.")]
    [OnValueChanged("MarkPreviewDirty")]
    public bool GenerateForWarping = false;

    [BoxGroup("Settings")]
    [ValueDropdown("GetResolutionOptions"), OnValueChanged("MarkPreviewDirty")]
    public int Resolution = 64;

    // --- NOISE PARAMETERS ---
    [Title("NOISE PARAMETERS", bold: true)]
    [BoxGroup("Parameters")]
    [Range(0.1f, 50f), OnValueChanged("MarkPreviewDirty")]
    public float Frequency = 5.0f;

    [BoxGroup("Parameters")]
    [Range(1, 8), OnValueChanged("MarkPreviewDirty")]
    public int Octaves = 4;

    [BoxGroup("Parameters"), ShowIf("TypeOfNoise", NoiseType.Worley_Cellular)]
    [EnumToggleButtons, OnValueChanged("MarkPreviewDirty")]
    [InfoBox("F1 returns distance to nearest point (good for solid cells). \nF2-F1 returns difference between 2nd and 1st nearest (good for cell walls).")]
    public WorleyFunction WorleyCalculation = WorleyFunction.F1;

    [BoxGroup("Parameters"), ShowIf("TypeOfNoise", NoiseType.Worley_Cellular)]
    [ToggleLeft, OnValueChanged("MarkPreviewDirty")]
    public bool InvertWorleyResult = false;

    // --- PREVIEW ---
    [Title("LIVE PREVIEW", bold: true)]
    [BoxGroup("Preview", showLabel: false)]
    [PreviewField(150, Sirenix.OdinInspector.ObjectFieldAlignment.Center), ReadOnly]
    public Texture3D PreviewTexture;

    [BoxGroup("Preview")]
    [Button("Update Preview", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
    private void GeneratePreview()
    {
        try
        {
            if (isDirty || PreviewTexture == null || PreviewTexture.width != Resolution)
            {
                RecreatePreviewTexture();
            }

            Color[] colors = new Color[Resolution * Resolution * Resolution];
            float maxProgress = Resolution;

            for (int z = 0; z < Resolution; z++)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Generating Preview", $"Processing slice {z + 1} of {Resolution}...", (float)z / maxProgress))
                    break;

                for (int y = 0; y < Resolution; y++)
                {
                    for (int x = 0; x < Resolution; x++)
                    {
                        float fx = (float)x / Resolution;
                        float fy = (float)y / Resolution;
                        float fz = (float)z / Resolution;
                        colors[x + y * Resolution + z * Resolution * Resolution] = CalculateColor(fx, fy, fz);
                    }
                }
            }
            PreviewTexture.SetPixels(colors);
            PreviewTexture.Apply();
            isDirty = false;
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // --- OUTPUT ---
    [Title("FILE OUTPUT", bold: true)]
    [BoxGroup("Output")]
    [SuffixLabel(".asset", true)]
    public string FileName = "T_3DNoise_Simplex";

    [BoxGroup("Output")]
    [Button(ButtonSizes.Large), GUIColor(0.4f, 1f, 0.4f)]
    public void SaveTextureToAsset()
    {
        if (PreviewTexture == null)
        {
            EditorUtility.DisplayDialog("No Preview", "Please generate a preview before saving.", "OK");
            return;
        }

        string path = EditorUtility.SaveFilePanelInProject("Save 3D Texture", FileName, "asset", "Choose a location to save the Texture3D asset.");
        if (string.IsNullOrEmpty(path)) return;

        Texture3D textureToSave = Instantiate(PreviewTexture);
        AssetDatabase.CreateAsset(textureToSave, path);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Success", $"Texture saved successfully at:\n{path}", "OK");
    }

    // --- INTERNAL LOGIC ---
    private bool isDirty = true;
    private void MarkPreviewDirty() => isDirty = true;

    private void RecreatePreviewTexture()
    {
        TextureFormat format = GenerateForWarping ? TextureFormat.RGB24 : TextureFormat.R8;
        if (PreviewTexture != null) DestroyImmediate(PreviewTexture);
        PreviewTexture = new Texture3D(Resolution, Resolution, Resolution, format, false) { wrapMode = TextureWrapMode.Repeat };
    }

    private Color CalculateColor(float x, float y, float z)
    {
        if (GenerateForWarping)
        {
            float r = GetNoiseValue(x, y, z);
            float g = GetNoiseValue(x + 10.5f, y - 20.1f, z + 5.3f);
            float b = GetNoiseValue(x - 5.2f, y + 15.6f, z - 12.9f);
            return new Color(r, g, b);
        }
        else
        {
            float noise = GetNoiseValue(x, y, z);
            return new Color(noise, noise, noise);
        }
    }

    private float GetNoiseValue(float x, float y, float z)
    {
        float val = 0;
        x *= Frequency; y *= Frequency; z *= Frequency;
        switch (TypeOfNoise)
        {
            case NoiseType.Simplex:
                val = NoiseAlgorithms.SimplexFBM(x, y, z, Octaves) * 0.5f + 0.5f;
                break;
            case NoiseType.Perlin:
                val = NoiseAlgorithms.PerlinFBM(x, y, z, Octaves);
                break;
            case NoiseType.Worley_Cellular:
                val = NoiseAlgorithms.Worley(x, y, z, WorleyCalculation);
                if (InvertWorleyResult) val = 1.0f - val;
                break;
        }
        return val;
    }

    private static ValueDropdownList<int> GetResolutionOptions() => new ValueDropdownList<int> { { "32x32x32", 32 }, { "64x64x64", 64 }, { "128x128x128", 128 } };

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (PreviewTexture != null) DestroyImmediate(PreviewTexture);
    }
}

// --- CORE NOISE LIBRARIES ---
// Self-contained to prevent dependency issues.

/// <summary>
/// A static library for generating various types of procedural noise.
/// </summary>
public static class NoiseAlgorithms
{
    // Perlin, Worley, and Simplex FBM wrappers are defined here.
    // They call the core SimplexNoise class below.
    #region FBM AND WRAPPERS
    public static float PerlinFBM(float x, float y, float z, int octaves)
    {
        float total = 0; float frequency = 1; float amplitude = 1; float maxValue = 0;
        for (int i = 0; i < octaves; i++)
        {
            total += Perlin(x * frequency, y * frequency, z * frequency) * amplitude;
            maxValue += amplitude; amplitude *= 0.5f; frequency *= 2;
        }
        return total / maxValue;
    }

    public static float SimplexFBM(float x, float y, float z, int octaves)
    {
        float total = 0; float frequency = 1.0f; float amplitude = 1.0f; float maxValue = 0;
        for (int i = 0; i < octaves; i++)
        {
            total += SimplexNoise.Noise(x * frequency, y * frequency, z * frequency) * amplitude;
            maxValue += amplitude; amplitude *= 0.5f; frequency *= 2.0f;
        }
        return total / maxValue;
    }

    public static float Worley(float x, float y, float z, AdvancedNoiseGenerator.WorleyFunction func)
    {
        int ix = Mathf.FloorToInt(x); int iy = Mathf.FloorToInt(y); int iz = Mathf.FloorToInt(z);
        float fx = x - ix; float fy = y - iy; float fz = z - iz;
        float minDist1 = 10f, minDist2 = 10f;
        for (int oz = -1; oz <= 1; oz++)
        {
            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    Vector3 randomPoint = RandomVector(ix + ox, iy + oy, iz + oz);
                    float dist = Vector3.Distance(new Vector3(fx, fy, fz), new Vector3(ox, oy, oz) + randomPoint);
                    if (dist < minDist1) { minDist2 = minDist1; minDist1 = dist; }
                    else if (dist < minDist2) { minDist2 = dist; }
                }
            }
        }
        if (func == AdvancedNoiseGenerator.WorleyFunction.F2_minus_F1) return Mathf.Clamp01(minDist2 - minDist1);
        return Mathf.Clamp01(minDist1);
    }
    #endregion

    #region LOW_LEVEL_IMPLEMENTATIONS
    private static Vector3 RandomVector(int x, int y, int z)
    {
        Random.InitState(x * 9283 + y * 1932 + z * 8272);
        return new Vector3(Random.value, Random.value, Random.value);
    }

    private static readonly int[] PerlinPermutation = { 151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148, 247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33, 88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166, 77, 146, 158, 231, 83, 111, 229, 122, 60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244, 102, 143, 54, 65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169, 200, 196, 135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64, 52, 217, 226, 250, 124, 123, 5, 202, 38, 147, 118, 126, 255, 82, 85, 212, 207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42, 223, 183, 170, 213, 119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9, 129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104, 218, 246, 97, 228, 251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241, 81, 51, 145, 235, 249, 14, 239, 107, 49, 192, 214, 31, 181, 199, 106, 157, 184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254, 138, 236, 205, 93, 222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180 };
    private static int[] p = new int[512];
    static NoiseAlgorithms() { for (int i = 0; i < 256; i++) p[256 + i] = p[i] = PerlinPermutation[i]; }
    private static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);
    private static float Lerp(float t, float a, float b) => a + t * (b - a);
    private static float Grad(int hash, float x, float y, float z) { int h = hash & 15; float u = h < 8 ? x : y; float v = h < 4 ? y : h == 12 || h == 14 ? x : z; return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v); }
    private static float Perlin(float x, float y, float z)
    {
        int X = (int)Mathf.Floor(x) & 255, Y = (int)Mathf.Floor(y) & 255, Z = (int)Mathf.Floor(z) & 255;
        x -= Mathf.Floor(x); y -= Mathf.Floor(y); z -= Mathf.Floor(z);
        float u = Fade(x), v = Fade(y), w = Fade(z);
        int A = p[X] + Y, AA = p[A] + Z, AB = p[A + 1] + Z, B = p[X + 1] + Y, BA = p[B] + Z, BB = p[B + 1] + Z;
        return Lerp(w, Lerp(v, Lerp(u, Grad(p[AA], x, y, z), Grad(p[BA], x - 1, y, z)), Lerp(u, Grad(p[AB], x, y - 1, z), Grad(p[BB], x - 1, y - 1, z))), Lerp(v, Lerp(u, Grad(p[AA + 1], x, y, z - 1), Grad(p[BA + 1], x - 1, y, z - 1)), Lerp(u, Grad(p[AB + 1], x, y - 1, z - 1), Grad(p[BB + 1], x - 1, y - 1, z - 1))));
    }
    #endregion
}

/// <summary>
/// The missing Simplex Noise implementation. Now included in the same file.
/// </summary>
public static class SimplexNoise
{
    private static readonly int[] grad3 = { 1, 1, 0, -1, 1, 0, 1, -1, 0, -1, -1, 0, 1, 0, 1, -1, 0, 1, 1, 0, -1, -1, 0, -1, 0, 1, 1, 0, -1, 1, 0, 1, -1, 0, -1, -1 };
    private static readonly int[] p = { 151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148, 247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33, 88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166, 77, 146, 158, 231, 83, 111, 229, 122, 60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244, 102, 143, 54, 65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169, 200, 196, 135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64, 52, 217, 226, 250, 124, 123, 5, 202, 38, 147, 118, 126, 255, 82, 85, 212, 207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42, 223, 183, 170, 213, 119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9, 129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104, 218, 246, 97, 228, 251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241, 81, 51, 145, 235, 249, 14, 239, 107, 49, 192, 214, 31, 181, 199, 106, 157, 184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254, 138, 236, 205, 93, 222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180 };
    private static readonly int[] perm = new int[512];
    static SimplexNoise() { for (int i = 0; i < 512; i++) perm[i] = p[i & 255]; }
    private static float Dot(int[] g, float x, float y, float z) => g[0] * x + g[1] * y + g[2] * z;
    public static float Noise(float x, float y, float z)
    {
        float n0, n1, n2, n3;
        const float F3 = 1.0f / 3.0f, G3 = 1.0f / 6.0f;
        float s = (x + y + z) * F3;
        int i = Mathf.FloorToInt(x + s), j = Mathf.FloorToInt(y + s), k = Mathf.FloorToInt(z + s);
        float t = (i + j + k) * G3;
        float x0 = x - (i - t), y0 = y - (j - t), z0 = z - (k - t);
        int i1, j1, k1, i2, j2, k2;
        if (x0 >= y0) { if (y0 >= z0) { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 1; k2 = 0; } else if (x0 >= z0) { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 0; k2 = 1; } else { i1 = 0; j1 = 0; k1 = 1; i2 = 1; j2 = 0; k2 = 1; } }
        else { if (y0 < z0) { i1 = 0; j1 = 0; k1 = 1; i2 = 0; j2 = 1; k2 = 1; } else if (x0 < z0) { i1 = 0; j1 = 1; k1 = 0; i2 = 0; j2 = 1; k2 = 1; } else { i1 = 0; j1 = 1; k1 = 0; i2 = 1; j2 = 1; k2 = 0; } }
        float x1 = x0 - i1 + G3, y1 = y0 - j1 + G3, z1 = z0 - k1 + G3;
        float x2 = x0 - i2 + 2.0f * G3, y2 = y0 - j2 + 2.0f * G3, z2 = z0 - k2 + 2.0f * G3;
        float x3 = x0 - 1.0f + 3.0f * G3, y3 = y0 - 1.0f + 3.0f * G3, z3 = z0 - 1.0f + 3.0f * G3;
        int ii = i & 255, jj = j & 255, kk = k & 255;
        int gi0 = perm[ii + perm[jj + perm[kk]]] % 12;
        int gi1 = perm[ii + i1 + perm[jj + j1 + perm[kk + k1]]] % 12;
        int gi2 = perm[ii + i2 + perm[jj + j2 + perm[kk + k2]]] % 12;
        int gi3 = perm[ii + 1 + perm[jj + 1 + perm[kk + 1]]] % 12;
        float t0 = 0.6f - x0 * x0 - y0 * y0 - z0 * z0; if (t0 < 0) n0 = 0.0f; else { t0 *= t0; n0 = t0 * t0 * Dot(new int[] { grad3[gi0 * 3], grad3[gi0 * 3 + 1], grad3[gi0 * 3 + 2] }, x0, y0, z0); }
        float t1 = 0.6f - x1 * x1 - y1 * y1 - z1 * z1; if (t1 < 0) n1 = 0.0f; else { t1 *= t1; n1 = t1 * t1 * Dot(new int[] { grad3[gi1 * 3], grad3[gi1 * 3 + 1], grad3[gi1 * 3 + 2] }, x1, y1, z1); }
        float t2 = 0.6f - x2 * x2 - y2 * y2 - z2 * z2; if (t2 < 0) n2 = 0.0f; else { t2 *= t2; n2 = t2 * t2 * Dot(new int[] { grad3[gi2 * 3], grad3[gi2 * 3 + 1], grad3[gi2 * 3 + 2] }, x2, y2, z2); }
        float t3 = 0.6f - x3 * x3 - y3 * y3 - z3 * z3; if (t3 < 0) n3 = 0.0f; else { t3 *= t3; n3 = t3 * t3 * Dot(new int[] { grad3[gi3 * 3], grad3[gi3 * 3 + 1], grad3[gi3 * 3 + 2] }, x3, y3, z3); }
        return 32.0f * (n0 + n1 + n2 + n3);
    }
}