using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace BillTheDev.ProfilerDirector
{
    [InitializeOnLoad]
    internal static class ProfilerDirector
    {
        public enum DisplayMode { Inspect, Overlay, Heatmap }
        public static DisplayMode CurrentDisplayMode { get; set; } = DisplayMode.Inspect;

        private static bool _isEnabled;
        private static float _lastAnalysisTime;

        private static DataModule _dataModule;
        private static DrawingModule _drawingModule;
        private static HeatmapModule _heatmapModule;
        private static readonly LabelLayoutManager _labelManager = new LabelLayoutManager();
        private static ProfilerDirectorSettings _settings;

        static ProfilerDirector()
        {
            // Được gọi khi Unity Editor khởi động, có thể dùng để tự động bật nếu muốn
        }

        [MenuItem("Tools/Profiler Director/Enable", false, 100)]
        private static void Enable()
        {
            if (_isEnabled) return;
            _isEnabled = true;

            _settings = ProfilerDirectorSettings.Instance;
            _dataModule = new DataModule(_settings);
            _drawingModule = new DrawingModule(_settings);
            _heatmapModule = new HeatmapModule(_settings);

            _dataModule.Initialize();
            ProfilerDirectorResources.Initialize();

            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.update += OnEditorUpdate;

            _lastAnalysisTime = (float)EditorApplication.timeSinceStartup;
            SceneView.RepaintAll();
        }

        [MenuItem("Tools/Profiler Director/Disable", false, 101)]
        private static void Disable()
        {
            if (!_isEnabled) return;
            _isEnabled = false;

            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.update -= OnEditorUpdate;

            _heatmapModule?.Clear();

            _dataModule?.Dispose();
            _drawingModule?.Dispose();
            _heatmapModule?.Dispose();
            ProfilerDirectorResources.Dispose();

            _dataModule = null;
            _drawingModule = null;
            _heatmapModule = null;

            SceneView.RepaintAll();
        }

        [MenuItem("Tools/Profiler Director/Enable", true)]
        private static bool ValidateEnable() => !_isEnabled;

        [MenuItem("Tools/Profiler Director/Disable", true)]
        private static bool ValidateDisable() => _isEnabled;

        private static void OnEditorUpdate()
        {
            if (!_isEnabled) return;

            _dataModule.UpdateCoreMetrics();

            if (ShouldPerformSceneAnalysis())
            {
                _dataModule.UpdateSceneAnalysis(GetCurrentSceneViewCamera());
                _lastAnalysisTime = (float)EditorApplication.timeSinceStartup;
            }

            SceneView.RepaintAll();
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!_isEnabled) return;

            DisplayMode previousMode = CurrentDisplayMode;

            Handles.BeginGUI();
            try
            {
                _drawingModule.DrawGlobalInfoPanel(_dataModule.GlobalMetrics);

                var displayMode = CurrentDisplayMode;
                _drawingModule.DrawModeToggle(ref displayMode);
                CurrentDisplayMode = displayMode;

                // Khi chuyển từ Heatmap sang mode khác, dọn dẹp nó đi
                if (previousMode == DisplayMode.Heatmap && CurrentDisplayMode != DisplayMode.Heatmap)
                {
                    _heatmapModule.Clear();
                }

                var visibleProfiles = GetFilteredProfiles();
                var camera = GetCurrentSceneViewCamera();

                switch (CurrentDisplayMode)
                {
                    case DisplayMode.Inspect:
                        _drawingModule.DrawInspectPanelForSelection(_dataModule);
                        break;
                    case DisplayMode.Overlay:
                        UpdateLabelLayout(visibleProfiles);
                        _drawingModule.DrawOverlayForRenderers(visibleProfiles, _labelManager);
                        break;
                    case DisplayMode.Heatmap:
                        // Truyền camera vào để module có thể gắn CommandBuffer
                        _heatmapModule.Apply(visibleProfiles, camera);
                        break;
                }
            }
            finally
            {
                Handles.EndGUI();
            }
        }

        private static bool ShouldPerformSceneAnalysis()
        {
            // Luôn phân tích nếu không ở chế độ Inspect, và theo chu kỳ
            return CurrentDisplayMode != DisplayMode.Inspect &&
                   EditorApplication.timeSinceStartup - _lastAnalysisTime > _settings.AnalysisInterval;
        }

        private static Camera GetCurrentSceneViewCamera()
        {
            return SceneView.currentDrawingSceneView?.camera;
        }

        private static IEnumerable<RendererProfile> GetFilteredProfiles()
        {
            return _dataModule.ProfileCache.Values
                .Where(profile => profile.Score >= _settings.ScoreThreshold)
                .OrderByDescending(profile => profile.Score)
                .Take(_settings.MaxVisibleObjects);
        }

        private static void UpdateLabelLayout(IEnumerable<RendererProfile> profiles)
        {
            var labelSize = _drawingModule.GetOverlayLabelSize();
            _labelManager.BeginFrame();
            foreach (var profile in profiles)
            {
                Vector2 screenPos = HandleUtility.WorldToGUIPoint(profile.SourceRenderer.bounds.center);
                _labelManager.RegisterLabel(profile.SourceRenderer.GetInstanceID(), screenPos, labelSize);
            }

            _labelManager.CalculateLayout(_settings.LayoutIterations, _settings.RepulsionForce, _settings.TetherStrength);
        }
    }
}