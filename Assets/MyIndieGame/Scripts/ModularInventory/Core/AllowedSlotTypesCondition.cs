// File: Assets/MyIndieGame/Scripts/ModularInventory/Conditions/AllowedSlotTypesCondition.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ModularInventory.Logic;
using Sirenix.OdinInspector;

namespace ModularInventory.Data.Conditions
{
    [System.Serializable]
    public class AllowedSlotTypesCondition : IEquipCondition
    {
        [EnumToggleButtons]
        public List<EquipmentSlotType> AllowedTypes;

        public bool CheckCondition(GameObject user, ItemStack itemToEquip, EquipmentSlot targetSlot)
        {
            if (AllowedTypes == null || AllowedTypes.Count == 0 || targetSlot == null) return false;
            return AllowedTypes.Contains(targetSlot.SlotType);
        }

        public string GetFailureMessage()
        {
            if (AllowedTypes == null || AllowedTypes.Count == 0) return "Item cannot be equipped in any slot.";
            string allowedSlotsText = string.Join(", ", AllowedTypes.Select(t => t.ToString()));
            return $"Can only be equipped in: {allowedSlotsText}.";
        }
    }
}