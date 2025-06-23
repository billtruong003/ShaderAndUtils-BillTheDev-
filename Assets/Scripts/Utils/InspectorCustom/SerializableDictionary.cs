// File: Assets/Utils/Bill/InspectorCustom/SerializableDictionary.cs
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Utils.Bill.InspectorCustom
{
    // Lớp cơ sở trừu tượng cho Dictionary có thể serialize.
    // Nó sẽ lưu trữ keys và values trong các List riêng biệt và chuyển đổi chúng.
    [Serializable]
    public abstract class SerializableDictionary<TKey, TValue> : ISerializationCallbackReceiver
    {
        [SerializeField] private List<TKey> keys = new List<TKey>();
        [SerializeField] private List<TValue> values = new List<TValue>();

        // Dictionary thực tế sẽ được sử dụng trong runtime
        private Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();

        // Phương thức để lấy Dictionary thực tế
        public Dictionary<TKey, TValue> GetDictionary() => _dictionary;

        // ISerializationCallbackReceiver: Được gọi trước khi Unity serialize đối tượng
        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (var pair in _dictionary)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }

        // ISerializationCallbackReceiver: Được gọi sau khi Unity deserialize đối tượng
        public void OnAfterDeserialize()
        {
            _dictionary.Clear();
            if (keys.Count != values.Count)
            {
                Debug.LogWarning($"[SerializableDictionary] Mismatch between key count ({keys.Count}) and value count ({values.Count}) during deserialization. Clearing dictionary to prevent errors. Property Path: {GetPropertyPathForDebug()}");
            }
            else
            {
                for (int i = 0; i < keys.Count; i++)
                {
                    // Xử lý trường hợp key trùng lặp nếu có
                    if (keys[i] != null && !_dictionary.ContainsKey(keys[i])) // Kiểm tra key null trước khi dùng ContainsKey
                    {
                        _dictionary.Add(keys[i], values[i]);
                    }
                    else
                    {
                        Debug.LogWarning($"[SerializableDictionary] Duplicate or null key '{keys[i]}' found during deserialization. Skipping this entry. Property Path: {GetPropertyPathForDebug()}");
                    }
                }
            }
        }

        // Helper để lấy đường dẫn thuộc tính cho debug, chỉ hoạt động trong Editor
        private string GetPropertyPathForDebug()
        {
#if UNITY_EDITOR
            // Đây là một cách hacky để lấy đường dẫn thuộc tính từ EditorContext
            // Nó không phải là một cách chuẩn và có thể không hoạt động trong mọi trường hợp
            // nhưng hữu ích cho việc debug trong Editor.
            try
            {
                if (UnityEditor.Selection.activeObject != null && UnityEditor.Selection.activeGameObject != null)
                {
                    var components = UnityEditor.Selection.activeGameObject.GetComponents<MonoBehaviour>();
                    foreach (var comp in components)
                    {
                        if (comp != null)
                        {
                            var serializedObject = new UnityEditor.SerializedObject(comp);
                            var prop = serializedObject.GetIterator();
                            if (prop.NextVisible(true)) // Iterate through all properties
                            {
                                do
                                {
                                    // prop.managedReferenceValue is for polymorphic serialization,
                                    // for direct serialized fields, it's just prop.objectReferenceValue or prop.GetValue() via reflection
                                    // Given this is an instance of SerializableDictionary, we need to find if any field holds this instance
                                    var fieldInfo = prop.serializedObject.targetObject.GetType().GetField(prop.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                    if (fieldInfo != null && fieldInfo.FieldType.IsSubclassOf(typeof(SerializableDictionary<,>)) && fieldInfo.GetValue(prop.serializedObject.targetObject) == this)
                                    {
                                        return prop.propertyPath;
                                    }

                                } while (prop.NextVisible(false));
                            }
                        }
                    }
                }
            }
            catch { }
#endif
            return "Unknown Path";
        }


        // Các phương thức truy cập Dictionary để sử dụng trong code
        public TValue this[TKey key]
        {
            get => _dictionary[key];
            set => _dictionary[key] = value;
        }

        public int Count => _dictionary.Count;
        public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);
        public void Add(TKey key, TValue value) => _dictionary.Add(key, value);
        public bool Remove(TKey key) => _dictionary.Remove(key);
        public void Clear() => _dictionary.Clear();
        public Dictionary<TKey, TValue>.Enumerator GetEnumerator() => _dictionary.GetEnumerator();
    }
}