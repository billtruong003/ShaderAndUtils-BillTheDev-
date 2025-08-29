using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public sealed class ModelProcessorWindow : EditorWindow
{
    private GameObject sourceObject;
    private Shader targetShader;
    private string baseSavePath = "Assets/GeneratedClones";
    private string prefabName = "NewClonedPrefab";

    private const string WindowTitle = "Clean Model Processor";
    private const string InvalidObjectError = "Source GameObject cannot be null.";
    private const string InvalidNameError = "Prefab Name cannot be empty.";
    private const string ProcessButtonText = "Process and Create Prefab";
    private const string SuccessDialogTitle = "Process Complete";

    [MenuItem("Tools/Clean Code/Model Processor")]
    private static void ShowWindow()
    {
        GetWindow<ModelProcessorWindow>(false, WindowTitle, true);
    }

    private void OnGUI()
    {
        RenderWindowTitle();
        RenderInputFields();
        RenderProcessButton();
    }

    private void RenderWindowTitle()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(WindowTitle, EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select a GameObject to duplicate it and its materials into a new, independent prefab. Optionally, assign a new shader to all duplicated materials.", MessageType.Info);
        EditorGUILayout.Space();
    }

    private void RenderInputFields()
    {
        sourceObject = (GameObject)EditorGUILayout.ObjectField("Source Object", sourceObject, typeof(GameObject), true);
        prefabName = EditorGUILayout.TextField("New Prefab Name", prefabName);
        baseSavePath = EditorGUILayout.TextField("Base Save Path", baseSavePath);
        targetShader = (Shader)EditorGUILayout.ObjectField("Apply New Shader (Optional)", targetShader, typeof(Shader), false);
    }

    private void RenderProcessButton()
    {
        EditorGUILayout.Space(20);
        if (GUILayout.Button(ProcessButtonText, GUILayout.Height(40)))
        {
            ExecuteProcessing();
        }
    }

    private void ExecuteProcessing()
    {
        if (!IsInputValid())
        {
            return;
        }

        string sanitizedPrefabName = SanitizeFileName(prefabName);
        string sessionPath = Path.Combine(baseSavePath, sanitizedPrefabName);
        string materialsPath = Path.Combine(sessionPath, "Materials");

        CreateAssetDirectories(sessionPath, materialsPath);

        Dictionary<Material, Material> originalToClonedMaterialMap = DuplicateAndMapMaterials(materialsPath);

        if (originalToClonedMaterialMap == null || originalToClonedMaterialMap.Count == 0)
        {
            Debug.LogWarning("Source object contains no renderers with materials to process.");
        }

        CreatePrefabWithClonedMaterials(sessionPath, sanitizedPrefabName, originalToClonedMaterialMap);
    }

    private bool IsInputValid()
    {
        if (sourceObject == null)
        {
            EditorUtility.DisplayDialog("Validation Error", InvalidObjectError, "OK");
            return false;
        }

        if (string.IsNullOrWhiteSpace(prefabName))
        {
            EditorUtility.DisplayDialog("Validation Error", InvalidNameError, "OK");
            return false;
        }

        return true;
    }

    private void CreateAssetDirectories(string rootPath, string materialsPath)
    {
        if (!AssetDatabase.IsValidFolder(rootPath))
        {
            // Split path and create folders incrementally
            string[] folders = rootPath.Split('/');
            string currentPath = folders[0];
            for (int i = 1; i < folders.Length; i++)
            {
                string parentPath = currentPath;
                currentPath = Path.Combine(currentPath, folders[i]);
                if (!AssetDatabase.IsValidFolder(currentPath))
                {
                    AssetDatabase.CreateFolder(parentPath, folders[i]);
                }
            }
        }
        if (!AssetDatabase.IsValidFolder(materialsPath))
        {
            AssetDatabase.CreateFolder(rootPath, "Materials");
        }
    }

    private Dictionary<Material, Material> DuplicateAndMapMaterials(string materialsPath)
    {
        var materialMap = new Dictionary<Material, Material>();
        var allRenderers = sourceObject.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in allRenderers)
        {
            foreach (Material originalMaterial in renderer.sharedMaterials)
            {
                if (originalMaterial != null && !materialMap.ContainsKey(originalMaterial))
                {
                    Material clonedMaterial = new Material(originalMaterial);
                    if (targetShader != null)
                    {
                        clonedMaterial.shader = targetShader;
                    }

                    string materialAssetName = SanitizeFileName($"{originalMaterial.name}_Clone");
                    string materialAssetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(materialsPath, $"{materialAssetName}.mat"));

                    AssetDatabase.CreateAsset(clonedMaterial, materialAssetPath);
                    materialMap.Add(originalMaterial, clonedMaterial);
                }
            }
        }
        return materialMap;
    }

    private void CreatePrefabWithClonedMaterials(string sessionPath, string name, Dictionary<Material, Material> materialMap)
    {
        GameObject instance = Instantiate(sourceObject);
        instance.name = sourceObject.name;

        var clonedRenderers = instance.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in clonedRenderers)
        {
            Material[] currentMaterials = renderer.sharedMaterials;
            Material[] newMaterials = new Material[currentMaterials.Length];

            for (int i = 0; i < currentMaterials.Length; i++)
            {
                Material originalMat = currentMaterials[i];
                if (originalMat != null && materialMap.TryGetValue(originalMat, out Material clonedMat))
                {
                    newMaterials[i] = clonedMat;
                }
                else
                {
                    newMaterials[i] = originalMat; // Keep original if not found (e.g., null)
                }
            }
            renderer.sharedMaterials = newMaterials;
        }

        string prefabPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(sessionPath, $"{name}.prefab"));
        GameObject createdPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(instance, prefabPath, InteractionMode.UserAction);

        DestroyImmediate(instance);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        FinalizeProcess(createdPrefab);
    }


    private void FinalizeProcess(GameObject createdPrefab)
    {
        EditorUtility.DisplayDialog(SuccessDialogTitle, $"Successfully created prefab and materials at:\n{Path.GetDirectoryName(AssetDatabase.GetAssetPath(createdPrefab))}", "OK");
        EditorGUIUtility.PingObject(createdPrefab);
        Selection.activeObject = createdPrefab;
    }

    private string SanitizeFileName(string name)
    {
        return Path.GetInvalidFileNameChars().Aggregate(name, (current, c) => current.Replace(c.ToString(), string.Empty));
    }
}