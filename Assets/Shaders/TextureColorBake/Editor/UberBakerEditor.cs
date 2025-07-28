using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;

public sealed class UberBakerEditor : EditorWindow
{
    // Lớp BakeTask không thay đổi
    private class BakeTask
    {
        public bool IsEnabled;
        public readonly string Label;
        public readonly string Suffix;
        public readonly string ShaderKeyword;
        public readonly Action DrawSettingsGUI;
        public readonly Action<TextureImporter> ConfigureImporter;

        public BakeTask(string label, string suffix, string shaderKeyword, Action drawSettingsGUI, Action<TextureImporter> configureImporter)
        {
            IsEnabled = false;
            Label = label;
            Suffix = suffix;
            ShaderKeyword = shaderKeyword;
            DrawSettingsGUI = drawSettingsGUI;
            ConfigureImporter = configureImporter;
        }
    }

    private enum TextureResolution
    {
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096,
        _8192 = 8192
    }

    private const string OUTPUT_FOLDER_PREF_KEY = "UberBaker.OutputDirectory";
    private const string SHADER_PATH = "Hidden/UberBakerURP";

    // --- State cho Input/Output ---
    private Texture2D sourceTexture;
    private string baseFileName = "BakedTexture";
    private string outputDirectory = "";
    private TextureResolution resolution = TextureResolution._1024;

    private Material bakerMaterial;
    private List<BakeTask> bakeTasks;
    private Vector2 scrollPosition;

    // --- State cho Preview ---
    private RenderTexture previewTexture;
    private bool isPreviewActive = false;
    private int previewTaskIndex = 0;

    // --- Bake Settings (không đổi) ---
    private float normalStrength = 5.0f;
    private float curvatureRadius = 1.0f;
    private float curvatureStrength = 1.5f;
    private float aoRadius = 20.0f;
    private int aoSampleDirections = 16;
    private int aoSampleSteps = 8;
    private float aoStrength = 1.2f;
    private float metallicLow = 0.0f;
    private float metallicHigh = 1.0f;
    private float metallicContrast = 2.0f;

    [MenuItem("Tools/Uber Baker")]
    public static void ShowWindow()
    {
        GetWindow<UberBakerEditor>("Uber Baker");
    }

    private void OnEnable()
    {
        InitializeBakerMaterial();
        InitializeBakeTasks();
        LoadPreferences();
    }

    private void OnDisable()
    {
        SavePreferences();
        CleanUpPreviewTexture();
        DestroyImmediate(bakerMaterial);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Uber Baker", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        DrawInputConfigurationGUI();
        DrawBakeTasksAndSettingsGUI();
        DrawPreviewGUI(); // <-- Vùng giao diện mới
        DrawExecutionGUI();
    }

    private void InitializeBakerMaterial()
    {
        var bakerShader = Shader.Find(SHADER_PATH);
        if (bakerShader == null)
        {
            Debug.LogError($"Could not find the Uber Baker shader at path: '{SHADER_PATH}'. Please ensure it exists.");
            return;
        }
        bakerMaterial = new Material(bakerShader);
    }

    private void LoadPreferences()
    {
        outputDirectory = EditorPrefs.GetString(OUTPUT_FOLDER_PREF_KEY, "");
    }

    private void SavePreferences()
    {
        EditorPrefs.SetString(OUTPUT_FOLDER_PREF_KEY, outputDirectory);
    }

    private void InitializeBakeTasks()
    {
        bakeTasks = new List<BakeTask>
        {
            new BakeTask("Normal Map", "_Normal", "_BAKE_NORMAL",
                DrawNormalMapSettings, ConfigureImporterForNormalMap),

            new BakeTask("Curvature", "_Curvature", "_BAKE_CURVATURE",
                DrawCurvatureSettings, ConfigureImporterForLinearData),

            new BakeTask("AO & Bent Normal", "_AoBentNormal", "_BAKE_AO_BENT_NORMAL",
                DrawAoBentNormalSettings, ConfigureImporterForLinearData),

            new BakeTask("Metallic", "_Metallic", "_BAKE_METALLIC",
                DrawMetallicSettings, ConfigureImporterForLinearData),

            new BakeTask("Height (Passthrough)", "_Height", "_BAKE_HEIGHT",
                null, ConfigureImporterForLinearData)
        };
    }

    private void DrawInputConfigurationGUI()
    {
        EditorGUILayout.LabelField("Input & Output", EditorStyles.boldLabel);
        sourceTexture = (Texture2D)EditorGUILayout.ObjectField("Source Height Map", sourceTexture, typeof(Texture2D), false);
        baseFileName = EditorGUILayout.TextField("Base File Name", baseFileName);

        EditorGUI.BeginChangeCheck();
        resolution = (TextureResolution)EditorGUILayout.EnumPopup("Resolution", resolution);
        if (EditorGUI.EndChangeCheck() && isPreviewActive)
        {
            UpdatePreviewTexture();
        }

        EditorGUILayout.BeginHorizontal();
        outputDirectory = EditorGUILayout.TextField("Output Directory", outputDirectory);
        if (GUILayout.Button("Browse", GUILayout.Width(80)))
        {
            BrowseForOutputDirectory();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }

    private void DrawBakeTasksAndSettingsGUI()
    {
        EditorGUILayout.LabelField("Bake Maps & Settings", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MinHeight(150), GUILayout.MaxHeight(300));
        if (bakeTasks == null) InitializeBakeTasks();
        foreach (var task in bakeTasks)
        {
            task.IsEnabled = EditorGUILayout.ToggleLeft(new GUIContent(task.Label, task.ShaderKeyword), task.IsEnabled, EditorStyles.boldLabel);
            if (task.IsEnabled)
            {
                EditorGUI.indentLevel++;
                task.DrawSettingsGUI?.Invoke();
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
            }
        }
        EditorGUILayout.EndScrollView();

        if (EditorGUI.EndChangeCheck() && isPreviewActive)
        {
            UpdatePreviewTexture();
        }
        EditorGUILayout.Space();
    }

    private void DrawPreviewGUI()
    {
        EditorGUILayout.LabelField("Live Preview", EditorStyles.boldLabel);

        string[] taskLabels = bakeTasks.Select(t => t.Label).ToArray();

        EditorGUI.BeginChangeCheck();
        previewTaskIndex = EditorGUILayout.Popup("Preview Map Type", previewTaskIndex, taskLabels);
        if (EditorGUI.EndChangeCheck() && isPreviewActive)
        {
            UpdatePreviewTexture();
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginDisabledGroup(sourceTexture == null);
        if (GUILayout.Button("Generate/Update Preview"))
        {
            UpdatePreviewTexture();
        }
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("Clear Preview", GUILayout.Width(120)))
        {
            CleanUpPreviewTexture();
        }
        EditorGUILayout.EndHorizontal();

        if (isPreviewActive && previewTexture != null)
        {
            EditorGUILayout.Space();
            // Vẽ texture preview trong một box với tỷ lệ 1:1
            Rect previewRect = GUILayoutUtility.GetRect(position.width, position.width, GUILayout.ExpandWidth(true));
            previewRect.width = Mathf.Min(previewRect.width, 300f); // Giới hạn kích thước tối đa
            previewRect.height = previewRect.width;
            previewRect.x = (position.width - previewRect.width) / 2; // Căn giữa

            EditorGUI.DrawPreviewTexture(previewRect, previewTexture, null, ScaleMode.ScaleToFit, 1.0f);
        }
        EditorGUILayout.Space();
    }

    private void DrawExecutionGUI()
    {
        bool isAnyTaskEnabled = bakeTasks.Any(t => t.IsEnabled);
        bool isConfigurationValid = sourceTexture != null && !string.IsNullOrWhiteSpace(baseFileName) && Directory.Exists(outputDirectory) && isAnyTaskEnabled;

        EditorGUI.BeginDisabledGroup(!isConfigurationValid);
        if (GUILayout.Button("Bake Selected Maps", GUILayout.Height(40)))
        {
            BakeSelectedMaps();
        }
        EditorGUI.EndDisabledGroup();

        if (sourceTexture == null) EditorGUILayout.HelpBox("Please assign a source Height Map.", MessageType.Warning);
        if (!Directory.Exists(outputDirectory)) EditorGUILayout.HelpBox("The specified output directory does not exist.", MessageType.Warning);
        if (!isAnyTaskEnabled) EditorGUILayout.HelpBox("Select at least one map type to bake.", MessageType.Info);
    }

    private void UpdatePreviewTexture()
    {
        if (sourceTexture == null || bakerMaterial == null)
        {
            CleanUpPreviewTexture();
            return;
        }

        EnsurePreviewTextureExists();

        BakeTask selectedTask = bakeTasks[previewTaskIndex];

        PassMaterialParameters();
        EnableExclusiveShaderKeyword(selectedTask.ShaderKeyword);

        Graphics.Blit(sourceTexture, previewTexture, bakerMaterial);

        isPreviewActive = true;
        Repaint(); // Yêu cầu vẽ lại cửa sổ để hiển thị texture mới
    }

    private void EnsurePreviewTextureExists()
    {
        int previewSize = 512; // Kích thước cố định cho preview để tiết kiệm tài nguyên
        if (previewTexture == null || previewTexture.width != previewSize)
        {
            CleanUpPreviewTexture(); // Xóa cái cũ trước khi tạo cái mới
            previewTexture = new RenderTexture(previewSize, previewSize, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            previewTexture.Create();
        }
    }

    private void CleanUpPreviewTexture()
    {
        if (previewTexture != null)
        {
            previewTexture.Release();
            DestroyImmediate(previewTexture);
            previewTexture = null;
        }
        isPreviewActive = false;
        Repaint();
    }

    private async void BakeSelectedMaps()
    {
        var tasksToRun = bakeTasks.Where(t => t.IsEnabled).ToList();
        int totalTasks = tasksToRun.Count;
        string progressTitle = "Uber Baker Processing";

        try
        {
            for (int i = 0; i < totalTasks; i++)
            {
                var task = tasksToRun[i];
                string progressInfo = $"Baking {task.Label}... ({i + 1}/{totalTasks})";
                EditorUtility.DisplayProgressBar(progressTitle, progressInfo, (float)i / totalTasks);

                PassMaterialParameters();
                EnableExclusiveShaderKeyword(task.ShaderKeyword);

                string outputPath = Path.Combine(outputDirectory, $"{baseFileName}{task.Suffix}.png");
                BakeAndSaveTexture(outputPath, (int)resolution);

                await Task.Yield();
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        PostProcessBakedTextures(tasksToRun);
        Debug.Log($"<color=lime>Uber Baker: Successfully baked {totalTasks} textures to '{outputDirectory}'.</color>");
        PingOutputDirectory();
    }

    private void PassMaterialParameters()
    {
        bakerMaterial.SetFloat("_NormalStrength", normalStrength);
        bakerMaterial.SetFloat("_CurvatureRadius", curvatureRadius);
        bakerMaterial.SetFloat("_CurvatureStrength", curvatureStrength);
        bakerMaterial.SetFloat("_AoRadius", aoRadius);
        bakerMaterial.SetInt("_AoSampleDirections", aoSampleDirections);
        bakerMaterial.SetInt("_AoSampleSteps", aoSampleSteps);
        bakerMaterial.SetFloat("_AoStrength", aoStrength);
        bakerMaterial.SetFloat("_MetallicLow", metallicLow);
        bakerMaterial.SetFloat("_MetallicHigh", metallicHigh);
        bakerMaterial.SetFloat("_MetallicContrast", metallicContrast);
    }

    private void EnableExclusiveShaderKeyword(string keyword)
    {
        bakerMaterial.shaderKeywords = new string[] { keyword };
    }

    private void BakeAndSaveTexture(string path, int size)
    {
        RenderTexture tempRT = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        Graphics.Blit(sourceTexture, tempRT, bakerMaterial);

        Texture2D bakedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        RenderTexture.active = tempRT;
        bakedTexture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        bakedTexture.Apply();
        RenderTexture.active = null;

        byte[] pngData = bakedTexture.EncodeToPNG();
        File.WriteAllBytes(path, pngData);

        DestroyImmediate(bakedTexture);
        RenderTexture.ReleaseTemporary(tempRT);
    }

    // Các hàm còn lại không thay đổi
    private void BrowseForOutputDirectory()
    {
        string projectPath = Application.dataPath;
        string absolutePath = EditorUtility.OpenFolderPanel("Select Output Directory", projectPath, "");
        if (!string.IsNullOrEmpty(absolutePath) && absolutePath.StartsWith(projectPath))
        {
            outputDirectory = "Assets" + absolutePath.Substring(projectPath.Length);
        }
        else if (!string.IsNullOrEmpty(absolutePath))
        {
            Debug.LogWarning("Please select a directory inside the project's 'Assets' folder.");
        }
    }
    private void PostProcessBakedTextures(List<BakeTask> bakedTasks)
    {
        AssetDatabase.Refresh();
        foreach (var task in bakedTasks)
        {
            string assetPath = Path.Combine(outputDirectory, $"{baseFileName}{task.Suffix}.png");
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                task.ConfigureImporter?.Invoke(importer);
                importer.SaveAndReimport();
            }
        }
    }
    private void PingOutputDirectory()
    {
        var folderObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(outputDirectory);
        if (folderObject != null) EditorGUIUtility.PingObject(folderObject);
    }
    private void ConfigureImporterForNormalMap(TextureImporter importer) { importer.textureType = TextureImporterType.NormalMap; importer.sRGBTexture = false; }
    private void ConfigureImporterForLinearData(TextureImporter importer) { importer.textureType = TextureImporterType.Default; importer.sRGBTexture = false; }
    private void DrawNormalMapSettings() => normalStrength = EditorGUILayout.Slider("Strength", normalStrength, 0f, 20f);
    private void DrawCurvatureSettings() { curvatureRadius = EditorGUILayout.Slider("Sample Radius", curvatureRadius, 0.1f, 10f); curvatureStrength = EditorGUILayout.Slider("Strength", curvatureStrength, 0f, 5f); }
    private void DrawAoBentNormalSettings() { aoRadius = EditorGUILayout.Slider("Radius", aoRadius, 1f, 50f); aoSampleDirections = EditorGUILayout.IntSlider("Directions", aoSampleDirections, 4, 64); aoSampleSteps = EditorGUILayout.IntSlider("Steps per Direction", aoSampleSteps, 4, 32); aoStrength = EditorGUILayout.Slider("Strength", aoStrength, 0f, 5f); }
    private void DrawMetallicSettings() { metallicLow = EditorGUILayout.Slider("Value at Low Height", metallicLow, 0f, 1f); metallicHigh = EditorGUILayout.Slider("Value at High Height", metallicHigh, 0f, 1f); metallicContrast = EditorGUILayout.Slider("Contrast", metallicContrast, 0.1f, 10f); }
}