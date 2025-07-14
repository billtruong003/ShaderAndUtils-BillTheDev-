using UnityEngine;
using UnityEngine.Profiling;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public readonly struct InspectedObjectDetailedData
{
    private const float DYNAMIC_SCORE_INSTRUCTION_WEIGHT = 0.1f;
    private const float DYNAMIC_SCORE_ALPHA_CLIP_PENALTY = 50f;
    private const float DYNAMIC_SCORE_EMISSION_PENALTY = 20f;
    private const float DYNAMIC_SCORE_METALLIC_PENALTY = 40f;
    private const float DYNAMIC_SCORE_FOLIAGE_PENALTY = 60f;
    private const float DYNAMIC_SCORE_FRESNEL_PENALTY = 15f;
    private const float DYNAMIC_SCORE_TRANSPARENCY_PENALTY = 150f;
    private const float DYNAMIC_SCORE_VERTEX_WEIGHT = 0.002f;
    private const float DYNAMIC_SCORE_EXTRA_MATERIAL_PENALTY = 25f;
    private const float DYNAMIC_SCORE_TEXTURE_MEMORY_WEIGHT = 0.00001f;

    public readonly Renderer Renderer;
    public readonly string Name;
    public readonly int VertexCount;
    public readonly int TriangleCount;
    public readonly int SubMeshCount;
    public readonly int MaterialCount;
    public readonly IReadOnlyList<string> MaterialNames;
    public readonly string ShaderName;
    public readonly int ShaderPassCount;
    public readonly bool IsTransparent;
    public readonly int SamplerCount;
    public readonly int TotalTextureCount;
    public readonly long TotalTextureMemoryBytes;
    public readonly bool IsStatic;
    public readonly bool IsPartOfStaticBatch;
    public readonly string LodInfo;
    public readonly float StaticPerformanceScore;
    public readonly float DynamicPerformanceScore;
    public readonly IReadOnlyList<string> ActiveKeywords;
    public IReadOnlyList<string> Suggestions => PerformanceHeuristics.GenerateSuggestions(this);

    public float PerformanceScore => DynamicPerformanceScore;

    public InspectedObjectDetailedData(Renderer renderer)
    {
        Renderer = renderer;
        Name = renderer.gameObject.name;

        var sharedMaterials = renderer.sharedMaterials;
        MaterialCount = sharedMaterials.Length;
        MaterialNames = sharedMaterials.Select(m => m ? m.name : "null").ToList();

        ExtractMeshData(renderer, out VertexCount, out TriangleCount, out SubMeshCount);
        ExtractTextureInfo(sharedMaterials, out TotalTextureCount, out TotalTextureMemoryBytes);

        var go = renderer.gameObject;
        IsStatic = go.isStatic;
        IsPartOfStaticBatch = renderer.isPartOfStaticBatch;
        LodInfo = GetLodInformation(renderer);

        long instructionCost = 0;
        var mainMaterial = renderer.sharedMaterial;
        if (mainMaterial != null && mainMaterial.shader != null)
        {
            var shader = mainMaterial.shader;
            ShaderName = shader.name;
            ShaderPassCount = mainMaterial.passCount;
            IsTransparent = mainMaterial.renderQueue >= (int)UnityEngine.Rendering.RenderQueue.Transparent;
            ActiveKeywords = mainMaterial.shaderKeywords;

            if (ShaderAnalysisCache.TryGetShaderStats(ShaderName, out var stats))
            {
                StaticPerformanceScore = stats.StaticComplexityScore;
                SamplerCount = stats.SamplerCount;
                instructionCost = stats.InstructionCost;
            }
            else
            {
                StaticPerformanceScore = 0;
                SamplerCount = 0;
            }
        }
        else
        {
            ShaderName = "N/A";
            ShaderPassCount = 0;
            IsTransparent = false;
            ActiveKeywords = System.Array.Empty<string>();
            StaticPerformanceScore = 0;
            SamplerCount = 0;
        }

        DynamicPerformanceScore = CalculateDynamicScore(instructionCost, ActiveKeywords, IsTransparent, VertexCount, MaterialCount, TotalTextureMemoryBytes);
    }

    private static float CalculateDynamicScore(long instructionCost, IReadOnlyList<string> keywords, bool isTransparent, int vertexCount, int materialCount, long textureMemory)
    {
        float score = 0;
        score += instructionCost * DYNAMIC_SCORE_INSTRUCTION_WEIGHT;
        if (keywords.Contains("_ALPHATEST_ON") || keywords.Contains("_ALPHACLIP_ON")) score += DYNAMIC_SCORE_ALPHA_CLIP_PENALTY;
        if (keywords.Contains("_EMISSION")) score += DYNAMIC_SCORE_EMISSION_PENALTY;
        if (keywords.Any(k => k.Contains("METALLIC"))) score += DYNAMIC_SCORE_METALLIC_PENALTY;
        if (keywords.Any(k => k.Contains("FOLIAGE"))) score += DYNAMIC_SCORE_FOLIAGE_PENALTY;
        if (keywords.Any(k => k.Contains("FRESNEL"))) score += DYNAMIC_SCORE_FRESNEL_PENALTY;
        if (isTransparent) score += DYNAMIC_SCORE_TRANSPARENCY_PENALTY;
        score += vertexCount * DYNAMIC_SCORE_VERTEX_WEIGHT;
        score += (materialCount - 1) * DYNAMIC_SCORE_EXTRA_MATERIAL_PENALTY;
        score += textureMemory * DYNAMIC_SCORE_TEXTURE_MEMORY_WEIGHT;
        return score;
    }

    private static void ExtractMeshData(Renderer renderer, out int vertexCount, out int triCount, out int subMeshCount)
    {
        vertexCount = 0;
        triCount = 0;
        subMeshCount = 0;
        Mesh mesh = null;

        if (renderer is MeshRenderer mr && mr.TryGetComponent<MeshFilter>(out var mf))
            mesh = mf.sharedMesh;
        else if (renderer is SkinnedMeshRenderer smr)
            mesh = smr.sharedMesh;

        if (mesh == null) return;

        vertexCount = mesh.vertexCount;
        subMeshCount = mesh.subMeshCount;

        if (mesh.isReadable)
        {
            triCount = mesh.triangles.Length / 3;
        }
        else
        {
            triCount = -1;
        }
    }

    private static void ExtractTextureInfo(Material[] materials, out int textureCount, out long totalMemory)
    {
        textureCount = 0; totalMemory = 0;
#if UNITY_EDITOR
        var uniqueTextures = new HashSet<Texture>();
        foreach (var mat in materials)
        {
            if (mat == null || mat.shader == null) continue;
            int propertyCount = ShaderUtil.GetPropertyCount(mat.shader);
            for (int i = 0; i < propertyCount; i++)
            {
                if (ShaderUtil.GetPropertyType(mat.shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    string propertyName = ShaderUtil.GetPropertyName(mat.shader, i);
                    var tex = mat.GetTexture(propertyName);
                    if (tex != null) uniqueTextures.Add(tex);
                }
            }
        }
        textureCount = uniqueTextures.Count;
        foreach (var tex in uniqueTextures) totalMemory += Profiler.GetRuntimeMemorySizeLong(tex);
#endif
    }

    private static string GetLodInformation(Renderer renderer)
    {
        var lodGroup = renderer.GetComponentInParent<LODGroup>();
        if (lodGroup == null) return "Not in a LOD Group";
        var lods = lodGroup.GetLODs();
        for (int i = 0; i < lods.Length; i++)
        {
            if (lods[i].renderers.Contains(renderer)) return $"LOD {i} / {lods.Length - 1}";
        }
        return "Part of LOD Group (Inactive)";
    }
}