// File: EquipmentManager.cs (Tái cấu trúc lớn)
using UnityEngine;
using System.Collections.Generic;

public class EquipmentManager : MonoBehaviour
{
    // Lớp nội bộ để dễ dàng cấu hình trong Inspector
    [System.Serializable]
    public class BodyPartCastPointMapping
    {
        public BodyPart bodyPart;
        public Transform castPoint;
    }

    [Header("Component References")]
    [SerializeField] private PlayerAnimator playerAnimator;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform weaponMountPoint;

    [Header("Default State")]
    [SerializeField] private WeaponData unarmedWeaponData;

    [Header("Unarmed Cast Points")]
    [Tooltip("Gán các điểm cast trên cơ thể Player vào các BodyPart tương ứng.")]
    [SerializeField] private List<BodyPartCastPointMapping> unarmedCastPointMappings;

    // --- THAY ĐỔI TỪ LIST SANG DICTIONARY ---
    // Từ điển này sẽ lưu trữ tất cả các điểm cast đang hoạt động (cả tay không và vũ khí)
    // Key: BodyPart, Value: Danh sách các Transform điểm cast cho bộ phận đó.
    public Dictionary<BodyPart, List<Transform>> AllCastPoints { get; private set; } = new Dictionary<BodyPart, List<Transform>>();

    public bool IsWeaponDrawn { get; private set; }
    public WeaponData CurrentWeapon { get; private set; }
    public WeaponData UnarmedWeaponData => unarmedWeaponData;

    private GameObject currentWeaponInstance;

    void Awake()
    {
        if (unarmedWeaponData == null) Debug.LogError("Unarmed Weapon Data is not set!", this);
        if (weaponMountPoint == null) Debug.LogError("Weapon Mount Point is not set!", this);

        // Khởi tạo từ điển
        InitializeCastPointDictionary();

        EquipWeapon(unarmedWeaponData);
        IsWeaponDrawn = false;
    }

    private void InitializeCastPointDictionary()
    {
        AllCastPoints.Clear();
        // Duyệt qua tất cả các giá trị của enum BodyPart để tạo các list rỗng
        foreach (BodyPart part in System.Enum.GetValues(typeof(BodyPart)))
        {
            AllCastPoints[part] = new List<Transform>();
        }

        // Điền dữ liệu từ các điểm cast tay không đã gán trong Inspector
        foreach (var mapping in unarmedCastPointMappings)
        {
            if (mapping.castPoint != null)
            {
                AllCastPoints[mapping.bodyPart].Add(mapping.castPoint);
            }
        }
    }

    public void EquipWeapon(WeaponData newWeapon)
    {
        // Xóa các điểm cast của vũ khí cũ
        if (currentWeaponInstance != null)
        {
            AllCastPoints[BodyPart.Weapon_Primary].Clear();
            AllCastPoints[BodyPart.Weapon_Secondary].Clear();
            Destroy(currentWeaponInstance);
        }

        CurrentWeapon = newWeapon != null ? newWeapon : unarmedWeaponData;
        animator.runtimeAnimatorController = CurrentWeapon.AnimatorOverride ?? animator.runtimeAnimatorController;
        playerAnimator.SetWeaponType(CurrentWeapon.WeaponTypeID);

        // Nếu là vũ khí thật, tạo instance và tìm các điểm cast trên model
        if (CurrentWeapon != unarmedWeaponData && CurrentWeapon.WeaponModelPrefab != null)
        {
            currentWeaponInstance = Instantiate(CurrentWeapon.WeaponModelPrefab, weaponMountPoint);
            FindCastPointsOnModel(currentWeaponInstance.transform);
        }
    }

    private void FindCastPointsOnModel(Transform parent)
    {
        foreach (Transform child in parent)
        {
            // Gán điểm cast vào BodyPart tương ứng dựa trên Tag
            if (child.CompareTag("WeaponCastPoint_Primary"))
            {
                AllCastPoints[BodyPart.Weapon_Primary].Add(child);
            }
            else if (child.CompareTag("WeaponCastPoint_Secondary"))
            {
                AllCastPoints[BodyPart.Weapon_Secondary].Add(child);
            }
            FindCastPointsOnModel(child);
        }
    }

    // --- Các hàm còn lại giữ nguyên ---
    public void ToggleWeaponStance()
    {
        IsWeaponDrawn = !IsWeaponDrawn;
        playerAnimator.SetWeaponDrawn(IsWeaponDrawn);
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
        if (CurrentWeapon.AttackCombo == null || CurrentWeapon.AttackCombo.Length == 0) return null;
        if (comboIndex >= 0 && comboIndex < CurrentWeapon.AttackCombo.Length)
        {
            return CurrentWeapon.AttackCombo[comboIndex];
        }
        return null;
    }
}