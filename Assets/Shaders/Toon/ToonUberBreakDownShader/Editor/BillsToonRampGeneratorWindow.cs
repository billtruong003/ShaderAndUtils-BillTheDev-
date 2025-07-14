using UnityEditor;
using UnityEngine;
using System.IO;

public class BillsToonRampGeneratorWindow : EditorWindow
{
    private Gradient gradient;
    private int textureWidth = 256;
    private readonly int textureHeight = 8;
    private string fileName = "NewToonRamp";
    private string saveFolderPath = "Assets/";
    private Material targetMaterial;
    private Texture2D previewTexture;

    private const string RampTextureProperty = "_Ramp";

    [MenuItem("Tools/Bill's Toon/Ramp Texture Generator")]
    public static void ShowWindow()
    {
        GetWindow<BillsToonRampGeneratorWindow>("Ramp Generator");
    }

    private void OnEnable()
    {
        InitializeDefaultGradient();
        GeneratePreviewTexture();
    }

    private void OnDisable()
    {
        if (previewTexture != null)
        {
            DestroyImmediate(previewTexture);
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Bill's Toon Ramp Generator", new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 14 });
        EditorGUILayout.HelpBox("Tạo ramp texture 1D từ một gradient. Được sử dụng bởi Surface Type 'Stylized Metal' trong Opaque Shader.", MessageType.Info);
        EditorGUILayout.Space();

        DrawSettingsPanel();
        DrawPreviewPanel();
        DrawOutputPanel();
    }

    private void DrawSettingsPanel()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Gradient Settings", EditorStyles.boldLabel);

        targetMaterial = (Material)EditorGUILayout.ObjectField("Target Material (Live Preview)", targetMaterial, typeof(Material), true);

        EditorGUI.BeginChangeCheck();
        gradient = EditorGUILayout.GradientField("Gradient", gradient);
        textureWidth = EditorGUILayout.IntSlider("Texture Width", textureWidth, 32, 1024);
        if (EditorGUI.EndChangeCheck())
        {
            GeneratePreviewTexture();
            ApplyPreviewToMaterial();
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    private void DrawPreviewPanel()
    {
        if (previewTexture == null) return;

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Texture Preview", EditorStyles.boldLabel);
        Rect previewRect = GUILayoutUtility.GetRect(position.width - 40, 40);
        EditorGUI.DrawTextureTransparent(previewRect, previewTexture, ScaleMode.StretchToFill);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    private void DrawOutputPanel()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);
        fileName = EditorGUILayout.TextField("File Name", fileName);
        DrawPathSelector();
        EditorGUILayout.Space(5);
        if (GUILayout.Button("Generate and Save Texture", GUILayout.Height(40)))
        {
            CreateAndSaveRampTexture();
        }
        EditorGUILayout.EndVertical();
    }

    private void GeneratePreviewTexture()
    {
        if (previewTexture != null) DestroyImmediate(previewTexture);
        previewTexture = CreateRampTexture(textureWidth, textureHeight, gradient);
        previewTexture.name = "Ramp_Preview_DoNotSave";
        previewTexture.hideFlags = HideFlags.HideAndDontSave;
    }

    private void ApplyPreviewToMaterial()
    {
        if (targetMaterial != null && targetMaterial.HasProperty(RampTextureProperty) && previewTexture != null)
        {
            targetMaterial.SetTexture(RampTextureProperty, previewTexture);
        }
    }

    private Texture2D CreateRampTexture(int width, int height, Gradient grad)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[width * height];
        for (int x = 0; x < width; x++)
        {
            float normalizedX = (float)x / (width - 1);
            Color pixelColor = grad.Evaluate(normalizedX);
            for (int y = 0; y < height; y++)
            {
                pixels[y * width + x] = pixelColor;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(false);
        return texture;
    }

    private void CreateAndSaveRampTexture()
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            EditorUtility.DisplayDialog("Error", "Tên tệp không được để trống.", "OK");
            return;
        }
        if (!Directory.Exists(saveFolderPath))
        {
            EditorUtility.DisplayDialog("Error", $"Thư mục lưu không tồn tại: {saveFolderPath}", "OK");
            return;
        }
        Texture2D rampTexture = CreateRampTexture(textureWidth, textureHeight, gradient);
        byte[] pngData = rampTexture.EncodeToPNG();
        DestroyImmediate(rampTexture);
        string finalPath = Path.Combine(saveFolderPath, fileName + ".png");
        File.WriteAllBytes(finalPath, pngData);
        AssetDatabase.Refresh();
        ConfigureTextureAsset(finalPath);
        Texture2D savedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(finalPath);
        if (targetMaterial != null && savedTexture != null && targetMaterial.HasProperty(RampTextureProperty))
        {
            targetMaterial.SetTexture(RampTextureProperty, savedTexture);
            Debug.Log($"[Bill's Toon] Texture '{fileName}.png' đã lưu tại '{finalPath}' và áp dụng cho material '{targetMaterial.name}'.");
        }
        else
        {
            Debug.Log($"[Bill's Toon] Texture '{fileName}.png' đã lưu tại '{finalPath}'.");
        }
        HighlightGeneratedAsset(finalPath);
    }

    private void DrawPathSelector()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Save Path");
        EditorGUILayout.SelectableLabel(saveFolderPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string absolutePath = EditorUtility.OpenFolderPanel("Choose Save Location", Application.dataPath, "");
            if (!string.IsNullOrEmpty(absolutePath) && absolutePath.StartsWith(Application.dataPath))
            {
                saveFolderPath = "Assets" + absolutePath.Substring(Application.dataPath.Length);
            }
            else if (!string.IsNullOrEmpty(absolutePath))
            {
                EditorUtility.DisplayDialog("Invalid Path", "Vui lòng chọn một thư mục bên trong thư mục 'Assets' của dự án.", "OK");
            }
        }
        EditorGUILayout.EndHorizontal();
    }
    private void InitializeDefaultGradient()
    {
        gradient = new Gradient()
        {
            colorKeys = new GradientColorKey[] { new GradientColorKey(Color.black, 0.45f), new GradientColorKey(Color.white, 0.55f) },
            alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) },
            mode = GradientMode.Fixed
        };
    }

    private void ConfigureTextureAsset(string assetPath)
    {
        if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }
    }

    private void HighlightGeneratedAsset(string assetPath)
    {
        Object generatedAsset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
        if (generatedAsset != null)
        {
            EditorGUIUtility.PingObject(generatedAsset);
            Selection.activeObject = generatedAsset;
        }
    }
}