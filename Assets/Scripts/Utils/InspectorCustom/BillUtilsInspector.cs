using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditorInternal;
#endif
namespace Utils.Bill.InspectorCustom
{
    // =================================================================================================
    // ATTRIBUTES: PHẢI NẰM NGOÀI #if UNITY_EDITOR để các script runtime có thể tham chiếu chúng.
    // =================================================================================================

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class CustomHeaderAttribute : PropertyAttribute
    {
        public string HeaderText { get; }
        public Color HeaderColor { get; }
        public string Tooltip { get; }
        public FontStyle FontStyle { get; }

        public CustomHeaderAttribute(string headerText, string tooltip = "", string colorHex = "#FFFFFF", FontStyle fontStyle = FontStyle.Bold)
        {
            HeaderText = headerText;
            Tooltip = tooltip;
            ColorUtility.TryParseHtmlString(colorHex, out Color color);
            HeaderColor = color;
            FontStyle = fontStyle;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CustomButtonAttribute : PropertyAttribute
    {
        public string ButtonText { get; }
        public string Tooltip { get; }
        public Color ButtonColor { get; }

        public CustomButtonAttribute(string buttonText = null, string tooltip = "", string colorHex = "#4CAF50")
        {
            ButtonText = buttonText;
            Tooltip = tooltip;
            ColorUtility.TryParseHtmlString(colorHex, out Color color);
            ButtonColor = color;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class CustomDictionaryAttribute : PropertyAttribute
    {
        // Attribute này không còn dùng để vẽ dictionary trực tiếp mà chỉ là một marker nếu cần
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ReadOnlyAttribute : PropertyAttribute
    {
        public Color BackgroundColor { get; }

        public ReadOnlyAttribute(string colorHex = "#E0E0E0")
        {
            ColorUtility.TryParseHtmlString(colorHex, out Color color);
            BackgroundColor = color;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class MinValueAttribute : PropertyAttribute
    {
        public float Min { get; }
        public float Max { get; }
        public Color BackgroundColor { get; }

        public MinValueAttribute(float min, float max = float.MaxValue, string colorHex = "#FFFFFF")
        {
            Min = min;
            Max = max;
            ColorUtility.TryParseHtmlString(colorHex, out Color color);
            BackgroundColor = color;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SerializeGUIDAttribute : PropertyAttribute
    {
        public Color BackgroundColor { get; }
        public bool ReadOnly { get; }

        public SerializeGUIDAttribute(string colorHex = "#FFFFFF", bool readOnly = false)
        {
            ColorUtility.TryParseHtmlString(colorHex, out Color color);
            BackgroundColor = color;
            ReadOnly = readOnly;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ProgressBarAttribute : PropertyAttribute
    {
        public string Label { get; }
        public float MaxValue { get; }
        public Color BarColor { get; }

        public ProgressBarAttribute(string label, float maxValue, string colorHex = "#4CAF50")
        {
            Label = label;
            MaxValue = maxValue;
            ColorUtility.TryParseHtmlString(colorHex, out Color color);
            BarColor = color;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class CustomSliderAttribute : PropertyAttribute
    {
        public float Min { get; }
        public float Max { get; }
        public string Tooltip { get; }

        public CustomSliderAttribute(float min, float max, string tooltip = "")
        {
            Min = min;
            Max = max;
            Tooltip = tooltip;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SerializeIfAttribute : PropertyAttribute
    {
        public string ConditionFieldName { get; }
        public object ConditionValue { get; }

        public SerializeIfAttribute(string conditionFieldName, object conditionValue)
        {
            ConditionFieldName = conditionFieldName;
            ConditionValue = conditionValue;
        }
    }

    public enum PreviewAlignment { Left, Center, Right }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ShowAssetPreviewAttribute : PropertyAttribute
    {
        public int Width { get; }
        public int Height { get; }
        public PreviewAlignment Alignment { get; }

        /// <summary>
        /// Hiển thị bản xem trước của một asset (Object) trong Inspector.
        /// </summary>
        /// <param name="width">Chiều rộng của khung xem trước.</param>
        /// <param name="height">Chiều cao của khung xem trước.</param>
        /// <param name="alignment">Căn lề của khung xem trước.</param>
        public ShowAssetPreviewAttribute(int width = 100, int height = 100, PreviewAlignment alignment = PreviewAlignment.Center)
        {
            Width = width;
            Height = height;
            Alignment = alignment;
        }
    }

    // =================================================================================================
    // PROPERTY DRAWERS: PHẢI NẰM TRONG #if UNITY_EDITOR
    // =================================================================================================
#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(CustomHeaderAttribute))]
    public class CustomHeaderDrawer : DecoratorDrawer
    {
        public override float GetHeight()
        {
            return base.GetHeight() + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position)
        {
            var attr = attribute as CustomHeaderAttribute;
            position.y += EditorGUIUtility.standardVerticalSpacing;
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontStyle = attr.FontStyle,
                normal = { textColor = attr.HeaderColor }
            };
            EditorGUI.LabelField(position, new GUIContent(attr.HeaderText, attr.Tooltip), style);
        }
    }

    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = attribute as ReadOnlyAttribute;
            label.tooltip = "This field is read-only.";
            EditorGUI.BeginProperty(position, label, property);
            GUI.backgroundColor = attr.BackgroundColor;
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
            GUI.backgroundColor = Color.white;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }

    [CustomPropertyDrawer(typeof(MinValueAttribute))]
    public class MinValueDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = attribute as MinValueAttribute;
            label.tooltip = $"Range: {attr.Min} to {attr.Max}";
            EditorGUI.BeginProperty(position, label, property);
            GUI.backgroundColor = attr.BackgroundColor;
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(position, property, label, true);
            if (EditorGUI.EndChangeCheck())
            {
                if (property.propertyType == SerializedPropertyType.Float)
                {
                    property.floatValue = Mathf.Clamp(property.floatValue, attr.Min, attr.Max);
                }
                else if (property.propertyType == SerializedPropertyType.Integer)
                {
                    property.intValue = Mathf.Clamp(property.intValue, (int)attr.Min, (int)attr.Max);
                }
                else
                {
                    Debug.LogWarning($"MinValueAttribute applied to unsupported property type: {property.propertyType}");
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }

    [CustomPropertyDrawer(typeof(SerializableDictionary<,>), true)]
    public class CustomDictionaryDrawer : PropertyDrawer
    {
        private Dictionary<string, ReorderableList> _lists = new Dictionary<string, ReorderableList>();
        private const float SPACING = 5f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            ReorderableList list = GetList(property);
            if (list == null) return EditorGUIUtility.singleLineHeight * 2;

            float height = EditorGUIUtility.singleLineHeight;
            height += list.GetHeight();
            height += SPACING;

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, label);

            ReorderableList list = GetList(property);
            if (list == null)
            {
                EditorGUI.HelpBox(new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, EditorGUIUtility.singleLineHeight * 2),
                                  "Dictionary serialization setup is invalid.", MessageType.Error);
                EditorGUI.EndProperty();
                return;
            }

            Rect listRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, list.GetHeight());
            list.DoList(listRect);

            EditorGUI.EndProperty();
        }

        private ReorderableList GetList(SerializedProperty property)
        {
            string path = property.propertyPath;

            if (!_lists.TryGetValue(path, out ReorderableList list))
            {
                SerializedProperty keysProperty = property.FindPropertyRelative("keys");
                SerializedProperty valuesProperty = property.FindPropertyRelative("values");

                if (keysProperty == null || valuesProperty == null)
                {
                    Debug.LogError($"[CustomDictionaryDrawer] Could not find 'keys' or 'values' properties for {property.propertyPath}. " +
                                   "Ensure your SerializableDictionary implementation has these fields.");
                    return null;
                }

                list = new ReorderableList(property.serializedObject, keysProperty, true, true, true, true);
                _lists[path] = list;

                list.drawHeaderCallback = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, label: $"Dictionary ({keysProperty.arraySize} items)");
                };

                list.elementHeightCallback = (int index) =>
                {
                    float keyHeight = EditorGUI.GetPropertyHeight(keysProperty.GetArrayElementAtIndex(index), true);
                    float valueHeight = EditorGUI.GetPropertyHeight(valuesProperty.GetArrayElementAtIndex(index), true);
                    return Mathf.Max(keyHeight, valueHeight) + EditorGUIUtility.standardVerticalSpacing;
                };

                list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    SerializedProperty keyProp = keysProperty.GetArrayElementAtIndex(index);
                    SerializedProperty valueProp = valuesProperty.GetArrayElementAtIndex(index);

                    float halfWidth = rect.width / 2f;
                    Rect keyRect = new Rect(rect.x, rect.y, halfWidth - SPACING, EditorGUIUtility.singleLineHeight);
                    Rect valueRect = new Rect(rect.x + halfWidth + SPACING, rect.y, halfWidth - SPACING, EditorGUIUtility.singleLineHeight);

                    EditorGUI.PropertyField(keyRect, keyProp, GUIContent.none);
                    EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none);
                };

                list.onAddCallback = (ReorderableList l) =>
                {
                    keysProperty.arraySize++;
                    valuesProperty.arraySize++;
                    l.index = l.count - 1;
                    var newKeyProp = keysProperty.GetArrayElementAtIndex(l.index);
                    var newValueProp = valuesProperty.GetArrayElementAtIndex(l.index);

                    switch (newKeyProp.propertyType)
                    {
                        case SerializedPropertyType.Integer:
                            newKeyProp.intValue = 0;
                            break;
                        case SerializedPropertyType.Float:
                            newKeyProp.floatValue = 0f;
                            break;
                        case SerializedPropertyType.Boolean:
                            newKeyProp.boolValue = false;
                            break;
                        case SerializedPropertyType.String:
                            newKeyProp.stringValue = string.Empty;
                            break;
                        case SerializedPropertyType.ObjectReference:
                            newKeyProp.objectReferenceValue = null;
                            break;
                    }

                    switch (newValueProp.propertyType)
                    {
                        case SerializedPropertyType.Integer:
                            newValueProp.intValue = 0;
                            break;
                        case SerializedPropertyType.Float:
                            newValueProp.floatValue = 0f;
                            break;
                        case SerializedPropertyType.Boolean:
                            newValueProp.boolValue = false;
                            break;
                        case SerializedPropertyType.String:
                            newValueProp.stringValue = string.Empty;
                            break;
                        case SerializedPropertyType.ObjectReference:
                            newValueProp.objectReferenceValue = null;
                            break;
                    }
                };

                list.onRemoveCallback = (ReorderableList l) =>
                {
                    if (EditorUtility.DisplayDialog("Warning!", "Are you sure you want to delete the selected element?", "Yes", "No"))
                    {
                        int oldIndex = l.index;
                        keysProperty.DeleteArrayElementAtIndex(oldIndex);
                        valuesProperty.DeleteArrayElementAtIndex(oldIndex);
                        property.serializedObject.ApplyModifiedProperties();

                        if (keysProperty.arraySize > 0)
                        {
                            l.index = Mathf.Min(oldIndex, keysProperty.arraySize - 1);
                        }
                        else
                        {
                            l.index = -1;
                        }
                    }
                };

                list.onReorderCallback = (ReorderableList l) =>
                {
                    // Không cần thêm logic vì ReorderableList tự đồng bộ keys và values
                };

                list.onChangedCallback = (ReorderableList l) =>
                {
                    // Logic OnAfterDeserialize của SerializableDictionary sẽ tự động xử lý
                };
            }

            return list;
        }
    }

    [CustomPropertyDrawer(typeof(SerializeGUIDAttribute))]
    public class SerializeGUIDDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = attribute as SerializeGUIDAttribute;
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType == SerializedPropertyType.String)
            {
                GUI.backgroundColor = attr.BackgroundColor;
                GUI.enabled = !attr.ReadOnly;
                label.tooltip = attr.ReadOnly ? "This GUID is read-only." : "Enter a valid GUID or generate one.";

                string newValue = EditorGUI.TextField(position, label, property.stringValue);

                if (!attr.ReadOnly)
                {
                    if (!string.IsNullOrEmpty(newValue) && Guid.TryParse(newValue, out _))
                    {
                        property.stringValue = newValue;
                    }
                    else if (!string.IsNullOrEmpty(newValue) && !Guid.TryParse(newValue, out _))
                    {
                        GUIContent warningLabel = new GUIContent("", "Invalid GUID format");
                        Rect warningRect = new Rect(position.x + EditorGUIUtility.labelWidth + 5, position.y, position.width - EditorGUIUtility.labelWidth - 5, position.height);
                        EditorGUI.LabelField(warningRect, warningLabel, EditorStyles.miniLabel);
                    }
                }

                GUI.enabled = true;
                GUI.backgroundColor = Color.white;
            }
            else
            {
                EditorGUI.LabelField(position, label, new GUIContent("SerializeGUID only supports string fields"));
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }

    [CustomPropertyDrawer(typeof(ProgressBarAttribute))]
    public class ProgressBarDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = attribute as ProgressBarAttribute;
            if (property.propertyType == SerializedPropertyType.Float || property.propertyType == SerializedPropertyType.Integer)
            {
                float value = property.propertyType == SerializedPropertyType.Float ? property.floatValue : property.intValue;
                float progress = Mathf.Clamp01(value / attr.MaxValue);

                Rect textRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(textRect, $"{attr.Label}: {value:F2} / {attr.MaxValue:F2}");

                Rect barRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);

                EditorGUI.DrawRect(barRect, new Color(0.1f, 0.1f, 0.1f, 0.5f));
                Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * progress, barRect.height);
                EditorGUI.DrawRect(fillRect, attr.BarColor);

                Rect valueRect = new Rect(position.x, position.y + 2 * EditorGUIUtility.singleLineHeight + 2 * EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(valueRect, property, new GUIContent(""), true);
            }
            else
            {
                EditorGUI.LabelField(position, label, new GUIContent("ProgressBar only supports float or int fields."));
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 3 + EditorGUIUtility.standardVerticalSpacing * 2;
        }
    }

    [CustomPropertyDrawer(typeof(CustomSliderAttribute))]
    public class CustomSliderDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = attribute as CustomSliderAttribute;
            label.tooltip = attr.Tooltip;

            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType == SerializedPropertyType.Float)
            {
                property.floatValue = EditorGUI.Slider(position, label, property.floatValue, attr.Min, attr.Max);
            }
            else if (property.propertyType == SerializedPropertyType.Integer)
            {
                property.intValue = (int)EditorGUI.Slider(position, label, property.intValue, (int)attr.Min, (int)attr.Max);
            }
            else
            {
                EditorGUI.LabelField(position, label, new GUIContent("CustomSlider only supports float or int fields."));
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }

    [CustomPropertyDrawer(typeof(SerializeIfAttribute))]
    public class SerializeIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = attribute as SerializeIfAttribute;
            var conditionProperty = property.serializedObject.FindProperty(attr.ConditionFieldName);

            if (conditionProperty != null)
            {
                bool shouldSerialize = CheckCondition(conditionProperty, attr.ConditionValue);
                if (shouldSerialize)
                {
                    EditorGUI.PropertyField(position, property, label, true);
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUI.PropertyField(position, property, label, true);
                    EditorGUI.EndDisabledGroup();
                }
            }
            else
            {
                EditorGUI.LabelField(position, label, new GUIContent($"Condition field '{attr.ConditionFieldName}' not found."));
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var attr = attribute as SerializeIfAttribute;
            var conditionProperty = property.serializedObject.FindProperty(attr.ConditionFieldName);

            if (conditionProperty != null)
            {
                bool shouldSerialize = CheckCondition(conditionProperty, attr.ConditionValue);
                return EditorGUI.GetPropertyHeight(property, label, true);
            }
            return EditorGUIUtility.singleLineHeight;
        }

        private bool CheckCondition(SerializedProperty conditionProperty, object conditionValue)
        {
            switch (conditionProperty.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    return conditionProperty.boolValue == (bool)conditionValue;
                case SerializedPropertyType.Integer:
                    return conditionProperty.intValue == (int)conditionValue;
                case SerializedPropertyType.Float:
                    return conditionProperty.floatValue == (float)conditionValue;
                case SerializedPropertyType.String:
                    return conditionProperty.stringValue == (string)conditionValue;
                case SerializedPropertyType.Enum:
                    return conditionProperty.enumValueIndex == (int)conditionValue;
                default:
                    Debug.LogWarning($"Unsupported condition property type: {conditionProperty.propertyType}");
                    return false;
            }
        }
    }

    [CustomPropertyDrawer(typeof(ShowAssetPreviewAttribute))]
    public class ShowAssetPreviewDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = attribute as ShowAssetPreviewAttribute;

            // Bắt đầu vẽ property
            EditorGUI.BeginProperty(position, label, property);

            // Chỉ hoạt động với các trường tham chiếu đến Object
            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                // Vẽ trường thuộc tính mặc định
                Rect propertyRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(propertyRect, property, label, true);

                // Nếu có asset được gán
                if (property.objectReferenceValue != null)
                {
                    // Lấy texture xem trước từ cache của Unity
                    Texture2D previewTexture = AssetPreview.GetAssetPreview(property.objectReferenceValue);

                    if (previewTexture != null)
                    {
                        // Tính toán vị trí cho khung xem trước
                        Rect previewRect = new Rect(position.x,
                                                    position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing,
                                                    attr.Width,
                                                    attr.Height);

                        // Căn lề cho khung xem trước
                        switch (attr.Alignment)
                        {
                            case PreviewAlignment.Center:
                                previewRect.x = position.x + (position.width - attr.Width) / 2;
                                break;
                            case PreviewAlignment.Right:
                                previewRect.x = position.x + position.width - attr.Width;
                                break;
                            case PreviewAlignment.Left:
                            default:
                                // Vị trí x đã đúng, không cần thay đổi
                                break;
                        }

                        // Vẽ texture xem trước
                        GUI.DrawTexture(previewRect, previewTexture, ScaleMode.ScaleToFit);
                    }
                    else
                    {
                        // Nếu preview chưa được tạo, yêu cầu editor repaint để thử lại ở frame sau
                        // Điều này hữu ích khi asset mới được gán.
                        HandleUtility.Repaint();
                    }
                }
            }
            else
            {
                EditorGUI.HelpBox(position, "ShowAssetPreview only works on Object reference fields.", MessageType.Warning);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var attr = attribute as ShowAssetPreviewAttribute;

            // Chiều cao cơ bản của một property field
            float baseHeight = EditorGUI.GetPropertyHeight(property, label, true);

            // Nếu có asset được gán, cộng thêm chiều cao của preview và khoảng cách
            if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null)
            {
                return baseHeight + attr.Height + EditorGUIUtility.standardVerticalSpacing;
            }

            // Nếu không, trả về chiều cao mặc định
            return baseHeight;
        }
    }

#endif
}