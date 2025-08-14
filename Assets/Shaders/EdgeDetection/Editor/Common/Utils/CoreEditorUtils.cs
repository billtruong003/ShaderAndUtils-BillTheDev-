using System;
using UnityEditor;
using UnityEngine;

namespace BillTheDev.Editor.BillOutline.Common.Utils
{
    public static class CoreEditorUtils
    {
        public static void DrawSplitter()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            rect.x = 0;
            rect.width = EditorGUIUtility.currentViewWidth;
            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f, 1.333f));
        }

        public static bool DrawHeaderToggle(GUIContent content, SerializedProperty group, SerializedProperty activeField, Action<Vector2> contextAction)
        {
            var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);
            var labelRect = backgroundRect;
            labelRect.xMin += 16f;
            labelRect.xMax -= 20f;

            var foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;

            var toggleRect = backgroundRect;
            toggleRect.x = backgroundRect.width - 12f;
            toggleRect.y += 2f;
            toggleRect.width = 13f;
            toggleRect.height = 13f;

            if (contextAction != null)
            {
                var menuIcon = EditorGUIUtility.IconContent("_Menu");
                var menuRect = new Rect(labelRect.xMax + 4f, labelRect.y + 1f, 12, 12);

                if (GUI.Button(menuRect, menuIcon, new GUIStyle { fixedHeight = 0, fixedWidth = 0, stretchHeight = true, stretchWidth = true }))
                {
                    contextAction(new Vector2(menuRect.x, menuRect.y));
                }
            }

            var e = Event.current;
            if (labelRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
            {
                group.isExpanded = !group.isExpanded;
                e.Use();
            }

            activeField.boolValue = GUI.Toggle(toggleRect, activeField.boolValue, string.Empty);
            group.isExpanded = GUI.Toggle(foldoutRect, group.isExpanded, content, EditorStyles.foldout);

            return group.isExpanded;
        }

        public static void SectionGUI(string title, SerializedProperty foldout, Action contents, SerializedObject serializedObject)
        {
            foldout.boolValue = EditorGUILayout.BeginFoldoutHeaderGroup(foldout.boolValue, title);
            if (foldout.boolValue)
            {
                EditorGUI.indentLevel++;
                contents.Invoke();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            serializedObject.ApplyModifiedProperties();
        }
    }
}