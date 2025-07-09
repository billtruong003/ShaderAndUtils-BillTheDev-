// File: Assets/MyIndieGame/Scripts/ModularInventory/Data/ConsumableItemDefinition.cs
using UnityEngine;
using Sirenix.OdinInspector;
using ModularInventory.Data.Actions;

namespace ModularInventory.Data
{
    [CreateAssetMenu(fileName = "NewConsumableItem", menuName = "My Indie Game/Modular Inventory/Consumable Item")]
    public class ConsumableItemDefinition : ItemDefinition
    {
        [Title("Consumable Logic")]
        [Tooltip("The action that occurs when this item is used.")]
        [SerializeReference, Required]
        public IItemAction ActionToExecute;
    }
}