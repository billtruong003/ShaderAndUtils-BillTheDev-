using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private PlayerAnimator playerAnimator;
    [SerializeField] private Animator animator;

    [Header("Default State")]
    [Tooltip("Dữ liệu cho trạng thái Tay không. Đây là trạng thái mặc định và không thể bỏ trống.")]
    [SerializeField] private WeaponData unarmedWeaponData;

    // --- CÁC THUỘC TÍNH CÔNG KHAI ĐƯỢC THÊM VÀO ĐÂY ---
    public bool IsWeaponDrawn { get; private set; }
    public WeaponData CurrentWeapon { get; private set; }
    public WeaponData UnarmedWeaponData => unarmedWeaponData; // Thuộc tính chỉ đọc để truy cập an toàn

    void Awake()
    {
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
        IsWeaponDrawn = false; // Đảm bảo trạng thái ban đầu là cất vũ khí
    }

    public void ToggleWeaponStance()
    {
        IsWeaponDrawn = !IsWeaponDrawn;
        playerAnimator.SetWeaponDrawn(IsWeaponDrawn);
    }

    public void EquipWeapon(WeaponData newWeapon)
    {
        CurrentWeapon = newWeapon != null ? newWeapon : unarmedWeaponData;

        animator.runtimeAnimatorController = CurrentWeapon.AnimatorOverride;
        playerAnimator.SetWeaponType(CurrentWeapon.WeaponTypeID);
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
        if (CurrentWeapon != null && comboIndex < CurrentWeapon.AttackCombo.Length)
        {
            return CurrentWeapon.AttackCombo[comboIndex];
        }

        // Dòng này để phòng trường hợp CurrentWeapon là null, nhưng logic ở trên đã xử lý
        if (unarmedWeaponData != null && comboIndex < unarmedWeaponData.AttackCombo.Length)
        {
            return unarmedWeaponData.AttackCombo[comboIndex];
        }

        return null;
    }
}