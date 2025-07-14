using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace MightyTracking
{
    [CustomEditor(typeof(MightyTracker))]
    [CanEditMultipleObjects]
    public class MightyTrackerEditor : Editor
    {
        private SerializedProperty updateIntervalProp;
        private SerializedProperty trackingColorProp;
        private SerializedProperty captureScreensProp;
        private SerializedProperty captureCameraProp;
        private SerializedProperty mainCameraProp;

        // New render texture and compression properties
        private SerializedProperty renderFormatProp;
        private SerializedProperty depthBufferProp;
        private SerializedProperty compressionFormatProp;
        private SerializedProperty jpgQualityProp;
        private SerializedProperty pngCompressionProp;
        private SerializedProperty qualityPresetProp;

        // Custom resolution properties
        private SerializedProperty forceCustomResolutionProp;
        private SerializedProperty customWidthProp;
        private SerializedProperty customHeightProp;

        private Color headerColor = Color.white;
        private Color backgroundColor = new Color(0f, 0.102f, 0.247f, 1f); // #001a3f

        private void OnEnable()
        {
            updateIntervalProp = serializedObject.FindProperty("updateInterval");
            trackingColorProp = serializedObject.FindProperty("trackingColor");
            captureScreensProp = serializedObject.FindProperty("captureScreens");
            mainCameraProp = serializedObject.FindProperty("mainCamera");
            captureCameraProp = serializedObject.FindProperty("captureCamera");

            // Initialize new properties
            renderFormatProp = serializedObject.FindProperty("renderFormat");
            depthBufferProp = serializedObject.FindProperty("depthBuffer");
            compressionFormatProp = serializedObject.FindProperty("compressionFormat");
            jpgQualityProp = serializedObject.FindProperty("jpgQuality");
            pngCompressionProp = serializedObject.FindProperty("pngCompression");
            qualityPresetProp = serializedObject.FindProperty("qualityPreset");

            // Initialize custom resolution properties
            forceCustomResolutionProp = serializedObject.FindProperty("forceCustomResolution");
            customWidthProp = serializedObject.FindProperty("customWidth");
            customHeightProp = serializedObject.FindProperty("customHeight");
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
            EditorGUI.LabelField(new Rect(10, headerRect.y + 5, headerRect.width - 20, 20), "Mighty Tracker Settings", headerStyle);

            // Basic Settings
            updateIntervalProp.floatValue = EditorGUILayout.Slider(
                new GUIContent("Update Interval (seconds)", "Time between data samples in seconds."),
                updateIntervalProp.floatValue,
                0.1f,
                10f
            );
            EditorGUILayout.PropertyField(
                trackingColorProp,
                new GUIContent("Tracking Color", "Color used for tracking visualization.")
            );

            EditorGUILayout.Space(10);

            // Camera Capture Section Header
            Rect captureHeaderRect = EditorGUILayout.GetControlRect(false, 30);
            captureHeaderRect.x = 0;
            captureHeaderRect.width = EditorGUIUtility.currentViewWidth;
            EditorGUI.DrawRect(captureHeaderRect, backgroundColor);
            EditorGUI.LabelField(new Rect(10, captureHeaderRect.y + 5, captureHeaderRect.width - 20, 20), "Camera Capture & Quality Settings", headerStyle);

            EditorGUILayout.Space(5);

            // Custom toggle button style
            var captureToggleStyle = new GUIStyle(EditorStyles.miniButton);
            captureToggleStyle.fixedHeight = 30;
            captureToggleStyle.fontStyle = FontStyle.Bold;
            captureToggleStyle.normal.textColor = captureScreensProp.boolValue ? Color.green : Color.gray;
            captureToggleStyle.hover.textColor = captureScreensProp.boolValue ? Color.green * 1.2f : Color.gray * 1.2f;

            // Toggle button with icon
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(
                new GUIContent(
                    captureScreensProp.boolValue ? "● Camera Capture Enabled" : "○ Camera Capture Disabled",
                    "Toggle screen capture functionality"
                ),
                captureToggleStyle,
                GUILayout.Width(200)))
            {
                captureScreensProp.boolValue = !captureScreensProp.boolValue;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (captureScreensProp.boolValue)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Camera Settings
                EditorGUILayout.PropertyField(
                    mainCameraProp,
                    new GUIContent("Is Main Camera", "Will capture all overlays and post effects if enabled, otherwise will only capture the camera raw settings.")
                );

                EditorGUILayout.PropertyField(
                    captureCameraProp,
                    new GUIContent("Capture Camera", "Camera to use for capture")
                );

                EditorGUILayout.Space(10);

                // Quality Preset Section
                EditorGUILayout.PropertyField(qualityPresetProp, new GUIContent("Quality Preset", "Choose a quality preset"));

                if (GUILayout.Button("Apply", GUILayout.Height(25)))
                {
                    var t = (MightyTracker)target;
                    t.ApplyQualityPreset((MightyTracker.QualityPreset)qualityPresetProp.enumValueIndex);
                    serializedObject.Update();
                }

                // Show preset description
                string presetDescription = GetQualityPresetDescription((MightyTracker.QualityPreset)qualityPresetProp.enumValueIndex);
                if (!string.IsNullOrEmpty(presetDescription))
                {
                    EditorGUILayout.HelpBox(presetDescription, MessageType.Info);
                }

                // Auto-detect button
                if (GUILayout.Button("Auto-Detect Pipeline Settings", GUILayout.Height(25)))
                {
                    var t = (MightyTracker)target;
                    t.AutoDetectRenderTextureSettings();
                    serializedObject.Update();
                }

                EditorGUILayout.Space(10);

                // Render Format & Depth (compact layout)
                EditorGUILayout.LabelField("Render Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(renderFormatProp, new GUIContent("Format", "Render texture format"));
                EditorGUILayout.PropertyField(depthBufferProp, new GUIContent("Depth Buffer", "Depth buffer precision: 0=None, 16=Mobile, 24=Standard, 32=High"));

                EditorGUILayout.Space(5);

                // Custom Resolution (compact layout)
                EditorGUILayout.PropertyField(forceCustomResolutionProp, new GUIContent("Custom Resolution", "Override with custom dimensions"));

                if (forceCustomResolutionProp.boolValue)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Size", GUILayout.Width(35));
                    customWidthProp.intValue = EditorGUILayout.IntField(customWidthProp.intValue, GUILayout.Width(60));
                    EditorGUILayout.LabelField("×", GUILayout.Width(15));
                    customHeightProp.intValue = EditorGUILayout.IntField(customHeightProp.intValue, GUILayout.Width(60));

                    // Show aspect ratio
                    if (customWidthProp.intValue > 0 && customHeightProp.intValue > 0)
                    {
                        float aspectRatio = (float)customWidthProp.intValue / customHeightProp.intValue;
                        EditorGUILayout.LabelField($"({aspectRatio:F2}:1)", EditorStyles.miniLabel, GUILayout.Width(50));
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    // Resolution presets dropdown
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Presets", GUILayout.Width(50));

                    string[] presetOptions = new string[]
                    {
                        "Select...",
                        "320×240 (Ultra Low)",
                        "480×270 (Mobile)",
                        "640×360 (Low)",
                        "640×480 (VGA)",
                        "960×540 (qHD)",
                        "1280×720 (HD)",
                        "1920×1080 (Full HD)",
                        "2560×1440 (QHD)",
                        "3840×2160 (4K)"
                    };

                    int selectedPreset = EditorGUILayout.Popup(0, presetOptions);

                    if (selectedPreset > 0)
                    {
                        switch (selectedPreset)
                        {
                            case 1: customWidthProp.intValue = 320; customHeightProp.intValue = 240; break;
                            case 2: customWidthProp.intValue = 480; customHeightProp.intValue = 270; break;
                            case 3: customWidthProp.intValue = 640; customHeightProp.intValue = 360; break;
                            case 4: customWidthProp.intValue = 640; customHeightProp.intValue = 480; break;
                            case 5: customWidthProp.intValue = 960; customHeightProp.intValue = 540; break;
                            case 6: customWidthProp.intValue = 1280; customHeightProp.intValue = 720; break;
                            case 7: customWidthProp.intValue = 1920; customHeightProp.intValue = 1080; break;
                            case 8: customWidthProp.intValue = 2560; customHeightProp.intValue = 1440; break;
                            case 9: customWidthProp.intValue = 3840; customHeightProp.intValue = 2160; break;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(5);

                // Compression Settings
                EditorGUILayout.LabelField("Compression", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(compressionFormatProp, new GUIContent("Format", "Image compression format"));

                if (compressionFormatProp.enumValueIndex == 1) // JPG selected
                {
                    EditorGUILayout.PropertyField(jpgQualityProp, new GUIContent("Quality", "JPG quality (1-100)"));
                }
                else // PNG selected
                {
                    EditorGUILayout.PropertyField(pngCompressionProp, new GUIContent("Compress PNG", "Enable PNG compression"));
                }

                EditorGUILayout.Space(10);

                // Storage Cost Estimation
                EditorGUILayout.LabelField("Storage Estimation", EditorStyles.boldLabel);

                var tracker = (MightyTracker)target;
                string storageCostText = tracker.GetStorageCostDisplayString();

                // Create a styled box for the storage cost
                var storageCostStyle = new GUIStyle(EditorStyles.helpBox);
                storageCostStyle.fontSize = 12;
                storageCostStyle.fontStyle = FontStyle.Bold;
                storageCostStyle.normal.textColor = tracker.captureScreens ?
                    (tracker.CalculateEstimatedStorageCostPerMinute() > 100f ? Color.red :
                     tracker.CalculateEstimatedStorageCostPerMinute() > 10f ? Color.yellow : Color.green) :
                    Color.gray;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("📊", GUILayout.Width(20));
                EditorGUILayout.LabelField(storageCostText, storageCostStyle);
                EditorGUILayout.EndHorizontal();

                // Additional info for context
                if (tracker.captureScreens && tracker.captureCamera != null)
                {
                    float capturesPerMinute = Mathf.Floor(60f / tracker.updateInterval);
                    Vector2 gameViewResolution = tracker.GetGameViewResolution();
                    string gameViewInfo = tracker.GetGameViewResolutionInfo();

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(25);
                    EditorGUILayout.LabelField($"• {capturesPerMinute:F0} captures/min at {gameViewResolution.x}×{gameViewResolution.y}", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(25);
                    EditorGUILayout.LabelField($"• {gameViewInfo}", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();

                    // Show per-capture cost for developer calculations
                    float mbPerCapture = tracker.CalculateEstimatedStorageCostPerCapture();
                    string perCaptureFormatted;
                    if (mbPerCapture < 0.001f)
                        perCaptureFormatted = $"{(mbPerCapture * 1024f * 1024f):F0} bytes";
                    else if (mbPerCapture < 0.1f)
                        perCaptureFormatted = $"{(mbPerCapture * 1024f):F1} KB";
                    else
                        perCaptureFormatted = $"{mbPerCapture:F2} MB";

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(25);
                    EditorGUILayout.LabelField($"• Per capture: {perCaptureFormatted}", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                }

                // Pipeline info and resolution explanation (compact)
                EditorGUILayout.Space(5);
                var currentPipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
                string pipelineInfo = currentPipeline != null ? currentPipeline.GetType().Name : "Built-in Pipeline";
                string resolutionExplanation = tracker.GetResolutionExplanation();

                EditorGUILayout.HelpBox($"Pipeline: {pipelineInfo}\n{resolutionExplanation}", MessageType.Info);

                EditorGUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private string GetQualityPresetDescription(MightyTracker.QualityPreset qualityPreset)
        {
            switch (qualityPreset)
            {
                case MightyTracker.QualityPreset.UltraLow:
                    return "Ultra Low: 320×240, RGB24, JPG 50%, 16-bit depth. Minimal storage for basic tracking.";
                case MightyTracker.QualityPreset.Low:
                    return "Low: 640×480, RGB24, JPG 60%, 16-bit depth. Optimized for performance and storage.";
                case MightyTracker.QualityPreset.Medium:
                    return "Medium: 1280×720, ARGB32, PNG compressed, 24-bit depth. Balanced quality and performance.";
                case MightyTracker.QualityPreset.High:
                    return "High: 1920×1080, RGBA32, PNG uncompressed, 32-bit depth. Maximum quality for detailed analysis.";
                case MightyTracker.QualityPreset.HDRP_UltraLow:
                    return "HDRP Ultra Low: 640×360, HDR format, JPG 50%, 24-bit depth. Minimal HDRP for testing.";
                case MightyTracker.QualityPreset.HDRP_Low:
                    return "HDRP Low: 1280×720, HDR format, JPG 70%, 32-bit depth. Performance-optimized for HDRP pipeline.";
                case MightyTracker.QualityPreset.HDRP_Medium:
                    return "HDRP Medium: 1920×1080, HDR format, PNG compressed, 32-bit depth. Balanced HDRP quality.";
                case MightyTracker.QualityPreset.HDRP_High:
                    return "HDRP High: 3840×2160 (4K), HDR format, PNG uncompressed, 32-bit depth. Maximum HDRP quality.";
                case MightyTracker.QualityPreset.URP_UltraLow:
                    return "URP Ultra Low: 480×270, RGBAHalf, JPG 45%, 16-bit depth. Minimal mobile performance.";
                case MightyTracker.QualityPreset.URP_Low:
                    return "URP Low: 960×540, RGBAHalf, JPG 65%, 24-bit depth. Mobile-optimized for URP pipeline.";
                case MightyTracker.QualityPreset.URP_Medium:
                    return "URP Medium: 1280×720, RGBAHalf, JPG 80%, 24-bit depth. Balanced URP quality.";
                case MightyTracker.QualityPreset.URP_High:
                    return "URP High: 1920×1080, RGBAHalf, PNG compressed, 24-bit depth. High-quality URP capture.";
                case MightyTracker.QualityPreset.Custom:
                    return "Custom: Manual control over all settings. Use this to fine-tune specific requirements.";
                default:
                    return string.Empty;
            }
        }
    }
}