using System;
using System.Text.RegularExpressions;
using BillTheDev.BillOutline.Common;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace BillTheDev.Editor.BillOutline.Common
{
    [CustomEditor(typeof(OutlineOverride))]
    public class OutlineOverrideEditor : UnityEditor.Editor
    {
        private ReorderableList reorderableList;

        private void OnEnable()
        {
            var overrides = serializedObject.FindProperty(nameof(OutlineOverride.overrides));

            reorderableList = new ReorderableList(serializedObject, overrides, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Property Overrides"),
                drawElementCallback = (rect, index, _, _) => DrawOverride(rect, overrides.GetArrayElementAtIndex(index)),
                elementHeightCallback = index => GetElementHeight(overrides.GetArrayElementAtIndex(index)),
                onAddDropdownCallback = (_, _) =>
                {
                    var menu = new GenericMenu();
                    foreach (var type in (ShaderPropertyType[])Enum.GetValues(typeof(ShaderPropertyType)))
                    {
                        menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(type.ToString())), false, () => AddOverride(overrides, type));
                    }
                    menu.ShowAsContext();
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.Space();
            reorderableList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        private void AddOverride(SerializedProperty overrides, ShaderPropertyType type)
        {
            var index = overrides.arraySize;
            overrides.InsertArrayElementAtIndex(index);
            var newElement = overrides.GetArrayElementAtIndex(index);
            UpdateDefaultsForType(newElement, type);
            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawOverride(Rect rect, SerializedProperty element)
        {
            var typeProperty = element.FindPropertyRelative(nameof(ShaderPropertyOverride.type));
            var nameProperty = element.FindPropertyRelative(nameof(ShaderPropertyOverride.propertyName));

            rect.height = EditorGUIUtility.singleLineHeight;
            var typeRect = new Rect(rect.x, rect.y, 80, rect.height);
            var nameRect = new Rect(typeRect.xMax + 5, rect.y, rect.width - typeRect.width - 5, rect.height);

            var newType = (ShaderPropertyType)EditorGUI.EnumPopup(typeRect, (ShaderPropertyType)typeProperty.enumValueIndex);
            if (newType != (ShaderPropertyType)typeProperty.enumValueIndex)
            {
                typeProperty.enumValueIndex = (int)newType;
                UpdateDefaultsForType(element, newType);
            }

            EditorGUI.PropertyField(nameRect, nameProperty, GUIContent.none);
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            var valueRect = new Rect(rect.x, rect.y, rect.width, rect.height);
            switch (newType)
            {
                case ShaderPropertyType.Float: EditorGUI.PropertyField(valueRect, element.FindPropertyRelative(nameof(ShaderPropertyOverride.floatValue)), new GUIContent("Value")); break;
                case ShaderPropertyType.Int: EditorGUI.PropertyField(valueRect, element.FindPropertyRelative(nameof(ShaderPropertyOverride.intValue)), new GUIContent("Value")); break;
                case ShaderPropertyType.Color: EditorGUI.PropertyField(valueRect, element.FindPropertyRelative(nameof(ShaderPropertyOverride.colorValue)), new GUIContent("Value")); break;
                case ShaderPropertyType.Vector: EditorGUI.PropertyField(valueRect, element.FindPropertyRelative(nameof(ShaderPropertyOverride.vectorValue)), new GUIContent("Value")); break;
            }
        }

        private static void UpdateDefaultsForType(SerializedProperty element, ShaderPropertyType type)
        {
            element.FindPropertyRelative(nameof(ShaderPropertyOverride.type)).enumValueIndex = (int)type;
            var name = element.FindPropertyRelative(nameof(ShaderPropertyOverride.propertyName));

            switch (type)
            {
                case ShaderPropertyType.Float: name.stringValue = "_MyFloat"; element.FindPropertyRelative(nameof(ShaderPropertyOverride.floatValue)).floatValue = 1.0f; break;
                case ShaderPropertyType.Int: name.stringValue = "_MyInt"; element.FindPropertyRelative(nameof(ShaderPropertyOverride.intValue)).intValue = 1; break;
                case ShaderPropertyType.Color: name.stringValue = "_MyColor"; element.FindPropertyRelative(nameof(ShaderPropertyOverride.colorValue)).colorValue = Color.white; break;
                case ShaderPropertyType.Vector: name.stringValue = "_MyVector"; element.FindPropertyRelative(nameof(ShaderPropertyOverride.vectorValue)).vector4Value = Vector4.one; break;
            }
        }

        private static float GetElementHeight(SerializedProperty element)
        {
            var lines = 2;
            var type = (ShaderPropertyType)element.FindPropertyRelative(nameof(ShaderPropertyOverride.type)).enumValueIndex;
            if (type == ShaderPropertyType.Vector) lines = 3;

            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * lines;
        }
    }
}