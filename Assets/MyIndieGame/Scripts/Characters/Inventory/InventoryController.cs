using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryController : MonoBehaviour
{
    [Header("Dependencies")]
    public ItemDatabase itemDatabase; // Database chứa mọi vật phẩm

    [Header("Settings")]
    public int initialSlots = 20; // Số ô khởi đầu
    public int maxSlots = 50;     // Số ô tối đa

    // Dữ liệu kho đồ - đây là "source of truth"
    public List<InventorySlot> slots { get; private set; }

    // Sự kiện để thông báo cho UI và các hệ thống khác
    public event Action OnInventoryChanged;

    private StatController statController; // Tham chiếu đến bộ điều khiển chỉ số

    void Awake()
    {
        // Lấy các component cần thiết trên cùng một GameObject
        statController = GetComponent<StatController>();

        // Khởi tạo
        if (itemDatabase != null)
        {
            itemDatabase.Initialize();
        }
        else
        {
            Debug.LogError("ItemDatabase is not assigned to InventoryController!");
        }

        slots = new List<InventorySlot>(initialSlots);
        for (int i = 0; i < initialSlots; i++)
        {
            slots.Add(new InventorySlot());
        }
    }

    #region Public API - Các hàm chính để hệ thống khác gọi

    // Thêm vật phẩm vào kho
    public bool AddItem(string itemID, int amount = 1)
    {
        ItemDefinition itemToAdd = itemDatabase.GetItemByID(itemID);
        if (itemToAdd == null || amount <= 0) return false;

        bool wasAdded = false;

        // Ưu tiên 1: Cộng dồn vào các slot đã có
        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i].IsEmpty() && slots[i].item.ItemID == itemID && slots[i].quantity < itemToAdd.MaxStack)
            {
                int spaceLeft = itemToAdd.MaxStack - slots[i].quantity;
                int amountToAdd = Mathf.Min(amount, spaceLeft);
                slots[i].quantity += amountToAdd;
                amount -= amountToAdd;
                wasAdded = true;
                if (amount <= 0) break;
            }
        }

        // Ưu tiên 2: Thêm vào các slot trống
        if (amount > 0)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsEmpty())
                {
                    int amountToAdd = Mathf.Min(amount, itemToAdd.MaxStack);
                    slots[i] = new InventorySlot(itemToAdd, amountToAdd);
                    amount -= amountToAdd;
                    wasAdded = true;
                    if (amount <= 0) break;
                }
            }
        }

        if (wasAdded)
        {
            OnInventoryChanged?.Invoke();
        }

        // Nếu amount > 0 sau khi chạy hết, nghĩa là kho đã đầy
        if (amount > 0)
        {
            Debug.Log($"Inventory full. Could not add {amount} of {itemID}.");
        }

        return wasAdded && amount == 0;
    }

    // Xóa vật phẩm khỏi một slot cụ thể
    public void RemoveItem(int slotIndex, int amount = 1)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count || slots[slotIndex].IsEmpty() || amount <= 0) return;

        slots[slotIndex].quantity -= amount;
        if (slots[slotIndex].quantity <= 0)
        {
            slots[slotIndex].Clear();
        }
        OnInventoryChanged?.Invoke();
    }

    // Di chuyển vật phẩm giữa các slot
    public void SwapItems(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= slots.Count || toIndex < 0 || toIndex >= slots.Count || fromIndex == toIndex) return;

        InventorySlot temp = slots[fromIndex];
        slots[fromIndex] = slots[toIndex];
        slots[toIndex] = temp;

        OnInventoryChanged?.Invoke();
    }

    // Sử dụng vật phẩm (ví dụ: uống bình máu)
    public void UseItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count || slots[slotIndex].IsEmpty()) return;

        ItemDefinition itemToUse = slots[slotIndex].item;

        // Logic sử dụng dựa trên loại vật phẩm
        if (itemToUse.Type == ItemType.Consumable)
        {
            // TODO: Implement consumable logic (e.g., heal player)
            Debug.Log($"Used {itemToUse.Name}");
            RemoveItem(slotIndex, 1); // Tiêu hao 1 vật phẩm
        }
        else if (itemToUse.Type == ItemType.Equipment)
        {
            // TODO: Equip item logic (call EquipmentController)
            Debug.Log($"Equipped {itemToUse.Name}");
        }
    }

    // Mở rộng kho đồ
    public bool ExpandInventory(int amount = 1)
    {
        if (slots.Count + amount <= maxSlots)
        {
            for (int i = 0; i < amount; i++)
            {
                slots.Add(new InventorySlot());
            }
            OnInventoryChanged?.Invoke();
            return true;
        }
        Debug.Log("Cannot expand inventory further.");
        return false;
    }

    #endregion
}