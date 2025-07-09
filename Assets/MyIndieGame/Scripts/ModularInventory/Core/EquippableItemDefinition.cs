// File: Assets/MyIndieGame/Scripts/ModularInventory/Data/EquippableItemDefinition.cs
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using ModularInventory.Data.Conditions;

namespace ModularInventory.Data
{
    [CreateAssetMenu(fileName = "NewEquippableItem", menuName = "My Indie Game/Modular Inventory/Equippable Item")]
    public class EquippableItemDefinition : ItemDefinition
    {
        [Title("Equipment Logic")]
        [Tooltip("The combat and animation data associated with this item when equipped.")]
        [AssetsOnly, Required]
        public WeaponData WeaponData;

        [Tooltip("A list of conditions that must all be met for the item to be equipped.")]
        [SerializeReference]
        public List<IEquipCondition> EquipConditions = new List<IEquipCondition>();
    }
}