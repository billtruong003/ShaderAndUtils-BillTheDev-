using UnityEngine;

public static class ShaderAnalysisCache
{
    private static ShaderAnalysisData _data;
    private static bool _isInitialized = false;

    private static void Initialize()
    {
        if (_isInitialized) return;

        _data = Resources.Load<ShaderAnalysisData>("ShaderAnalysisData");
        if (_data != null)
        {
            _data.BuildLookup();
        }
        _isInitialized = true;
    }

    public static bool TryGetShaderStats(string shaderName, out ShaderAnalysisData.ShaderStats stats)
    {
        Initialize();
        stats = default;
        return _data != null && _data.TryGetShaderStats(shaderName, out stats);
    }
}