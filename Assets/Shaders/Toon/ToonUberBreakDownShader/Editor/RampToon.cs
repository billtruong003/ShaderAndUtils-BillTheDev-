using UnityEditor;
using UnityEngine;
using System.IO;

public class RampToonWindow : EditorWindow
{
    private Gradient gradient;
    private int textureWidth = 256;
    private int textureHeight = 8;
    private string fileName = "NewToonRamp";
    private string saveFolderPath = "Assets/";

    private Material targetMaterial;
    private Texture2D previewTexture;

    private static readonly string RampTextureProperty = "_Ramp";

    [MenuItem("Tools/Toon Uber/Ramp Texture Generator")]
    public static void ShowWindow()
    {
        GetWindow<RampGeneratorWindow>("Ramp Generator");
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
        EditorGUILayout.LabelField("Ramp Texture Generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Create a 1D ramp texture from a gradient. This can be used for the 'Stylized Metal' surface type.", MessageType.Info);

        EditorGUILayout.Space();

        targetMaterial = (Material)EditorGUILayout.ObjectField("Target Material (Live Preview)", targetMaterial, typeof(Material), true);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Gradient Settings", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        gradient = EditorGUILayout.GradientField("Gradient", gradient);
        textureWidth = EditorGUILayout.IntSlider("Texture Width", textureWidth, 32, 1024);
        if (EditorGUI.EndChangeCheck())
        {
            GeneratePreviewTexture();
            ApplyPreviewToMaterial();
        }

        if (previewTexture != null)
        {
            EditorGUILayout.LabelField("Preview:");
            Rect previewRect = GUILayoutUtility.GetRect(position.width - 40, 40);
            EditorGUI.DrawTextureTransparent(previewRect, previewTexture, ScaleMode.StretchToFill);
            EditorGUILayout.Space();
        }

        EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);
        fileName = EditorGUILayout.TextField("File Name", fileName);
        DrawPathSelector();

        EditorGUILayout.Space();

        if (GUILayout.Button("Save and Apply Texture", GUILayout.Height(40)))
        {
            CreateAndSaveRampTexture();
        }
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
        if (targetMaterial != null && previewTexture != null)
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
            EditorUtility.DisplayDialog("Error", "File name cannot be empty.", "OK");
            return;
        }

        if (!Directory.Exists(saveFolderPath))
        {
            EditorUtility.DisplayDialog("Error", $"Save folder does not exist: {saveFolderPath}", "OK");
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

        if (targetMaterial != null && savedTexture != null)
        {
            targetMaterial.SetTexture(RampTextureProperty, savedTexture);
            Debug.Log($"Texture '{fileName}.png' saved to '{finalPath}' and applied to material '{targetMaterial.name}'.");
        }
        else
        {
            Debug.Log($"Texture '{fileName}.png' saved to '{finalPath}'.");
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
                EditorUtility.DisplayDialog("Invalid Path", "Please select a folder inside the project's 'Assets' directory.", "OK");
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