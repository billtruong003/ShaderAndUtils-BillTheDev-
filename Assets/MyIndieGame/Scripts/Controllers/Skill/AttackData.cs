// File: Assets/MyIndieGame/Scripts/Controllers/Skill/AttackData.cs

using UnityEngine;

[System.Serializable]
public class AttackData
{
    [Header("Animation & Timing")]
    [Tooltip("ID phải khớp với Threshold trong ActionExecution Blend Tree.")]
    public int AttackID;
    public AnimationClip AnimationClip;
    public float Duration = 1.0f;
    [Tooltip("Thời điểm trong animation mà người chơi có thể bấm để thực hiện đòn combo tiếp theo.")]
    public float ComboWindowStartTime = 0.4f;

    [Header("Movement During Attack")]
    [Tooltip("Tốc độ di chuyển tới trước trong khi tấn công.")]
    public float moveForwardSpeed = 2f;
    [Tooltip("Thời gian di chuyển tới trước.")]
    public float moveDuration = 0.25f;

    [Header("Hit Detection & Damage")]
    [Tooltip("Bộ phận cơ thể (hoặc vũ khí) sẽ thực hiện đòn tấn công này.")]
    public BodyPart attackingPart; // Quan trọng: Chỉ định bộ phận tấn công

    [Tooltip("Đòn đánh bắt đầu có thể gây sát thương tại giây này của animation.")]
    public float hitboxStartTime = 0.1f;
    [Tooltip("Đòn đánh kết thúc gây sát thương tại giây này.")]
    public float hitboxEndTime = 0.5f;

    [Tooltip("Nhân sát thương cho đòn đánh này. 1 = 100% sát thương, 1.5 = 150%...")]
    public float damageMultiplier = 1.0f;
    [Tooltip("Sát thương gây ra cho thanh Poise/Stagger của địch.")]
    public float poiseDamage = 10f;
    [Tooltip("Lượng thể lực tiêu hao cho đòn đánh này.")]
    public float staminaCost = 0f;
}