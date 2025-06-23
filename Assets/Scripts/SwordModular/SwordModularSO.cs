using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Enum mới để định nghĩa chủ đề cho các bộ phận
public enum SwordTheme { None, Royal, Elven, Brutish, Demonic, SciFi }

[System.Serializable]
public class SwordMeshDefinition
{
    [Tooltip("Mesh của bộ phận.")]
    [PreviewField(ObjectFieldAlignment.Left, Height = 75)]
    [AssetsOnly]
    public Mesh Mesh;

    [Tooltip("Material mặc định sẽ được áp dụng cho mesh này.")]
    [AssetsOnly]
    public Material DefaultMaterial;

    // --- TRƯỜNG MỚI ĐƯỢC THÊM VÀO ---
    [Tooltip("Chủ đề của bộ phận này, dùng cho tính năng tạo bộ.")]
    public SwordTheme PartTheme = SwordTheme.None;
}

[System.Serializable]
public class PartCollection
{
    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, ShowFoldout = true, DefaultExpandedState = true)]
    public List<SwordMeshDefinition> MeshDefinitions = new List<SwordMeshDefinition>();

#if UNITY_EDITOR
    [BoxGroup("Import Tools")]
    [FolderPath(AbsolutePath = false, RequireExistingPath = true)]
    public string ImportFolderPath;

    [BoxGroup("Import Tools")]
    [AssetsOnly]
    public Material DefaultMaterialForImport;

    [BoxGroup("Import Tools")]
    [Button("Scan Folder & Add Meshes", ButtonSizes.Large), GUIColor(0.2f, 0.8f, 0.2f)]
    private void ScanAndAddMeshes()
    {
        if (string.IsNullOrEmpty(ImportFolderPath))
        {
            Debug.LogError("Vui lòng chọn một folder để import.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Mesh", new[] { ImportFolderPath });
        if (guids.Length == 0)
        {
            Debug.LogWarning($"Không tìm thấy mesh nào trong folder: {ImportFolderPath}");
            return;
        }

        int newMeshesAdded = 0;
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);

            if (mesh != null && !MeshDefinitions.Any(def => def.Mesh == mesh))
            {
                MeshDefinitions.Add(new SwordMeshDefinition
                {
                    Mesh = mesh,
                    DefaultMaterial = DefaultMaterialForImport
                });
                newMeshesAdded++;
            }
        }

        if (newMeshesAdded > 0)
        {
            Debug.Log($"Đã thêm thành công {newMeshesAdded} mesh mới từ folder '{ImportFolderPath}'.");
        }
        else
        {
            Debug.Log("Tất cả các mesh trong folder đã có sẵn trong danh sách.");
        }
    }
#endif
}

[CreateAssetMenu(fileName = "SwordPartCollection", menuName = "Modular Swords/Sword Part Collection", order = 0)]
public class SwordPartCollectionSO : SerializedScriptableObject
{
    [TitleGroup("BLADES")]
    [HideLabel]
    public PartCollection Blades;

    [TitleGroup("HILTS")]
    [HideLabel]
    public PartCollection Hilts;

    [TitleGroup("GRIPS")]
    [HideLabel]
    public PartCollection Grips;
}