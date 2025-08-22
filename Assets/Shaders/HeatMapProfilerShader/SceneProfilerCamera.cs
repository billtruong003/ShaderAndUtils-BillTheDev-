#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using UnityEngine.Rendering;
using UnityEngine.Profiling;
using System;
using System.Linq;
using Unity.Profiling;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class SceneProfilerCamera : MonoBehaviour
{
    #region Configuration

    [Header("Activation & Throttling")]
    public bool IsProfilingEnabled = true;
    [Min(0.2f)] public float AnalysisInterval = 1.0f;

    [Header("Clutter Reduction")]
    [Tooltip("Only show labels for objects with a heuristic score above this value.")]
    public float ScoreThreshold = 10f;
    [Tooltip("The absolute maximum number of labels to show, sorted by the highest score.")]
    [Range(1, 50)] public int MaxVisibleLabels = 15;

    [Header("Heuristic Scoring Weights")]
    [Range(0, 100)] public float PassCountWeight = 10.0f;
    [Range(0, 100)] public float VertexCountWeight = 1.0f;
    [Range(0, 100)] public float TransparencyPenalty = 25.0f;

    [Header("Label Layout")]
    [Range(1, 20)] public int LayoutIterations = 12;
    [Range(0.1f, 10.0f)] public float RepulsionForce = 2.0f;
    [Range(0.01f, 1.0f)] public float TetherStrength = 0.1f;

    [Header("Glassmorphism Style")]
    [Range(0.0f, 1.0f)] public float DefaultOpacity = 0.85f;
    [Range(0.0f, 1.0f)] public float UnselectedOpacity = 0.65f;
    public Color InspectorHighlightColor = new Color(0.2f, 0.8f, 1f, 1f);
    public Color GoodColor = new Color(0.1f, 0.8f, 0.1f, 1f);
    public Color WarningColor = new Color(0.9f, 0.9f, 0.1f, 1f);
    public Color PoorColor = new Color(0.9f, 0.2f, 0.1f, 1f);
    public Color GlassBorderColor = new Color(1f, 1f, 1f, 0.3f);
    public int LabelFontSize = 9;
    public float CornerRadius = 8f;
    [Range(1f, 3f)] public float BorderWidth = 1.5f;

    #endregion

    #region Private Fields
    private Camera profilerCamera;
    private float lastAnalysisTime;
    private float deltaTime;
    private float lastFrameTime;

    private DataModule dataModule;
    private DrawingModule drawingModule;
    private readonly LabelLayoutManager labelManager = new LabelLayoutManager();
    #endregion

    #region Unity Lifecycle
    private void OnEnable()
    {
        profilerCamera = GetComponent<Camera>();
        dataModule = new DataModule();
        dataModule.Initialize();
        drawingModule = new DrawingModule(this);

        EditorApplication.update += ManagedUpdate;
        SceneView.duringSceneGui += OnSceneGUI;
        lastFrameTime = (float)EditorApplication.timeSinceStartup;
    }

    private void OnDisable()
    {
        EditorApplication.update -= ManagedUpdate;
        SceneView.duringSceneGui -= OnSceneGUI;
        dataModule?.Dispose();
        drawingModule?.Dispose();
    }

    private void ManagedUpdate()
    {
        UpdateDeltaTime();
        if (!IsProfilingEnabled || dataModule == null) return;

        dataModule.Update();
        if (ShouldPerformHeuristicAnalysis())
        {
            dataModule.AnalyzeAllRenderers(this);
            lastAnalysisTime = (float)EditorApplication.timeSinceStartup;
            SceneView.RepaintAll();
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!IsProfilingEnabled || profilerCamera == null || drawingModule == null || dataModule == null) return;

        drawingModule.InitializeStyles();

        var renderersToDisplay = GetAndFilterRenderers();
        PrepareLabelLayout(renderersToDisplay);

        Handles.BeginGUI();
        try
        {
            drawingModule.DrawGlobalInfoPanel(deltaTime, dataModule.CpuInspector.AverageRenderThreadMs);
            foreach (var renderer in renderersToDisplay)
            {
                drawingModule.DrawVisualizationFor(renderer, dataModule, labelManager);
            }
        }
        finally
        {
            Handles.EndGUI();
        }
    }
    #endregion

    #region Core Logic Helpers
    private void UpdateDeltaTime()
    {
        float currentTime = (float)EditorApplication.timeSinceStartup;
        deltaTime = currentTime - lastFrameTime;
        lastFrameTime = currentTime;
    }

    private bool ShouldPerformHeuristicAnalysis() => EditorApplication.timeSinceStartup - lastAnalysisTime > AnalysisInterval;

    private List<Renderer> GetAndFilterRenderers()
    {
        var visibleRenderers = new List<Renderer>();
        if (dataModule == null) return visibleRenderers;

        var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(profilerCamera);

        var candidates = new List<(Renderer renderer, float score)>();

        foreach (var kvp in dataModule.HeuristicCache)
        {
            Renderer renderer = kvp.Key;
            ProfileData data = kvp.Value;

            if (renderer != null && renderer.enabled && data.Score >= ScoreThreshold && GeometryUtility.TestPlanesAABB(frustumPlanes, renderer.bounds))
            {
                candidates.Add((renderer, data.Score));
            }
        }

        return candidates.OrderByDescending(c => c.score)
                         .Take(MaxVisibleLabels)
                         .Select(c => c.renderer)
                         .ToList();
    }

    private void PrepareLabelLayout(IEnumerable<Renderer> renderersToDisplay)
    {
        labelManager.BeginFrame();
        foreach (var renderer in renderersToDisplay)
        {
            Vector2 screenPos = HandleUtility.WorldToGUIPoint(renderer.bounds.center);
            labelManager.RegisterLabel(renderer.GetInstanceID(), screenPos, drawingModule.GetLabelSize());
        }
        labelManager.CalculateLayout(LayoutIterations, RepulsionForce, TetherStrength);
    }
    #endregion

    #region Nested Core Classes

    private class RollingAverage
    {
        private readonly float[] samples;
        private int currentIndex;
        private float total;
        private int filledSamples;
        public float Average { get; private set; }
        public RollingAverage(int sampleCount) => samples = new float[Mathf.Max(1, sampleCount)];
        public void AddSample(float value)
        {
            total -= samples[currentIndex];
            samples[currentIndex] = value;
            total += value;
            currentIndex = (currentIndex + 1) % samples.Length;
            if (filledSamples < samples.Length) filledSamples++;
            Average = total / filledSamples;
        }
    }

    public class DataModule : IDisposable
    {
        public GpuTimeInspector GpuInspector { get; } = new GpuTimeInspector();
        public CpuRenderThreadInspector CpuInspector { get; } = new CpuRenderThreadInspector();
        public Dictionary<Renderer, ProfileData> HeuristicCache { get; } = new Dictionary<Renderer, ProfileData>();
        private GameObject lastSelectedObject;

        public void Initialize()
        {
            GpuInspector.Initialize();
            CpuInspector.Initialize();
        }

        public void Update()
        {
            GpuInspector.Update();
            CpuInspector.Update();
            UpdateInspectorTarget();
        }

        private void UpdateInspectorTarget()
        {
            GameObject selectedObject = Selection.activeGameObject;
            if (selectedObject != lastSelectedObject) lastSelectedObject = selectedObject;
            if (selectedObject != null && selectedObject.TryGetComponent<Renderer>(out var selectedRenderer)) GpuInspector.Measure(selectedRenderer);
        }

        public void AnalyzeAllRenderers(SceneProfilerCamera director)
        {
            HeuristicCache.Clear();
            var allRenderers = FindObjectsOfType<Renderer>();
            foreach (var renderer in allRenderers)
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                int vertexCount = GetVertexCount(renderer);
                GetPassInfo(renderer, out int passCount, out bool isTransparent, out string shaderName);
                float score = (passCount * director.PassCountWeight) + ((vertexCount / 1000f) * director.VertexCountWeight) + (isTransparent ? director.TransparencyPenalty : 0);
                var color = GetPerformanceColor(score, director);
                HeuristicCache[renderer] = new ProfileData(score, passCount, vertexCount, shaderName, color);
            }
        }

        private Color GetPerformanceColor(float score, SceneProfilerCamera director)
        {
            if (score < 20.0f) return director.GoodColor;
            if (score < 50.0f) return director.WarningColor;
            return director.PoorColor;
        }

        private int GetVertexCount(Renderer renderer)
        {
            if (renderer is MeshRenderer mr && mr.TryGetComponent<MeshFilter>(out var mf)) return mf.sharedMesh?.vertexCount ?? 0;
            if (renderer is SkinnedMeshRenderer smr) return smr.sharedMesh?.vertexCount ?? 0;
            return 0;
        }

        private void GetPassInfo(Renderer renderer, out int passCount, out bool isTransparent, out string shaderName)
        {
            passCount = 0; isTransparent = false; shaderName = "N/A";
            if (renderer.sharedMaterials.Length == 0 || renderer.sharedMaterial == null) return;
            shaderName = renderer.sharedMaterial.shader?.name ?? "Missing Shader";
            foreach (var mat in renderer.sharedMaterials)
            {
                if (mat == null) continue;
                passCount += mat.passCount;
                if (mat.renderQueue >= (int)RenderQueue.Transparent) isTransparent = true;
            }
        }

        public void Dispose()
        {
            GpuInspector.Dispose();
            CpuInspector.Dispose();
        }
    }

    public class GpuTimeInspector : IDisposable
    {
        private GraphicsFence endFence;
        private readonly Stopwatch cpuStopwatch = new Stopwatch();
        private readonly RollingAverage rollingAverage = new RollingAverage(60);
        private bool isMeasurementActive;
        public float AverageGpuTimeMs => rollingAverage.Average;
        public bool IsSupported { get; private set; }
        public void Initialize() => IsSupported = SystemInfo.supportsGraphicsFence;
        public void Measure(Renderer renderer)
        {
            if (!IsSupported || renderer == null || isMeasurementActive) return;
            var cmd = new CommandBuffer { name = "ProfilerDirector.GpuMeasure" };
            cmd.DrawRenderer(renderer, renderer.sharedMaterial);
            endFence = cmd.CreateGraphicsFence(GraphicsFenceType.AsyncQueueSynchronisation, SynchronisationStageFlags.AllGPUOperations);
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Release();
            cpuStopwatch.Restart();
            isMeasurementActive = true;
        }
        public void Update()
        {
            if (!isMeasurementActive || !endFence.passed) return;
            cpuStopwatch.Stop();
            rollingAverage.AddSample((float)cpuStopwatch.Elapsed.TotalMilliseconds);
            isMeasurementActive = false;
        }
        public void Dispose() { }
    }

    public class CpuRenderThreadInspector : IDisposable
    {
        private ProfilerRecorder recorder;
        private readonly RollingAverage rollingAverage = new RollingAverage(60);
        public float AverageRenderThreadMs => rollingAverage.Average;
        public void Initialize() => recorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Render.CPU");
        public void Update() { if (recorder.Valid) rollingAverage.AddSample(recorder.LastValue / 1_000_000f); }
        public void Dispose() => recorder.Dispose();
    }

    private class LabelLayoutManager
    {
        private class LabelInfo { public int InstanceId; public Rect ScreenRect; }
        private readonly List<LabelInfo> visibleLabels = new List<LabelInfo>();
        private readonly Dictionary<int, Vector2> offsetCache = new Dictionary<int, Vector2>();
        public void BeginFrame() => visibleLabels.Clear();
        public void RegisterLabel(int instanceId, Vector2 screenPos, Vector2 size)
        {
            var rect = new Rect(screenPos - size * 0.5f, size);
            visibleLabels.Add(new LabelInfo { InstanceId = instanceId, ScreenRect = rect });
        }
        public void CalculateLayout(int iterations, float repulsionForce, float tetherStrength)
        {
            if (offsetCache.Count != visibleLabels.Count) { offsetCache.Clear(); }
            if (!offsetCache.Any()) { foreach (var label in visibleLabels) offsetCache[label.InstanceId] = Vector2.zero; }
            for (int i = 0; i < iterations; i++)
            {
                foreach (var labelA in visibleLabels)
                    foreach (var labelB in visibleLabels)
                    {
                        if (labelA == labelB) continue;
                        Rect rectA = labelA.ScreenRect; rectA.position += offsetCache[labelA.InstanceId];
                        Rect rectB = labelB.ScreenRect; rectB.position += offsetCache[labelB.InstanceId];
                        if (rectA.Overlaps(rectB))
                        {
                            Vector2 direction = rectA.center - rectB.center;
                            Vector2 move = (direction.sqrMagnitude < 0.001f ? Vector2.up : direction.normalized) * repulsionForce;
                            offsetCache[labelA.InstanceId] += move;
                            offsetCache[labelB.InstanceId] -= move;
                        }
                    }
                var keys = new List<int>(offsetCache.Keys);
                foreach (var key in keys) { offsetCache[key] *= (1.0f - tetherStrength); }
            }
        }
        public Vector2 GetOffset(int instanceId) => offsetCache.TryGetValue(instanceId, out var offset) ? offset : Vector2.zero;
    }

    public readonly struct ProfileData
    {
        public readonly float Score;
        public readonly int PassCount;
        public readonly int VertexCount;
        public readonly string ShaderName;
        public readonly Color PerformanceColor;
        public ProfileData(float score, int passCount, int vertexCount, string shaderName, Color color)
        { Score = score; PassCount = passCount; VertexCount = vertexCount; ShaderName = shaderName; PerformanceColor = color; }
    }

    #region Base64 Encoded Icons
    private static readonly string gaugeIconBase64 = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAADISURBVEhL7dOxCsJgEAXQXbvoIYiL+Cg6OInawsWlnaXo7uLg5ir4GA6ubaGgKBa+iIOdwMGe/wTflxce+GAmoGEYy9YF7KsgkHCO4xM2GKcKzpgMViWc4zyxAgpYQAHfFBCKkU9KKf8XwMYgC3j5wB2E+Z0LAk9U4K+g/Lso4JIRBbyUK2AIhlyy82EWSHm+z0/K/CgLOHkEbejj+yV8pIAnGlDCaTj1i2hACScp8ElIgeI/UzC4LwUT9kMglmAaFkYxx3j/y8A1nZc1SbsAAAAASUVORK5CYII=";
    private static readonly string passIconBase64 = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAACNSURBVEhL7daxCYAwEAXRPUUXsIKjOITZzV0c3F0dxEEcxIWc5V5CgpoEifdwkBe8/CYhgNbW0s35JS2sJcAbxBzjVn3gI5hYxKk/eKOsxBm+cUMpY4IQTowBCmFCiBAnhCVhQggnZgQnhAlhQggnZgSnhAlhQgiXjgnPGEPGEUYI4/AV4A0S9hQBLG/kKgAAAABJRU5ErkJggg==";
    private static readonly string vertIconBase64 = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAACWSURBVEhL7daxCgAgEATAs3gED+GBPBQHsbiKR/CwE3gYjwN4jI2l9E+E8WJvLpALnmzyx4QpG0CEEG3tK+P50Q14GgJ4A2HGOP/eA0+A4Ygjjz4Q0xhjxBEHeEAwxxhzhAGGMMYcYAQyxhxghDLGGGeMEMYcYQQzxhxghDPG+acJ4xlz/gd4A2gBE7TEG14mAAAAAElFTkSuQmCC";
    #endregion

    private class DrawingModule : IDisposable
    {
        private readonly SceneProfilerCamera director;
        private GUIStyle labelStyle, shaderNameStyle;
        private readonly StringBuilder stringBuilder = new StringBuilder(256);
        private readonly Vector2 labelSize = new Vector2(200, 68);
        private Texture2D gaugeIcon, passIcon, vertIcon;

        public DrawingModule(SceneProfilerCamera director) => this.director = director;
        public Vector2 GetLabelSize() => labelSize;

        public void InitializeStyles()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(EditorStyles.label) { fontSize = director.LabelFontSize, richText = true, alignment = TextAnchor.MiddleLeft, normal = { textColor = Color.white } };
                shaderNameStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleRight };
            }
        }

        public void Dispose() { DestroyImmediate(gaugeIcon); DestroyImmediate(passIcon); DestroyImmediate(vertIcon); }
        private void DestroyImmediate(UnityEngine.Object obj) { if (obj != null) UnityEngine.Object.DestroyImmediate(obj); }
        private void CreateIcons()
        {
            if (gaugeIcon != null) return;
            gaugeIcon = CreateTextureFromBase64(gaugeIconBase64);
            passIcon = CreateTextureFromBase64(passIconBase64);
            vertIcon = CreateTextureFromBase64(vertIconBase64);
        }
        private Texture2D CreateTextureFromBase64(string base64)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            tex.LoadImage(Convert.FromBase64String(base64));
            tex.hideFlags = HideFlags.HideAndDontSave;
            return tex;
        }

        public void DrawGlobalInfoPanel(float dt, float renderThreadMs)
        {
            CreateIcons();
            stringBuilder.Clear();
            stringBuilder.Append($"<b>FPS: </b>{(1.0f / dt):F0}\n");
            stringBuilder.Append($"<b>CPU Render Thread: </b>{renderThreadMs:F2} ms");
            Rect panelRect = new Rect(15, 15, 220, 45);
            DrawGlassPanel(panelRect, new Color(0.1f, 0.1f, 0.1f, director.DefaultOpacity), director.GlassBorderColor);
            GUI.Label(new Rect(panelRect.x + 8, panelRect.y + 5, panelRect.width - 16, panelRect.height - 10), stringBuilder.ToString(), labelStyle);
        }

        public void DrawVisualizationFor(Renderer renderer, DataModule data, LabelLayoutManager layoutManager)
        {
            bool isInspected = Selection.activeGameObject == renderer.gameObject;
            if (!data.HeuristicCache.TryGetValue(renderer, out var profile)) return;

            Vector3 worldPos = renderer.bounds.center;
            Vector2 screenPos = HandleUtility.WorldToGUIPoint(worldPos);
            Vector2 offset = layoutManager.GetOffset(renderer.GetInstanceID());
            Rect labelRect = new Rect(screenPos + offset - labelSize * 0.5f, labelSize);

            Color panelColor = isInspected ? director.InspectorHighlightColor : profile.PerformanceColor;
            float opacity = isInspected ? director.DefaultOpacity : director.UnselectedOpacity;

            DrawGlassPanel(labelRect, new Color(panelColor.r, panelColor.g, panelColor.b, opacity), new Color(director.GlassBorderColor.r, director.GlassBorderColor.g, director.GlassBorderColor.b, opacity));
            DrawConnectorLine(worldPos, GetClosestPointOnRect(labelRect, screenPos), isInspected);

            if (isInspected) { Handles.color = director.InspectorHighlightColor; Handles.DrawWireCube(renderer.bounds.center, renderer.bounds.size * 1.05f); }

            DrawLabelContent(labelRect, profile, data.GpuInspector, isInspected);
        }

        private void DrawLabelContent(Rect area, ProfileData profile, GpuTimeInspector gpuInspector, bool isInspected)
        {
            GUI.BeginGroup(area);
            Rect localArea = new Rect(Vector2.zero, area.size);
            if (isInspected)
            {
                stringBuilder.Clear().AppendLine("<b><color=white>INSPECTING GPU</color></b>").Append(gpuInspector.IsSupported ? $"Avg Time: <b>{gpuInspector.AverageGpuTimeMs:F3} ms</b>" : "<b><color=yellow>Not Supported</color></b>");
                GUI.Label(new Rect(localArea.x + 8, localArea.y + 8, localArea.width - 16, localArea.height - 16), stringBuilder.ToString(), labelStyle);
            }
            else
            {
                GUI.DrawTexture(new Rect(8, 8, 16, 16), gaugeIcon, ScaleMode.ScaleToFit);
                GUI.Label(new Rect(28, 9, 100, 16), $"<b>Score: {profile.Score:F1}</b>", labelStyle);
                GUI.Label(new Rect(localArea.width - 118, 9, 110, 16), $"<color=#CCCCCC>{ShortenString(profile.ShaderName, 15)}</color>", shaderNameStyle);
                float statY = 38, h = 20, iconSize = 14, iconPad = 8, textPad = 26;
                GUI.DrawTexture(new Rect(iconPad, statY, iconSize, iconSize), passIcon, ScaleMode.ScaleToFit);
                GUI.Label(new Rect(textPad, statY, 100, h), $"Passes: {profile.PassCount}", labelStyle);
                GUI.DrawTexture(new Rect(localArea.width / 2f + iconPad, statY, iconSize, iconSize), vertIcon, ScaleMode.ScaleToFit);
                GUI.Label(new Rect(localArea.width / 2f + textPad, statY, 100, h), $"Verts: {profile.VertexCount / 1000f:F1}k", labelStyle);
            }
            GUI.EndGroup();
        }

        private string ShortenString(string name, int maxLength) => (name.Length <= maxLength) ? name : name.Substring(0, maxLength - 1) + "...";

        private void DrawGlassPanel(Rect rect, Color bgColor, Color borderColor)
        {
            var verts = GetRoundedRectVerts(rect, director.CornerRadius);
            Handles.BeginGUI();
            Handles.DrawSolidRectangleWithOutline(verts, bgColor, Color.clear);
            Handles.DrawAAPolyLine(director.BorderWidth, verts);
            Handles.EndGUI();
        }

        private Vector3[] GetRoundedRectVerts(Rect rect, float radius)
        {
            const int segments = 15;
            radius = Mathf.Min(radius, rect.width / 2, rect.height / 2);
            var verts = new List<Vector3>();
            if (radius < 1) { verts.AddRange(new Vector3[] { rect.min, new Vector2(rect.xMax, rect.yMin), rect.max, new Vector2(rect.xMin, rect.yMax) }); }
            else
            {
                Action<Vector2, float> addQuarter = (center, angleStart) => { for (int i = 0; i <= segments; i++) verts.Add(center + new Vector2(Mathf.Cos(angleStart + i * Mathf.PI / 2 / segments), Mathf.Sin(angleStart + i * Mathf.PI / 2 / segments)) * radius); };
                addQuarter(new Vector2(rect.xMin + radius, rect.yMin + radius), Mathf.PI);
                addQuarter(new Vector2(rect.xMax - radius, rect.yMin + radius), -Mathf.PI / 2);
                addQuarter(new Vector2(rect.xMax - radius, rect.yMax - radius), 0);
                addQuarter(new Vector2(rect.xMin + radius, rect.yMax - radius), Mathf.PI / 2);
            }
            verts.Add(verts[0]);
            return verts.ToArray();
        }

        private void DrawConnectorLine(Vector3 worldObjectCenter, Vector2 guiLabelEdgePoint, bool isInspected)
        {
            Vector3 worldLabelEdge = HandleUtility.GUIPointToWorldRay(guiLabelEdgePoint).GetPoint(10);
            Handles.color = isInspected ? director.InspectorHighlightColor : director.GlassBorderColor;
            Handles.DrawAAPolyLine(director.BorderWidth, worldObjectCenter, worldLabelEdge);
        }

        private Vector2 GetClosestPointOnRect(Rect rect, Vector2 point) => new Vector2(Mathf.Clamp(point.x, rect.xMin, rect.xMax), Mathf.Clamp(point.y, rect.yMin, rect.yMax));
    }
    #endregion
}
#endif