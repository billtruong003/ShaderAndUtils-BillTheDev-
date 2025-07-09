using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ModularInventory.Data;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

[CreateAssetMenu(fileName = "GameItemDatabase", menuName = "My Indie Game/Database/Item Database")]
public class ItemDatabase : SerializedScriptableObject
{
    [Title("Game Item Database", "Central management for all in-game items.", TitleAlignments.Centered)]
    [InfoBox("Click an item to edit it inline. Use the tools below to manage the database.", InfoMessageType.Info)]
    [ListDrawerSettings(DraggableItems = true, Expanded = false, ShowPaging = true, NumberOfItemsPerPage = 20)]
    [InlineEditor(Expanded = true, DrawHeader = false)]
    public List<ItemDefinition> allItems;

#if UNITY_EDITOR
    [Title("Database Tools")]
    [GUIColor(0.7f, 1f, 0.7f)]
    [Button(ButtonSizes.Large, Name = "Scan Project for All Items")]
    [PropertyOrder(-1)]
    private void FindAllItemsInProject()
    {
        this.allItems = new List<ItemDefinition>();
        string[] guids = AssetDatabase.FindAssets("t:ItemDefinition");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item != null && !this.allItems.Contains(item))
            {
                this.allItems.Add(item);
            }
        }
        this.allItems = this.allItems.OrderBy(item => item.ItemID).ToList();
    }

    [BoxGroup("Validation", ShowLabel = false)]
    [Button("Validate Database for Duplicates & Errors")]
    private void ValidateDatabase()
    {
        HashSet<string> ids = new HashSet<string>();
        bool hasError = false;
        if (allItems == null) return;

        for (int i = 0; i < allItems.Count; i++)
        {
            var item = allItems[i];
            if (item == null)
            {
                Debug.LogError($"ItemDatabase contains a NULL entry at index {i}. Please remove it.", this);
                hasError = true;
                continue;
            }
            if (string.IsNullOrEmpty(item.ItemID))
            {
                Debug.LogError($"Item '{item.name}' has a NULL or EMPTY ItemID!", item);
                hasError = true;
            }
            else if (!ids.Add(item.ItemID))
            {
                Debug.LogError($"Duplicate ItemID '{item.ItemID}' found on item '{item.name}'. ItemIDs must be unique.", item);
                hasError = true;
            }
        }
        if (!hasError)
        {
            Debug.Log("<color=cyan>ItemDatabase validation complete. No errors found!</color>");
        }
    }

    [Title("Create New Item")]
    [PropertySpace(20)]
    [ValueDropdown("GetItemTypes", AppendNextDrawer = true)]
    [OnValueChanged("CreateNewItemAsset")]
    public System.Type itemTypeToCreate;

    private IEnumerable<System.Type> GetItemTypes()
    {
        return typeof(ItemDefinition).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(ItemDefinition).IsAssignableFrom(t));
    }

    private void CreateNewItemAsset()
    {
        if (this.itemTypeToCreate == null) return;
        var asset = CreateInstance(this.itemTypeToCreate);
        string path = "Assets/MyIndieGame/Data/Items/"; // Bạn có thể thay đổi đường dẫn này
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        string fullPath = AssetDatabase.GenerateUniqueAssetPath(path + "New" + this.itemTypeToCreate.Name + ".asset");
        AssetDatabase.CreateAsset(asset, fullPath);
        this.allItems.Add((ItemDefinition)asset);
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
        this.itemTypeToCreate = null;
    }
#endif

    private Dictionary<string, ItemDefinition> itemDictionary;

    public void Initialize()
    {
        if (itemDictionary != null && itemDictionary.Count > 0) return;

        itemDictionary = new Dictionary<string, ItemDefinition>();
        if (allItems == null) return;

        foreach (var item in allItems.Where(i => i != null && !string.IsNullOrEmpty(i.ItemID)))
        {
            if (!itemDictionary.ContainsKey(item.ItemID))
            {
                itemDictionary.Add(item.ItemID, item);
            }
        }
    }

    public ItemDefinition GetItemByID(string id)
    {
        if (itemDictionary == null) Initialize();
        if (string.IsNullOrEmpty(id)) return null;
        itemDictionary.TryGetValue(id, out ItemDefinition item);
        return item;
    }
}