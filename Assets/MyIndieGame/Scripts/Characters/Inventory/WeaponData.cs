using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Luminaria/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Weapon Info")]
    [Tooltip("ID phải khớp với Parameter 'WeaponTypeID' trong Animator.")]
    public int WeaponTypeID; // 0=Unarmed, 1=Sword, 2=Spear...
    public AnimatorOverrideController AnimatorOverride;

    [Header("Attack Combo Chain")]
    public AttackData[] AttackCombo;
    // Trong WeaponData.cs
    public List<StatModifier> BaseModifiers; // Các chỉ số cố định của vũ khí
}