using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace BillTheDev.ProfilerDirector
{
    internal class ProfilerDirectorSettings : ScriptableObject
    {
        internal const string SettingsPath = "Assets/Shaders/ProfilerDirector/Editor/Resources/ProfilerDirectorSettings.asset";

        private static ProfilerDirectorSettings _instance;
        public static ProfilerDirectorSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<ProfilerDirectorSettings>(Path.GetFileNameWithoutExtension(SettingsPath));
                    if (_instance == null)
                    {
                        _instance = CreateInstance<ProfilerDirectorSettings>();
                        string resourcePath = Path.GetDirectoryName(SettingsPath);
                        if (!Directory.Exists(resourcePath))
                        {
                            Directory.CreateDirectory(resourcePath);
                        }
                        AssetDatabase.CreateAsset(_instance, SettingsPath);
                        AssetDatabase.SaveAssets();
                    }
                }
                return _instance;
            }
        }

        [Header("Activation & Throttling")]
        [Min(0.1f)] public float AnalysisInterval = 0.5f;

        [Header("GPU Measurement")]
        [Tooltip("Limits how many new GPU measurements can be started per second.")]
        [Range(1, 60)] public int GpuMeasurementsPerSecond = 30;
        [Tooltip("The number of frames to average GPU time over.")]
        [Range(5, 120)] public int GpuTimeAverageFrames = 60;

        [Header("Overlay & Heatmap: Filtering")]
        [Tooltip("Only show labels/heatmap for objects with a score above this value.")]
        public float ScoreThreshold = 10f;
        [Tooltip("The maximum number of objects to display in Overlay or Heatmap mode.")]
        [Range(1, 100)] public int MaxVisibleObjects = 20;

        [Header("Heuristic Scoring Weights")]
        [Range(0, 100)] public float PassCountWeight = 10.0f;
        [Range(0, 100)] public float VertexCountWeight = 1.0f;
        [Range(0, 100)] public float ScreenSizeWeight = 5.0f;
        [Range(0, 100)] public float TransparencyPenalty = 25.0f;

        [Header("Overlay Mode: Label Layout")]
        [Range(1, 20)] public int LayoutIterations = 12;
        [Range(0.1f, 10.0f)] public float RepulsionForce = 2.0f;
        [Range(0.01f, 1.0f)] public float TetherStrength = 0.1f;

        [Header("Heatmap Mode")]
        [Range(0.1f, 1.0f)] public float HeatmapOpacity = 0.6f;

        [Header("Glassmorphism Style")]
        [Range(0.0f, 1.0f)] public float DefaultOpacity = 0.9f;
        [Range(0.0f, 1.0f)] public float UnselectedOpacity = 0.7f;
        public Color InspectorHighlightColor = new Color(0.2f, 0.8f, 1f, 1f);
        public Color GoodColor = new Color(0.1f, 0.8f, 0.1f, 1f);
        public Color WarningColor = new Color(0.9f, 0.9f, 0.1f, 1f);
        public Color PoorColor = new Color(0.9f, 0.2f, 0.1f, 1f);
        public Color GlassBorderColor = new Color(1f, 1f, 1f, 0.3f);

        [Header("Font Sizes")]
        public int GlobalFontSize = 10;
        public int InspectFontSize = 11;
        public int OverlayFontSize = 9;

        [Header("Appearance")]
        public float CornerRadius = 8f;
        [Range(1f, 3f)] public float BorderWidth = 1.5f;

        [Header("Performance Thresholds (Score)")]
        public float GoodScoreThreshold = 20.0f;
        public float WarningScoreThreshold = 50.0f;
    }


    internal static class ProfilerDirectorSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new SettingsProvider("Project/Profiler Director", SettingsScope.Project)
            {
                label = "Profiler Director",
                guiHandler = (searchContext) =>
                {
                    var settings = ProfilerDirectorSettings.Instance;
                    var editor = Editor.CreateEditor(settings);
                    editor.OnInspectorGUI();
                },
                keywords = new HashSet<string>(new[] { "Profiler", "Director", "Performance", "GPU", "CPU", "Debug", "Heatmap" })
            };
            return provider;
        }
    }
}