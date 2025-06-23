// File: Assets/Utils/Bill/InspectorCustom/Example/AllCustomInspectorExamplesEditor.cs
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
namespace Utils.Bill.InspectorCustom.Example
{
    // =================================================================================================
    // CÁC EDITOR CHO CÁC VÍ DỤ
    // =================================================================================================

    // Mỗi editor này sẽ tự động sử dụng BillUtilsBaseEditor
    // để vẽ các Property và Button được định nghĩa trong lớp MonoBehavior tương ứng,
    // trừ khi OnInspectorGUI được override.

    [CustomEditor(typeof(HeaderExample)), CanEditMultipleObjects]
    public class HeaderExampleEditor : BillUtilsBaseEditor { }

    [CustomEditor(typeof(ButtonExample)), CanEditMultipleObjects]
    public class ButtonExampleEditor : BillUtilsBaseEditor { }

    [CustomEditor(typeof(ReadOnlyExample)), CanEditMultipleObjects]
    public class ReadOnlyExampleEditor : BillUtilsBaseEditor { }

    [CustomEditor(typeof(MinValueExample)), CanEditMultipleObjects]
    public class MinValueExampleEditor : BillUtilsBaseEditor { }

    // DictionaryExampleEditor: Kế thừa BillUtilsBaseEditor.
    // Các thuộc tính Dictionary sẽ được vẽ bởi CustomDictionaryDrawer.
    // Các CustomButton sẽ được vẽ bởi BillUtilsBaseEditor.
    [CustomEditor(typeof(DictionaryExample)), CanEditMultipleObjects]
    public class DictionaryExampleEditor : BillUtilsBaseEditor { }

    [CustomEditor(typeof(SerializeGUIDExample)), CanEditMultipleObjects]
    public class SerializeGUIDExampleEditor : BillUtilsBaseEditor { }

    // NEW EDITORS FOR NEW EXAMPLES
    [CustomEditor(typeof(ProgressBarExample)), CanEditMultipleObjects]
    public class ProgressBarExampleEditor : BillUtilsBaseEditor { }

    [CustomEditor(typeof(SliderExample)), CanEditMultipleObjects]
    public class SliderExampleEditor : BillUtilsBaseEditor { }

    // Custom Editor cho LayoutExample để trình bày các nhóm và Foldout
    [CustomEditor(typeof(LayoutExample)), CanEditMultipleObjects]
    public class LayoutExampleEditor : BillUtilsBaseEditor
    {
        public override void OnInspectorGUI()
        {
            // Luôn gọi base.OnInspectorGUI() để các CustomButtonAttribute hoạt động
            // và để tự động vẽ các PropertyField trừ m_Script
            base.OnInspectorGUI();

            serializedObject.Update();

            LayoutExample myTarget = (LayoutExample)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Custom Layout Elements", EditorStyles.boldLabel);

            // Basic Settings Group with Foldout
            myTarget.showBasicSettings = EditorGUILayout.Foldout(myTarget.showBasicSettings, "Basic Settings", true, EditorStyles.foldoutHeader);
            if (myTarget.showBasicSettings)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("objectName"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("objectScale"));
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();

            // Advanced Settings Group with Foldout
            myTarget.showAdvancedSettings = EditorGUILayout.Foldout(myTarget.showAdvancedSettings, "Advanced Settings", true, EditorStyles.foldoutHeader);
            if (myTarget.showAdvancedSettings)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("offset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rotation"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("tagList"));
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

    // Custom Editor cho GridExample để vẽ lưới
    [CustomEditor(typeof(GridExample)), CanEditMultipleObjects]
    public class GridExampleEditor : BillUtilsBaseEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI(); // For CustomHeader, CustomButton

            serializedObject.Update();

            GridExample myTarget = (GridExample)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            SerializedProperty gridSizeXProp = serializedObject.FindProperty("gridSizeX");
            SerializedProperty gridSizeYProp = serializedObject.FindProperty("gridSizeY");

            // Đảm bảo kích thước tối thiểu là 1
            gridSizeXProp.intValue = EditorGUILayout.IntField("Grid Size X", Mathf.Max(1, gridSizeXProp.intValue));
            gridSizeYProp.intValue = EditorGUILayout.IntField("Grid Size Y", Mathf.Max(1, gridSizeYProp.intValue));

            if (EditorGUI.EndChangeCheck())
            {
                myTarget.InitializeGridData(); // Khởi tạo lại lưới nếu kích thước thay đổi
                serializedObject.Update(); // Cập nhật serializedObject để phản ánh thay đổi kích thước List
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grid Data (Click to toggle 0/1)", EditorStyles.boldLabel);

            SerializedProperty gridDataProp = serializedObject.FindProperty("gridData");

            int currentX = gridSizeXProp.intValue;
            int currentY = gridSizeYProp.intValue;

            // Đảm bảo gridData list size khớp với gridSizeX * gridSizeY
            if (gridDataProp.arraySize != currentX * currentY)
            {
                // Nếu kích thước không khớp (ví dụ: sau một lỗi hoặc thao tác undo/redo),
                // cố gắng khởi tạo lại.
                myTarget.InitializeGridData();
                serializedObject.Update(); // Rất quan trọng để cập nhật lại SerializedObject sau khi thay đổi dữ liệu trực tiếp
            }

            // Styling for grid cells
            GUIStyle gridButtonStyle = new GUIStyle(GUI.skin.button);
            gridButtonStyle.fixedWidth = 30;
            gridButtonStyle.fixedHeight = 30;
            gridButtonStyle.margin = new RectOffset(2, 2, 2, 2);

            for (int y = 0; y < currentY; y++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace(); // Căn giữa lưới
                for (int x = 0; x < currentX; x++)
                {
                    int index = y * currentX + x;
                    if (index >= gridDataProp.arraySize) continue; // Safety check

                    SerializedProperty cellProp = gridDataProp.GetArrayElementAtIndex(index);

                    Color originalGUIColor = GUI.backgroundColor;
                    // Màu cho ô đã chọn và chưa chọn
                    GUI.backgroundColor = cellProp.intValue == 1 ? Color.cyan : (cellProp.intValue == 0 ? new Color(0.7f, 0.7f, 0.7f) : Color.red); // Default to red for unexpected values

                    if (GUILayout.Button(cellProp.intValue.ToString(), gridButtonStyle))
                    {
                        // Left-click to toggle (0 -> 1 -> 0)
                        cellProp.intValue = (cellProp.intValue == 0) ? 1 : 0;
                    }
                    GUI.backgroundColor = originalGUIColor; // Reset color
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif