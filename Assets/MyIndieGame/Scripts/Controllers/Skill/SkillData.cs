using UnityEngine;

[CreateAssetMenu(fileName = "New Skill", menuName = "Luminaria/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Skill Info")]
    public string skillName;
    public string description;
    public Sprite skillIcon;
    public float manaCost;
    public float cooldown;

    [Header("Animation Info")]
    [Tooltip("ID phải khớp với Threshold trong Actions Blend Tree của Animator")]
    public int animationID; // 1 = Dash, 2 = Heavy Slash...
    public float animationDuration; // Thời gian khóa hành động
    public RequiredWeaponType requiredWeapon;
}

public enum RequiredWeaponType { Any, Unarmed, Sword, Spear, DualSword }