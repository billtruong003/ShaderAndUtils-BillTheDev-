// File: Assets/MyIndieGame/Scripts/Characters/Inventory/WeaponData.cs (PHIÊN BẢN SỬA ĐỔI)
using UnityEngine;
using Sirenix.OdinInspector;

public enum AttackStrategyType { MeleeSphereCast, Projectile }

[CreateAssetMenu(fileName = "New Weapon", menuName = "Luminaria/Data/Weapon Data")]
public class WeaponData : SerializedScriptableObject
{
    // ... (Các trường General Info, Socket, Core Stats giữ nguyên) ...
    [Title("General Info")]
    public string weaponName;
    public GameObject WeaponModelPrefab;
    public int WeaponTypeID;
    public AnimatorOverrideController AnimatorOverride;

    [Title("Socket Configuration")]
    public CharacterSocketType EquipSocket = CharacterSocketType.Hand_Right;
    public CharacterSocketType SheathSocket = CharacterSocketType.Back_Primary;

    [Title("Core Stats")]
    public float baseDamage = 10f;

    [Title("Attack Strategy")]
    [EnumToggleButtons, OnValueChanged("OnStrategyChanged")]
    public AttackStrategyType AttackStrategyType;

    [BoxGroup("Strategy Settings")]
    [ShowIf("AttackStrategyType", AttackStrategyType.MeleeSphereCast)]
    [InfoBox("Bán kính của hình cầu dùng để phát hiện va chạm khi tấn công.")]
    public float castRadius = 0.2f;

    [BoxGroup("Strategy Settings")]
    [ShowIf("AttackStrategyType", AttackStrategyType.Projectile)]
    [Tooltip("Kéo file ProjectileData.asset tương ứng vào đây (ví dụ: NormalArrow.asset).")]
    // THAY ĐỔI QUAN TRỌNG: Đây là một tham chiếu đến một ScriptableObject khác.
    public ProjectileData projectileData;

    [Title("Attack Combo Chain")]
    public AttackData[] AttackCombo;

    public AttackData GetAttackData(int index)
    {
        if (AttackCombo == null || index < 0 || index >= AttackCombo.Length) return null;
        return AttackCombo[index];
    }

#if UNITY_EDITOR
    // Hàm này giúp UI của Odin Inspector trông gọn gàng hơn
    private void OnStrategyChanged() { }
#endif
}