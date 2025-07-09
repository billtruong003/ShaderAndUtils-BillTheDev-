// File: Assets/MyIndieGame/Scripts/ModularInventory/Conditions/RequireLevelCondition.cs
using UnityEngine;
using ModularInventory.Logic;
using Sirenix.OdinInspector;

namespace ModularInventory.Data.Conditions
{
    [System.Serializable]
    public class RequiredLevelCondition : IEquipCondition
    {
        [MinValue(1)]
        public int RequiredLevel = 1;

        public bool CheckCondition(GameObject user, ItemStack itemToEquip, EquipmentSlot targetSlot)
        {
            if (!user.TryGetComponent<StatController>(out var stats)) return true; // No stats system, no level requirement
            return stats.Level >= RequiredLevel;
        }

        public string GetFailureMessage()
        {
            return $"Requires Level {RequiredLevel}.";
        }
    }
}