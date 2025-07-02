// File: Assets/MyIndieGame/Scripts/Controllers/Skill/AttackData.cs (Phiên bản nâng cấp)

using UnityEngine;

[System.Serializable]
public class AttackData
{
    [Header("Animation & Timing")]
    [Tooltip("ID phải khớp với Threshold trong ActionExecution Blend Tree.")]
    public int AttackID;
    public AnimationClip AnimationClip;
    public float Duration = 1.0f;
    public float ComboWindowStartTime = 0.4f;

    [Header("Movement During Attack")]
    public float moveForwardSpeed = 2f;
    public float moveDuration = 0.25f;

    // --- PHẦN MỚI THÊM VÀO ---
    [Header("Hitbox & Damage")]
    [Tooltip("Đòn đánh bắt đầu có thể gây sát thương tại giây này của animation.")]
    public float hitboxStartTime = 0.1f;
    [Tooltip("Đòn đánh kết thúc gây sát thương tại giây này.")]
    public float hitboxEndTime = 0.5f;
    [Tooltip("Bán kính của vùng tấn công (dùng cho SphereCast/OverlapSphere).")]
    public float hitboxRadius = 1.2f;
    [Tooltip("Nhân sát thương cho đòn đánh này. 1 = 100% sát thương, 1.5 = 150%...")]
    public float damageMultiplier = 1.0f;
    [Tooltip("Lượng thể lực tiêu hao cho đòn đánh này.")]
    public float staminaCost = 0f;
}