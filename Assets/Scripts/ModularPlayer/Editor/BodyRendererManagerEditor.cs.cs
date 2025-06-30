// File: Assets/Scripts/ModularSystem/Editor/ModularCharacterEditor.cs
// (Phải nằm trong một thư mục có tên "Editor")

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Utils.Bill.InspectorCustom;

[CustomEditor(typeof(ModularCharacter))]
public class ModularCharacterEditor : BillUtilsBaseEditor
{
    private ModularCharacter _target;
    private Dictionary<string, List<BoneDataSO.SkinMeshData>> _partsByCategory;
    private List<string> _allCategories;
    private SerializedProperty _characterDataProp;
    private SerializedProperty _chanceForNoneProp;

    protected override void OnEnable()
    {
        base.OnEnable();
        _target = (ModularCharacter)target;

        _characterDataProp = serializedObject.FindProperty("characterData");
        _chanceForNoneProp = serializedObject.FindProperty("chanceForNone");

        CachePartData();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();

        // ====================================================================
        // === THAY ĐỔI QUAN TRỌNG Ở ĐÂY ===
        // Sử dụng overload có tham số `includeChildren` để đảm bảo PropertyDrawer được kích hoạt đúng.
        EditorGUILayout.PropertyField(_characterDataProp, true);
        EditorGUILayout.PropertyField(_chanceForNoneProp, true);
        // ====================================================================

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            CachePartData();
        }

        DrawEquipmentSelector();

        EditorGUILayout.Space();
        base.DrawCustomButtons();
    }

    private void DrawEquipmentSelector()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Equipment Selector", EditorStyles.boldLabel);

        if (_target.characterData == null || _allCategories == null || _allCategories.Count == 0)
        {
            EditorGUILayout.HelpBox("Gán Character Data và nhấn 'Rebuild Character'.", MessageType.Info);
            return;
        }

        if (_target.GetCategoriesFromData().Count + 1 != _allCategories.Count)
        {
            CachePartData();
        }

        foreach (var category in _allCategories)
        {
            DrawCategoryPopup(category);
        }
    }

    private void CachePartData()
    {
        _partsByCategory = new Dictionary<string, List<BoneDataSO.SkinMeshData>>();
        _allCategories = new List<string>();

        if (_target.characterData == null) return;

        _allCategories.Add("Body");
        if (_target.characterData.bodyData != null && _target.characterData.bodyData.mesh != null)
        {
            _partsByCategory["Body"] = new List<BoneDataSO.SkinMeshData> { _target.characterData.bodyData };
        }

        foreach (var category in _target.characterData.partCategories)
        {
            _allCategories.Add(category.categoryName);
            _partsByCategory[category.categoryName] = category.parts;
        }

        _allCategories = _allCategories.Distinct().OrderBy(c => c == "Body" ? 0 : 1).ThenBy(c => c).ToList();
    }

    private void DrawCategoryPopup(string category)
    {
        _partsByCategory.TryGetValue(category, out var partsList);
        partsList = partsList ?? new List<BoneDataSO.SkinMeshData>();

        var displayNames = new List<string> { "None" };
        displayNames.AddRange(partsList.Select(p => p.id));

        var currentEntry = _target.equippedParts.FirstOrDefault(e => e.category == category);
        string currentPartId = currentEntry?.partId ?? "None";
        int currentIndex = displayNames.IndexOf(currentPartId);
        if (currentIndex == -1) currentIndex = 0;

        bool isBodyCategory = category.Equals("Body", System.StringComparison.OrdinalIgnoreCase);
        if (isBodyCategory)
        {
            EditorGUI.BeginDisabledGroup(true);
        }

        EditorGUI.BeginChangeCheck();
        int newIndex = EditorGUILayout.Popup(category, currentIndex, displayNames.ToArray());

        if (isBodyCategory)
        {
            EditorGUI.EndDisabledGroup();
        }

        if (EditorGUI.EndChangeCheck())
        {
            string newPartId = displayNames[newIndex];

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
#endif