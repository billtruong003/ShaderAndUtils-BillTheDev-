using UnityEngine;
using UnityEditor;

namespace MightyFPSHeatmap
{
    [CustomEditor(typeof(MightyFPSHeatmapper))]
    [CanEditMultipleObjects]
    public class MightyFPSHeatmapperEditor : Editor
    {
        private SerializedProperty trackFPSProp;
        private SerializedProperty trackMemoryProp;
        private SerializedProperty updateIntervalProp;
        private SerializedProperty fpsThresholdProp;
        private SerializedProperty memorySpikeThresholdProp;
        private SerializedProperty polyThresholdProp;

        private SerializedProperty useLODFilterProp;
        private SerializedProperty lodMaskProp;
        private SerializedProperty logNonLODObjectsProp;

        private SerializedProperty useOcclusionCullingProp;
        private SerializedProperty occlusionLayerMaskProp;

        private SerializedProperty logTerrainTreesProp;
        private SerializedProperty trackBillboardsProp;

        private Color headerColor = Color.white;
        private Color backgroundColor = new Color(0f, 0.102f, 0.247f, 1f); // #001a3f

        private void OnEnable()
        {
            trackFPSProp = serializedObject.FindProperty("trackFPS");
            trackMemoryProp = serializedObject.FindProperty("trackMemory");
            updateIntervalProp = serializedObject.FindProperty("updateInterval");
            fpsThresholdProp = serializedObject.FindProperty("fpsThreshold");
            memorySpikeThresholdProp = serializedObject.FindProperty("memorySpikeThreshold");
            polyThresholdProp = serializedObject.FindProperty("polyThreshold");

            useLODFilterProp = serializedObject.FindProperty("useLODFilter");
            lodMaskProp = serializedObject.FindProperty("lodMask");
            logNonLODObjectsProp = serializedObject.FindProperty("logNonLODObjects");

            useOcclusionCullingProp = serializedObject.FindProperty("useOcclusionCulling");
            occlusionLayerMaskProp = serializedObject.FindProperty("occlusionLayerMask");

            logTerrainTreesProp = serializedObject.FindProperty("logTerrainTrees");
            trackBillboardsProp = serializedObject.FindProperty("trackBillboards");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Header style
            var headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.normal.textColor = headerColor;
            headerStyle.fontSize = 14;
            headerStyle.margin = new RectOffset(10, 10, 10, 10);

            // Draw main header with background
            Rect headerRect = EditorGUILayout.GetControlRect(false, 30);
            headerRect.x = 0;
            headerRect.width = EditorGUIUtility.currentViewWidth;
            EditorGUI.DrawRect(headerRect, backgroundColor);
            EditorGUI.LabelField(new Rect(10, headerRect.y + 5, headerRect.width - 20, 20), "FPS Heatmap Tracker Settings", headerStyle);

            EditorGUILayout.Space(5);

            // Custom toggle button style for tracking options
            var trackingToggleStyle = new GUIStyle(EditorStyles.miniButton);
            trackingToggleStyle.fixedHeight = 25;
            trackingToggleStyle.fontStyle = FontStyle.Bold;

            EditorGUILayout.LabelField("Performance Tracking", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            // FPS Toggle
            trackingToggleStyle.normal.textColor = trackFPSProp.boolValue ? Color.green : Color.gray;
            trackingToggleStyle.hover.textColor = trackFPSProp.boolValue ? Color.green * 1.2f : Color.gray * 1.2f;
            if (GUILayout.Button(
                new GUIContent(
                    trackFPSProp.boolValue ? "● FPS Tracking" : "○ FPS Tracking",
                    "Enable tracking of FPS performance data"
                ),
                trackingToggleStyle))
            {
                trackFPSProp.boolValue = !trackFPSProp.boolValue;
            }

            // Memory Toggle (disabled)
            GUI.enabled = false;
            trackingToggleStyle.normal.textColor = Color.gray;
            trackingToggleStyle.hover.textColor = Color.gray;
            GUILayout.Button(
                new GUIContent("○ Memory (Soon)", "Memory tracking coming soon!"),
                trackingToggleStyle
            );
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // General Settings Header
            Rect generalHeaderRect = EditorGUILayout.GetControlRect(false, 30);
            generalHeaderRect.x = 0;
            generalHeaderRect.width = EditorGUIUtility.currentViewWidth;
            EditorGUI.DrawRect(generalHeaderRect, backgroundColor);
            EditorGUI.LabelField(new Rect(10, generalHeaderRect.y + 5, generalHeaderRect.width - 20, 20), "General & Performance Settings", headerStyle);

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            updateIntervalProp.floatValue = EditorGUILayout.Slider(
                new GUIContent("Update Interval (seconds)", "Time between data samples in seconds."),
                updateIntervalProp.floatValue,
                0.1f,
                10f
            );

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Thresholds Header
            Rect thresholdHeaderRect = EditorGUILayout.GetControlRect(false, 30);
            thresholdHeaderRect.x = 0;
            thresholdHeaderRect.width = EditorGUIUtility.currentViewWidth;
            EditorGUI.DrawRect(thresholdHeaderRect, backgroundColor);
            EditorGUI.LabelField(new Rect(10, thresholdHeaderRect.y + 5, thresholdHeaderRect.width - 20, 20), "Performance Thresholds", headerStyle);

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            polyThresholdProp.intValue = EditorGUILayout.IntSlider(
                new GUIContent("Polygon Threshold", "Objects below this polycount will not be tracked."),
                polyThresholdProp.intValue,
                0,
                50000
            );

            fpsThresholdProp.floatValue = EditorGUILayout.Slider(
                new GUIContent("Avg FPS Drop Threshold", "If FPS suddenly drops this many frames, it will be recorded as a spike."),
                fpsThresholdProp.floatValue,
                1f,
                60f
            );

            // Memory threshold (disabled for now)
            GUI.enabled = false;
            float memorySpikeThresholdMB = memorySpikeThresholdProp.longValue / (1024f * 1024f);
            memorySpikeThresholdMB = EditorGUILayout.Slider(
                new GUIContent("Memory Spike Threshold (MB)", "Memory tracking coming soon!"),
                memorySpikeThresholdMB,
                1f,
                1000f
            );
            GUI.enabled = true;

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // LOD Settings Header
            Rect lodHeaderRect = EditorGUILayout.GetControlRect(false, 30);
            lodHeaderRect.x = 0;
            lodHeaderRect.width = EditorGUIUtility.currentViewWidth;
            EditorGUI.DrawRect(lodHeaderRect, backgroundColor);
            EditorGUI.LabelField(new Rect(10, lodHeaderRect.y + 5, lodHeaderRect.width - 20, 20), "Level of Detail (LOD) Settings", headerStyle);

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // LOD Filter toggle
            var lodToggleStyle = new GUIStyle(EditorStyles.miniButton);
            lodToggleStyle.fixedHeight = 25;
            lodToggleStyle.fontStyle = FontStyle.Bold;
            lodToggleStyle.normal.textColor = useLODFilterProp.boolValue ? Color.green : Color.gray;
            lodToggleStyle.hover.textColor = useLODFilterProp.boolValue ? Color.green * 1.2f : Color.gray * 1.2f;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(
                new GUIContent(
                    useLODFilterProp.boolValue ? "● LOD Filtering Enabled" : "○ LOD Filtering Disabled",
                    "Enable filtering of objects based on LOD levels"
                ),
                lodToggleStyle,
                GUILayout.Width(200)))
            {
                useLODFilterProp.boolValue = !useLODFilterProp.boolValue;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (useLODFilterProp.boolValue)
            {
                EditorGUILayout.Space(5);

                // Non-LOD objects toggle
                var nonLodToggleStyle = new GUIStyle(EditorStyles.miniButton);
                nonLodToggleStyle.fixedHeight = 20;
                nonLodToggleStyle.normal.textColor = logNonLODObjectsProp.boolValue ? Color.green : Color.gray;

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(
                    new GUIContent(
                        logNonLODObjectsProp.boolValue ? "● Include Non-LOD Objects" : "○ Include Non-LOD Objects",
                        "Include objects without an LODGroup in the tracking"
                    ),
                    nonLodToggleStyle))
                {
                    logNonLODObjectsProp.boolValue = !logNonLODObjectsProp.boolValue;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("LOD Levels to Track:", EditorStyles.boldLabel);

                // LOD level toggles in a nice grid
                EditorGUILayout.BeginHorizontal();
                for (int i = 0; i < 4; i++)
                {
                    bool isEnabled = (lodMaskProp.intValue & (1 << i)) != 0;
                    var lodLevelStyle = new GUIStyle(EditorStyles.miniButton);
                    lodLevelStyle.normal.textColor = isEnabled ? Color.green : Color.gray;

                    bool newValue = GUILayout.Toggle(
                        isEnabled,
                            new GUIContent($"LOD{i}", $"Toggle tracking for LOD level {i}"),
                            lodLevelStyle
                    );

                    if (newValue != isEnabled)
                    {
                        if (newValue)
                            lodMaskProp.intValue |= (1 << i);
                        else
                            lodMaskProp.intValue &= ~(1 << i);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Occlusion Settings Header
            Rect occlusionHeaderRect = EditorGUILayout.GetControlRect(false, 30);
            occlusionHeaderRect.x = 0;
            occlusionHeaderRect.width = EditorGUIUtility.currentViewWidth;
            EditorGUI.DrawRect(occlusionHeaderRect, backgroundColor);
            EditorGUI.LabelField(new Rect(10, occlusionHeaderRect.y + 5, occlusionHeaderRect.width - 20, 20), "Visibility & Occlusion Settings", headerStyle);

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Occlusion culling toggle
            var occlusionToggleStyle = new GUIStyle(EditorStyles.miniButton);
            occlusionToggleStyle.fixedHeight = 25;
            occlusionToggleStyle.fontStyle = FontStyle.Bold;
            occlusionToggleStyle.normal.textColor = useOcclusionCullingProp.boolValue ? Color.green : Color.gray;
            occlusionToggleStyle.hover.textColor = useOcclusionCullingProp.boolValue ? Color.green * 1.2f : Color.gray * 1.2f;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(
                new GUIContent(
                    useOcclusionCullingProp.boolValue ? "● Unity Occlusion Culling" : "○ Unity Occlusion Culling",
                    "Prefer Unity's built-in occlusion culling if available"
                ),
                occlusionToggleStyle,
                GUILayout.Width(200)))
            {
                useOcclusionCullingProp.boolValue = !useOcclusionCullingProp.boolValue;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(
                occlusionLayerMaskProp,
                new GUIContent("Occlusion Layer Mask", "Layers considered as occluders for custom visibility checks")
            );

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Terrain Settings Header
            Rect terrainHeaderRect = EditorGUILayout.GetControlRect(false, 30);
            terrainHeaderRect.x = 0;
            terrainHeaderRect.width = EditorGUIUtility.currentViewWidth;
            EditorGUI.DrawRect(terrainHeaderRect, backgroundColor);
            EditorGUI.LabelField(new Rect(10, terrainHeaderRect.y + 5, terrainHeaderRect.width - 20, 20), "Terrain & Vegetation Settings", headerStyle);

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Terrain trees toggle
            var terrainToggleStyle = new GUIStyle(EditorStyles.miniButton);
            terrainToggleStyle.fixedHeight = 25;
            terrainToggleStyle.normal.textColor = logTerrainTreesProp.boolValue ? Color.green : Color.gray;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(
                new GUIContent(
                    logTerrainTreesProp.boolValue ? "● Track Terrain Trees" : "○ Track Terrain Trees",
                    "Enable tracking of terrain tree instances"
                ),
                terrainToggleStyle))
            {
                logTerrainTreesProp.boolValue = !logTerrainTreesProp.boolValue;
            }

            // Billboards toggle
            var billboardToggleStyle = new GUIStyle(EditorStyles.miniButton);
            billboardToggleStyle.fixedHeight = 25;
            billboardToggleStyle.normal.textColor = trackBillboardsProp.boolValue ? Color.green : Color.gray;

            if (GUILayout.Button(
                new GUIContent(
                    trackBillboardsProp.boolValue ? "● Track Billboards" : "○ Track Billboards",
                    "Include billboarded trees in terrain tracking (may impact performance)"
                ),
                billboardToggleStyle))
            {
                trackBillboardsProp.boolValue = !trackBillboardsProp.boolValue;
            }
            EditorGUILayout.EndHorizontal();

            if (logTerrainTreesProp.boolValue)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("Terrain trees will be tracked based on camera distance and LOD settings. " +
                                      "Billboards are typically used for distant trees to improve performance.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Performance Impact Estimation
            Rect perfHeaderRect = EditorGUILayout.GetControlRect(false, 30);
            perfHeaderRect.x = 0;
            perfHeaderRect.width = EditorGUIUtility.currentViewWidth;
            EditorGUI.DrawRect(perfHeaderRect, backgroundColor);
            EditorGUI.LabelField(new Rect(10, perfHeaderRect.y + 5, perfHeaderRect.width - 20, 20), "Performance Impact Estimation", headerStyle);

            EditorGUILayout.Space(5);

            // Performance estimation display
            var perfEstimateStyle = new GUIStyle(EditorStyles.helpBox);
            perfEstimateStyle.fontSize = 11;
            perfEstimateStyle.fontStyle = FontStyle.Bold;

            string performanceEstimate = GetPerformanceEstimate();
            Color performanceColor = GetPerformanceColor();
            perfEstimateStyle.normal.textColor = performanceColor;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("⚡", GUILayout.Width(20));
            EditorGUILayout.LabelField(performanceEstimate, perfEstimateStyle);
            EditorGUILayout.EndHorizontal();

            // Additional performance info
            if (trackFPSProp.boolValue)
            {
                float samplesPerMinute = Mathf.Floor(60f / updateIntervalProp.floatValue);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(25);
                EditorGUILayout.LabelField($"• {samplesPerMinute:F0} samples/min at {updateIntervalProp.floatValue:F1}s intervals", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(25);
                EditorGUILayout.LabelField($"• Polygon threshold: {polyThresholdProp.intValue:N0} polys", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(25);
                EditorGUILayout.LabelField($"• FPS trigger: Below {fpsThresholdProp.floatValue:F0} FPS", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private string GetPerformanceEstimate()
        {
            if (!trackFPSProp.boolValue)
                return "FPS tracking disabled - No performance impact";

            float updateInterval = updateIntervalProp.floatValue;
            int polyThreshold = polyThresholdProp.intValue;
            bool terrain = logTerrainTreesProp.boolValue;
            bool lod = useLODFilterProp.boolValue;

            if (updateInterval >= 2.0f && polyThreshold >= 5000 && lod)
                return "Low Impact - Optimized settings for production";
            else if (updateInterval >= 1.0f && polyThreshold >= 1000)
                return "Medium Impact - Balanced performance and detail";
            else if (updateInterval >= 0.5f)
                return "High Impact - Detailed tracking, may affect performance";
            else
                return "Very High Impact - Maximum detail, significant performance cost";
        }

        private Color GetPerformanceColor()
        {
            if (!trackFPSProp.boolValue)
                return Color.gray;

            float updateInterval = updateIntervalProp.floatValue;
            int polyThreshold = polyThresholdProp.intValue;
            bool lod = useLODFilterProp.boolValue;

            if (updateInterval >= 2.0f && polyThreshold >= 5000 && lod)
                return Color.green;
            else if (updateInterval >= 1.0f && polyThreshold >= 1000)
                return Color.yellow;
            else if (updateInterval >= 0.5f)
                return new Color(1f, 0.5f, 0f); // Orange
            else
                return Color.red;
        }
    }
}