#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace Utils.Bill.InspectorCustom
{
    public abstract class BillUtilsBaseEditor : Editor
    {
        private List<(MethodInfo Method, CustomButtonAttribute Attribute)> buttonMethods = new List<(MethodInfo, CustomButtonAttribute)>();

        protected virtual void OnEnable()
        {
            var targetType = target.GetType();
            var methods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                if (method.GetParameters().Length == 0)
                {
                    var buttonAttr = method.GetCustomAttribute<CustomButtonAttribute>();
                    if (buttonAttr != null)
                    {
                        buttonMethods.Add((method, buttonAttr));
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Gọi các hàm vẽ tách biệt
            DrawDefaultProperties();
            DrawCustomButtons();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Vẽ tất cả các thuộc tính có thể serialize, trừ m_Script.
        /// Lớp con có thể override hàm này để có hành vi vẽ tùy chỉnh.
        /// </summary>
        protected virtual void DrawDefaultProperties()
        {
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                if (iterator.name == "m_Script")
                {
                    continue;
                }
                EditorGUILayout.PropertyField(iterator, true);
                enterChildren = false;
            }
        }

        /// <summary>
        /// Vẽ các nút bấm được định nghĩa bằng [CustomButton].
        /// Lớp con có thể gọi hàm này trực tiếp.
        /// </summary>
        protected virtual void DrawCustomButtons()
        {
            foreach (var (method, buttonAttr) in buttonMethods)
            {
                string buttonText = string.IsNullOrEmpty(buttonAttr.ButtonText) ? method.Name : buttonAttr.ButtonText;
                GUI.backgroundColor = buttonAttr.ButtonColor;
                if (GUILayout.Button(new GUIContent(buttonText, buttonAttr.Tooltip)))
                {
                    try
                    {
                        method.Invoke(target, null);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Lỗi khi gọi phương thức {method.Name}: {ex.Message}");
                    }
                }
                GUI.backgroundColor = Color.white;
            }
        }
    }
}
#endif