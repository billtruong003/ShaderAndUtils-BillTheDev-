using UnityEngine;
using UnityEditor;
using System.IO;

public class TextureBakerWindow : EditorWindow
{
    private Material processingMaterial;
    private string saveFolderPath = "Assets/BakedTextures";
    private string fileName = "NewBakedTexture.png";

    [MenuItem("Tools/Clean Code/Texture Baker")]
    public static void ShowWindow()
    {
        GetWindow<TextureBakerWindow>("Texture Baker");
    }

    private void OnGUI()
    {
        GUILayout.Label("Texture Baking Tool", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Assign the Material that is using the color replacement shader. The source texture will be taken from it.", MessageType.Info);

        processingMaterial = (Material)EditorGUILayout.ObjectField("Processing Material", processingMaterial, typeof(Material), false);

        EditorGUILayout.Space();

        GUILayout.Label("Save Settings", EditorStyles.boldLabel);
        DrawPathSelector();
        fileName = EditorGUILayout.TextField("File Name", fileName);

        EditorGUILayout.Space();

        GUI.enabled = IsBakeConfigurationValid();
        if (GUILayout.Button("Bake Texture", GUILayout.Height(40)))
        {
            ExecuteBakeProcess();
        }
        GUI.enabled = true;
    }

    private void DrawPathSelector()
    {
        EditorGUILayout.BeginHorizontal();
        saveFolderPath = EditorGUILayout.TextField("Save Folder", saveFolderPath);
        if (GUILayout.Button("Browse", GUILayout.Width(80)))
        {
            BrowseForSaveFolder();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void BrowseForSaveFolder()
    {
        string absolutePath = EditorUtility.SaveFolderPanel("Select Save Folder", "Assets", "");
        if (!string.IsNullOrEmpty(absolutePath))
        {
            // Unity requires a path relative to the project root.
            if (absolutePath.StartsWith(Application.dataPath))
            {
                saveFolderPath = "Assets" + absolutePath.Substring(Application.dataPath.Length);
            }
            else
            {
                EditorUtility.DisplayDialog("Invalid Path", "Please select a folder inside the project's 'Assets' directory.", "OK");
            }
        }
    }

    private void ExecuteBakeProcess()
    {
        Texture2D sourceTexture = GetSourceTextureFromMaterial();
        string fullPath = Path.Combine(saveFolderPath, fileName);

        if (!IsOverwriteConfirmed(fullPath)) return;

        RenderTexture temporaryRT = CreateTemporaryRenderTexture(sourceTexture);
        Graphics.Blit(sourceTexture, temporaryRT, processingMaterial);

        Texture2D bakedTexture = CreateFinalTextureFromRT(temporaryRT);

        SaveTextureToFile(bakedTexture, fullPath);

        CleanUp(temporaryRT, bakedTexture);
    }

    private bool IsBakeConfigurationValid()
    {
        return processingMaterial != null && !string.IsNullOrWhiteSpace(fileName);
    }

    private bool IsOverwriteConfirmed(string path)
    {
        if (File.Exists(path))
        {
            return EditorUtility.DisplayDialog(
                "File Exists",
                $"The file '{Path.GetFileName(path)}' already exists. Do you want to overwrite it?",
                "Overwrite",
                "Cancel"
            );
        }
        return true;
    }

    private Texture2D GetSourceTextureFromMaterial() => processingMaterial.mainTexture as Texture2D;

    private RenderTexture CreateTemporaryRenderTexture(Texture source)
    {
        return RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
    }

    private Texture2D CreateFinalTextureFromRT(RenderTexture rt)
    {
        Texture2D finalTexture = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture.active = rt;
        finalTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        finalTexture.Apply();
        RenderTexture.active = previousActive;
        return finalTexture;
    }

    private void SaveTextureToFile(Texture2D texture, string path)
    {
        byte[] pngData = texture.EncodeToPNG();
        if (pngData == null) return;

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, pngData);
        AssetDatabase.Refresh();

        Debug.Log($"Texture successfully baked and saved to: {path}");

        // Highlight the newly created asset in the project window
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(path));
    }

    private void CleanUp(RenderTexture tempRT, Texture2D finalTexture)
    {
        RenderTexture.ReleaseTemporary(tempRT);
        // We created this texture in memory, so we must destroy it.
        DestroyImmediate(finalTexture);
    }
}