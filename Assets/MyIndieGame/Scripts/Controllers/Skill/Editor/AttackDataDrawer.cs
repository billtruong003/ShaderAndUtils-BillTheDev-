// File: Assets/Editor/AttackDataDrawer.cs (Phiên bản đã sửa)

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(AttackData))]
public class AttackDataDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.LabelField(position, label);
        var currentRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.indentLevel++;

        void DrawField(string propertyName, bool includeChildren = true)
        {
            var prop = property.FindPropertyRelative(propertyName);
            EditorGUI.PropertyField(currentRect, prop, includeChildren);
            currentRect.y += EditorGUI.GetPropertyHeight(prop, includeChildren) + EditorGUIUtility.standardVerticalSpacing;
        }

        // Animation & Timing
        DrawField("AttackID");
        DrawField("AnimationClip");
        DrawField("Duration");
        DrawField("ComboWindowStartTime");

        // Movement During Attack
        DrawField("moveForwardSpeed");
        DrawField("moveDuration");

        // --- CÁC DÒNG MỚI THÊM VÀO ---
        // Hitbox & Damage (Header sẽ được vẽ tự động)
        DrawField("hitboxStartTime");
        DrawField("hitboxEndTime");
        DrawField("hitboxRadius");
        DrawField("damageMultiplier");
        DrawField("staminaCost");
        // --- KẾT THÚC PHẦN MỚI ---

        var animClipProp = property.FindPropertyRelative("AnimationClip");
        var animClip = animClipProp.objectReferenceValue as AnimationClip;
        if (animClip != null)
        {
            var buttonWidth = (currentRect.width / 2) - 5;
            var button1Rect = new Rect(currentRect.x, currentRect.y, buttonWidth, EditorGUIUtility.singleLineHeight);
            var button2Rect = new Rect(currentRect.x + buttonWidth + 10, currentRect.y, buttonWidth, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(button1Rect, "Set Duration"))
            {
                property.FindPropertyRelative("Duration").floatValue = animClip.length;
            }
            if (GUI.Button(button2Rect, "Detect Movement"))
            {
                DetectAndApplyMovement_ThresholdBased(animClip, property);
            }
        }

        EditorGUI.indentLevel--;
        property.serializedObject.ApplyModifiedProperties();
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float totalHeight = EditorGUIUtility.singleLineHeight;

        void AddPropHeight(string propName)
        {
            totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(propName), true) + EditorGUIUtility.standardVerticalSpacing;
        }

        AddPropHeight("AttackID");
        AddPropHeight("AnimationClip");
        AddPropHeight("Duration");
        AddPropHeight("ComboWindowStartTime");
        AddPropHeight("moveForwardSpeed");
        AddPropHeight("moveDuration");

        // --- CÁC DÒNG MỚI THÊM VÀO ---
        AddPropHeight("hitboxStartTime");
        AddPropHeight("hitboxEndTime");
        AddPropHeight("hitboxRadius");
        AddPropHeight("damageMultiplier");
        AddPropHeight("staminaCost");
        // --- KẾT THÚC PHẦN MỚI ---

        if (property.FindPropertyRelative("AnimationClip").objectReferenceValue != null)
        {
            totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }
        return totalHeight;
    }


    /// <summary>
    /// PHIÊN BẢN CẢI TIẾN: Logic phân tích chuyển động dựa trên "Ngưỡng Trạng Thái".
    /// Tìm khoảng thời gian đầu tiên và cuối cùng mà vận tốc vượt qua một ngưỡng cố định.
    /// </summary>
    private void DetectAndApplyMovement_ThresholdBased(AnimationClip clip, SerializedProperty attackDataProperty)
    {
        // ... (Logic của hàm này giữ nguyên, không cần thay đổi) ...
        const float MOVEMENT_VELOCITY_THRESHOLD = 0.2f;

        var zCurve = GetZCurve(clip);
        if (zCurve == null || zCurve.keys.Length < 2)
        {
            Debug.LogWarning($"[AttackDataDrawer] No root motion Z-curve found in '{clip.name}'.");
            SetMovementProperties(attackDataProperty, 0, 0);
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
            SetMovementProperties(attackDataProperty, 0, 0);
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
            Debug.Log($"[AttackDataDrawer] No significant movement detected for '{clip.name}' (Threshold: {MOVEMENT_VELOCITY_THRESHOLD}).");
            SetMovementProperties(attackDataProperty, 0, 0);
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

        Debug.Log($"[AttackDataDrawer] Detected movement for '{clip.name}': Speed={averageSpeed:F2}, Duration={moveDuration:F2} (From {startTime:F2}s to {endTime:F2}s)");
        SetMovementProperties(attackDataProperty, averageSpeed, moveDuration);
    }

    private void SetMovementProperties(SerializedProperty property, float speed, float duration)
    {
        property.FindPropertyRelative("moveForwardSpeed").floatValue = (float)System.Math.Round(speed, 2);
        property.FindPropertyRelative("moveDuration").floatValue = (float)System.Math.Round(duration, 2);
    }

    private AnimationCurve GetZCurve(AnimationClip clip)
    {
        var binding = AnimationUtility.GetCurveBindings(clip)
            .FirstOrDefault(b => b.type == typeof(Transform) && b.propertyName == "m_LocalPosition.z");
        return binding.propertyName != null ? AnimationUtility.GetEditorCurve(clip, binding) : null;
    }
}
#endif