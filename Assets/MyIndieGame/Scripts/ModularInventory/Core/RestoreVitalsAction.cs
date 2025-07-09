// File: Assets/MyIndieGame/Scripts/ModularInventory/Actions/RestoreVitalsAction.cs
using UnityEngine;
using Sirenix.OdinInspector;
using ModularInventory.Logic;

namespace ModularInventory.Data.Actions
{
    [System.Serializable]
    public class RestoreVitalsAction : IItemAction
    {
        [SuffixLabel("HP", true)] public float HealthToRestore;
        [SuffixLabel("MP", true)] public float ManaToRestore;
        [SuffixLabel("SP", true)] public float StaminaToRestore;

        public bool ExecuteAction(GameObject user, ItemStack sourceStack)
        {
            if (!user.TryGetComponent<StatController>(out var stats)) return false;

            bool actionTaken = false;
            if (HealthToRestore > 0)
            {
                stats.RestoreHealth(HealthToRestore);
                actionTaken = true;
            }
            if (ManaToRestore > 0)
            {
                stats.RestoreMana(ManaToRestore);
                actionTaken = true;
            }
            if (StaminaToRestore > 0)
            {
                stats.RestoreStamina(StaminaToRestore);
                actionTaken = true;
            }
            return actionTaken;
        }

        public string GetActionDescription()
        {
            var parts = new System.Collections.Generic.List<string>();
            if (HealthToRestore > 0) parts.Add($"Restores {HealthToRestore} Health");
            if (ManaToRestore > 0) parts.Add($"Restores {ManaToRestore} Mana");
            if (StaminaToRestore > 0) parts.Add($"Restores {StaminaToRestore} Stamina");
            return string.Join("\n", parts);
        }
    }
}