using UnityEngine;

[System.Serializable]
public class AttackData
{
    [Tooltip("ID phải khớp với Threshold trong ActionExecution Blend Tree.")]
    public int AttackID; // VÍ DỤ: 11

    public AnimationClip AnimationClip; // Vẫn cần để ghi đè, hoặc để tham khảo

    // ... các trường khác giữ nguyên ...
    public float Duration = 1.0f;
    public float ComboWindowStartTime = 0.4f;
}