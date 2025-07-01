using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Luminaria/Database/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemDefinition> allItems;

    // Dùng Dictionary để tra cứu ID cực nhanh (O(1))
    // Sẽ được khởi tạo một lần lúc game bắt đầu
    private Dictionary<string, ItemDefinition> itemDictionary;

    public void Initialize()
    {
        if (itemDictionary != null) return; // Đã khởi tạo rồi thì thôi

        itemDictionary = new Dictionary<string, ItemDefinition>();
        foreach (var item in allItems)
        {
            if (!itemDictionary.ContainsKey(item.ItemID))
            {
                itemDictionary.Add(item.ItemID, item);
            }
            else
            {
                Debug.LogWarning($"Duplicate ItemID found in Database: {item.ItemID}");
            }
        }
    }

    public ItemDefinition GetItemByID(string id)
    {
        if (itemDictionary == null) Initialize();

        if (itemDictionary.TryGetValue(id, out ItemDefinition item))
        {
            return item;
        }
        Debug.LogError($"Item with ID '{id}' not found in database!");
        return null;
    }
}