// File: Assets/MyIndieGame/Scripts/Controllers/Skill/Editor/AttackDataDrawer.cs

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(AttackData))]
public class AttackDataDrawer : PropertyDrawer
{
    private static readonly Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        string propertyPath = property.propertyPath;
        foldoutStates.TryGetValue(propertyPath, out bool isExpanded);
        
        var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        isExpanded = EditorGUI.Foldout(foldoutRect, isExpanded, label, true);
        foldoutStates[propertyPath] = isExpanded;

        float currentY = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        if (isExpanded)
        {
            EditorGUI.indentLevel++;
            
            void DrawField(string propertyName, bool includeChildren = true)
            {
                var prop = property.FindPropertyRelative(propertyName);
                if (prop == null) 
                {
                    Debug.LogWarning($"Could not find property: {propertyName}");
                    return;
                }
                var propHeight = EditorGUI.GetPropertyHeight(prop, includeChildren);
                var propRect = new Rect(position.x, currentY, position.width, propHeight);
                EditorGUI.PropertyField(propRect, prop, includeChildren);
                currentY += propHeight + EditorGUIUtility.standardVerticalSpacing;
            }
            
            // Vẽ các trường theo đúng thứ tự trong AttackData.cs
            
            // Animation & Timing
            DrawField("AttackID");
            DrawField("AnimationClip");
            DrawField("Duration");
            DrawField("ComboWindowStartTime");

            // Movement
            DrawField("moveForwardSpeed");
            DrawField("moveDuration");
            
            // Hit Detection & Damage
            // --- THÊM LẠI TRƯỜNG "attackingPart" VÀO ĐÚNG VỊ TRÍ ---
            DrawField("attackingPart");
            DrawField("hitboxStartTime");
            DrawField("hitboxEndTime");
            DrawField("damageMultiplier");
            DrawField("poiseDamage");
            DrawField("staminaCost");
            
            // Các nút tiện ích
            var animClipProp = property.FindPropertyRelative("AnimationClip");
            var animClip = animClipProp.objectReferenceValue as AnimationClip;
            if (animClip != null)
            {
                currentY += EditorGUIUtility.standardVerticalSpacing;
                var buttonsRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
                var buttonWidth = EditorGUIUtility.labelWidth + (buttonsRect.width - EditorGUIUtility.labelWidth) / 2 - 2;
                
                using (new EditorGUI.DisabledScope(false))
                {
                    var button1Rect = new Rect(buttonsRect.x + EditorGUIUtility.labelWidth - EditorGUI.indentLevel * 15, buttonsRect.y, buttonWidth, buttonsRect.height);
                    var button2Rect = new Rect(button1Rect.xMax + 4, buttonsRect.y, buttonWidth, buttonsRect.height);

                    if (GUI.Button(button1Rect, "Set Duration from Clip"))
                    {
                        property.FindPropertyRelative("Duration").floatValue = animClip.length;
                    }
                    if (GUI.Button(button2Rect, "Detect Movement from Clip"))
                    {
                        DetectAndApplyMovement_ThresholdBased(animClip, property);
                    }
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
            void AddPropHeight(string propName)
            {
                var prop = property.FindPropertyRelative(propName);
                if (prop != null)
                {
                    totalHeight += EditorGUI.GetPropertyHeight(prop, true) + EditorGUIUtility.standardVerticalSpacing;
                }
            }
            
            // Tính toán chiều cao cho tất cả các trường
            AddPropHeight("AttackID");
            AddPropHeight("AnimationClip");
            AddPropHeight("Duration");
            AddPropHeight("ComboWindowStartTime");
            AddPropHeight("moveForwardSpeed");
            AddPropHeight("moveDuration");

            // --- THÊM LẠI TRƯỜNG "attackingPart" VÀO TÍNH TOÁN ---
            AddPropHeight("attackingPart");
            AddPropHeight("hitboxStartTime");
            AddPropHeight("hitboxEndTime");
            AddPropHeight("damageMultiplier");
            AddPropHeight("poiseDamage");
            AddPropHeight("staminaCost");

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