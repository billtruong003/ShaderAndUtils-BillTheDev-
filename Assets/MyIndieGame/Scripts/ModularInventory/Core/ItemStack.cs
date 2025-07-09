// File: Assets/MyIndieGame/Scripts/ModularInventory/Logic/ItemStack.cs
using System;
using ModularInventory.Data;

namespace ModularInventory.Logic
{
    [Serializable]
    public class ItemStack
    {
        public ItemDefinition Definition;
        public int Amount;

        public bool IsEmpty => Definition == null || Amount <= 0;
        public int MaxStackSize => Definition?.MaxStackSize ?? 1;
        public bool IsFull => Amount >= MaxStackSize;

        public ItemStack(ItemDefinition definition, int amount)
        {
            Definition = definition;
            Amount = amount;
        }
    }
}