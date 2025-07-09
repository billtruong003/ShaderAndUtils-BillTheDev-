// File: Assets/MyIndieGame/Scripts/ModularInventory/UI/ItemTooltipView.cs
using UnityEngine;
using TMPro;
using Sirenix.OdinInspector;
using ModularInventory.Data;
using ModularInventory.Logic;
using ModularInventory.Data.Actions;

namespace ModularInventory.UI
{
    public class ItemTooltipView : MonoBehaviour
    {
        [Required][SerializeField] private GameObject tooltipPanel;
        [Required][SerializeField] private TextMeshProUGUI itemNameText;
        [Required][SerializeField] private TextMeshProUGUI itemDescriptionText;
        [Required][SerializeField] private TextMeshProUGUI itemStatsText;

        public static ItemTooltipView Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
            HideTooltip();
        }

        public void ShowTooltip(ItemStack itemStack)
        {
            if (itemStack == null || itemStack.IsEmpty) return;

            ItemDefinition item = itemStack.Definition;
            itemNameText.text = item.DisplayName;
            itemDescriptionText.text = item.Description;

            string statsString = BuildStatsString(item);
            itemStatsText.text = statsString;
            itemStatsText.gameObject.SetActive(!string.IsNullOrEmpty(statsString));

            tooltipPanel.SetActive(true);
        }

        public void HideTooltip() => tooltipPanel.SetActive(false);

        private string BuildStatsString(ItemDefinition item)
        {
            if (item is ConsumableItemDefinition consumable && consumable.ActionToExecute != null)
            {
                return consumable.ActionToExecute.GetActionDescription();
            }
            // Future: Add logic to display stats for EquippableItemDefinition
            return "";
        }
    }
}