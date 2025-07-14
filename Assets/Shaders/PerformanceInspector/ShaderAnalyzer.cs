#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public static class ShaderAnalyzer
{
    private const string DataAssetPath = "Assets/Resources/ShaderAnalysisData.asset";

    private const float PassWeight = 2f;
    private const float PropertyWeight = 0.5f;
    private const float VariantWeight = 0.05f;
    private const float InstructionWeight = 0.1f;

    private static MethodInfo _getSubshaderCountMethod;
    private static MethodInfo _getTotalPassCountMethod;

    [MenuItem("Tools/Performance Inspector/Analyze All Project Shaders")]
    public static void AnalyzeShadersInProject()
    {
        EnsureResourcesFolderExists();
        ShaderAnalysisData data = LoadOrCreateAnalysisData();

        data.AllShaderStats.Clear();
        var allShaderGuids = AssetDatabase.FindAssets("t:Shader");

        EditorUtility.DisplayProgressBar("Analyzing Shaders", "Starting analysis...", 0f);
        try
        {
            for (int i = 0; i < allShaderGuids.Length; i++)
            {
                var guid = allShaderGuids[i];
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);

                if (shader == null || !shader.isSupported || string.IsNullOrEmpty(path)) continue;

                string progressInfo = $"Analyzing: {shader.name}";
                float progress = (float)i / allShaderGuids.Length;
                if (EditorUtility.DisplayCancelableProgressBar("Analyzing Shaders", progressInfo, progress))
                {
                    break;
                }

                var stats = CalculateStaticShaderStats(shader, path);
                data.AllShaderStats.Add(stats);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Shader analysis complete. Analyzed {data.AllShaderStats.Count} shaders. Data saved to '{DataAssetPath}'");
    }

    private static ShaderAnalysisData LoadOrCreateAnalysisData()
    {
        ShaderAnalysisData data = AssetDatabase.LoadAssetAtPath<ShaderAnalysisData>(DataAssetPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<ShaderAnalysisData>();
            AssetDatabase.CreateAsset(data, DataAssetPath);
        }
        return data;
    }

    private static ShaderAnalysisData.ShaderStats CalculateStaticShaderStats(Shader shader, string path)
    {
        int passCount = GetTotalPassCountViaReflection(shader);
        int propertyCount = ShaderUtil.GetPropertyCount(shader);
        long variantCount = CalculateShaderVariantCount(path);
        long instructionCost = AnalyzeHlslInstructionCost(path, new HashSet<string>());

        float staticScore = (passCount * PassWeight) +
                              (propertyCount * PropertyWeight) +
                              (variantCount * VariantWeight) +
                              (instructionCost * InstructionWeight);

        return new ShaderAnalysisData.ShaderStats
        {
            ShaderName = shader.name,
            StaticComplexityScore = staticScore,
            SamplerCount = CountSamplerProperties(shader),
            InstructionCost = instructionCost
        };
    }

    private static long AnalyzeHlslInstructionCost(string filePath, HashSet<string> processedIncludes)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath) || !processedIncludes.Add(filePath))
            return 0;

        string content = File.ReadAllText(filePath);
        long cost = 0;

        cost += Regex.Matches(content, @"\b(tex2D|tex2Dlod|texCUBE|texCUBElod|SAMPLE_TEXTURE2D|SAMPLE_TEXTURE2D_LOD|SAMPLE_TEXTURECUBE|SAMPLE_TEXTURECUBE_LOD)\b").Count * 10;
        cost += Regex.Matches(content, @"\b(sin|cos|tan|asin|acos|atan|pow|exp|log|sqrt|rsqrt|ddx|ddy)\b").Count * 3;
        cost += Regex.Matches(content, @"\b(if|for|while)\b").Count * 2;

        var includeRegex = new Regex(@"#include\s+""([^""]+)""");
        var matches = includeRegex.Matches(content);
        foreach (Match match in matches)
        {
            string includePath = match.Groups[1].Value;
            string fullIncludePath = FindIncludeFile(filePath, includePath);
            cost += AnalyzeHlslInstructionCost(fullIncludePath, processedIncludes);
        }
        return cost;
    }

    private static string FindIncludeFile(string currentFilePath, string includePath)
    {
        string currentDir = Path.GetDirectoryName(currentFilePath);
        string absolutePath = Path.GetFullPath(Path.Combine(currentDir, includePath));
        if (File.Exists(absolutePath)) return absolutePath;

        string projectRoot = Path.GetFullPath(Application.dataPath + "/..");
        string packagePath = Path.Combine(projectRoot, includePath);
        if (File.Exists(packagePath)) return packagePath;

        return null;
    }

    private static long CalculateShaderVariantCount(string shaderPath)
    {
        if (string.IsNullOrEmpty(shaderPath) || !File.Exists(shaderPath)) return 1;

        string content = File.ReadAllText(shaderPath);
        var regex = new Regex(@"#pragma\s+(?:shader_feature|multi_compile|shader_feature_local|multi_compile_fragment|multi_compile_vertex)\s+(.*)");
        var matches = regex.Matches(content);

        if (matches.Count == 0) return 1;

        long totalVariants = 1;
        foreach (Match match in matches)
        {
            if (match.Groups.Count < 2) continue;

            string keywordsString = match.Groups[1].Value.Trim();
            if (keywordsString.StartsWith("instancing") || keywordsString.Contains("EDITOR_VISUALIZATION")) continue;

            int variantCountInLine = keywordsString.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).Length;
            if (variantCountInLine == 0) variantCountInLine = 1;

            if (match.Value.Contains("shader_feature"))
            {
                variantCountInLine += 1;
            }

            if (variantCountInLine > 1)
            {
                if (long.MaxValue / variantCountInLine < totalVariants) return long.MaxValue;
                totalVariants *= variantCountInLine;
            }
        }
        return totalVariants;
    }

    private static int GetTotalPassCountViaReflection(Shader shader)
    {
        if (_getSubshaderCountMethod == null)
            _getSubshaderCountMethod = typeof(ShaderUtil).GetMethod("GetShaderSubshaderCount", BindingFlags.Static | BindingFlags.NonPublic);
        if (_getTotalPassCountMethod == null)
            _getTotalPassCountMethod = typeof(ShaderUtil).GetMethod("GetShaderTotalPassCount", BindingFlags.Static | BindingFlags.NonPublic);

        if (_getSubshaderCountMethod == null || _getTotalPassCountMethod == null)
        {
            Debug.LogWarning("Could not find internal shader analysis methods via reflection. Pass count will be inaccurate.");
            return 1;
        }

        try
        {
            int subShaderCount = (int)_getSubshaderCountMethod.Invoke(null, new object[] { shader });
            int totalPassCount = 0;
            for (int i = 0; i < subShaderCount; i++)
            {
                totalPassCount += (int)_getTotalPassCountMethod.Invoke(null, new object[] { shader, i });
            }
            return totalPassCount;
        }
        catch { return 1; }
    }

    private static void EnsureResourcesFolderExists()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
    }

    private static int CountSamplerProperties(Shader shader)
    {
        int count = 0;
        int propertyCount = ShaderUtil.GetPropertyCount(shader);
        for (int i = 0; i < propertyCount; i++)
        {
            if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                count++;
        }
        return count;
    }
}
#endif