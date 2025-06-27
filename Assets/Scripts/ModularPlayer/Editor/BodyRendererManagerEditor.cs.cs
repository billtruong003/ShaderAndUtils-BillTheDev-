// Đặt tại: Assets/Editor/ModularCharacterEditor.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(ModularCharacter))]
public class ModularCharacterEditor : Editor
{
    private ModularCharacter _target;
    private SerializedProperty _characterDataProp;
    private SerializedProperty _equippedPartsProp;

    private Dictionary<string, List<BoneDataSO.SkinMeshData>> _partsByCategory;
    private List<string> _categories;

    private void OnEnable()
    {
        _target = (ModularCharacter)target;
        _characterDataProp = serializedObject.FindProperty("characterData");
        _equippedPartsProp = serializedObject.FindProperty("equippedParts");

        CachePartData();
    }

    private void CachePartData()
    {
        _partsByCategory = new Dictionary<string, List<BoneDataSO.SkinMeshData>>();
        _categories = new List<string>();

        if (_target.characterData == null) return;

        _categories = _target.GetCategoriesFromData();

        foreach (var categoryName in _categories)
        {
            if (categoryName == "Body")
            {
                // Xử lý Body như một trường hợp đặc biệt
                _partsByCategory[categoryName] = new List<BoneDataSO.SkinMeshData> { _target.characterData.bodyData };
                continue;
            }

            var parts = _target.characterData.partCategories
                .Where(p => p.renderer != null && p.renderer.transform.parent != null && p.renderer.transform.parent.name == categoryName)
                .ToList();
            _partsByCategory[categoryName] = parts;
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("1. Configuration", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(_characterDataProp);
        if (EditorGUI.EndChangeCheck() || (_target.characterData != null && _categories.Count == 0))
        {
            serializedObject.ApplyModifiedProperties();
            CachePartData();
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("2. Build Controls", EditorStyles.boldLabel);
        if (GUILayout.Button("Rebuild Character", GUILayout.Height(30)))
        {
            _target.RebuildCharacter();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("3. Equipment", EditorStyles.boldLabel);

        if (_target.characterData == null || _categories == null || _categories.Count == 0)
        {
            EditorGUILayout.HelpBox("Assign Character Data and press 'Rebuild Character'.", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        foreach (var category in _categories)
        {
            DrawCategoryPopup(category);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug Data (Read-only)", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(_equippedPartsProp, true);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCategoryPopup(string category)
    {
        if (!_partsByCategory.TryGetValue(category, out var partsList)) return;

        // Tạo danh sách ID và tên hiển thị
        var partIds = partsList.Select(p => p.id).ToList();
        var displayNames = new List<string> { "None" };
        displayNames.AddRange(partsList.Select(p => p.id)); // Hiển thị ID gốc vì nó không có cấu trúc đẹp

        // Tìm lựa chọn hiện tại
        var currentEntry = _target.equippedParts.FirstOrDefault(e => e.category == category);
        int currentIndex = 0;

        if (currentEntry != null && !string.IsNullOrEmpty(currentEntry.partId) && !currentEntry.partId.Equals("None"))
        {
            currentIndex = partIds.IndexOf(currentEntry.partId) + 1;
        }

        EditorGUI.BeginChangeCheck();
        int newIndex = EditorGUILayout.Popup(category, currentIndex, displayNames.ToArray());

        if (EditorGUI.EndChangeCheck())
        {
            string newPartId = (newIndex == 0) ? "None" : partIds[newIndex - 1];

            if (currentEntry == null)
            {
                currentEntry = new ModularCharacter.EquipmentEntry { category = category };
                _target.equippedParts.Add(currentEntry);
            }
            currentEntry.partId = newPartId;

            _target.EquipPart(category, newPartId);

            EditorUtility.SetDirty(_target);
        }
    }
}