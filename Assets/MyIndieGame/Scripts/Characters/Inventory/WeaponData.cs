// File: WeaponData.cs (Đã cập nhật)
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Luminaria/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Weapon Info")]
    public string weaponName;
    public GameObject WeaponModelPrefab;
    [Tooltip("ID phải khớp với Parameter 'WeaponTypeID' trong Animator.")]
    public int WeaponTypeID;
    public AnimatorOverrideController AnimatorOverride;

    [Header("Stats & Modifiers")]
    public float baseDamage = 10f; // Sát thương cơ bản của vũ khí
    public List<StatModifier> BaseModifiers;

    [Header("Attack Combo Chain")]
    public AttackData[] AttackCombo;

    // --- THÊM MỚI ---
    [Header("Hit Detection (SphereCast)")]
    [Tooltip("Bán kính của hình cầu được sử dụng để phát hiện va chạm khi tấn công.")]
    public float castRadius = 0.2f;
    // --- KẾT THÚC THÊM MỚI ---

    // Hàm này không còn cần thiết nhưng có thể giữ lại cho mục đích khác
    public List<GameObject> GetRequiredHitboxPrefabs()
    {
        return new List<GameObject>(); // Placeholder
    }
}