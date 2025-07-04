// File: Assets/MyIndieGame/Scripts/Characters/Inventory/ItemDatabase.cs (Phiên bản Odin với Inline Editing)

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using System.IO;
#endif

using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "GameItemDatabase", menuName = "Luminaria/Database/Item Database")]
public class ItemDatabase : SerializedScriptableObject
{
    //================================================================================
    // DỮ LIỆU CHÍNH (DATA)
    //================================================================================

    [Title("Item Database", "Trung tâm quản lý và chỉnh sửa tất cả các vật phẩm trong game.", TitleAlignments.Centered)]
    [InfoBox("Click vào từng vật phẩm để mở rộng và chỉnh sửa trực tiếp. Dùng các công cụ bên dưới để quản lý database.", InfoMessageType.Info)]

    // [ListDrawerSettings] là một thuộc tính cực kỳ mạnh mẽ để tùy biến cách hiển thị một List.
    // DraggableItems = true: Cho phép kéo thả để sắp xếp lại các vật phẩm.
    // Expanded = false: Mặc định các phần tử trong list sẽ được thu gọn.
    // ShowPaging = true: Hiển thị phân trang nếu danh sách quá dài (ví dụ > 15 vật phẩm).
    // NumberOfItemsPerPage = 15: Số lượng vật phẩm trên mỗi trang.
    [ListDrawerSettings(DraggableItems = true, Expanded = false, ShowPaging = true, NumberOfItemsPerPage = 15)]

    // [InlineEditor] là chìa khóa để chỉnh sửa nội dung SO ngay tại chỗ.
    // Expanded = true: Khi bạn click vào một vật phẩm, nội dung của nó sẽ được hiển thị đầy đủ.
    // DrawHeader = false: Ẩn đi phần header mặc định của SO để giao diện gọn gàng hơn.
    [InlineEditor(Expanded = true, DrawHeader = false)]
    public List<ItemDefinition> allItems;

    //================================================================================
    // CÁC HÀM TIỆN ÍCH TRONG EDITOR (EDITOR-ONLY FUNCTIONALITY)
    //================================================================================

#if UNITY_EDITOR
    [Title("Database Tools")]
    [GUIColor(0.7f, 1f, 0.7f)] // Màu xanh lá cây
    [Button(ButtonSizes.Large, Name = "Scan Project for All Items")] // Đổi tên nút cho rõ ràng hơn
    [PropertyOrder(-1)] // Đưa nút này lên trên cùng của khu vực Tools
    private void FindAllItemsInProject()
    {
        this.allItems = new List<ItemDefinition>();

        string[] guids = AssetDatabase.FindAssets("t:ItemDefinition");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item != null && !this.allItems.Contains(item)) // Tránh thêm trùng lặp
            {
                this.allItems.Add(item);
            }
        }

        this.allItems = this.allItems.OrderBy(item => item.ItemID).ToList();

        Debug.Log($"<color=green>ItemDatabase: Found and added {this.allItems.Count} items.</color>");
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
    [ValueDropdown("GetItemTypes", AppendNextDrawer = true)] // AppendNextDrawer giữ nút và dropdown trên cùng một hàng
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
        string path = "Assets/MyIndieGame/Data/Items/";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        string fullPath = AssetDatabase.GenerateUniqueAssetPath(path + "New " + this.itemTypeToCreate.Name + ".asset");
        AssetDatabase.CreateAsset(asset, fullPath);

        this.allItems.Add((ItemDefinition)asset);

        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;

        this.itemTypeToCreate = null;
    }
#endif

    //================================================================================
    // LOGIC RUNTIME (GAME LOGIC) - KHÔNG THAY ĐỔI
    //================================================================================

    private Dictionary<string, ItemDefinition> itemDictionary;

    public void Initialize()
    {
        if (itemDictionary != null) return;

        itemDictionary = new Dictionary<string, ItemDefinition>();
        if (allItems == null)
        {
            Debug.LogError("ItemDatabase 'allItems' list is not assigned!", this);
            return;
        }

        foreach (var item in allItems)
        {
            if (item == null) continue;

            if (!string.IsNullOrEmpty(item.ItemID) && !itemDictionary.ContainsKey(item.ItemID))
            {
                itemDictionary.Add(item.ItemID, item);
            }
            else
            {
                if (string.IsNullOrEmpty(item.ItemID)) continue;
                Debug.LogWarning($"Duplicate ItemID '{item.ItemID}' found in Database. The first one was kept.", this);
            }
        }
    }

    public ItemDefinition GetItemByID(string id)
    {
        if (itemDictionary == null)
        {
            Initialize();
        }

        if (string.IsNullOrEmpty(id)) return null;

        if (itemDictionary.TryGetValue(id, out ItemDefinition item))
        {
            return item;
        }

        return null;
    }
}