#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Text;
using System.Linq;

public class PerformanceVisualizer : IPerformanceVisualizer
{
    private GUIStyle _panelStyle, _headerStyle, _labelStyle, _richTextLabelStyle, _suggestionStyle, _frozenLabelStyle;
    private bool _stylesInitialized = false;

    private readonly StringBuilder _detailsStringBuilder = new StringBuilder(2048);
    private static readonly Rect InitialTargetDetailWindowRect = new Rect(10, 100, 420, 520);
    private Rect _targetDetailWindowRect = InitialTargetDetailWindowRect;

    private Material _heatmapMaterial;

    public void DrawScreenOverlays(PerformanceInspector inspector)
    {
        EnsureStylesAreInitialized();

        DrawGlobalStatsPanel(inspector);
        if (inspector.IsAnalysisFrozen)
        {
            DrawFrozenIndicator(inspector.InspectorCamera.pixelRect);
        }

        bool hasTarget = inspector.TryGetInspectedDataAtScreenCenter(out _, out var targetData);
        _targetDetailWindowRect = GUILayout.Window(0, _targetDetailWindowRect, id => DrawTargetDetailsWindow(id, inspector, hasTarget, targetData), "Target Details");
    }

    public void DrawSceneVisuals(PerformanceInspector inspector)
    {
        EnsureResourcesAreInitialized();
        DrawHeatmap(inspector);
        if (inspector.TryGetInspectedDataAtScreenCenter(out _, out var targetData))
        {
            DrawTargetSelection(targetData.Renderer);
        }
    }

    private void DrawTargetDetailsWindow(int windowID, PerformanceInspector inspector, bool hasTarget, in InspectedObjectDetailedData targetData)
    {
        if (hasTarget)
        {
            BuildTargetDetailsString(targetData);
            GUILayout.Label(_detailsStringBuilder.ToString(), _richTextLabelStyle);

            if (targetData.Suggestions.Any())
            {
                GUILayout.Space(10);
                GUILayout.Label("<b>Suggestions:</b>", _richTextLabelStyle);
                foreach (var suggestion in targetData.Suggestions)
                {
                    GUILayout.Label($"• {suggestion}", _suggestionStyle);
                }
            }
            DrawRuntimeAnalysisSection(inspector, targetData);
        }
        else
        {
            GUILayout.Label("No target found. Point camera at an object with a Renderer.", _labelStyle);
        }
        GUI.DragWindow();
    }

    private void DrawGlobalStatsPanel(PerformanceInspector inspector)
    {
        var data = inspector.DataCollector;
        GUILayout.BeginArea(new Rect(10, 10, 280, 85), string.Empty, _panelStyle);
        GUILayout.Label("Global Stats", _headerStyle);
        GUILayout.Label($"GPU Time: {data.GpuFrameTimeMs:F2} ms", _labelStyle);
        GUILayout.Label($"CPU Time: {data.CpuFrameTimeMs:F2} ms", _labelStyle);
        if (GUILayout.Button("Re-Analyze All Shaders", GUILayout.Height(20)))
        {
            ShaderAnalyzer.AnalyzeShadersInProject();
        }
        GUILayout.EndArea();
    }

    private void DrawFrozenIndicator(Rect cameraRect)
    {
        GUI.Label(new Rect(cameraRect.width / 2 - 100, 20, 200, 30), "ANALYSIS FROZEN", _frozenLabelStyle);
    }

    private void BuildTargetDetailsString(in InspectedObjectDetailedData targetData)
    {
        _detailsStringBuilder.Clear();
        _detailsStringBuilder.AppendLine($"<b>Name:</b> {targetData.Name}");
        _detailsStringBuilder.AppendLine($"<b>Dynamic Score:</b> <color=#f5b041>{targetData.DynamicPerformanceScore:F0}</color> <i>(Heuristic cost)</i>");
        _detailsStringBuilder.AppendLine($"<b>Static Score:</b> <color=#a0a0a0>{targetData.StaticPerformanceScore:F0}</color> <i>(Shader file complexity)</i>");
        _detailsStringBuilder.AppendLine($"<color=grey>───────────────────────────</color>");

        string meshInfo = targetData.TriangleCount >= 0
            ? $"{targetData.VertexCount:N0} Verts, {targetData.TriangleCount:N0} Tris, {targetData.SubMeshCount} Sub-meshes"
            : $"{targetData.VertexCount:N0} Verts, <color=orange>N/A Tris (Mesh not readable)</color>, {targetData.SubMeshCount} Sub-meshes";
        _detailsStringBuilder.AppendLine($"<b>Mesh:</b> {meshInfo}");

        _detailsStringBuilder.AppendLine($"<b>Materials ({targetData.MaterialCount}):</b> {string.Join(", ", targetData.MaterialNames)}");
        _detailsStringBuilder.AppendLine($"<b>Shader:</b> {targetData.ShaderName}");
        _detailsStringBuilder.AppendLine($"    <color=#aed6f1><b>Active Keywords:</b> {(targetData.ActiveKeywords.Any() ? string.Join(", ", targetData.ActiveKeywords) : "None")}</color>");
        _detailsStringBuilder.AppendLine($"<b>Textures:</b> {targetData.TotalTextureCount} ({targetData.TotalTextureMemoryBytes / 1024f:F1} KB)");
        _detailsStringBuilder.AppendLine($"<color=grey>───────────────────────────</color>");
        _detailsStringBuilder.AppendLine($"<b>State:</b> {(targetData.IsStatic ? "Static" : "Dynamic")} {(targetData.IsPartOfStaticBatch ? "<b>(Statically Batched)</b>" : "")}");
        _detailsStringBuilder.AppendLine($"<b>LODs:</b> {targetData.LodInfo}");
    }

    private void DrawRuntimeAnalysisSection(PerformanceInspector inspector, in InspectedObjectDetailedData targetData)
    {
        GUILayout.Space(10);
        GUILayout.Label("<b>Runtime GPU Analysis (Hotkey: G)</b>", _headerStyle);

        var gpuAnalyzer = inspector.GpuAnalyzer;
        if (gpuAnalyzer == null) return;

        GUI.enabled = !gpuAnalyzer.IsAnalyzing;
        if (GUILayout.Button(gpuAnalyzer.IsAnalyzing ? "Analyzing..." : "Analyze GPU Runtime Cost"))
        {
            gpuAnalyzer.TriggerAnalysis(targetData.Renderer);
        }
        GUI.enabled = true;

        string displayText;
        if (gpuAnalyzer.LastAnalyzedTarget == targetData.Renderer)
        {
            if (gpuAnalyzer.IsAnalyzing)
            {
                displayText = "Waiting for GPU...";
            }
            else if (gpuAnalyzer.IsMeasurementReady)
            {
                displayText = $"<b>{FormatGpuTime(gpuAnalyzer.GpuTimeMilliseconds)}</b>";
            }
            else
            {
                displayText = "Ready to analyze.";
            }
        }
        else
        {
            displayText = "Point at target and press 'G'.";
        }
        GUILayout.Label($"Measured GPU Cost: {displayText}", _richTextLabelStyle);
    }

    private void DrawHeatmap(PerformanceInspector inspector)
    {
        foreach (var data in inspector.VisibleObjectsData)
        {
            if (data.Renderer == null) continue;

            float normalizedScore = (inspector.MaxPerformanceScore > inspector.MinPerformanceScore)
                ? Mathf.InverseLerp(inspector.MinPerformanceScore, inspector.MaxPerformanceScore, data.DynamicPerformanceScore)
                : 0f;
            Color heatColor = inspector.HeatmapGradient.Evaluate(normalizedScore);
            heatColor.a = inspector.HeatmapAlpha;

            Mesh meshToDraw = GetMeshFromRenderer(data.Renderer);
            if (meshToDraw == null) continue;

            try
            {
                _heatmapMaterial.color = heatColor;
                if (_heatmapMaterial.SetPass(0))
                {
                    Graphics.DrawMeshNow(meshToDraw, data.Renderer.transform.localToWorldMatrix, 0);
                }
            }
            catch (System.Exception)
            {
                // Mesh is not readable or another issue occurred. Silently ignore to prevent tool from crashing.
            }
        }
    }

    private Mesh GetMeshFromRenderer(Renderer renderer)
    {
        if (renderer is MeshRenderer mr && mr.TryGetComponent<MeshFilter>(out var mf))
        {
            return mf.sharedMesh;
        }
        if (renderer is SkinnedMeshRenderer smr)
        {
            return smr.sharedMesh;
        }
        return null;
    }

    private void DrawTargetSelection(Renderer targetRenderer)
    {
        if (targetRenderer == null) return;
        Handles.color = Color.yellow;
        Handles.DrawWireCube(targetRenderer.bounds.center, targetRenderer.bounds.size);
    }

    private string FormatGpuTime(float milliseconds)
    {
        return $"{milliseconds:F4} ms";
    }

    private void EnsureResourcesAreInitialized()
    {
        if (_heatmapMaterial == null)
        {
            _heatmapMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
        }
    }

    private void EnsureStylesAreInitialized()
    {
        if (_stylesInitialized) return;

        _panelStyle = new GUIStyle(EditorStyles.helpBox);
        _headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color(0.4f, 0.9f, 1f) } };
        _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true };
        _richTextLabelStyle = new GUIStyle(_labelStyle) { richText = true };
        _suggestionStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Italic, wordWrap = true, normal = { textColor = Color.yellow } };
        _frozenLabelStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.cyan } };

        _stylesInitialized = true;
    }
}
#endif