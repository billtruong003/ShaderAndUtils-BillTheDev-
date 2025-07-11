using UnityEditor;
using UnityEngine;
using System.IO;

public class RampGeneratorWindow : EditorWindow
{
    private Gradient gradient;
    private int textureWidth = 256;
    private int textureHeight = 16;
    private string fileName = "NewToonRamp";
    private string saveFolderPath = "Assets/";

    private Material targetMaterial;
    private Texture2D previewTexture;

    [MenuItem("Tools/Shmackle/Ramp Texture Generator")]
    public static void ShowWindow()
    {
        GetWindow<RampGeneratorWindow>("Ramp Generator");
    }

    private void OnEnable()
    {
        if (gradient == null)
        {
            InitializeDefaultGradient();
        }
    }

    private void OnDisable()
    {
        if (previewTexture != null)
        {
            DestroyImmediate(previewTexture);
            previewTexture = null;
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Ramp Texture Settings", EditorStyles.boldLabel);

        targetMaterial = (Material)EditorGUILayout.ObjectField("Target Material (Live Preview)", targetMaterial, typeof(Material), true);
        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        gradient = EditorGUILayout.GradientField("Gradient", gradient);
        textureWidth = EditorGUILayout.IntField("Texture Width", textureWidth);
        textureHeight = EditorGUILayout.IntField("Texture Height", textureHeight);
        if (EditorGUI.EndChangeCheck())
        {
            UpdatePreviewTexture();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        fileName = EditorGUILayout.TextField("File Name", fileName);
        DrawPathSelector();
        EditorGUILayout.Space();

        if (GUILayout.Button("Save and Apply Texture", GUILayout.Height(40)))
        {
            CreateAndSaveRampTexture();
        }
    }

    private Texture2D CreateRampTexture(int width, int height, Gradient grad)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
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
        texture.Apply();
        return texture;
    }

    private void UpdatePreviewTexture()
    {
        if (targetMaterial == null) return;

        if (previewTexture != null) DestroyImmediate(previewTexture);

        previewTexture = CreateRampTexture(textureWidth, textureHeight, gradient);
        previewTexture.name = "Ramp_Preview_DoNotSave";
        previewTexture.hideFlags = HideFlags.HideAndDontSave;

        targetMaterial.SetTexture("_Ramp", previewTexture);
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
            targetMaterial.SetTexture("_Ramp", savedTexture);
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
        EditorGUILayout.LabelField("Save Path", GUILayout.Width(EditorGUIUtility.labelWidth - 5));
        EditorGUILayout.SelectableLabel(saveFolderPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));

        if (GUILayout.Button("Browse...", GUILayout.Width(75)))
        {
            string chosenPath = EditorUtility.OpenFolderPanel("Choose Save Location", saveFolderPath, "");
            if (!string.IsNullOrEmpty(chosenPath) && IsPathInAssets(chosenPath))
            {
                saveFolderPath = ConvertToRelativePath(chosenPath);
            }
            else if (!string.IsNullOrEmpty(chosenPath))
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
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
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

    private bool IsPathInAssets(string path)
    {
        return path.StartsWith(Application.dataPath);
    }

    private string ConvertToRelativePath(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath)) return "Assets/";
        return "Assets" + absolutePath.Substring(Application.dataPath.Length);
    }
}