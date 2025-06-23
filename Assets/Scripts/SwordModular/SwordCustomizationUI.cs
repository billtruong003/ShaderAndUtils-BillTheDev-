using UnityEngine;
using UnityEngine.UI; // Cần thiết cho các component UI như Toggle
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Script quản lý chính cho toàn bộ giao diện người dùng tùy biến kiếm.
/// Điều phối hoạt động giữa các UI element và SwordAssembler.
/// </summary>
public class SwordCustomizationUI : MonoBehaviour
{
    [Header("Core References")]
    [Tooltip("Gán Sword Assembler đang điều khiển thanh kiếm trong scene.")]
    [SerializeField] private SwordAssembler swordAssembler;

    [Header("UI Components")]
    [Tooltip("Gán các GameObject chứa script UIPartSelector vào đây.")]
    [SerializeField] private UIPartSelector[] partSelectors;

    [Tooltip("Gán các GameObject chứa script UIPartOffsetSlider vào đây.")]
    [SerializeField] private UIPartOffsetSlider[] offsetSliders;

    [Tooltip("Gán các GameObject chứa script UIPartRotateSlider vào đây.")]
    [SerializeField] private UIPartRotateSlider[] rotateSliders;

    [Header("Speed-Up Features")]
    [Tooltip("Danh sách các thiết kế mẫu có sẵn để duyệt qua.")]
    [SerializeField] private List<SwordPresetSO> availablePresets;
    private int currentPresetIndex = -1;

    [Header("Locking Toggles")]
    [Tooltip("Gán Toggle để khóa/mở khóa Blade.")]
    [SerializeField] private Toggle bladeLockToggle;
    [Tooltip("Gán Toggle để khóa/mở khóa Hilt.")]
    [SerializeField] private Toggle hiltLockToggle;
    [Tooltip("Gán Toggle để khóa/mở khóa Grip.")]
    [SerializeField] private Toggle gripLockToggle;

    private bool isBladeLocked = false;
    private bool isHiltLocked = false;
    private bool isGripLocked = false;

    private SwordPartType currentCategory = SwordPartType.Blade;
    private SwordPartType lastSelectedCategory;

    void Start()
    {
        if (swordAssembler == null)
        {
            Debug.LogError("Chưa gán Sword Assembler! UI sẽ không hoạt động.", this);
            this.enabled = false;
            return;
        }

        InitializeAllUI();
        SetupToggleListeners();

        lastSelectedCategory = currentCategory;
        SelectCategory((int)currentCategory);
    }

    /// <summary>
    /// Khởi tạo hoặc làm mới tất cả các UI element.
    /// </summary>
    private void InitializeAllUI()
    {
        foreach (var selector in partSelectors) { if (selector != null) selector.Initialize(swordAssembler); }
        foreach (var slider in offsetSliders) { if (slider != null) slider.Initialize(swordAssembler); }
        foreach (var slider in rotateSliders) { if (slider != null) slider.Initialize(swordAssembler); }
    }

    /// <summary>
    /// Đăng ký listener cho các Toggle khóa.
    /// </summary>
    private void SetupToggleListeners()
    {
        if (bladeLockToggle != null) bladeLockToggle.onValueChanged.AddListener(value => isBladeLocked = value);
        if (hiltLockToggle != null) hiltLockToggle.onValueChanged.AddListener(value => isHiltLocked = value);
        if (gripLockToggle != null) gripLockToggle.onValueChanged.AddListener(value => isGripLocked = value);
    }

    /// <summary>
    /// Chọn một loại bộ phận để tùy chỉnh và làm nổi bật nó.
    /// </summary>
    public void SelectCategory(int categoryIndex)
    {
        swordAssembler.HighlightPart(lastSelectedCategory, false);
        currentCategory = (SwordPartType)categoryIndex;
        swordAssembler.HighlightPart(currentCategory, true);
        lastSelectedCategory = currentCategory;
    }

    /// <summary>
    /// Chuyển sang loại bộ phận tiếp theo.
    /// </summary>
    public void CycleNextCategory()
    {
        int nextCategoryIndex = ((int)currentCategory + 1) % System.Enum.GetValues(typeof(SwordPartType)).Length;
        SelectCategory(nextCategoryIndex);
    }

    /// <summary>
    /// Lưu cấu hình hiện tại (hiện chỉ log ra console).
    /// </summary>
    public void SaveConfiguration()
    {
        int blade = swordAssembler.GetPartIndex(SwordPartType.Blade);
        int hilt = swordAssembler.GetPartIndex(SwordPartType.Hilt);
        int grip = swordAssembler.GetPartIndex(SwordPartType.Grip);
        string config = $"Saved Sword: Blade({blade}), Hilt({hilt}), Grip({grip})";
        Debug.Log(config);

        foreach (SwordPartType partType in System.Enum.GetValues(typeof(SwordPartType)))
        {
            if (swordAssembler.GetPartIndex(partType) != -1)
            {
                var controller = swordAssembler.GetPartController(partType);
                if (controller != null)
                {
                    controller.TriggerFlash();
                }
            }
        }
    }

    #region Speed-Up Button Functions

    /// <summary>
    /// NÚT 1: Chỉ ngẫu nhiên hóa các bộ phận, không ngẫu nhiên thông số.
    /// </summary>
    public void RandomizeAll()
    {
        // --- BƯỚC 1: Ngẫu nhiên hóa các bộ phận (mesh) ---
        foreach (SwordPartType partType in System.Enum.GetValues(typeof(SwordPartType)))
        {
            int partCount = swordAssembler.GetPartCount(partType);
            swordAssembler.SetPart(partType, Random.Range(-1, partCount));
        }

        // --- BƯỚC 2: Đặt lại tất cả các thông số (slider) về 0 ---
        swordAssembler.SetBladeOffset(0f);
        swordAssembler.SetGripOffset(0f);
        swordAssembler.SetBladeRotation(0f);
        swordAssembler.SetHiltRotation(0f);
        swordAssembler.SetGripRotation(0f);

        // --- BƯỚC 3: Cập nhật lại toàn bộ UI để hiển thị thay đổi ---
        InitializeAllUI();
    }

    /// <summary>
    /// NÚT 2: Tạo một bộ kiếm khớp với một chủ đề ngẫu nhiên.
    /// </summary>
    public void GenerateThemedSet()
    {
        var allThemes = System.Enum.GetValues(typeof(SwordTheme)).Cast<SwordTheme>()
                              .Where(t => t != SwordTheme.None).ToList();
        if (allThemes.Count == 0)
        {
            Debug.LogWarning("Không có chủ đề nào được định nghĩa. Chạy RandomizeAll thay thế.");
            RandomizeAll();
            return;
        }

        SwordTheme randomTheme = allThemes[Random.Range(0, allThemes.Count)];
        Debug.Log($"Generating set for theme: {randomTheme}");

        foreach (SwordPartType partType in System.Enum.GetValues(typeof(SwordPartType)))
        {
            List<int> themedIndices = swordAssembler.GetPartIndicesByTheme(partType, randomTheme);
            swordAssembler.SetPart(partType, themedIndices.Count > 0 ? themedIndices[Random.Range(0, themedIndices.Count)] : -1);
        }

        // Sau khi chọn bộ phận theo chủ đề, cũng đặt lại các slider về 0
        swordAssembler.SetBladeOffset(0);
        swordAssembler.SetGripOffset(0);
        swordAssembler.SetBladeRotation(0);
        swordAssembler.SetHiltRotation(0);
        swordAssembler.SetGripRotation(0);

        InitializeAllUI();
    }

    /// <summary>
    /// NÚT 3: Tải thiết kế mẫu tiếp theo trong danh sách.
    /// </summary>
    public void LoadNextPreset()
    {
        if (availablePresets == null || availablePresets.Count == 0)
        {
            Debug.LogWarning("Không có Preset nào được gán vào SwordCustomizationUI.");
            return;
        }

        currentPresetIndex = (currentPresetIndex + 1) % availablePresets.Count;
        SwordPresetSO preset = availablePresets[currentPresetIndex];
        swordAssembler.ApplyPreset(preset);

        InitializeAllUI();
    }

    /// <summary>
    /// NÚT 4: Đặt lại tất cả về trạng thái mặc định.
    /// </summary>
    public void ResetToDefault()
    {
        swordAssembler.SetPart(SwordPartType.Blade, -1);
        swordAssembler.SetPart(SwordPartType.Hilt, -1);
        swordAssembler.SetPart(SwordPartType.Grip, -1);

        swordAssembler.SetBladeOffset(0);
        swordAssembler.SetGripOffset(0);
        swordAssembler.SetBladeRotation(0);
        swordAssembler.SetHiltRotation(0);
        swordAssembler.SetGripRotation(0);

        InitializeAllUI();
    }

    /// <summary>
    /// NÚT 5: Ngẫu nhiên hóa các bộ phận chưa bị khóa.
    /// </summary>
    public void RandomizeUnlockedParts()
    {
        if (!isBladeLocked)
        {
            int partCount = swordAssembler.GetPartCount(SwordPartType.Blade);
            swordAssembler.SetPart(SwordPartType.Blade, Random.Range(-1, partCount));
            swordAssembler.SetBladeOffset(0f); // Đặt lại về 0
            swordAssembler.SetBladeRotation(0f); // Đặt lại về 0
        }

        if (!isHiltLocked)
        {
            int partCount = swordAssembler.GetPartCount(SwordPartType.Hilt);
            swordAssembler.SetPart(SwordPartType.Hilt, Random.Range(-1, partCount));
            swordAssembler.SetHiltRotation(0f); // Đặt lại về 0
        }

        if (!isGripLocked)
        {
            int partCount = swordAssembler.GetPartCount(SwordPartType.Grip);
            swordAssembler.SetPart(SwordPartType.Grip, Random.Range(-1, partCount));
            swordAssembler.SetGripOffset(0f); // Đặt lại về 0
            swordAssembler.SetGripRotation(0f); // Đặt lại về 0
        }

        InitializeAllUI();
    }

    /// <summary>
    /// NÚT 6: Lật ngược các giá trị của slider.
    /// </summary>
    public void MirrorDesign()
    {
        swordAssembler.SetBladeOffset(1.0f - swordAssembler.BladeOffsetValue);
        swordAssembler.SetGripOffset(1.0f - swordAssembler.GripOffsetValue);

        swordAssembler.SetBladeRotation(-swordAssembler.BladeRotationValue);
        swordAssembler.SetHiltRotation(-swordAssembler.HiltRotationValue);
        swordAssembler.SetGripRotation(-swordAssembler.GripRotationValue);

        InitializeAllUI();
    }

    #endregion
}