using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using UnityEngine.Rendering;
using System.Linq;
using Unity.Profiling;
using System;

namespace BillTheDev.ProfilerDirector
{
    #region Core Logic Modules

    internal class DataModule : IDisposable
    {
        public Dictionary<int, RendererProfile> ProfileCache { get; } = new Dictionary<int, RendererProfile>();
        public GlobalMetrics GlobalMetrics { get; private set; }
        public readonly GpuTimeModule GpuTime;
        private readonly ProfilerDirectorSettings _settings;

        private float _deltaTime;
        private float _lastFrameTime;
        private ProfilerRecorder _cpuRenderThreadRecorder;
        private ProfilerRecorder _drawCallsRecorder;
        private ProfilerRecorder _setPassCallsRecorder;

        public DataModule(ProfilerDirectorSettings settings)
        {
            _settings = settings;
            GpuTime = new GpuTimeModule(settings);
        }

        public void Initialize()
        {
            GpuTime.Initialize();
            _cpuRenderThreadRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Render.CPU");
            _drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            _setPassCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            _lastFrameTime = (float)EditorApplication.timeSinceStartup;
        }

        public void UpdateCoreMetrics()
        {
            UpdateDeltaTime();
            GpuTime.Update();

            GlobalMetrics = new GlobalMetrics(
                _deltaTime,
                _cpuRenderThreadRecorder.Valid ? _cpuRenderThreadRecorder.LastValue / 1_000_000f : 0f,
                _drawCallsRecorder.Valid ? _drawCallsRecorder.LastValue : 0,
                _setPassCallsRecorder.Valid ? _setPassCallsRecorder.LastValue : 0
            );
        }

        public void UpdateSceneAnalysis(Camera camera)
        {
            if (camera == null) return;
            ProfileCache.Clear();

            var allRenderers = UnityEngine.Object.FindObjectsOfType<Renderer>();
            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);

            foreach (var renderer in allRenderers)
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || !GeometryUtility.TestPlanesAABB(frustumPlanes, renderer.bounds))
                {
                    continue;
                }

                GpuTime.TryGetAverageGpuTime(renderer.GetInstanceID(), out float gpuTime);
                ProfileCache[renderer.GetInstanceID()] = new RendererProfile(renderer, _settings, camera, gpuTime);
            }

            QueueExpensiveRenderersForGpuMeasurement();
        }

        public bool TryGetProfileForSelection(out RendererProfile profile)
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null || !selected.TryGetComponent<Renderer>(out var renderer))
            {
                profile = default;
                return false;
            }

            // Luôn đảm bảo profile cho đối tượng được chọn là mới nhất
            var camera = SceneView.currentDrawingSceneView?.camera;
            if (camera == null)
            {
                profile = default;
                return false;
            }
            GpuTime.QueueMeasurement(renderer); // Ưu tiên đo đối tượng được chọn
            GpuTime.TryGetAverageGpuTime(renderer.GetInstanceID(), out float gpuTime);
            profile = new RendererProfile(renderer, _settings, camera, gpuTime);
            ProfileCache[renderer.GetInstanceID()] = profile;
            return true;
        }

        private void QueueExpensiveRenderersForGpuMeasurement()
        {
            var expensiveRenderers = ProfileCache.Values
                .OrderByDescending(p => p.Score)
                .Take(_settings.MaxVisibleObjects)
                .Select(p => p.SourceRenderer);

            foreach (var renderer in expensiveRenderers)
            {
                GpuTime.QueueMeasurement(renderer);
            }
        }

        private void UpdateDeltaTime()
        {
            float currentTime = (float)EditorApplication.timeSinceStartup;
            _deltaTime = currentTime - _lastFrameTime;
            _lastFrameTime = currentTime;
        }

        public void Dispose()
        {
            GpuTime.Dispose();
            _cpuRenderThreadRecorder.Dispose();
            _drawCallsRecorder.Dispose();
            _setPassCallsRecorder.Dispose();
        }
    }

    internal class GpuTimeModule : IDisposable
    {
        private readonly Dictionary<int, RollingAverage> _gpuTimeAverages = new Dictionary<int, RollingAverage>();
        private readonly Queue<Renderer> _measurementQueue = new Queue<Renderer>();
        private readonly Stopwatch _cpuStopwatch = new Stopwatch();
        private readonly ProfilerDirectorSettings _settings;

        private GraphicsFence _endFence;
        private bool _isMeasurementActive;
        private Renderer _currentRenderer;
        private float _lastMeasurementTime;
        public bool IsSupported { get; private set; }

        public GpuTimeModule(ProfilerDirectorSettings settings) => _settings = settings;

        public void Initialize() => IsSupported = SystemInfo.supportsGraphicsFence;

        public void QueueMeasurement(Renderer renderer)
        {
            if (!IsSupported || renderer == null || _measurementQueue.Contains(renderer)) return;
            _measurementQueue.Enqueue(renderer);
        }

        public void Update()
        {
            if (!IsSupported) return;

            ProcessCompletedMeasurement();
            StartNewMeasurement();
        }

        private void ProcessCompletedMeasurement()
        {
            if (!_isMeasurementActive || !_endFence.passed) return;

            _cpuStopwatch.Stop();
            int id = _currentRenderer.GetInstanceID();

            if (!_gpuTimeAverages.ContainsKey(id))
            {
                _gpuTimeAverages[id] = new RollingAverage(_settings.GpuTimeAverageFrames);
            }
            _gpuTimeAverages[id].AddSample((float)_cpuStopwatch.Elapsed.TotalMilliseconds);
            _isMeasurementActive = false;
            _currentRenderer = null;
        }

        private void StartNewMeasurement()
        {
            float timeSinceLast = Time.realtimeSinceStartup - _lastMeasurementTime;
            float interval = 1f / _settings.GpuMeasurementsPerSecond;

            if (_isMeasurementActive || _measurementQueue.Count == 0 || timeSinceLast < interval) return;

            var rendererToMeasure = _measurementQueue.Dequeue();
            if (rendererToMeasure == null) return;

            try
            {
                var cmd = new CommandBuffer { name = "ProfilerDirector.GpuMeasure" };
                cmd.DrawRenderer(rendererToMeasure, rendererToMeasure.sharedMaterial);
                _endFence = cmd.CreateGraphicsFence(GraphicsFenceType.AsyncQueueSynchronisation, SynchronisationStageFlags.AllGPUOperations);
                Graphics.ExecuteCommandBuffer(cmd);
                cmd.Release();

                _cpuStopwatch.Restart();
                _isMeasurementActive = true;
                _currentRenderer = rendererToMeasure;
                _lastMeasurementTime = Time.realtimeSinceStartup;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"Profiler Director: Could not measure GPU time for {rendererToMeasure.name}. Reason: {e.Message}");
                _isMeasurementActive = false;
                _currentRenderer = null;
            }
        }

        public bool TryGetAverageGpuTime(int instanceId, out float gpuTime)
        {
            if (_gpuTimeAverages.TryGetValue(instanceId, out var rollingAverage))
            {
                gpuTime = rollingAverage.Average;
                return true;
            }
            gpuTime = 0f;
            return false;
        }

        public void Dispose() { }
    }

    internal class HeatmapModule : IDisposable
    {
        private readonly CommandBuffer _commandBuffer;
        private readonly Dictionary<int, Material> _tempMaterialCache = new Dictionary<int, Material>();
        private readonly ProfilerDirectorSettings _settings;
        private readonly Material _heatmapOverlayMaterial;

        private Camera _lastAppliedCamera;
        private bool _isActive;

        public HeatmapModule(ProfilerDirectorSettings settings)
        {
            _settings = settings;
            _commandBuffer = new CommandBuffer { name = "ProfilerDirector.Heatmap" };

            if (ProfilerDirectorResources.HeatmapOverlayShader != null)
            {
                _heatmapOverlayMaterial = new Material(ProfilerDirectorResources.HeatmapOverlayShader);
            }
        }

        public void Apply(IEnumerable<RendererProfile> profiles, Camera camera)
        {
            if (_heatmapOverlayMaterial == null || camera == null) return;

            // Gắn CommandBuffer nếu chưa có hoặc camera đã thay đổi
            if (!_isActive || _lastAppliedCamera != camera)
            {
                RemoveCommandBufferFromCamera(); // Dọn dẹp cái cũ nếu có
                camera.AddCommandBuffer(CameraEvent.BeforeImageEffects, _commandBuffer);
                _lastAppliedCamera = camera;
                _isActive = true;
            }

            _commandBuffer.Clear();

            foreach (var profile in profiles)
            {
                var renderer = profile.SourceRenderer;
                if (renderer == null || !renderer.gameObject.activeInHierarchy) continue;

                if (!_tempMaterialCache.TryGetValue(renderer.GetInstanceID(), out var tempMaterial))
                {
                    tempMaterial = new Material(_heatmapOverlayMaterial);
                    _tempMaterialCache[renderer.GetInstanceID()] = tempMaterial;
                }

                Color heatColor = profile.PerformanceColor;
                heatColor.a = _settings.HeatmapOpacity;
                tempMaterial.SetColor("_HeatColor", heatColor);

                // Lệnh vẽ renderer với material đã tùy chỉnh
                _commandBuffer.DrawRenderer(renderer, tempMaterial);
            }
        }

        private void RemoveCommandBufferFromCamera()
        {
            if (_lastAppliedCamera != null && _commandBuffer != null)
            {
                _lastAppliedCamera.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, _commandBuffer);
            }
            _lastAppliedCamera = null;
        }

        public void Clear()
        {
            if (!_isActive) return;

            RemoveCommandBufferFromCamera();
            _commandBuffer?.Clear();

            foreach (var mat in _tempMaterialCache.Values)
            {
                if (mat != null) UnityEngine.Object.DestroyImmediate(mat);
            }
            _tempMaterialCache.Clear();

            _isActive = false;
        }

        public void Dispose()
        {
            Clear();
            _commandBuffer?.Dispose();
            if (_heatmapOverlayMaterial != null)
            {
                UnityEngine.Object.DestroyImmediate(_heatmapOverlayMaterial);
            }
        }
    }

    internal class DrawingModule : IDisposable
    {
        private readonly ProfilerDirectorSettings _settings;
        private readonly StringBuilder _stringBuilder = new StringBuilder(512);
        private readonly Vector2 _overlayLabelSize = new Vector2(200, 68);

        public DrawingModule(ProfilerDirectorSettings settings) => _settings = settings;

        public void Dispose() { }

        public Vector2 GetOverlayLabelSize() => _overlayLabelSize;

        public void DrawGlobalInfoPanel(GlobalMetrics metrics)
        {
            ProfilerDirectorResources.BaseStyle.fontSize = _settings.GlobalFontSize;
            _stringBuilder.Clear();
            _stringBuilder.Append($"<b>FPS: </b>{metrics.Fps:F0} | ");
            _stringBuilder.Append($"<b>CPU RT: </b>{metrics.CpuRenderThreadMs:F4} ms\n");
            _stringBuilder.Append($"<b>DrawCalls: </b>{metrics.DrawCalls} | ");
            _stringBuilder.Append($"<b>SetPass: </b>{metrics.SetPassCalls}");

            Rect panelRect = new Rect(15, 15, 280, 45);
            DrawGlassPanel(panelRect, new Color(0.1f, 0.1f, 0.1f, _settings.DefaultOpacity), _settings.GlassBorderColor);
            GUI.Label(new Rect(panelRect.x + 8, panelRect.y + 5, panelRect.width - 16, panelRect.height - 10), _stringBuilder.ToString(), ProfilerDirectorResources.BaseStyle);
        }

        public void DrawModeToggle(ref ProfilerDirector.DisplayMode currentMode)
        {
            Rect area = new Rect(Screen.width - 220, 15, 190, 20);
            GUILayout.BeginArea(area);
            GUILayout.BeginHorizontal();

            bool inspectActive = currentMode == ProfilerDirector.DisplayMode.Inspect;
            if (GUILayout.Toggle(inspectActive, "Inspect", ProfilerDirectorResources.ToggleStyle) && !inspectActive)
            {
                currentMode = ProfilerDirector.DisplayMode.Inspect;
            }

            bool overlayActive = currentMode == ProfilerDirector.DisplayMode.Overlay;
            if (GUILayout.Toggle(overlayActive, "Overlay", ProfilerDirectorResources.ToggleStyle) && !overlayActive)
            {
                currentMode = ProfilerDirector.DisplayMode.Overlay;
            }

            bool heatmapActive = currentMode == ProfilerDirector.DisplayMode.Heatmap;
            if (GUILayout.Toggle(heatmapActive, "Heatmap", ProfilerDirectorResources.ToggleStyle) && !heatmapActive)
            {
                currentMode = ProfilerDirector.DisplayMode.Heatmap;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        public void DrawOverlayForRenderers(IEnumerable<RendererProfile> profiles, LabelLayoutManager layoutManager)
        {
            foreach (var profile in profiles)
            {
                bool isInspected = Selection.activeGameObject == profile.SourceRenderer.gameObject;

                Vector3 worldPos = profile.SourceRenderer.bounds.center;
                Vector2 screenPos = HandleUtility.WorldToGUIPoint(worldPos);
                Vector2 offset = layoutManager.GetOffset(profile.SourceRenderer.GetInstanceID());
                Rect labelRect = new Rect(screenPos + offset - _overlayLabelSize * 0.5f, _overlayLabelSize);

                Color panelColor = isInspected ? _settings.InspectorHighlightColor : profile.PerformanceColor;
                float opacity = isInspected ? _settings.DefaultOpacity : _settings.UnselectedOpacity;

                DrawGlassPanel(labelRect, new Color(panelColor.r, panelColor.g, panelColor.b, opacity), new Color(_settings.GlassBorderColor.r, _settings.GlassBorderColor.g, _settings.GlassBorderColor.b, opacity));
                DrawConnectorLine(worldPos, GetClosestPointOnRect(labelRect, screenPos), isInspected);

                if (isInspected)
                {
                    Handles.color = _settings.InspectorHighlightColor;
                    Handles.DrawWireCube(profile.SourceRenderer.bounds.center, profile.SourceRenderer.bounds.size);
                }

                DrawOverlayLabelContent(labelRect, profile);
            }
        }

        private void DrawOverlayLabelContent(Rect area, RendererProfile profile)
        {
            ProfilerDirectorResources.BaseStyle.fontSize = _settings.OverlayFontSize;
            ProfilerDirectorResources.ShaderNameStyle.fontSize = _settings.OverlayFontSize;

            GUI.BeginGroup(area);
            Rect localArea = new Rect(Vector2.zero, area.size);

            GUI.DrawTexture(new Rect(8, 8, 16, 16), ProfilerDirectorResources.GaugeIcon, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(28, 9, 100, 16), $"<b>Score: {profile.Score:F1}</b>", ProfilerDirectorResources.BaseStyle);
            GUI.Label(new Rect(localArea.width - 118, 9, 110, 16), $"<color=#CCCCCC>{ShortenString(profile.ShaderName, 15)}</color>", ProfilerDirectorResources.ShaderNameStyle);

            float statY = 38, h = 20, iconSize = 14, iconPad = 8, textPad = 26;

            GUI.DrawTexture(new Rect(iconPad, statY, iconSize, iconSize), ProfilerDirectorResources.TriIcon, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(textPad, statY, 100, h), $"Tris: {profile.TriangleCount / 1000f:F1}k", ProfilerDirectorResources.BaseStyle);

            GUI.DrawTexture(new Rect(localArea.width / 2f + iconPad, statY, iconSize, iconSize), ProfilerDirectorResources.PassIcon, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(localArea.width / 2f + textPad, statY, 100, h), $"Passes: {profile.PassCount}", ProfilerDirectorResources.BaseStyle);

            GUI.EndGroup();
        }

        public void DrawInspectPanelForSelection(DataModule dataModule)
        {
            if (!dataModule.TryGetProfileForSelection(out var profile))
            {
                DrawInfoMessage("Select a GameObject with a Renderer component to inspect.");
                return;
            }

            Rect panelRect = new Rect(Screen.width - 340, 45, 320, 260);
            DrawGlassPanel(panelRect, new Color(0.1f, 0.1f, 0.1f, _settings.DefaultOpacity), _settings.InspectorHighlightColor);
            Handles.color = _settings.InspectorHighlightColor;
            Handles.DrawWireCube(profile.SourceRenderer.bounds.center, profile.SourceRenderer.bounds.size);

            GUILayout.BeginArea(panelRect);
            DrawInspectPanelContent(profile, dataModule.GpuTime);
            GUILayout.EndArea();
        }

        private void DrawInfoMessage(string message)
        {
            Rect panelRect = new Rect(Screen.width - 340, 45, 320, 60);
            DrawGlassPanel(panelRect, new Color(0.1f, 0.1f, 0.1f, _settings.DefaultOpacity), _settings.GlassBorderColor);
            Rect paddedRect = panelRect;
            paddedRect.x += 10; paddedRect.y += 10; paddedRect.width -= 20; paddedRect.height -= 20;
            GUI.Label(paddedRect, message, ProfilerDirectorResources.HeaderStyle);
        }

        private void DrawInspectPanelContent(RendererProfile profile, GpuTimeModule gpuTimeModule)
        {
            ProfilerDirectorResources.BaseStyle.fontSize = _settings.InspectFontSize;
            ProfilerDirectorResources.TitleStyle.fontSize = _settings.InspectFontSize;
            ProfilerDirectorResources.ValueStyle.fontSize = _settings.InspectFontSize;
            ProfilerDirectorResources.HeaderStyle.fontSize = 14;

            GUILayout.Space(10);
            GUILayout.Label($"<b>{profile.ObjectName}</b>", ProfilerDirectorResources.HeaderStyle);
            GUILayout.Space(5);

            DrawDetailRow("Mesh Name", profile.MeshName);
            DrawDetailRow("Shader", profile.ShaderName);

            GUILayout.Space(8);

            string gpuTimeString;
            if (gpuTimeModule.IsSupported)
            {
                gpuTimeString = gpuTimeModule.TryGetAverageGpuTime(profile.SourceRenderer.GetInstanceID(), out float time)
                    ? $"{time:F4} ms"
                    : "<color=grey>Measuring...</color>";
            }
            else
            {
                gpuTimeString = "<color=yellow>Not Supported</color>";
            }

            DrawPerformanceRow("GPU Time (Avg)", gpuTimeString);
            GUILayout.Space(8);
            DrawMetricGrid(profile);
        }

        private void DrawMetricGrid(RendererProfile profile)
        {
            GUILayout.BeginHorizontal();
            DrawMetricCell(ProfilerDirectorResources.TriIcon, "Tris", $"{profile.TriangleCount:N0}");
            DrawMetricCell(ProfilerDirectorResources.VertIcon, "Verts", $"{profile.VertexCount:N0}");
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            DrawMetricCell(ProfilerDirectorResources.PassIcon, "Passes", $"{profile.PassCount}");
            DrawMetricCell(ProfilerDirectorResources.MatIcon, "Materials", $"{profile.MaterialCount}");
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            DrawMetricCell(null, "Sub-Meshes", $"{profile.SubMeshCount}");
            DrawMetricCell(null, "Screen Size", $"{profile.ScreenSpacePercentage:F2}%");
            GUILayout.EndHorizontal();
        }

        private void DrawDetailRow(string title, string value)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(18));
            GUILayout.Label($" {title}:", ProfilerDirectorResources.TitleStyle, GUILayout.Width(80));
            GUILayout.Label(value, ProfilerDirectorResources.ValueStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(15);
            GUILayout.EndHorizontal();
        }

        private void DrawPerformanceRow(string title, string value)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(18));
            GUILayout.Label($" {title}:", ProfilerDirectorResources.TitleStyle, GUILayout.Width(120));
            GUILayout.Label($"<b>{value}</b>", ProfilerDirectorResources.ValueStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(15);
            GUILayout.EndHorizontal();
        }

        private void DrawMetricCell(Texture2D icon, string title, string value)
        {
            GUILayout.BeginVertical("box", GUILayout.Height(40));
            GUILayout.Label(title, ProfilerDirectorResources.TitleStyle);
            GUILayout.BeginHorizontal();
            if (icon != null) GUILayout.Box(icon, GUIStyle.none, GUILayout.Width(14), GUILayout.Height(14));
            GUILayout.FlexibleSpace();
            GUILayout.Label($"<b>{value}</b>", ProfilerDirectorResources.BaseStyle);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private string ShortenString(string name, int maxLength)
        {
            return (name.Length <= maxLength) ? name : name.Substring(0, maxLength - 1) + "...";
        }

        private void DrawGlassPanel(Rect rect, Color bgColor, Color borderColor)
        {
            var vertices = GetRoundedRectVerts(rect, _settings.CornerRadius);
            Handles.DrawSolidRectangleWithOutline(vertices, bgColor, Color.clear);
            Handles.color = borderColor;
            Handles.DrawAAPolyLine(_settings.BorderWidth, vertices);
        }

        private Vector3[] GetRoundedRectVerts(Rect rect, float radius)
        {
            const int segments = 15;
            radius = Mathf.Min(radius, rect.width / 2, rect.height / 2);
            var verts = new List<Vector3>();
            if (radius < 1)
            {
                verts.AddRange(new Vector3[] { rect.min, new Vector2(rect.xMax, rect.yMin), rect.max, new Vector2(rect.xMin, rect.yMax), rect.min });
            }
            else
            {
                Action<Vector2, float> addQuarter = (center, angleStart) =>
                {
                    for (int i = 0; i <= segments; i++)
                        verts.Add(center + new Vector2(Mathf.Cos(angleStart + i * Mathf.PI / 2 / segments), Mathf.Sin(angleStart + i * Mathf.PI / 2 / segments)) * radius);
                };
                addQuarter(new Vector2(rect.xMin + radius, rect.yMin + radius), Mathf.PI);
                addQuarter(new Vector2(rect.xMax - radius, rect.yMin + radius), -Mathf.PI / 2);
                addQuarter(new Vector2(rect.xMax - radius, rect.yMax - radius), 0);
                addQuarter(new Vector2(rect.xMin + radius, rect.yMax - radius), Mathf.PI / 2);
                verts.Add(verts[0]);
            }
            return verts.ToArray();
        }

        private void DrawConnectorLine(Vector3 worldObjectCenter, Vector2 guiLabelEdgePoint, bool isInspected)
        {
            Vector3 worldLabelEdge = HandleUtility.GUIPointToWorldRay(guiLabelEdgePoint).GetPoint(10);
            Handles.color = isInspected ? _settings.InspectorHighlightColor : _settings.GlassBorderColor;
            Handles.DrawAAPolyLine(_settings.BorderWidth, worldObjectCenter, worldLabelEdge);
        }

        private Vector2 GetClosestPointOnRect(Rect rect, Vector2 point)
        {
            return new Vector2(Mathf.Clamp(point.x, rect.xMin, rect.xMax), Mathf.Clamp(point.y, rect.yMin, rect.yMax));
        }
    }

    #endregion

    #region Helper Classes

    internal class LabelLayoutManager
    {
        private class LabelInfo { public int InstanceId; public Rect ScreenRect; }
        private readonly List<LabelInfo> _visibleLabels = new List<LabelInfo>();
        private readonly Dictionary<int, Vector2> _offsetCache = new Dictionary<int, Vector2>();

        public void BeginFrame() => _visibleLabels.Clear();

        public void RegisterLabel(int instanceId, Vector2 screenPos, Vector2 size)
        {
            var rect = new Rect(screenPos - size * 0.5f, size);
            _visibleLabels.Add(new LabelInfo { InstanceId = instanceId, ScreenRect = rect });
        }

        public void CalculateLayout(int iterations, float repulsionForce, float tetherStrength)
        {
            if (_visibleLabels.Count == 0)
            {
                _offsetCache.Clear();
                return;
            }

            var currentIds = new HashSet<int>(_visibleLabels.Select(l => l.InstanceId));
            var oldIds = _offsetCache.Keys.ToList();
            foreach (var id in oldIds.Where(id => !currentIds.Contains(id)))
            {
                _offsetCache.Remove(id);
            }

            foreach (var label in _visibleLabels)
            {
                if (!_offsetCache.ContainsKey(label.InstanceId))
                {
                    _offsetCache[label.InstanceId] = Vector2.zero;
                }
            }

            for (int i = 0; i < iterations; i++)
            {
                foreach (var labelA in _visibleLabels)
                {
                    foreach (var labelB in _visibleLabels)
                    {
                        if (labelA == labelB) continue;

                        Rect rectA = labelA.ScreenRect; rectA.position += _offsetCache[labelA.InstanceId];
                        Rect rectB = labelB.ScreenRect; rectB.position += _offsetCache[labelB.InstanceId];

                        if (rectA.Overlaps(rectB))
                        {
                            Vector2 direction = rectA.center - rectB.center;
                            Vector2 move = (direction.sqrMagnitude < 0.001f ? Vector2.up : direction.normalized) * repulsionForce;
                            _offsetCache[labelA.InstanceId] += move;
                            _offsetCache[labelB.InstanceId] -= move;
                        }
                    }
                }

                var keys = new List<int>(_offsetCache.Keys);
                foreach (var key in keys)
                {
                    _offsetCache[key] *= (1.0f - tetherStrength);
                }
            }
        }

        public Vector2 GetOffset(int instanceId)
        {
            return _offsetCache.TryGetValue(instanceId, out var offset) ? offset : Vector2.zero;
        }
    }

    internal class RollingAverage
    {
        private readonly float[] _samples;
        private int _currentIndex;
        private float _total;
        private int _filledSamples;
        public float Average { get; private set; }

        public RollingAverage(int sampleCount) => _samples = new float[Mathf.Max(1, sampleCount)];

        public void AddSample(float value)
        {
            _total -= _samples[_currentIndex];
            _samples[_currentIndex] = value;
            _total += value;
            _currentIndex = (_currentIndex + 1) % _samples.Length;
            if (_filledSamples < _samples.Length) _filledSamples++;
            Average = _filledSamples > 0 ? _total / _filledSamples : 0;
        }
    }

    #endregion
}