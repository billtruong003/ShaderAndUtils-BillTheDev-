// File: Assets/MyIndieGame/Scripts/ModularInventory/Data/ItemDefinition.cs
using UnityEngine;
using Sirenix.OdinInspector;
using ModularInventory.Enums;

namespace ModularInventory.Data
{
    public class ItemDefinition : SerializedScriptableObject
    {
        [Title("Core Properties")]
        [HorizontalGroup("Core", 80, LabelWidth = 65)]
        [PreviewField(80, ObjectFieldAlignment.Center), HideLabel]
        public Sprite Icon;

        [VerticalGroup("Core/Right")]
        [Required("Item ID is crucial for saving, loading, and identifying items.")]
        [Tooltip("A unique identifier for this item (e.g., 'wpn_iron_sword', 'pot_health_small').")]
        public string ItemID;

        [VerticalGroup("Core/Right")]
        public string DisplayName;

        [VerticalGroup("Core/Right")]
        [TextArea(3, 5)]
        public string Description;

        [Title("Details")]
        [EnumToggleButtons]
        public ItemType Type = ItemType.Generic;

        [Range(1, 999)]
        public int MaxStackSize = 1;

        [ShowInInspector, ReadOnly]
        [PropertyOrder(99)]
        public bool IsStackable => MaxStackSize > 1;

#if UNITY_EDITOR
        [Button("Generate Unique ID", ButtonSizes.Small)]
        [PropertyOrder(-1)]
        private void GenerateID()
        {
            if (string.IsNullOrEmpty(ItemID))
            {
                ItemID = System.Guid.NewGuid().ToString();
            }
        }
#endif
    }
}

