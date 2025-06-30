// File: Assets/Scripts/ModularSystem/ModularCharacter.cs
// (Bạn có thể đặt ở bất kỳ đâu)

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Utils.Bill.InspectorCustom;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ModularCharacter : MonoBehaviour
{
    [Tooltip("Dữ liệu nhân vật được trích xuất từ BoneDataSO gốc")]
    [ShowAssetPreview(60, 60, PreviewAlignment.Left)]
    public BoneDataSO characterData;

    [System.Serializable]
    public class EquipmentEntry
    {
        public string category;
        public string partId;
    }

    public List<EquipmentEntry> equippedParts = new List<EquipmentEntry>();

    private readonly Dictionary<string, Transform> _boneMap = new Dictionary<string, Transform>();
    private readonly Dictionary<string, SkinnedMeshRenderer> _slotRenderers = new Dictionary<string, SkinnedMeshRenderer>();
    private Transform _skeletonRoot;

    [CustomButton("Rebuild Character", "Xóa và xây dựng lại tất cả renderer và slot trang bị.", "#6495ED")]
    public void RebuildCharacter()
    {
        if (characterData == null) { Debug.LogError("CharacterData chưa được gán!", this); return; }
        ClearRenderers();
        MapSkeleton();
        if (_boneMap.Count == 0) { Debug.LogError("Không thể ánh xạ bộ xương.", this); return; }
        BuildSlots();
        EquipAllPartsFromData();
        Debug.Log("Rebuild Character hoàn tất!");
    }

    [Tooltip("Tỷ lệ % mặc một món đồ trống (None) cho các bộ phận không bắt buộc. 0 = luôn có đồ, 100 = luôn trống.")]
    [Range(0, 100)]
    public float chanceForNone = 25f;

    [CustomButton("Randomize Equipment", "Mặc ngẫu nhiên một bộ trang bị.")]
    public void RandomizeAllEquipment()
    {
        if (characterData == null) { Debug.LogError("CharacterData chưa được gán!", this); return; }
        var categories = GetCategoriesFromData();
        foreach (var category in categories)
        {
            string finalId = "";
            var categoryParts = characterData.partCategories.FirstOrDefault(c => c.categoryName == category);
            bool isMandatory = category.Equals("Body", StringComparison.OrdinalIgnoreCase) || category.Equals("Face", StringComparison.OrdinalIgnoreCase);

            if (isMandatory)
            {
                if (category.Equals("Body", StringComparison.OrdinalIgnoreCase))
                    finalId = characterData.bodyData?.id;
                else
                {
                    if (categoryParts != null && categoryParts.parts.Any())
                        finalId = categoryParts.parts[UnityEngine.Random.Range(0, categoryParts.parts.Count)].id;
                    else
                        finalId = "None";
                }
            }
            else
            {
                if (UnityEngine.Random.Range(0f, 100f) < chanceForNone || categoryParts == null || !categoryParts.parts.Any())
                    finalId = "None";
                else
                    finalId = categoryParts.parts[UnityEngine.Random.Range(0, categoryParts.parts.Count)].id;
            }
            UpdateAndEquip(category, finalId);
        }
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    private void UpdateAndEquip(string category, string partId)
    {
        var entry = equippedParts.FirstOrDefault(e => e.category == category);
        if (entry == null)
        {
            entry = new EquipmentEntry { category = category };
            equippedParts.Add(entry);
        }
        entry.partId = partId;
        EquipPart(category, partId);
    }

    [CustomButton("Unequip All", "Gỡ bỏ tất cả trang bị, chỉ giữ lại Body.", "#FF6347")]
    public void UnequipAll()
    {
        if (characterData == null) return;
        var categories = GetCategoriesFromData();
        foreach (var category in categories)
        {
            if (category == "Body") continue;
            var entry = equippedParts.FirstOrDefault(e => e.category == category);
            if (entry != null) entry.partId = "None";
            EquipPart(category, "None");
        }
    }

    private void MapSkeleton()
    {
        _boneMap.Clear();
        if (characterData?.boneHierarchy == null) return;
        _skeletonRoot = transform.Find(characterData.boneHierarchy.bonePath);
        if (_skeletonRoot == null) _skeletonRoot = transform;
        MapBoneRecursive(characterData.boneHierarchy);
    }

    private void MapBoneRecursive(BoneDataSO.BoneInfo boneInfo)
    {
        if (boneInfo == null) return;
        var boneTransform = transform.Find(boneInfo.bonePath);
        if (boneTransform != null) _boneMap[boneInfo.bonePath] = boneTransform;
        if (boneInfo.children != null) foreach (var childInfo in boneInfo.children) MapBoneRecursive(childInfo);
    }

    private void BuildSlots()
    {
        _slotRenderers.Clear();
        var categories = GetCategoriesFromData();
        categories.Insert(0, "Body");
        foreach (var category in categories.Distinct())
        {
            var slotGo = new GameObject($"[SLOT] {category}");
            slotGo.transform.SetParent(transform, false);
            _slotRenderers[category] = slotGo.AddComponent<SkinnedMeshRenderer>();
        }
    }

    private void ClearRenderers()
    {
        _slotRenderers.Clear();
        var renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        for (int i = renderers.Length - 1; i >= 0; i--)
        {
            if (renderers[i].gameObject.name.StartsWith("[SLOT]"))
            {
                if (Application.isPlaying) Destroy(renderers[i].gameObject);
                else DestroyImmediate(renderers[i].gameObject);
            }
        }
    }

    private void EquipAllPartsFromData()
    {
        if (!equippedParts.Any(e => e.category == "Body"))
        {
            equippedParts.Insert(0, new EquipmentEntry { category = "Body", partId = characterData.bodyData?.id });
        }
        foreach (var entry in equippedParts) EquipPart(entry.category, entry.partId);
    }

    public void EquipPart(string category, string partId)
    {
        if (!_slotRenderers.TryGetValue(category, out var targetRenderer)) return;
        BoneDataSO.SkinMeshData partData = GetPartDataById(category, partId);

        if (string.IsNullOrEmpty(partId) || partId.Equals("None", StringComparison.OrdinalIgnoreCase) || partData == null || partData.mesh == null)
        {
            targetRenderer.sharedMesh = null;
            targetRenderer.sharedMaterials = new Material[0];
            return;
        }

        targetRenderer.sharedMesh = partData.mesh;
        LoadAndApplyMaterials(targetRenderer, partData);
        var newBones = new Transform[partData.bonePaths.Length];
        for (int i = 0; i < partData.bonePaths.Length; i++)
        {
            if (!_boneMap.TryGetValue(partData.bonePaths[i], out newBones[i]))
                newBones[i] = transform;
        }
        targetRenderer.bones = newBones;

        if (!string.IsNullOrEmpty(partData.rootBonePath) && _boneMap.TryGetValue(partData.rootBonePath, out Transform rootBone))
            targetRenderer.rootBone = rootBone;
        else if (_skeletonRoot != null)
            targetRenderer.rootBone = _skeletonRoot;
    }

    private void LoadAndApplyMaterials(SkinnedMeshRenderer renderer, BoneDataSO.SkinMeshData partData)
    {
        if (partData.materialPaths == null || partData.materialPaths.Length == 0)
        {
            renderer.sharedMaterials = new Material[0];
            return;
        }
        var loadedMaterials = new Material[partData.materialPaths.Length];
#if UNITY_EDITOR
        for (int i = 0; i < partData.materialPaths.Length; i++)
        {
            loadedMaterials[i] = AssetDatabase.LoadAssetAtPath<Material>(partData.materialPaths[i]);
        }
#else
        Debug.LogError("Material loading tại runtime chưa được triển khai.");
#endif
        renderer.sharedMaterials = loadedMaterials;
    }

    public List<string> GetCategoriesFromData()
    {
        if (characterData == null) return new List<string>();
        return characterData.partCategories.Select(c => c.categoryName).Distinct().OrderBy(c => c).ToList();
    }

    public BoneDataSO.SkinMeshData GetPartDataById(string category, string partId)
    {
        if (characterData == null) return null;
        if (category == "Body") return characterData.bodyData;
        var foundCategory = characterData.partCategories.FirstOrDefault(c => c.categoryName == category);
        return foundCategory?.parts.FirstOrDefault(p => p.id == partId);
    }
}