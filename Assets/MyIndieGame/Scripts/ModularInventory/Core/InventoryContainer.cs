// File: Assets/MyIndieGame/Scripts/ModularInventory/Logic/InventoryContainer.cs (PHIÊN BẢN SỬA LỖI)
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using ModularInventory.Data;
using ModularInventory.Logic;

[DisallowMultipleComponent]
public class InventoryContainer : MonoBehaviour
{
    [Title("Container Configuration")]
    [Range(1, 100)]
    [OnValueChanged("OnSizeChanged")]
    public int Size = 20;

    [Title("Container Content")]
    [TableList(IsReadOnly = true, AlwaysExpanded = true)]
    [SerializeField]
    private List<InventorySlot> slots = new List<InventorySlot>();

    public IReadOnlyList<InventorySlot> Slots
    {
        get
        {
            // Đảm bảo container luôn được khởi tạo trước khi trả về dữ liệu
            EnsureInitialized();
            return slots;
        }
    }

    public event System.Action<InventoryContainer> OnInventoryUpdated;

    // Cờ để đảm bảo logic khởi tạo chỉ chạy một lần
    private bool isInitialized = false;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        // Nếu đã khởi tạo hoặc đang trong chế độ editor và không chạy, không làm gì cả
        if (isInitialized || !Application.isPlaying) return;

        // Đây là khối khởi tạo thực sự
        while (slots.Count < Size)
        {
            slots.Add(new InventorySlot());
        }
        while (slots.Count > Size)
        {
            slots.RemoveAt(slots.Count - 1);
        }

        isInitialized = true;
        OnInventoryUpdated?.Invoke(this);
    }

    // Các hàm còn lại giữ nguyên...
    public bool TryAddItem(ItemDefinition item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;
        return TryAddItemStack(new ItemStack(item, amount));
    }

    public bool TryAddItemStack(ItemStack itemStackToAdd)
    {
        EnsureInitialized();
        if (itemStackToAdd.IsEmpty) return true;

        if (itemStackToAdd.Definition.IsStackable)
        {
            foreach (var slot in slots.Where(s => !s.IsEmpty && s.ItemStack.Definition == itemStackToAdd.Definition && !s.ItemStack.IsFull))
            {
                int spaceAvailable = slot.ItemStack.MaxStackSize - slot.ItemStack.Amount;
                int amountToTransfer = Mathf.Min(spaceAvailable, itemStackToAdd.Amount);

                slot.AddAmount(amountToTransfer);
                itemStackToAdd.Amount -= amountToTransfer;

                if (itemStackToAdd.IsEmpty)
                {
                    OnInventoryUpdated?.Invoke(this);
                    return true;
                }
            }
        }

        foreach (var slot in slots.Where(s => s.IsEmpty))
        {
            slot.SetItemStack(new ItemStack(itemStackToAdd.Definition, itemStackToAdd.Amount));
            itemStackToAdd.Amount = 0;
            OnInventoryUpdated?.Invoke(this);
            return true;
        }

        return false;
    }

    public void SwapSlots(int fromIndex, int toIndex)
    {
        EnsureInitialized();
        if (fromIndex < 0 || fromIndex >= slots.Count || toIndex < 0 || toIndex >= slots.Count || fromIndex == toIndex) return;

        var tempStack = slots[toIndex].ItemStack;
        slots[toIndex].SetItemStack(slots[fromIndex].ItemStack);
        slots[fromIndex].SetItemStack(tempStack);
        OnInventoryUpdated?.Invoke(this);
    }

    [Button("Sort Items")]
    public void SortItems()
    {
        EnsureInitialized();
        var sortedItems = slots
            .Where(s => !s.IsEmpty)
            .Select(s => s.ItemStack)
            .OrderBy(i => i.Definition.Type)
            .ThenBy(i => i.Definition.DisplayName)
            .ToList();

        foreach (var slot in slots) slot.Clear();
        foreach (var item in sortedItems) TryAddItemStack(item);

        OnInventoryUpdated?.Invoke(this);
    }

#if UNITY_EDITOR
    // Hàm này giúp UI trong Inspector tự cập nhật khi thay đổi Size
    private void OnSizeChanged()
    {
        if (Application.isPlaying) return;

        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            while (slots.Count < Size) slots.Add(new InventorySlot());
            while (slots.Count > Size) slots.RemoveAt(slots.Count - 1);
            UnityEditor.EditorUtility.SetDirty(this);
        };
    }
#endif
}