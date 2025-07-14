using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
[CreateAssetMenu(fileName = "ShaderAnalysisData", menuName = "Performance Inspector/Shader Analysis Data")]
#endif
public class ShaderAnalysisData : ScriptableObject
{
    [System.Serializable]
    public struct ShaderStats
    {
        public string ShaderName;
        public float StaticComplexityScore;
        public int SamplerCount;
        public long InstructionCost;
    }

    public List<ShaderStats> AllShaderStats = new List<ShaderStats>();
    private Dictionary<string, ShaderStats> _statsLookup;
    private bool _isLookupBuilt = false;

    public void BuildLookup()
    {
        if (AllShaderStats == null) AllShaderStats = new List<ShaderStats>();

        _statsLookup = new Dictionary<string, ShaderStats>(AllShaderStats.Count);
        foreach (var stat in AllShaderStats)
        {
            if (!_statsLookup.ContainsKey(stat.ShaderName))
            {
                _statsLookup.Add(stat.ShaderName, stat);
            }
        }
        _isLookupBuilt = true;
    }

    public bool TryGetShaderStats(string shaderName, out ShaderStats stats)
    {
        if (!_isLookupBuilt)
        {
            BuildLookup();
        }
        stats = default;
        return _statsLookup != null && _statsLookup.TryGetValue(shaderName, out stats);
    }
}