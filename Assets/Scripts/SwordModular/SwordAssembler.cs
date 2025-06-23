using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor; // Cần thiết cho việc tạo Asset và các tính năng trong Editor
#endif

// Enum SwordPartType được giữ nguyên
public enum SwordPartType { Blade, Hilt, Grip }

[AddComponentMenu("Modular Swords/Sword Assembler")]
public class SwordAssembler : MonoBehaviour
{
    // =======================================================================
    // TAB 1: LIVE CUSTOMIZATION - Tùy chỉnh trực quan trong Inspector
    // =======================================================================

    [TabGroup("MainTabs", "Live Customization")]
    [Title("Data & Targets", bold: false)]
    [Required, AssetsOnly]
    public SwordPartCollectionSO PartCollection;

    [HorizontalGroup("MainTabs/Live Customization/Targets")]
    [SceneObjectsOnly, LabelWidth(40)] public MeshFilter BladeMeshFilter;
    [HorizontalGroup("MainTabs/Live Customization/Targets")]
    [SceneObjectsOnly, LabelWidth(30)] public MeshFilter HiltMeshFilter;
    [HorizontalGroup("MainTabs/Live Customization/Targets")]
    [SceneObjectsOnly, LabelWidth(30)] public MeshFilter GripMeshFilter;

    [PropertySpace(10)]
    [TabGroup("MainTabs", "Live Customization")]
    [Title("Part Selection", bold: false)]
    [HorizontalGroup("MainTabs/Live Customization/Selectors", Width = 0.33f, LabelWidth = 50, MarginLeft = 5, MarginRight = 5)]
    [ValueDropdown("GetBladeOptions"), OnValueChanged("RequestDelayedUpdate"), HideLabel]
    [SerializeField] private int bladeIndex = -1;

    [HorizontalGroup("MainTabs/Live Customization/Selectors")]
    [ValueDropdown("GetHiltOptions"), OnValueChanged("RequestDelayedUpdate"), HideLabel]
    [SerializeField] private int hiltIndex = -1;

    [HorizontalGroup("MainTabs/Live Customization/Selectors")]
    [ValueDropdown("GetGripOptions"), OnValueChanged("RequestDelayedUpdate"), HideLabel]
    [SerializeField] private int gripIndex = -1;

    [PropertySpace(10)]
    [TabGroup("MainTabs", "Live Customization")]
    [Title("Part Modifiers", "Tinh chỉnh vị trí và góc xoay của từng bộ phận.", bold: false)]

    // --- BỐ CỤC MỚI: Dùng HorizontalGroup chứa các BoxGroup để tạo các cột đồng đều ---
    [HorizontalGroup("MainTabs/Live Customization/ModifiersGrid", LabelWidth = 70)]

    // --- CỘT BLADE ---
    [BoxGroup("MainTabs/Live Customization/ModifiersGrid/Blade", LabelText = "Blade")]
    [OnValueChanged("RequestDelayedUpdate"), Range(-1, 1)]
    public float BladeOffsetValue = 0f;
    [BoxGroup("MainTabs/Live Customization/ModifiersGrid/Blade")]
    [SuffixLabel("units", true)] public float BladeOffsetLimit = 0.5f;
    [BoxGroup("MainTabs/Live Customization/ModifiersGrid/Blade")]
    [OnValueChanged("RequestDelayedUpdate"), Range(-180, 180)]
    public float BladeRotationValue = 0f;

    // --- CỘT HILT ---
    [BoxGroup("MainTabs/Live Customization/ModifiersGrid/Hilt", LabelText = "Hilt")]
    [OnValueChanged("RequestDelayedUpdate"), Range(-180, 180)]
    public float HiltRotationValue = 0f;

    // --- CỘT GRIP ---
    [BoxGroup("MainTabs/Live Customization/ModifiersGrid/Grip", LabelText = "Grip")]
    [OnValueChanged("RequestDelayedUpdate"), Range(-1, 1)]
    public float GripOffsetValue = 0f;
    [BoxGroup("MainTabs/Live Customization/ModifiersGrid/Grip")]
    [SuffixLabel("units", true)] public float GripOffsetLimit = 0.5f;
    [BoxGroup("MainTabs/Live Customization/ModifiersGrid/Grip")]
    [OnValueChanged("RequestDelayedUpdate"), Range(-180, 180)]
    public float GripRotationValue = 0f;


    // =======================================================================
    // TAB 2: PRESET CREATOR - Công cụ tạo Preset
    // =======================================================================
#if UNITY_EDITOR
    [TabGroup("MainTabs", "Preset Creator")]
    [InfoBox("Quy trình tạo Preset:\n1. Dùng các công cụ trong tab 'Live Customization' để thiết kế một thanh kiếm ưng ý.\n2. Đặt tên cho Preset ở bên dưới.\n3. Nhấn nút 'Save' để tạo file ScriptableObject mới.", InfoMessageType.Info)]
    [BoxGroup("MainTabs/Preset Creator/Creator")]
    [Tooltip("Tên file cho preset mới. Ví dụ: 'Elven_Kings_Blade'")]
    [LabelWidth(100)]
    public string presetName;

    [BoxGroup("MainTabs/Preset Creator/Creator")]
    [Tooltip("Thư mục để lưu file preset. Mặc định là 'Assets/'.")]
    [FolderPath]
    [LabelWidth(100)]
    public string presetSavePath = "Assets/";

    [BoxGroup("MainTabs/Preset Creator/Creator")]
    [Button("Save Current Design as Preset", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
    public void CreatePresetFromCurrent()
    {
        if (string.IsNullOrWhiteSpace(presetName))
        {
            Debug.LogError("Vui lòng đặt tên cho preset trước khi lưu!");
            return;
        }

        SwordPresetSO preset = ScriptableObject.CreateInstance<SwordPresetSO>();

        preset.BladeIndex = this.bladeIndex;
        preset.HiltIndex = this.hiltIndex;
        preset.GripIndex = this.gripIndex;

        preset.BladeOffsetValue = this.BladeOffsetValue;
        preset.GripOffsetValue = this.GripOffsetValue;
        preset.BladeRotationValue = this.BladeRotationValue;
        preset.HiltRotationValue = this.HiltRotationValue;
        preset.GripRotationValue = this.GripRotationValue;

        string path = $"{presetSavePath}/{presetName}.asset";
        string uniquePath = AssetDatabase.GenerateUniqueAssetPath(path);

        AssetDatabase.CreateAsset(preset, uniquePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = preset;

        Debug.Log($"<color=lime>Preset '{presetName}' đã được lưu thành công tại: {uniquePath}</color>");
    }
#endif

    #region PUBLIC API

    public void SetPart(SwordPartType partType, int newIndex)
    {
        switch (partType)
        {
            case SwordPartType.Blade: bladeIndex = newIndex; break;
            case SwordPartType.Hilt: hiltIndex = newIndex; break;
            case SwordPartType.Grip: gripIndex = newIndex; break;
        }
        UpdateSword();
    }

    public int GetPartIndex(SwordPartType partType)
    {
        switch (partType)
        {
            case SwordPartType.Blade: return bladeIndex;
            case SwordPartType.Hilt: return hiltIndex;
            case SwordPartType.Grip: return gripIndex;
            default: return -1;
        }
    }

    public int GetPartCount(SwordPartType partType)
    {
        if (PartCollection == null) return 0;
        switch (partType)
        {
            case SwordPartType.Blade: return PartCollection.Blades.MeshDefinitions.Count;
            case SwordPartType.Hilt: return PartCollection.Hilts.MeshDefinitions.Count;
            case SwordPartType.Grip: return PartCollection.Grips.MeshDefinitions.Count;
            default: return 0;
        }
    }

    public SwordMeshDefinition GetPartDefinition(SwordPartType partType, int index)
    {
        if (PartCollection == null || index < 0) return null;
        List<SwordMeshDefinition> definitions = null;
        switch (partType)
        {
            case SwordPartType.Blade: definitions = PartCollection.Blades.MeshDefinitions; break;
            case SwordPartType.Hilt: definitions = PartCollection.Hilts.MeshDefinitions; break;
            case SwordPartType.Grip: definitions = PartCollection.Grips.MeshDefinitions; break;
        }
        return (definitions != null && index < definitions.Count) ? definitions[index] : null;
    }

    public void HighlightPart(SwordPartType partType, bool highlight)
    {
        var controller = GetPartController(partType);
        if (controller != null && controller.gameObject.activeInHierarchy)
        {
            controller.SetOutline(highlight);
        }
    }

    public InteractiveToonController GetPartController(SwordPartType partType)
    {
        MeshFilter targetFilter = null;
        switch (partType)
        {
            case SwordPartType.Blade: targetFilter = BladeMeshFilter; break;
            case SwordPartType.Hilt: targetFilter = HiltMeshFilter; break;
            case SwordPartType.Grip: targetFilter = GripMeshFilter; break;
        }
        return targetFilter?.GetComponent<InteractiveToonController>();
    }

    public void SetBladeOffset(float value) { BladeOffsetValue = Mathf.Clamp(value, -1, 1); UpdateSword(); }
    public void SetGripOffset(float value) { GripOffsetValue = Mathf.Clamp(value, -1, 1); UpdateSword(); }
    public void SetBladeRotation(float degrees) { BladeRotationValue = degrees; UpdateSword(); }
    public void SetHiltRotation(float degrees) { HiltRotationValue = degrees; UpdateSword(); }
    public void SetGripRotation(float degrees) { GripRotationValue = degrees; UpdateSword(); }

    public float GetPartRotation(SwordPartType partType)
    {
        switch (partType)
        {
            case SwordPartType.Blade: return BladeRotationValue;
            case SwordPartType.Hilt: return HiltRotationValue;
            case SwordPartType.Grip: return GripRotationValue;
            default: return 0f;
        }
    }

    public void ApplyPreset(SwordPresetSO preset)
    {
        if (preset == null) return;
        bladeIndex = preset.BladeIndex;
        hiltIndex = preset.HiltIndex;
        gripIndex = preset.GripIndex;
        BladeOffsetValue = preset.BladeOffsetValue;
        GripOffsetValue = preset.GripOffsetValue;
        BladeRotationValue = preset.BladeRotationValue;
        HiltRotationValue = preset.HiltRotationValue;
        GripRotationValue = preset.GripRotationValue;
        UpdateSword();
    }

    public List<int> GetPartIndicesByTheme(SwordPartType partType, SwordTheme theme)
    {
        var indices = new List<int>();
        if (PartCollection == null) return indices;
        List<SwordMeshDefinition> definitions = null;
        switch (partType)
        {
            case SwordPartType.Blade: definitions = PartCollection.Blades.MeshDefinitions; break;
            case SwordPartType.Hilt: definitions = PartCollection.Hilts.MeshDefinitions; break;
            case SwordPartType.Grip: definitions = PartCollection.Grips.MeshDefinitions; break;
        }
        if (definitions != null)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i].PartTheme == theme)
                {
                    indices.Add(i);
                }
            }
        }
        return indices;
    }
    #endregion

    [Button(ButtonSizes.Large, Name = "Force Update Visuals")]
    [TabGroup("MainTabs", "Live Customization")]
    public void UpdateSword()
    {
        if (PartCollection == null) return;
        UpdatePart(BladeMeshFilter, PartCollection.Blades.MeshDefinitions, bladeIndex, -BladeOffsetValue * BladeOffsetLimit, BladeRotationValue);
        UpdatePart(HiltMeshFilter, PartCollection.Hilts.MeshDefinitions, hiltIndex, 0, HiltRotationValue);
        UpdatePart(GripMeshFilter, PartCollection.Grips.MeshDefinitions, gripIndex, GripOffsetValue * GripOffsetLimit, GripRotationValue);
    }

    private void UpdatePart(MeshFilter targetFilter, List<SwordMeshDefinition> definitions, int index, float zOffset, float zRotation)
    {
        if (targetFilter == null) return;
        bool isValidIndex = index >= 0 && index < definitions.Count;
        if (targetFilter.gameObject.activeSelf != isValidIndex) targetFilter.gameObject.SetActive(isValidIndex);

        if (isValidIndex)
        {
            var selectedPart = definitions[index];
            if (targetFilter.sharedMesh != selectedPart.Mesh) targetFilter.sharedMesh = selectedPart.Mesh;
            var renderer = targetFilter.GetComponent<MeshRenderer>();
            if (renderer != null && renderer.sharedMaterial != selectedPart.DefaultMaterial)
            {
                renderer.sharedMaterial = selectedPart.DefaultMaterial;
            }
            targetFilter.transform.localPosition = new Vector3(0, 0, zOffset);
            targetFilter.transform.localRotation = Quaternion.Euler(0, 0, zRotation);
        }
    }

    private void OnValidate() { RequestDelayedUpdate(); }

    private void RequestDelayedUpdate()
    {
#if UNITY_EDITOR
        EditorApplication.delayCall += UpdateSwordOnce;
#endif
    }
    private void UpdateSwordOnce()
    {
#if UNITY_EDITOR
        EditorApplication.delayCall -= UpdateSwordOnce;
        if (this != null) UpdateSword();
#endif
    }

    #region Odin Dropdowns
    private IEnumerable<ValueDropdownItem<int>> GetBladeOptions() => CreateDropdownOptions(PartCollection?.Blades.MeshDefinitions, "Blade");
    private IEnumerable<ValueDropdownItem<int>> GetHiltOptions() => CreateDropdownOptions(PartCollection?.Hilts.MeshDefinitions, "Hilt");
    private IEnumerable<ValueDropdownItem<int>> GetGripOptions() => CreateDropdownOptions(PartCollection?.Grips.MeshDefinitions, "Grip");
    private IEnumerable<ValueDropdownItem<int>> CreateDropdownOptions(List<SwordMeshDefinition> definitions, string partName)
    {
        yield return new ValueDropdownItem<int>($"None ({partName})", -1);
        if (definitions == null) yield break;
        for (int i = 0; i < definitions.Count; i++)
        {
            string meshName = definitions[i].Mesh?.name ?? "Not Set";
            yield return new ValueDropdownItem<int>($"{i}: {meshName}", i);
        }
    }
    #endregion
}