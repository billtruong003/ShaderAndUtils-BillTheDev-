using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using ModularInventory.Logic;
using ModularInventory.Data;
using ModularInventory.Data.Conditions;
using ModularInventory.Data.Actions;
using System.Linq;
using Sirenix.OdinInspector;

namespace ModularInventory.UI
{
    public sealed class InventorySlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
                                     IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
    {
        [Required] public Image SlotIcon;
        [Required] public TextMeshProUGUI AmountText;

        public InventoryContainer ParentContainer { get; private set; }
        public int SlotIndex { get; private set; }
        private InventorySlot boundSlot;
        private IUserNotifier notifier;

        private void Awake()
        {
            notifier = DebugLogNotifier.Instance;
        }

        public void Bind(InventorySlot slotToBind, InventoryContainer parentContainer, int slotIndex)
        {
            Unbind();
            boundSlot = slotToBind;
            ParentContainer = parentContainer;
            SlotIndex = slotIndex;
            boundSlot.OnSlotUpdated += UpdateSlotView;
            UpdateSlotView(boundSlot);
        }

        private void Unbind()
        {
            if (boundSlot != null)
            {
                boundSlot.OnSlotUpdated -= UpdateSlotView;
            }
            boundSlot = null;
        }

        private void OnDestroy() => Unbind();

        private void UpdateSlotView(InventorySlot updatedSlot)
        {
            bool hasItem = !updatedSlot.IsEmpty;
            SlotIcon.enabled = hasItem;
            AmountText.enabled = hasItem && updatedSlot.ItemStack.Amount > 1;

            if (hasItem)
            {
                SlotIcon.sprite = updatedSlot.ItemStack.Definition.Icon;
                AmountText.text = updatedSlot.ItemStack.Amount.ToString();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right || boundSlot.IsEmpty)
            {
                return;
            }

            HandleItemUsage();
        }

        private void HandleItemUsage()
        {
            ItemDefinition itemDef = boundSlot.ItemStack.Definition;

            if (itemDef is ConsumableItemDefinition consumable)
            {
                HandleConsumableUsage(consumable);
            }
            else if (itemDef is EquippableItemDefinition equippable)
            {
                HandleEquippableUsage(equippable);
            }
        }

        private void HandleConsumableUsage(ConsumableItemDefinition consumable)
        {
            IItemAction action = consumable.ActionToExecute;
            if (action == null)
            {
                notifier?.ShowNotification("This item has no defined action.", NotificationType.Error);
                return;
            }

            GameObject user = ParentContainer.gameObject;
            if (action.ExecuteAction(user, boundSlot.ItemStack))
            {
                boundSlot.DecreaseAmount(1);
                notifier?.ShowNotification($"Used {consumable.DisplayName}.", NotificationType.Success);
            }
            else
            {
                notifier?.ShowNotification($"Cannot use {consumable.DisplayName} right now.", NotificationType.Warning);
            }
        }

        private void HandleEquippableUsage(EquippableItemDefinition equippable)
        {
            GameObject user = ParentContainer.gameObject;
            if (!user.TryGetComponent<EquipmentContainer>(out var equipmentContainer))
            {
                notifier?.ShowNotification("Character has no equipment container.", NotificationType.Error);
                return;
            }

            EquipmentSlot targetSlot = FindBestValidEquipmentSlot(equipmentContainer, equippable);
            if (targetSlot == null)
            {
                notifier?.ShowNotification($"No valid slot to equip {equippable.DisplayName}.", NotificationType.Warning);
                return;
            }

            ItemStack itemStackToEquip = boundSlot.ItemStack;
            if (equipmentContainer.TryEquipItem(itemStackToEquip, targetSlot, out ItemStack previouslyEquippedItem, out string failureMessage))
            {
                boundSlot.SetItemStack(previouslyEquippedItem);
                notifier?.ShowNotification($"Equipped {equippable.DisplayName}.", NotificationType.Success);
            }
            else
            {
                notifier?.ShowNotification(failureMessage, NotificationType.Error);
            }
        }

        private EquipmentSlot FindBestValidEquipmentSlot(EquipmentContainer container, EquippableItemDefinition itemDef)
        {
            var allowedTypesCondition = itemDef.EquipConditions.OfType<AllowedSlotTypesCondition>().FirstOrDefault();
            if (allowedTypesCondition == null || allowedTypesCondition.AllowedTypes.Count == 0)
            {
                return null;
            }

            var validSlots = container.EquipmentSlots
                .Where(s => allowedTypesCondition.AllowedTypes.Contains(s.SlotType));

            return validSlots.FirstOrDefault(s => s.IsEmpty) ?? validSlots.FirstOrDefault();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (boundSlot != null && !boundSlot.IsEmpty) ItemTooltipView.Instance?.ShowTooltip(boundSlot.ItemStack);
        }

        public void OnPointerExit(PointerEventData eventData) => ItemTooltipView.Instance?.HideTooltip();

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (boundSlot == null || boundSlot.IsEmpty) return;
            DragDropController.Instance?.StartDrag(this);
            SlotIcon.color = new Color(1, 1, 1, 0.5f);
        }

        public void OnDrag(PointerEventData eventData) { }

        public void OnEndDrag(PointerEventData eventData)
        {
            DragDropController.Instance?.EndDrag();
            SlotIcon.color = Color.white;
        }

        public void OnDrop(PointerEventData eventData)
        {
            var sourceSlotView = DragDropController.Instance?.SourceSlotView;
            if (sourceSlotView == null || sourceSlotView == this || sourceSlotView.ParentContainer == null) return;
            sourceSlotView.ParentContainer.SwapSlots(sourceSlotView.SlotIndex, this.SlotIndex);
        }
    }
}