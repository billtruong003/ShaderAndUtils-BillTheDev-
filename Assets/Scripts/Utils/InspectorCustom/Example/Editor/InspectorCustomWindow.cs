using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.Collections.Generic;
using System.IO;

#if UNITY_EDITOR
namespace Utils.Bill.InspectorCustom.Example
{
    public class InspectorCustomWindow : EditorWindow
    {
        private enum ExampleCategory
        {
            Introduction,
            BasicAttributes,
            DataStructures,
            VisualElements,
            Customization,
            SerializeIf
        }

        private ExampleCategory selectedCategory = ExampleCategory.Introduction;

        private GameObject currentExampleGameObject;
        private Editor currentComponentEditor;

        private Vector2 mainScrollViewPos;
        private Vector2 codeScrollViewPos;

        private Dictionary<Type, string> exampleCodes = new Dictionary<Type, string>();

        private const float CATEGORY_PANEL_WIDTH = 180;
        private const float CODE_AREA_HEIGHT = 250;

        private Color customHeaderColor = Color.white;
        private Color customButtonColor = Color.green;
        private Color customGUIDColor = Color.white;

        [MenuItem("Tools/BillUtils/InspectorCustom Demo")]
        public static void ShowWindow()
        {
            var window = GetWindow<InspectorCustomWindow>("Inspector Custom Demo");
            window.minSize = new Vector2(500, 600);
        }

        private void OnEnable()
        {
            LoadAllExampleCodes();
            DestroyCurrentExample();
        }

        private void OnDisable()
        {
            DestroyCurrentExample();
        }

        private void DestroyCurrentExample()
        {
            if (currentComponentEditor != null)
            {
                DestroyImmediate(currentComponentEditor);
            }
            if (currentExampleGameObject != null)
            {
                if ((currentExampleGameObject.hideFlags & HideFlags.HideAndDontSave) != HideFlags.HideAndDontSave)
                {
                    if (EditorSceneManager.GetActiveScene().isLoaded && currentExampleGameObject.scene.IsValid())
                    {
                        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                    }
                }
                DestroyImmediate(currentExampleGameObject);
            }
            currentComponentEditor = null;
            currentExampleGameObject = null;
        }

        private void LoadAllExampleCodes()
        {
            exampleCodes.Clear();

            string exampleFilePath = "/Volumes/Database/Bill/PersonalLearning/ShaderAndUtilsBillTheDev/Assets/Scripts/Utils/InspectorCustom/Example/Editor/AllCustomInspectorExamplesEditor.cs";

            exampleCodes.Add(typeof(HeaderExample), GetSourceCode(exampleFilePath, "HeaderExample"));
            exampleCodes.Add(typeof(ButtonExample), GetSourceCode(exampleFilePath, "ButtonExample"));
            exampleCodes.Add(typeof(ReadOnlyExample), GetSourceCode(exampleFilePath, "ReadOnlyExample"));
            exampleCodes.Add(typeof(MinValueExample), GetSourceCode(exampleFilePath, "MinValueExample"));
            exampleCodes.Add(typeof(DictionaryExample), GetSourceCode(exampleFilePath, "DictionaryExample"));
            exampleCodes.Add(typeof(SerializeGUIDExample), GetSourceCode(exampleFilePath, "SerializeGUIDExample"));
            exampleCodes.Add(typeof(ProgressBarExample), GetSourceCode(exampleFilePath, "ProgressBarExample"));
            exampleCodes.Add(typeof(SliderExample), GetSourceCode(exampleFilePath, "SliderExample"));
            exampleCodes.Add(typeof(LayoutExample), GetSourceCode(exampleFilePath, "LayoutExample"));
            exampleCodes.Add(typeof(GridExample), GetSourceCode(exampleFilePath, "GridExample"));
            exampleCodes.Add(typeof(SerializeIfExample), GetSourceCode(exampleFilePath, "SerializeIfExample"));
        }

        private string GetSourceCode(string filePath, string className)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"GetSourceCode: Không tìm thấy tệp mã nguồn tại: {filePath}");
                return $"// Lỗi: Không tìm thấy tệp mã nguồn tại: {filePath}";
            }

            string fullText = File.ReadAllText(filePath);

            string classDefinitionStartPattern = $"public class {className}";
            int startIndex = fullText.IndexOf(classDefinitionStartPattern);

            if (startIndex == -1)
            {
                classDefinitionStartPattern = $"class {className}";
                startIndex = fullText.IndexOf(classDefinitionStartPattern);
            }

            if (startIndex == -1)
            {
                Debug.LogError($"GetSourceCode: Không tìm thấy lớp '{className}' trong tệp '{filePath}'.");
                return $"// Lỗi: Không tìm thấy lớp '{className}' trong tệp.";
            }

            int openBraceCount = 0;
            int endIndex = -1;
            int searchStart = startIndex + classDefinitionStartPattern.Length;

            int firstOpenBrace = fullText.IndexOf('{', searchStart);
            if (firstOpenBrace == -1)
            {
                Debug.LogError($"GetSourceCode: Không tìm thấy dấu mở ngoặc nhọn cho lớp '{className}'.");
                return $"// Lỗi: Không tìm thấy thân lớp cho '{className}'.";
            }

            openBraceCount = 0;
            for (int i = firstOpenBrace; i < fullText.Length; i++)
            {
                if (fullText[i] == '{')
                {
                    openBraceCount++;
                }
                else if (fullText[i] == '}')
                {
                    openBraceCount--;
                }

                if (openBraceCount == 0 && fullText[i] == '}')
                {
                    endIndex = i;
                    break;
                }
            }

            if (endIndex != -1)
            {
                return fullText.Substring(startIndex, endIndex - startIndex + 1);
            }

            Debug.LogError($"GetSourceCode: Không thể trích xuất mã nguồn cho lớp '{className}'.");
            return $"// Lỗi: Không thể trích xuất mã nguồn cho lớp '{className}'.";
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(CATEGORY_PANEL_WIDTH), GUILayout.ExpandHeight(true));
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Categories", EditorStyles.boldLabel);
            GUILayout.Space(5);

            ExampleCategory[] categories = (ExampleCategory[])Enum.GetValues(typeof(ExampleCategory));
            for (int i = 0; i < categories.Length; i++)
            {
                GUI.backgroundColor = (selectedCategory == categories[i]) ? new Color(0.6f, 0.8f, 1.0f, 1.0f) : Color.white;
                if (GUILayout.Button(categories[i].ToString().Replace("_", " "), GUILayout.Height(30)))
                {
                    selectedCategory = categories[i];
                    DestroyCurrentExample();
                }
            }
            GUI.backgroundColor = Color.white;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            mainScrollViewPos = EditorGUILayout.BeginScrollView(mainScrollViewPos);

            switch (selectedCategory)
            {
                case ExampleCategory.Introduction:
                    DrawIntroductionSection();
                    break;
                case ExampleCategory.BasicAttributes:
                    EditorGUILayout.LabelField("Basic Inspector Attributes", EditorStyles.largeLabel);
                    EditorGUILayout.HelpBox("These attributes provide fundamental ways to enhance and control fields in the Inspector.", MessageType.Info);
                    EditorGUILayout.Space();

                    DrawMiniExampleBlock(typeof(HeaderExample), "Custom Headers & Tooltips", "Add stylized headers with optional tooltips above your fields.");
                    DrawMiniExampleBlock(typeof(ButtonExample), "Custom Buttons", "Add clickable buttons directly in the Inspector to invoke methods.");
                    DrawMiniExampleBlock(typeof(ReadOnlyExample), "Read-Only Fields", "Prevent accidental modifications to important fields.");
                    DrawMiniExampleBlock(typeof(MinValueExample), "Min/Max Value Clamp", "Automatically clamp numerical inputs within a specified range.");
                    DrawMiniExampleBlock(typeof(SerializeGUIDExample), "Serialize GUID", "Generate and manage unique GUIDs for string fields, with read-only option.");
                    break;
                case ExampleCategory.DataStructures:
                    EditorGUILayout.LabelField("Custom Data Structure Display", EditorStyles.largeLabel);
                    EditorGUILayout.HelpBox("This example demonstrates how to make a Dictionary serializable by Unity and provides a custom Inspector UI.", MessageType.Info);
                    EditorGUILayout.Space();

                    DrawExampleSection(typeof(DictionaryExample), "Serializable & Editable Dictionary",
                        "A custom SerializableDictionary class allows Unity to save your dictionary data.");
                    break;
                case ExampleCategory.VisualElements:
                    EditorGUILayout.LabelField("Custom Visual & Layout Elements", EditorStyles.largeLabel);
                    EditorGUILayout.HelpBox("Showcasing custom property drawers for visual elements and custom editor layouts.", MessageType.Info);
                    EditorGUILayout.Space();

                    DrawMiniExampleBlock(typeof(ProgressBarExample), "Progress Bar", "Visualize progress for float or integer fields using a custom progress bar.");
                    DrawMiniExampleBlock(typeof(SliderExample), "Custom Slider", "An enhanced slider control.");
                    DrawMiniExampleBlock(typeof(LayoutExample), "Custom Layout & Grouping", "Organize properties into expandable sections and visual groups.");
                    DrawMiniExampleBlock(typeof(GridExample), "Custom Grid View", "Visualize and interact with data in a custom grid structure.");
                    break;
                case ExampleCategory.Customization:
                    DrawCustomizationSection();
                    break;
                case ExampleCategory.SerializeIf:
                    DrawExampleSection(typeof(SerializeIfExample), "Serialize If Example", "Demonstrates the SerializeIf attribute");
                    break;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            if (GUI.changed)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }

        private void DrawIntroductionSection()
        {
            EditorGUILayout.LabelField("Welcome to Inspector Custom Demo!", EditorStyles.largeLabel);
            EditorGUILayout.HelpBox("This tool showcases various custom attributes and editor features designed to enhance your Unity Inspector workflows.", MessageType.Info);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Key Features:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("- **Basic Attributes:** Headers, Buttons, Read-Only, Min/Max, GUIDs.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("- **Data Structures:** Improved display for Dictionaries.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("- **Visual Elements:** Progress Bars, Custom Sliders, and Advanced Layouts.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Select a category from the left panel to see examples in action.", MessageType.None);
        }

        private void DrawExampleSection(Type exampleComponentType, string title, string description)
        {
            EditorGUILayout.LabelField(title, EditorStyles.largeLabel);
            EditorGUILayout.HelpBox(description, MessageType.Info);
            EditorGUILayout.Space();

            if (currentExampleGameObject == null || currentExampleGameObject.GetComponent(exampleComponentType) == null)
            {
                DestroyCurrentExample();
                currentExampleGameObject = new GameObject($"Temp{exampleComponentType.Name}");
                currentExampleGameObject.hideFlags = HideFlags.HideInHierarchy;
                currentExampleGameObject.AddComponent(exampleComponentType);
                currentComponentEditor = Editor.CreateEditor(currentExampleGameObject.GetComponent(exampleComponentType));
            }

            EditorGUILayout.LabelField("Inspector Preview:", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            if (currentComponentEditor != null)
            {
                currentComponentEditor.OnInspectorGUI();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            if (GUILayout.Button("Refresh Example Instance"))
            {
                DestroyCurrentExample();
            }
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Source Code:", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            codeScrollViewPos = EditorGUILayout.BeginScrollView(codeScrollViewPos, GUILayout.Height(CODE_AREA_HEIGHT));
            GUI.enabled = false;
            if (exampleCodes.ContainsKey(exampleComponentType))
            {
                EditorGUILayout.TextArea(exampleCodes[exampleComponentType], GUILayout.ExpandHeight(true));
            }
            else
            {
                EditorGUILayout.HelpBox("Source code not found for this example.", MessageType.Warning);
            }
            GUI.enabled = true;
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawMiniExampleBlock(Type exampleComponentType, string title, string description)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(description, MessageType.None);
            EditorGUILayout.Space(5);

            GameObject tempGo = new GameObject($"Temp{exampleComponentType.Name}_MiniBlock");
            tempGo.hideFlags = HideFlags.HideAndDontSave;
            MonoBehaviour tempComp = tempGo.AddComponent(exampleComponentType) as MonoBehaviour;
            Editor tempEditor = null;
            try
            {
                tempEditor = Editor.CreateEditor(tempComp);
                if (tempEditor != null)
                {
                    tempEditor.OnInspectorGUI();
                }
            }
            finally
            {
                if (tempEditor != null) DestroyImmediate(tempEditor);
                if (tempGo != null) DestroyImmediate(tempGo);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawCustomizationSection()
        {
            EditorGUILayout.LabelField("Customization Options", EditorStyles.largeLabel);
            EditorGUILayout.HelpBox("These options demonstrate how you might allow users to customize colors.", MessageType.Warning);
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical(GUI.skin.box);
            customHeaderColor = EditorGUILayout.ColorField("Header Color (Demo)", customHeaderColor);
            customButtonColor = EditorGUILayout.ColorField("Button Color (Demo)", customButtonColor);
            customGUIDColor = EditorGUILayout.ColorField("GUID Field Color (Demo)", customGUIDColor);
            if (GUILayout.Button("Apply Colors (Log Only)"))
            {
                Debug.Log($"Custom colors set: Header={customHeaderColor}, Button={customButtonColor}, GUID={customGUIDColor}.");
            }
            EditorGUILayout.EndVertical();
        }
    }
}
#endif