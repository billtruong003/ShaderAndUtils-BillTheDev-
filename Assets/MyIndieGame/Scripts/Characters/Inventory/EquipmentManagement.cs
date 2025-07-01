using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    [Header("Component References")]
    // Giữ lại PlayerAnimator để gọi các hàm tùy chỉnh như SetWeaponDrawn
    [SerializeField] private PlayerAnimator playerAnimator;
    // Thêm tham chiếu trực tiếp đến Animator để thay đổi Controller
    [SerializeField] private Animator animator;

    [Header("Default State")]
    [Tooltip("Dữ liệu cho trạng thái Tay không. Đây là trạng thái mặc định và không thể bỏ trống.")]
    [SerializeField] private WeaponData unarmedWeaponData;

    public bool IsWeaponDrawn { get; private set; }
    private WeaponData currentWeapon;

    void Awake()
    {
        // Kiểm tra lỗi sớm
        if (unarmedWeaponData == null)
        {
            Debug.LogError("Unarmed Weapon Data is not set on the EquipmentManager!", this);
            this.enabled = false;
            return;
        }
        if (animator == null)
        {
            Debug.LogError("Animator is not set on the EquipmentManager!", this);
            this.enabled = false;
            return;
        }

        // Khởi đầu với trạng thái tay không
        EquipWeapon(unarmedWeaponData);
    }

    public void ToggleWeaponStance()
    {
        IsWeaponDrawn = !IsWeaponDrawn;
        // Dùng PlayerAnimator cho các hàm tùy chỉnh
        playerAnimator.SetWeaponDrawn(IsWeaponDrawn);
    }

    public void EquipWeapon(WeaponData newWeapon)
    {
        currentWeapon = newWeapon != null ? newWeapon : unarmedWeaponData;

        // --- ĐÂY LÀ DÒNG ĐÃ SỬA ---
        // Dùng Animator thật để thay đổi runtimeAnimatorController
        animator.runtimeAnimatorController = currentWeapon.AnimatorOverride;

        // Dùng PlayerAnimator cho các hàm tùy chỉnh
        playerAnimator.SetWeaponType(currentWeapon.WeaponTypeID);
    }

    public void UnequipWeapon()
    {
        if (IsWeaponDrawn)
        {
            ToggleWeaponStance();
        }
        EquipWeapon(unarmedWeaponData);
    }

    public AttackData GetCurrentAttackData(int comboIndex)
    {
        if (currentWeapon != null && comboIndex < currentWeapon.AttackCombo.Length)
        {
            return currentWeapon.AttackCombo[comboIndex];
        }

        if (unarmedWeaponData != null && comboIndex < unarmedWeaponData.AttackCombo.Length)
        {
            return unarmedWeaponData.AttackCombo[comboIndex];
        }

        return null;
    }
}