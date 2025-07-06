// File: Assets/MyIndieGame/Scripts/Characters/Inventory/EquipmentManager.cs
using UnityEngine;
using System;

public class EquipmentManager : MonoBehaviour
{
    [Header("Default State")]
    [Tooltip("Dữ liệu cho trạng thái tấn công tay không. Bắt buộc phải có.")]
    [SerializeField] private WeaponData unarmedWeaponData;

    // --- TRẠNG THÁI HIỆN TẠI (Dữ liệu) ---
    public bool IsWeaponDrawn { get; private set; }
    public WeaponData CurrentWeapon { get; private set; }
    public WeaponData UnarmedWeaponData => unarmedWeaponData;

    // --- SỰ KIỆN (Để các hệ thống khác lắng nghe) ---
    // Thông báo khi có vũ khí mới được trang bị hoặc cởi bỏ.
    // Gửi đi: (dữ liệu vũ khí mới, dữ liệu vũ khí cũ)
    public event Action<WeaponData, WeaponData> OnEquipmentChanged;

    // Thông báo khi vũ khí được rút ra hoặc cất đi.
    // Gửi đi: (trạng thái rút/cất, dữ liệu vũ khí liên quan)
    public event Action<bool, WeaponData> OnWeaponStanceChanged;

    // --- THAM CHIẾU NỘI BỘ ---
    private PlayerAnimator playerAnimator;
    private Animator animator;
    private RuntimeAnimatorController originalAnimatorController;

    void Awake()
    {
        playerAnimator = GetComponentInChildren<PlayerAnimator>();
        animator = playerAnimator.GetAnimator();

        if (animator != null)
        {
            // Lưu lại Animator Controller gốc để có thể quay về khi cởi vũ khí.
            originalAnimatorController = animator.runtimeAnimatorController;
        }
        else
        {
            Debug.LogError("EquipmentManager could not find the Animator component on this GameObject!", this);
        }

        // Đảm bảo unarmedWeaponData được gán để tránh lỗi
        if (unarmedWeaponData == null)
        {
            Debug.LogError("CRITICAL: Unarmed Weapon Data is not assigned in the EquipmentManager!", this);
        }

        // Khởi tạo trạng thái ban đầu, nhưng chưa phát sự kiện
        CurrentWeapon = unarmedWeaponData;
    }

    void Start()
    {
        // Phát sự kiện ban đầu trong Start().
        // Tại thời điểm này, các hệ thống khác đã đăng ký lắng nghe sự kiện.
        OnEquipmentChanged?.Invoke(CurrentWeapon, null);
        SetWeaponDrawn(false); // Cập nhật trạng thái và Animator (tay không sẽ tự động thành true)
    }

    /// <summary>
    /// Trang bị một vũ khí mới. Nếu truyền vào null, sẽ quay về trạng thái tay không.
    /// </summary>
    /// <param name="newWeapon">Dữ liệu của vũ khí mới.</param>
    public void EquipWeapon(WeaponData newWeapon)
    {
        // Luôn đảm bảo có một vũ khí hợp lệ. Nếu newWeapon là null, unarmedWeaponData sẽ là phương án dự phòng.
        newWeapon = newWeapon ?? unarmedWeaponData;

        if (CurrentWeapon == newWeapon) return;

        Debug.Log($"<color=orange>EquipmentManager: Equipping '{newWeapon.name}'.</color>");

        WeaponData oldWeapon = CurrentWeapon;
        CurrentWeapon = newWeapon;

        ApplyAnimatorOverride();

        playerAnimator.SetWeaponType(CurrentWeapon.WeaponTypeID);

        // Phát sự kiện để báo cho các hệ thống khác (Visual & Weapon Controller)
        OnEquipmentChanged?.Invoke(CurrentWeapon, oldWeapon);

        // Khi trang bị vũ khí mới, luôn bắt đầu ở trạng thái cất đi
        bool shouldBeDrawn = (CurrentWeapon == unarmedWeaponData);
        SetWeaponDrawn(shouldBeDrawn);
    }

    /// <summary>
    /// Thay đổi trạng thái rút/cất vũ khí.
    /// </summary>
    public void ToggleWeaponStance()
    {
        // Không cho phép cất vũ khí tay không
        if (CurrentWeapon == unarmedWeaponData) return;

        SetWeaponDrawn(!IsWeaponDrawn);
    }

    /// <summary>
    /// Lấy dữ liệu của đòn tấn công trong chuỗi combo hiện tại.
    /// </summary>
    public AttackData GetCurrentAttackData(int comboIndex)
    {
        return CurrentWeapon?.GetAttackData(comboIndex);
    }

    /// <summary>
    /// Hàm nội bộ để áp dụng Animator Override Controller.
    /// </summary>
    private void ApplyAnimatorOverride()
    {
        if (animator == null) return;

        // Nếu vũ khí mới có override, áp dụng nó.
        if (CurrentWeapon != null && CurrentWeapon.AnimatorOverride != null)
        {
            Debug.Log($"<color=lightblue>Applying Animator Override: '{CurrentWeapon.AnimatorOverride.name}'</color>");
            animator.runtimeAnimatorController = CurrentWeapon.AnimatorOverride;
        }
        // Nếu không, quay về controller gốc.
        else
        {
            Debug.Log("<color=lightblue>Reverting to Original Animator Controller.</color>");
            animator.runtimeAnimatorController = originalAnimatorController;
        }
    }

    /// <summary>
    /// Hàm nội bộ để thiết lập trạng thái rút/cất và phát sự kiện.
    /// </summary>
    private void SetWeaponDrawn(bool isDrawn)
    {
        // Logic đặc biệt: Tay không luôn được coi là "rút ra"
        if (CurrentWeapon == unarmedWeaponData)
        {
            isDrawn = true;
        }

        IsWeaponDrawn = isDrawn;
        playerAnimator.SetWeaponDrawn(IsWeaponDrawn);

        // Phát sự kiện để Visual Controller di chuyển model vũ khí
        OnWeaponStanceChanged?.Invoke(IsWeaponDrawn, CurrentWeapon);
    }
}