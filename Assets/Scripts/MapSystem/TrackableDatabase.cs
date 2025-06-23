// File: TrackableDatabase.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Attribute này cho phép bạn tạo một instance của ScriptableObject trong Project
// bằng cách chuột phải -> Create -> Minimap -> Trackable Database
[CreateAssetMenu(fileName = "TrackableDatabase", menuName = "Minimap/Trackable Database")]
public class TrackableDatabase : ScriptableObject
{
    // Danh sách các loại đối tượng và dữ liệu tương ứng của chúng
    [SerializeField]
    private List<TrackableTypeData> trackableTypesData;

    // Sử dụng Dictionary để truy cập cực nhanh, thay vì phải lặp qua danh sách mỗi lần
    private Dictionary<TrackableType, TrackableTypeData> dataDictionary;

    // OnEnable được gọi khi ScriptableObject được load
    private void OnEnable()
    {
        dataDictionary = new Dictionary<TrackableType, TrackableTypeData>();
        foreach (var data in trackableTypesData)
        {
            if (!dataDictionary.ContainsKey(data.type))
            {
                dataDictionary.Add(data.type, data);
            }
            else
            {
                Debug.LogWarning($"TrackableDatabase: Loại '{data.type}' bị trùng lặp!");
            }
        }
    }

    /// <summary>
    /// Lấy dữ liệu của một loại đối tượng cụ thể.
    /// </summary>
    /// <param name="type">Loại đối tượng cần lấy dữ liệu.</param>
    /// <returns>Dữ liệu tương ứng hoặc null nếu không tìm thấy.</returns>
    public TrackableTypeData GetData(TrackableType type)
    {
        if (dataDictionary.TryGetValue(type, out TrackableTypeData data))
        {
            return data;
        }

        Debug.LogWarning($"TrackableDatabase: Không tìm thấy dữ liệu cho loại '{type}'.");
        return null;
    }
}
// File: TrackableType.cs

// Enum này định nghĩa tất cả các loại đối tượng có thể xuất hiện trên minimap.
public enum TrackableType
{
    None, // Mặc định, không hiển thị
    Enemy_Melee,
    Enemy_Ranged,
    Boss,
    QuestItem,
    FriendlyNPC,
    Chest,
    ExitPoint
}

// Đánh dấu [System.Serializable] để Unity có thể hiển thị nó trong Inspector
[System.Serializable]
public class TrackableTypeData
{
    public TrackableType type;
    public Sprite iconSprite;
    public Color iconColor = Color.white;
    public float iconSize = 20f;
}