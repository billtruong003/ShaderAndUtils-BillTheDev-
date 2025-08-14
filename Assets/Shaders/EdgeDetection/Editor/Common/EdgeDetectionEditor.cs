// using BillTheDev.BillOutline.EdgeDetection;
// using BillTheDev.Editor.BillOutline.Common.Utils;
// using UnityEditor;
// using UnityEngine;
// using System.IO;

// namespace BillTheDev.Editor.BillOutline.EdgeDetection
// {
//     [CustomEditor(typeof(BillTheDev.BillOutline.EdgeDetection.EdgeDetection))]
//     public class EdgeDetectionEditor : UnityEditor.Editor
//     {
//         private static class Styles
//         {
//             public static readonly GUIContent Settings = EditorGUIUtility.TrTextContent("Settings Profile", "The settings asset that controls the appearance of the outline.");
//             public static readonly GUIContent SectionShader = EditorGUIUtility.TrTextContent("Section Shader", "Shader used to render object IDs into a buffer.");
//             public static readonly GUIContent SectionMaskShader = EditorGUIUtility.TrTextContent("Section Mask Shader", "Shader used to mask out areas from the effect.");
//             public static readonly GUIContent OutlineShader = EditorGUIUtility.TrTextContent("Outline Shader", "Shader that performs edge detection and composites the final outline.");
//         }

//         private SerializedProperty settings;
//         private SerializedProperty sectionShader;
//         private SerializedProperty sectionMaskShader;
//         private SerializedProperty outlineShader;
//         private bool initialized;

//         private void Initialize()
//         {
//             settings = serializedObject.FindProperty("settings");
//             sectionShader = serializedObject.FindProperty("sectionShader");
//             sectionMaskShader = serializedObject.FindProperty("sectionMaskShader");
//             outlineShader = serializedObject.FindProperty("outlineShader");
//             initialized = true;
//         }

//         public override void OnInspectorGUI()
//         {
//             if (!initialized) Initialize();

//             serializedObject.Update();

//             EditorGUILayout.LabelField("Edge Detection", EditorStyles.boldLabel);

//             DrawSettingsAssetGUI();

//             CoreEditorUtils.DrawSplitter();

//             DrawShaderDependenciesGUI();

//             serializedObject.ApplyModifiedProperties();
//         }

//         private void DrawSettingsAssetGUI()
//         {
//             EditorGUILayout.BeginHorizontal();
//             EditorGUILayout.PropertyField(settings, Styles.Settings);

//             if (settings.objectReferenceValue == null)
//             {
//                 if (GUILayout.Button("Create", EditorStyles.miniButton, GUILayout.Width(70.0f)))
//                 {
//                     CreateSettingsAsset();
//                 }
//             }
//             else
//             {
//                 if (GUILayout.Button("Open", EditorStyles.miniButton, GUILayout.Width(70.0f)))
//                 {
//                     EditorUtils.OpenInspectorWindow(settings.objectReferenceValue);
//                 }
//             }
//             EditorGUILayout.EndHorizontal();
//         }

//         private void DrawShaderDependenciesGUI()
//         {
//             EditorGUILayout.LabelField("Shader Dependencies", EditorStyles.boldLabel);
//             EditorGUILayout.PropertyField(sectionShader, Styles.SectionShader);
//             EditorGUILayout.PropertyField(sectionMaskShader, Styles.SectionMaskShader);
//             EditorGUILayout.PropertyField(outlineShader, Styles.OutlineShader);

//             if (sectionShader.objectReferenceValue == null ||
//                 sectionMaskShader.objectReferenceValue == null ||
//                 outlineShader.objectReferenceValue == null)
//             {
//                 EditorGUILayout.HelpBox("One or more required shaders are missing. The effect will not render.", MessageType.Warning);
//             }
//         }

//         private void CreateSettingsAsset()
//         {
//             const string assetPath = "Assets/Shaders/BillTheDev/EdgeDetection/Edge Detection Settings.asset";
//             string directoryPath = Path.GetDirectoryName(assetPath);

//             if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
//             {
//                 Directory.CreateDirectory(directoryPath);
//             }

//             var createdSettings = CreateInstance<EdgeDetectionSettings>();
//             AssetDatabase.CreateAsset(createdSettings, assetPath);
//             AssetDatabase.SaveAssets();
//             AssetDatabase.Refresh();

//             settings.objectReferenceValue = createdSettings;
//             EditorUtility.FocusProjectWindow();
//             Selection.activeObject = createdSettings;
//         }
//     }
// }