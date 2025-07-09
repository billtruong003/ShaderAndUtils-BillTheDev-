// File: Assets/MyIndieGame/Scripts/ModularInventory/Logic/InventorySlot.cs
using System;
using Sirenix.OdinInspector;

namespace ModularInventory.Logic
{
    [Serializable]
    public class InventorySlot
    {
        [ShowInInspector, InlineProperty, HideLabel]
        public ItemStack ItemStack { get; private set; } = new ItemStack(null, 0);
        public event Action<InventorySlot> OnSlotUpdated;

        public bool IsEmpty => ItemStack == null || ItemStack.IsEmpty;

        public void SetItemStack(ItemStack newItemStack)
        {
            ItemStack = newItemStack ?? new ItemStack(null, 0);
            OnSlotUpdated?.Invoke(this);
        }

        public void Clear() => SetItemStack(null);

        public void AddAmount(int amountToAdd)
        {
            if (IsEmpty) return;
            ItemStack.Amount += amountToAdd;
            OnSlotUpdated?.Invoke(this);
        }

        public void DecreaseAmount(int amountToDecrease)
        {
            if (IsEmpty) return;
            ItemStack.Amount -= amountToDecrease;
            if (ItemStack.Amount <= 0) Clear();
            else OnSlotUpdated?.Invoke(this);
        }
    }
}