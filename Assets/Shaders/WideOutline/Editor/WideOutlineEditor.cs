using System.IO;
using System.Linq;
using BillTheDev.BillOutline;
using BillTheDev.Editor.BillOutline.Common.Utils;
using UnityEditor;
using UnityEngine;

namespace BillTheDev.Editor.BillOutline
{
    [CustomEditor(typeof(WideOutline))]
    public class WideOutlineEditor : UnityEditor.Editor
    {
        private static class Styles
        {
            public static readonly GUIContent Settings = EditorGUIUtility.TrTextContent("Settings", "The settings for the Wide Outline renderer feature.");
        }

        private SerializedProperty settings;
        private bool initialized;

        private void Initialize()
        {
            settings = serializedObject.FindProperty("settings");
            initialized = true;
        }

        public override void OnInspectorGUI()
        {
            if (!initialized)
            {
                Initialize();
            }

            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(settings, Styles.Settings);

            if (settings.objectReferenceValue == null)
            {
                if (GUILayout.Button("Create", EditorStyles.miniButton, GUILayout.Width(70.0f)))
                {
                    const string assetPath = "Assets/Shaders/BillTheDev/WideOutline/Wide Outline Settings.asset";
                    string directoryPath = Path.GetDirectoryName(assetPath);

                    if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    var createdSettings = CreateInstance<WideOutlineSettings>();
                    AssetDatabase.CreateAsset(createdSettings, assetPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    settings.objectReferenceValue = createdSettings;
                    EditorUtility.FocusProjectWindow();
                    Selection.activeObject = createdSettings;
                }
            }
            else
            {
                if (GUILayout.Button("Open", EditorStyles.miniButton, GUILayout.Width(70.0f)))
                {
                    EditorUtils.OpenInspectorWindow(settings.objectReferenceValue);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (settings.objectReferenceValue != null && !((WideOutlineSettings)settings.objectReferenceValue).Outlines.Any(outline => outline.IsActive()))
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("No active outlines present. Effect will not render. Open the settings to add/enable outlines.", MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}