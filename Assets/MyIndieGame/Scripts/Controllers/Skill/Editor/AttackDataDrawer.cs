// File: Assets/MyIndieGame/Scripts/Controllers/Skill/Editor/AttackDataDrawer.cs
// (PHIÊN BẢN ĐÃ SỬA LỖI HOÀN CHỈNH)

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(AttackData))]
public class AttackDataDrawer : PropertyDrawer
{
    // Dùng Dictionary để lưu trạng thái foldout cho từng thuộc tính riêng biệt
    private static readonly Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        string propertyPath = property.propertyPath;
        foldoutStates.TryGetValue(propertyPath, out bool isExpanded);

        // Tạo label có thể gập lại (foldout)
        var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        // Gán lại tên cho label để hiển thị thông tin hữu ích
        var animClipProp = property.FindPropertyRelative("AnimationClip");
        var animClip = animClipProp.objectReferenceValue as AnimationClip;
        string foldoutLabel = animClip != null ? $"{label.text} ({animClip.name})" : label.text;

        isExpanded = EditorGUI.Foldout(foldoutRect, isExpanded, foldoutLabel, true);
        foldoutStates[propertyPath] = isExpanded;

        // Bắt đầu vẽ các trường con nếu foldout được mở
        float currentY = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        if (isExpanded)
        {
            EditorGUI.indentLevel++;

            // Hàm helper để vẽ một trường và tự động cập nhật vị trí Y
            void DrawField(string propertyName, bool includeChildren = true)
            {
                var prop = property.FindPropertyRelative(propertyName);
                if (prop == null)
                {
                    Debug.LogWarning($"[AttackDataDrawer] Could not find property: {propertyName}");
                    return;
                }
                var propHeight = EditorGUI.GetPropertyHeight(prop, includeChildren);
                var propRect = new Rect(position.x, currentY, position.width, propHeight);
                EditorGUI.PropertyField(propRect, prop, includeChildren);
                currentY += propHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            // === VẼ CÁC TRƯỜNG THEO ĐÚNG THỨ TỰ LOGIC ===

            DrawField("AttackID");
            DrawField("AnimationClip");
            DrawField("Duration");
            DrawField("ComboWindowStartTime");

            DrawField("moveForwardSpeed");
            DrawField("moveDuration");

            // --- DÒNG QUAN TRỌNG ĐÃ ĐƯỢC THÊM LẠI ---
            DrawField("attackingPart");

            DrawField("hitboxStartTime");
            DrawField("hitboxEndTime");
            DrawField("damageMultiplier");
            DrawField("poiseDamage");
            DrawField("staminaCost");

            // === CÁC NÚT TIỆN ÍCH ===
            if (animClip != null)
            {
                currentY += EditorGUIUtility.standardVerticalSpacing;
                var buttonsRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
                // Canh lề các nút cho đẹp
                var indentedRect = EditorGUI.IndentedRect(buttonsRect);
                var buttonWidth = (indentedRect.width - 4) / 2;

                var button1Rect = new Rect(indentedRect.x, indentedRect.y, buttonWidth, indentedRect.height);
                var button2Rect = new Rect(button1Rect.xMax + 4, indentedRect.y, buttonWidth, indentedRect.height);

                if (GUI.Button(button1Rect, "Set Duration from Clip"))
                {
                    property.FindPropertyRelative("Duration").floatValue = animClip.length;
                }
                if (GUI.Button(button2Rect, "Detect Movement from Clip"))
                {
                    DetectAndApplyMovement_ThresholdBased(animClip, property);
                }
            }

            EditorGUI.indentLevel--;
        }

        property.serializedObject.ApplyModifiedProperties();
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        string propertyPath = property.propertyPath;
        foldoutStates.TryGetValue(propertyPath, out bool isExpanded);

        float totalHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        if (isExpanded)
        {
            // Hàm helper để cộng dồn chiều cao của một trường
            void AddPropHeight(string propName)
            {
                var prop = property.FindPropertyRelative(propName);
                if (prop != null)
                {
                    totalHeight += EditorGUI.GetPropertyHeight(prop, true) + EditorGUIUtility.standardVerticalSpacing;
                }
            }

            // --- TÍNH TOÁN CHIỀU CAO CHO TẤT CẢ CÁC TRƯỜNG ---
            AddPropHeight("AttackID");
            AddPropHeight("AnimationClip");
            AddPropHeight("Duration");
            AddPropHeight("ComboWindowStartTime");
            AddPropHeight("moveForwardSpeed");
            AddPropHeight("moveDuration");

            // --- DÒNG QUAN TRỌNG ĐÃ ĐƯỢC THÊM LẠI ---
            AddPropHeight("attackingPart");

            AddPropHeight("hitboxStartTime");
            AddPropHeight("hitboxEndTime");
            AddPropHeight("damageMultiplier");
            AddPropHeight("poiseDamage");
            AddPropHeight("staminaCost");

            // Thêm chiều cao cho các nút tiện ích nếu có animation clip
            if (property.FindPropertyRelative("AnimationClip").objectReferenceValue != null)
            {
                totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2;
            }
        }

        return totalHeight;
    }

    // Các hàm helper còn lại giữ nguyên
    private void DetectAndApplyMovement_ThresholdBased(AnimationClip clip, SerializedProperty attackDataProperty)
    {
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