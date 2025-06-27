// Đặt tại: Assets/Scripts/ModularCharacter.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[ExecuteInEditMode]
public class ModularCharacter : MonoBehaviour
{
    [Tooltip("Dữ liệu nhân vật được trích xuất từ BoneDataSO gốc")]
    public BoneDataSO characterData;

    [System.Serializable]
    public class EquipmentEntry
    {
        public string category;
        public string partId; // Lưu ID của trang bị, ví dụ: "Top_5", "Hair_12", hoặc "None"
    }

    [Tooltip("Danh sách các trang bị đang được mặc. Được quản lý bởi Editor Script.")]
    public List<EquipmentEntry> equippedParts = new List<EquipmentEntry>();

    private readonly Dictionary<string, Transform> _boneMap = new Dictionary<string, Transform>();
    private readonly Dictionary<string, SkinnedMeshRenderer> _slotRenderers = new Dictionary<string, SkinnedMeshRenderer>();

    /// <summary>
    /// Xây dựng lại toàn bộ nhân vật từ đầu.
    /// </summary>
    [Utils.Bill.InspectorCustom.CustomButton]
    public void RebuildCharacter()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        _boneMap.Clear();
        _slotRenderers.Clear();

        if (characterData == null)
        {
            Debug.LogError("CharacterData is not assigned!", this);
            return;
        }

        BuildSkeleton();
        BuildSlots();
        EquipAllPartsFromData();
    }

    private void BuildSkeleton()
    {
        if (characterData.boneHierarchy == null || characterData.boneHierarchy.Length == 0) return;

        var skeletonRoot = new GameObject("Skeleton").transform;
        skeletonRoot.SetParent(transform, false);

        foreach (var boneInfo in characterData.boneHierarchy)
        {
            BuildBoneRecursive(boneInfo, skeletonRoot);
        }
    }

    private void BuildBoneRecursive(BoneDataSO.BoneInfo boneInfo, Transform parent)
    {
        if (boneInfo == null || boneInfo.bone == null) return;

        var newBone = new GameObject(boneInfo.bone.name).transform;
        newBone.SetParent(parent, false);
        newBone.localPosition = boneInfo.bone.localPosition;
        newBone.localRotation = boneInfo.bone.localRotation;
        newBone.localScale = boneInfo.bone.localScale;

        _boneMap[boneInfo.bone.name] = newBone;

        if (boneInfo.children != null)
        {
            foreach (var childInfo in boneInfo.children)
            {
                BuildBoneRecursive(childInfo, newBone);
            }
        }
    }

    private void BuildSlots()
    {
        if (characterData == null) return;

        var categories = GetCategoriesFromData();

        foreach (var category in categories)
        {
            var slotGo = new GameObject($"[SLOT] {category}");
            slotGo.transform.SetParent(transform, false);
            var smr = slotGo.AddComponent<SkinnedMeshRenderer>();
            _slotRenderers[category] = smr;
        }
    }

    private void EquipAllPartsFromData()
    {
        foreach (var slot in _slotRenderers.Values)
        {
            slot.sharedMesh = null;
        }

        foreach (var entry in equippedParts)
        {
            EquipPart(entry.category, entry.partId);
        }
    }

    public void EquipPart(string category, string partId)
    {
        if (!_slotRenderers.TryGetValue(category, out var targetRenderer))
        {
            Debug.LogWarning($"Slot for category '{category}' not found.", this);
            return;
        }

        if (string.IsNullOrEmpty(partId) || partId.Equals("None"))
        {
            targetRenderer.sharedMesh = null;
            return;
        }

        BoneDataSO.SkinMeshData partData = (category == "Body")
            ? characterData.bodyData
            : characterData.partCategories.FirstOrDefault(p => p.id == partId);

        if (partData == null || partData.renderer == null)
        {
            targetRenderer.sharedMesh = null;
            Debug.LogWarning($"Part data for ID '{partId}' not found in BoneDataSO.", this);
            return;
        }

        targetRenderer.sharedMesh = partData.mesh;
        targetRenderer.materials = partData.renderer.sharedMaterials;

        var sourceBones = partData.renderer.bones;
        var newBones = new Transform[sourceBones.Length];
        for (int i = 0; i < sourceBones.Length; i++)
        {
            string boneName = sourceBones[i].name;
            if (!_boneMap.TryGetValue(boneName, out newBones[i]))
            {
                Debug.LogError($"Failed to map bone '{boneName}' for part '{partId}'. Is the skeleton built correctly?", this);
                newBones[i] = transform;
            }
        }
        targetRenderer.bones = newBones;

        // Lấy tên root bone từ path
        string rootBoneName = GetRootBoneNameFromPath(partData.rootBonePath);
        if (!string.IsNullOrEmpty(rootBoneName) && _boneMap.TryGetValue(rootBoneName, out Transform rootBone))
        {
            targetRenderer.rootBone = rootBone;
        }
        else if (characterData.hipsBone != null && _boneMap.TryGetValue(characterData.hipsBone.bone.name, out Transform hipsBone))
        {
            targetRenderer.rootBone = hipsBone;
        }
    }

    // --- Các hàm Helper để làm việc với BoneDataSO gốc ---

    public List<string> GetCategoriesFromData()
    {
        if (characterData == null) return new List<string>();

        // Lấy tên thư mục cha của từng part để xác định category
        var categories = characterData.partCategories
            .Where(p => p.renderer != null && p.renderer.transform.parent != null)
            .Select(p => p.renderer.transform.parent.name)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        // Thêm "Body" vào đầu danh sách nếu có
        if (characterData.bodyData != null && !categories.Contains("Body"))
        {
            categories.Insert(0, "Body");
        }
        return categories;
    }

    private string GetRootBoneNameFromPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var parts = path.Split('/');
        return parts.Length > 0 ? parts[parts.Length - 1] : null;
    }
}