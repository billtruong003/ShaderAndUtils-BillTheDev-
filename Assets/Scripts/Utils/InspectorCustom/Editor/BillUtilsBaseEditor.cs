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

            foreach (var (method, buttonAttr) in buttonMethods)
            {
                string buttonText = string.IsNullOrEmpty(buttonAttr.ButtonText) ? method.Name : buttonAttr.ButtonText;
                GUI.backgroundColor = buttonAttr.ButtonColor;
                if (GUILayout.Button(new GUIContent(buttonText, buttonAttr.Tooltip)))
                {
                    try
                    {
                        // Đã kiểm tra parameters.Length == 0 ở OnEnable, nên ở đây an toàn để gọi.
                        method.Invoke(target, null);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Lỗi khi gọi phương thức {method.Name}: {ex.Message}");
                    }
                }
                GUI.backgroundColor = Color.white;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif