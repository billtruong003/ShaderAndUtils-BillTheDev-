// File: Assets/Utils/Bill/InspectorCustom/ConcreteSerializableDictionaries.cs
using System;
using UnityEngine;

namespace Utils.Bill.InspectorCustom
{
    // Các lớp Dictionary cụ thể mà Unity có thể serialize.
    // Bạn cần định nghĩa một lớp cụ thể cho mỗi cặp Key-Value Type mà bạn muốn serialize.
    // Ví dụ:
    [Serializable]
    public class StringFloatDictionary : SerializableDictionary<string, float> { }

    [Serializable]
    public class IntStringDictionary : SerializableDictionary<int, string> { }

    // Thêm các loại khác nếu bạn cần:
    // [Serializable]
    // public class StringIntDictionary : SerializableDictionary<string, int> { }

    // [Serializable]
    // public class StringGameObjectDictionary : SerializableDictionary<string, GameObject> { }

    // [Serializable]
    // public class StringSpriteDictionary : SerializableDictionary<string, Sprite> { }
}