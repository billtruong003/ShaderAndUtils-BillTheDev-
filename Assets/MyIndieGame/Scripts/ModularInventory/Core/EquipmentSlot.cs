// File: Assets/MyIndieGame/Scripts/ModularInventory/Logic/EquipmentSlot.cs
using System;
using Sirenix.OdinInspector;
using ModularInventory.Data;

namespace ModularInventory.Logic
{
    [Serializable]
    public class EquipmentSlot
    {
        [ReadOnly] public string SlotName;
        [EnumToggleButtons, OnValueChanged("UpdateName"), ReadOnly] public EquipmentSlotType SlotType;

        [ShowInInspector, InlineProperty, HideLabel]
        public ItemStack EquippedItemStack { get; private set; } = new ItemStack(null, 0);

        public event Action<EquipmentSlot> OnEquipmentChanged;
        public bool IsEmpty => EquippedItemStack == null || EquippedItemStack.IsEmpty;

        public EquipmentSlot(EquipmentSlotType type)
        {
            this.SlotType = type;
            UpdateName();
        }

        public void EquipItem(ItemStack itemStack)
        {
            EquippedItemStack = itemStack ?? new ItemStack(null, 0);
            OnEquipmentChanged?.Invoke(this);
        }

        private void UpdateName() => SlotName = SlotType.ToString();
    }
}