// File: Assets/MyIndieGame/Scripts/Controllers/Skill/AttackData.cs
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class AttackData
{
    [Title("$LabelText", bold: true, horizontalLine: false)]
    [BoxGroup("General")]
    public int AttackID;

    [BoxGroup("General")]
    [OnValueChanged("OnClipChanged")]
    public AnimationClip AnimationClip;

    [BoxGroup("General")]
    public float Duration;

    [BoxGroup("General")]
    public float ComboWindowStartTime;

    [Title("Movement")]
    public float moveForwardSpeed;
    public float moveDuration;

    [Title("Hitbox & Damage")]
    public BodyPart attackingPart;
    public float hitboxStartTime;
    public float hitboxEndTime;
    public float damageMultiplier;
    public float poiseDamage;
    public float staminaCost;

    // --- THÊM MỚI: Cấu hình Lunge To Target ---
    [Header("Lunge To Target")]
    [Tooltip("Bật/Tắt tính năng tự động lướt tới mục tiêu khi tấn công.")]
    public bool canLunge = true;

    [Tooltip("Khoảng cách tối đa từ người chơi đến mục tiêu để có thể thực hiện cú lướt.")]
    [EnableIf("canLunge")]
    public float maxLungeDistance = 6f;

    [Tooltip("Khoảng cách lý tưởng so với mục tiêu sau khi lướt xong. Giúp nhân vật không bị lướt dính vào địch.")]
    [EnableIf("canLunge")]
    public float idealAttackRange = 1.5f;

    [Tooltip("Thời gian thực hiện cú lướt (tính bằng giây).")]
    [EnableIf("canLunge")]
    public float lungeDuration = 0.2f;
    // --- KẾT THÚC THÊM MỚI ---

#if UNITY_EDITOR
    private string LabelText => AnimationClip != null ? $"Attack ({AnimationClip.name})" : "Attack (No Clip)";

    [Button("Set Duration from Clip", ButtonSizes.Medium)]
    [ButtonGroup("ClipActions")]
    [EnableIf("AnimationClip")]
    private void SetDurationFromClip()
    {
        if (AnimationClip == null) return;
        this.Duration = AnimationClip.length;
    }

    [Button("Detect Movement from Clip", ButtonSizes.Medium)]
    [ButtonGroup("ClipActions")]
    [EnableIf("AnimationClip")]
    private void DetectAndApplyMovement()
    {
        DetectAndApplyMovement_ThresholdBased(this.AnimationClip);
    }

    private void OnClipChanged()
    {
        if (AnimationClip != null)
        {
            this.Duration = AnimationClip.length;
        }
    }

    private void DetectAndApplyMovement_ThresholdBased(AnimationClip clip)
    {
        const float MOVEMENT_VELOCITY_THRESHOLD = 0.2f;
        var zCurve = GetZCurve(clip);

        if (zCurve == null || zCurve.keys.Length < 2)
        {
            Debug.LogWarning($"[AttackData] No root motion Z-curve found in '{clip.name}'.");
            SetMovementProperties(0, 0);
            return;
        }

        var velocities = new List<(float time, float velocity)>();
        for (int i = 0; i < zCurve.keys.Length - 1; i++)
        {
            var key1 = zCurve.keys[i];
            var key2 = zCurve.keys[i + 1];
            float timeDelta = key2.time - key1.time;
            if (timeDelta > 0.0001f)
            {
                float velocity = (key2.value - key1.value) / timeDelta;
                velocities.Add((key1.time, velocity));
            }
        }

        if (velocities.Count == 0)
        {
            SetMovementProperties(0, 0);
            return;
        }

        float startTime = -1f;
        float endTime = -1f;

        foreach (var v in velocities)
        {
            if (v.velocity > MOVEMENT_VELOCITY_THRESHOLD)
            {
                startTime = v.time;
                break;
            }
        }

        if (startTime < 0)
        {
            Debug.Log($"[AttackData] No significant movement detected for '{clip.name}' (Threshold: {MOVEMENT_VELOCITY_THRESHOLD}).");
            SetMovementProperties(0, 0);
            return;
        }

        for (int i = velocities.Count - 1; i >= 0; i--)
        {
            if (velocities[i].velocity > MOVEMENT_VELOCITY_THRESHOLD)
            {
                endTime = (i + 1 < velocities.Count) ? velocities[i + 1].time : clip.length;
                break;
            }
        }

        if (endTime <= startTime)
        {
            endTime = startTime + 0.1f;
        }

        float moveDuration = endTime - startTime;
        float startPos = zCurve.Evaluate(startTime);
        float endPos = zCurve.Evaluate(endTime);
        float moveDistance = endPos - startPos;
        float averageSpeed = (moveDuration > 0) ? moveDistance / moveDuration : 0;

        Debug.Log($"[AttackData] Detected movement for '{clip.name}': Speed={averageSpeed:F2}, Duration={moveDuration:F2} (From {startTime:F2}s to {endTime:F2}s)");
        SetMovementProperties(averageSpeed, moveDuration);
    }

    private void SetMovementProperties(float speed, float duration)
    {
        this.moveForwardSpeed = (float)System.Math.Round(speed, 2);
        this.moveDuration = (float)System.Math.Round(duration, 2);
    }

    private AnimationCurve GetZCurve(AnimationClip clip)
    {
        var binding = AnimationUtility.GetCurveBindings(clip)
            .FirstOrDefault(b => b.type == typeof(Transform) && b.propertyName == "m_LocalPosition.z");

        return binding.propertyName != null ? AnimationUtility.GetEditorCurve(clip, binding) : null;
    }
#endif
}