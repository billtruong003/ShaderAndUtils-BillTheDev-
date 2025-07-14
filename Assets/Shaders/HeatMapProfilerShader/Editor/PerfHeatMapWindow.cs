using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace PerfHeatMap
{
    public class PerfHeatMapWindow : EditorWindow
    {
        private PerfHeatMapSceneSettings _sceneSettings;
        private PerfHeatMapVisualizer _visualizer;
        private PerfHeatMapData _currentData;

        private Vector2 _scrollPosition;
        private bool _showGlobalSettings, _showCameraSettings, _showLocalSettings, _showVisualization;

        private Vector4 _displayMinValues;
        private Vector4 _displayMaxValues;

        private readonly string[] _statLabels = { "DrawCalls", "Triangles", "GPU Time (ms)", "FrameTime (ms)" };

        [MenuItem("Window/Analysis/PerfHeatMap")]
        public static void ShowWindow()
        {
            GetWindow<PerfHeatMapWindow>("PerfHeatMap");
        }

        private void OnEnable()
        {
            FindOrCreateSceneSettings();
            FindOrCreateVisualizer();
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            if (_visualizer != null && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                DestroyImmediate(_visualizer.gameObject);
            }
        }

        private void OnFocus()
        {
            if (_sceneSettings == null)
            {
                FindOrCreateSceneSettings();
            }
            if (_visualizer == null)
            {
                FindOrCreateVisualizer();
            }
        }

        private void OnGUI()
        {
            if (_sceneSettings == null)
            {
                EditorGUILayout.HelpBox("No PerfHeatMap Scene Settings found. Please create one.", MessageType.Warning);
                if (GUILayout.Button("Create Scene Settings Object"))
                {
                    FindOrCreateSceneSettings();
                }
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawQuickSetup();
            DrawSettingsSections();
            DrawVisualizationControls();

            EditorGUILayout.EndScrollView();
        }

        private void DrawQuickSetup()
        {
            EditorGUILayout.LabelField("Quick Set-up", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("1. Adjust the green volume in the Scene View to cover the area you want to profile.\n2. Click 'Capture Stats'.", MessageType.Info);

            if (GUILayout.Button("Capture Stats"))
            {
                CaptureStatsAsync();
            }

            GUI.enabled = _currentData != null;
            if (GUILayout.Button("Rebuild Heatmap from Current Data"))
            {
                RebuildHeatmap();
            }
            GUI.enabled = true;

            EditorGUILayout.Space();
        }

        private void DrawSettingsSections()
        {
            _showGlobalSettings = EditorGUILayout.Foldout(_showGlobalSettings, "Global Settings", true, EditorStyles.foldoutHeader);
            if (_showGlobalSettings) DrawGlobalSettings();

            _showCameraSettings = EditorGUILayout.Foldout(_showCameraSettings, "Camera Settings", true, EditorStyles.foldoutHeader);
            if (_showCameraSettings) DrawCameraSettings();

            _showLocalSettings = EditorGUILayout.Foldout(_showLocalSettings, "Local Settings (Scene Specific)", true, EditorStyles.foldoutHeader);
            if (_showLocalSettings) DrawLocalSettings();
        }

        private void DrawGlobalSettings()
        {
            PerfHeatMapGlobalSettings.CaptureInPlayMode = EditorGUILayout.Toggle("Capture in Play Mode", PerfHeatMapGlobalSettings.CaptureInPlayMode);
            PerfHeatMapGlobalSettings.LockSceneView = EditorGUILayout.Toggle("Lock Scene View on Capture", PerfHeatMapGlobalSettings.LockSceneView);
        }

        private void DrawCameraSettings()
        {
            PerfHeatMapGlobalSettings.Use360Camera = EditorGUILayout.Toggle("Use 360° Camera", PerfHeatMapGlobalSettings.Use360Camera);
            GUI.enabled = !PerfHeatMapGlobalSettings.Use360Camera;
            PerfHeatMapGlobalSettings.HorizontalFOV = EditorGUILayout.FloatField("Horizontal FOV", PerfHeatMapGlobalSettings.HorizontalFOV);
            PerfHeatMapGlobalSettings.AspectRatio = EditorGUILayout.FloatField("Aspect Ratio", PerfHeatMapGlobalSettings.AspectRatio);
            GUI.enabled = true;

            EditorGUILayout.LabelField("Camera Resolution");
            EditorGUI.indentLevel++;
            PerfHeatMapGlobalSettings.CameraResolutionX = EditorGUILayout.IntField("Width", PerfHeatMapGlobalSettings.CameraResolutionX);
            PerfHeatMapGlobalSettings.CameraResolutionY = EditorGUILayout.IntField("Height", PerfHeatMapGlobalSettings.CameraResolutionY);
            EditorGUI.indentLevel--;
        }

        private void DrawLocalSettings()
        {
            EditorGUI.BeginChangeCheck();
            var newCellSize = EditorGUILayout.Vector3Field("Cell Size", _sceneSettings.CellSize);
            _sceneSettings.ExcludeCellsTooFarFromGround = EditorGUILayout.Toggle("Exclude Cells Far From Ground", _sceneSettings.ExcludeCellsTooFarFromGround);
            if (_sceneSettings.ExcludeCellsTooFarFromGround)
                _sceneSettings.MaxDistanceFromGround = EditorGUILayout.FloatField("Max Distance From Ground", _sceneSettings.MaxDistanceFromGround);

            _sceneSettings.ExcludeCellsInsideColliders = EditorGUILayout.Toggle("Exclude Cells Inside Colliders", _sceneSettings.ExcludeCellsInsideColliders);
            if (_sceneSettings.ExcludeCellsInsideColliders)
            {
                _sceneSettings.ExclusionLayers = EditorGUILayout.LayerField("Exclusion Layers", _sceneSettings.ExclusionLayers);
            }

            if (EditorGUI.EndChangeCheck())
            {
                _sceneSettings.CellSize = newCellSize;
                EditorUtility.SetDirty(_sceneSettings);
            }
        }

        private void DrawVisualizationControls()
        {
            EditorGUILayout.Space();
            _showVisualization = EditorGUILayout.Foldout(_showVisualization, "Visualization", true, EditorStyles.foldoutHeader);
            if (!_showVisualization || _currentData == null || _visualizer == null) return;

            var material = _visualizer.GetComponent<MeshRenderer>().sharedMaterial;
            if (material == null) return;

            EditorGUI.BeginChangeCheck();

            material.SetFloat("_Intensity", EditorGUILayout.Slider("Intensity", material.GetFloat("_Intensity"), 1f, 20f));
            material.SetFloat("_StepSize", EditorGUILayout.Slider("Detail (Step Size)", material.GetFloat("_StepSize"), 0.01f, 0.2f));

            for (int i = 0; i < 4; i++)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(_statLabels[i], EditorStyles.boldLabel);
                float min = _displayMinValues[i];
                float max = _displayMaxValues[i];

                var colorProp = $"_Color{i + 1}";
                var rangeProp = $"_Range{i + 1}";

                material.SetColor(colorProp, EditorGUILayout.ColorField("Color", material.GetColor(colorProp)));
                EditorGUILayout.MinMaxSlider($"Display Range ({min:F2} - {max:F2})", ref min, ref max, _visualizer.MinValues[i], _visualizer.MaxValues[i]);
                _displayMinValues[i] = min;
                _displayMaxValues[i] = max;
                material.SetVector(rangeProp, new Vector4(min, max, 0, 0));
            }

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }
        }

        private async void CaptureStatsAsync()
        {
            if (EditorApplication.isCompiling)
            {
                EditorUtility.DisplayDialog("PerfHeatMap", "Editor is compiling, please wait.", "OK");
                return;
            }

            if (PerfHeatMapGlobalSettings.LockSceneView)
            {
                Focus();
            }

            var capture = new PerfHeatMapCapture(_sceneSettings);
            var data = await capture.ExecuteAsync();

            if (data != null)
            {
                _currentData = data;
                string scenePath = EditorSceneManager.GetActiveScene().path;
                string sceneName = string.IsNullOrEmpty(scenePath) ? "UntitledScene" : Path.GetFileNameWithoutExtension(scenePath);
                string path = EditorUtility.SaveFilePanelInProject("Save Capture Data", $"{sceneName}_PerfHeatMap", "asset", "Please enter a file name to save the capture data.");
                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.CreateAsset(data, path);
                    AssetDatabase.SaveAssets();
                    _currentData = AssetDatabase.LoadAssetAtPath<PerfHeatMapData>(path);
                }

                RebuildHeatmap();
            }
        }

        private void RebuildHeatmap()
        {
            if (_currentData != null && _visualizer != null)
            {
                _visualizer.Display(_currentData);
                _displayMinValues = _visualizer.MinValues;
                _displayMaxValues = _visualizer.MaxValues;
            }
        }

        private void FindOrCreateSceneSettings()
        {
            _sceneSettings = FindObjectOfType<PerfHeatMapSceneSettings>();
            if (_sceneSettings == null)
            {
                var go = new GameObject("PerfHeatMap_Settings");
                _sceneSettings = go.AddComponent<PerfHeatMapSceneSettings>();
                EditorUtility.SetDirty(_sceneSettings);
                EditorSceneManager.MarkSceneDirty(go.scene);
            }
        }

        private void FindOrCreateVisualizer()
        {
            _visualizer = FindObjectOfType<PerfHeatMapVisualizer>();
            if (_visualizer == null)
            {
                var go = new GameObject("PerfHeatMap_Visualizer");
                _visualizer = go.AddComponent<PerfHeatMapVisualizer>();
                _visualizer.Initialize();
            }
            _visualizer.Clear();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (_sceneSettings != null)
            {
                EditorGUI.BeginChangeCheck();

                Bounds newBounds = DrawBoundsHandle(_sceneSettings.CaptureBounds);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_sceneSettings, "Change PerfHeatMap Capture Bounds");
                    _sceneSettings.CaptureBounds = newBounds;
                    EditorUtility.SetDirty(_sceneSettings);
                }
            }
        }

        private Bounds DrawBoundsHandle(Bounds bounds)
        {
            float handleSize = HandleUtility.GetHandleSize(bounds.center) * 0.1f;
            Vector3 newMin = bounds.min;
            Vector3 newMax = bounds.max;
            Vector3 center = bounds.center;

            // X Axis Handles
            Handles.color = Handles.xAxisColor;
            float maxHandleX = Handles.Slider(new Vector3(newMax.x, center.y, center.z), Vector3.right, handleSize, Handles.CubeHandleCap, 0f).x;
            float minHandleX = Handles.Slider(new Vector3(newMin.x, center.y, center.z), Vector3.right, handleSize, Handles.CubeHandleCap, 0f).x;

            // Y Axis Handles
            Handles.color = Handles.yAxisColor;
            float maxHandleY = Handles.Slider(new Vector3(center.x, newMax.y, center.z), Vector3.up, handleSize, Handles.CubeHandleCap, 0f).y;
            float minHandleY = Handles.Slider(new Vector3(center.x, newMin.y, center.z), Vector3.up, handleSize, Handles.CubeHandleCap, 0f).y;

            // Z Axis Handles
            Handles.color = Handles.zAxisColor;
            float maxHandleZ = Handles.Slider(new Vector3(center.x, center.y, newMax.z), Vector3.forward, handleSize, Handles.CubeHandleCap, 0f).z;
            float minHandleZ = Handles.Slider(new Vector3(center.x, center.y, newMin.z), Vector3.forward, handleSize, Handles.CubeHandleCap, 0f).z;

            // Center Position Handle
            Handles.color = Color.white;
            Vector3 newCenter = Handles.PositionHandle(center, Quaternion.identity);

            // Construct new bounds from handle results
            var resultingBounds = new Bounds();
            resultingBounds.SetMinMax(
                new Vector3(Mathf.Min(minHandleX, maxHandleX), Mathf.Min(minHandleY, maxHandleY), Mathf.Min(minHandleZ, maxHandleZ)),
                new Vector3(Mathf.Max(minHandleX, maxHandleX), Mathf.Max(minHandleY, maxHandleY), Mathf.Max(minHandleZ, maxHandleZ))
            );

            // Apply center movement delta
            Vector3 centerDelta = newCenter - center;
            resultingBounds.center += centerDelta;

            return resultingBounds;
        }
    }
}