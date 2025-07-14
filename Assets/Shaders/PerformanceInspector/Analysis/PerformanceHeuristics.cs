using System.Collections.Generic;
using UnityEngine;

public static class PerformanceHeuristics
{
    private const int HIGH_VERTEX_COUNT_THRESHOLD = 80000;
    private const int HIGH_STATIC_SCORE_THRESHOLD = 100;
    private const int HIGH_KEYWORD_COUNT_THRESHOLD = 5;
    private const long HIGH_TEXTURE_MEMORY_THRESHOLD = 1024 * 1024; // 1 MB

    public static List<string> GenerateSuggestions(InspectedObjectDetailedData data)
    {
        var suggestions = new List<string>();

        if (data.VertexCount > HIGH_VERTEX_COUNT_THRESHOLD)
            suggestions.Add($"High Vertex Count (>{HIGH_VERTEX_COUNT_THRESHOLD / 1000}k): Consider mesh simplification or using LODs.");

        if (data.MaterialCount > 1)
            suggestions.Add("Multiple Materials: Consider creating a texture atlas to use a single material.");

        if (data.StaticPerformanceScore > HIGH_STATIC_SCORE_THRESHOLD)
            suggestions.Add($"High Static Shader Score ({data.StaticPerformanceScore:F0}): Shader is complex. Simplify logic or reduce variants.");

        if (data.TotalTextureMemoryBytes > HIGH_TEXTURE_MEMORY_THRESHOLD)
            suggestions.Add($"High Texture Memory (>{data.TotalTextureMemoryBytes / 1024f / 1024f:F2} MB): Consider texture compression or resizing.");

        if (data.IsTransparent)
            suggestions.Add("Transparent Shader: Contributes to overdraw. Ensure transparency is necessary.");

        if (data.ActiveKeywords.Count > HIGH_KEYWORD_COUNT_THRESHOLD)
            suggestions.Add($"Many Active Keywords ({data.ActiveKeywords.Count}): Can increase shader variants, memory, and build time.");

        if (data.StaticPerformanceScore == 0 && !string.IsNullOrEmpty(data.ShaderName) && data.ShaderName != "N/A")
            suggestions.Add("Static analysis data missing. Run 'Tools/Performance Inspector/Analyze All Project Shaders'.");

        if (!data.IsStatic && !IsAnimated(data.Renderer))
            suggestions.Add("Object is dynamic but not animated: If it doesn't move, mark as static to enable static batching.");

        if (data.IsStatic && !data.IsPartOfStaticBatch && data.MaterialCount == 1 && data.Renderer.GetComponentInParent<LODGroup>() == null)
            suggestions.Add("Static object not batched: Check material sharing with other static objects to enable static batching.");

        return suggestions;
    }

    private static bool IsAnimated(Renderer renderer)
    {
        return renderer.GetComponent<Animation>() != null || renderer.GetComponent<Animator>() != null;
    }
}