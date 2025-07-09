// File: Assets/MyIndieGame/Scripts/ModularInventory/UI/InventorySlotView.cs (PHIÊN BẢN NÂNG CẤP)
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using ModularInventory.Logic;
using ModularInventory.Data;
using System.Linq;
using Sirenix.OdinInspector;
using ModularInventory.Data.Conditions;

namespace ModularInventory.UI
{
    public class InventorySlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
                                     IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
    {
        [Required] public Image SlotIcon;
        [Required] public TextMeshProUGUI AmountText;
        public InventoryContainer ParentContainer { get; private set; }
        public int SlotIndex { get; private set; }
        private InventorySlot boundSlot;

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
            if (boundSlot != null) boundSlot.OnSlotUpdated -= UpdateSlotView;
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
            // Thêm log để biết sự kiện click đã được nhận
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                Debug.Log($"<color=lime>[InventorySlotView]</color> Right-clicked on slot {SlotIndex}. Item: {(boundSlot.IsEmpty ? "None" : boundSlot.ItemStack.Definition.DisplayName)}");
            }

            if (eventData.button != PointerEventData.InputButton.Right || boundSlot.IsEmpty) return;
            HandleItemUsage();
        }

        private void HandleItemUsage()
        {
            if (ParentContainer == null) return;

            GameObject user = ParentContainer.gameObject;
            ItemDefinition itemDef = boundSlot.ItemStack.Definition;

            if (itemDef is ConsumableItemDefinition consumable && consumable.ActionToExecute != null)
            {
                if (consumable.ActionToExecute.ExecuteAction(user, boundSlot.ItemStack))
                {
                    boundSlot.DecreaseAmount(1);
                }
            }
            else if (itemDef is EquippableItemDefinition equippable)
            {
                if (user.TryGetComponent<EquipmentContainer>(out var equipmentContainer))
                {
                    AttemptToEquip(equippable, equipmentContainer);
                }
                else
                {
                    Debug.LogError($"[InventorySlotView] User '{user.name}' is missing an EquipmentContainer component.", user);
                }
            }
        }

        private void AttemptToEquip(EquippableItemDefinition itemToEquip, EquipmentContainer equipmentContainer)
        {
            // TÌM SLOT HỢP LỆ
            EquipmentSlot targetSlot = FindValidEquipmentSlot(equipmentContainer, itemToEquip);
            if (targetSlot == null)
            {
                // Báo lỗi nếu không tìm thấy slot nào phù hợp
                Debug.LogWarning($"<color=yellow>Could not find a valid equipment slot for '{itemToEquip.DisplayName}'.</color> Check item's AllowedSlotTypes and character's EquipmentContainer setup.");
                return;
            }

            Debug.Log($"<color=lime>[InventorySlotView]</color> Attempting to equip '{itemToEquip.DisplayName}' to slot '{targetSlot.SlotType}'.");

            ItemStack itemStackToEquip = boundSlot.ItemStack;

            // THỰC HIỆN TRANG BỊ VÀ TRÁO ĐỔI
            if (equipmentContainer.TryEquipItem(itemStackToEquip, targetSlot, out ItemStack previouslyEquippedItem))
            {
                // Nếu trang bị thành công, đặt vật phẩm cũ vào slot kho đồ này
                // (SetItemStack sẽ tự động cập nhật lại UI qua event)
                boundSlot.SetItemStack(previouslyEquippedItem);
                Debug.Log($"<color=green>Equip successful.</color> Item '{previouslyEquippedItem.Definition?.DisplayName ?? "Nothing"}' was moved to inventory slot {SlotIndex}.");
            }
            // Nếu không thành công, TryEquipItem đã tự log lý do.
        }

        private EquipmentSlot FindValidEquipmentSlot(EquipmentContainer container, EquippableItemDefinition itemDef)
        {
            var allowedTypesCondition = itemDef.EquipConditions.OfType<AllowedSlotTypesCondition>().FirstOrDefault();
            if (allowedTypesCondition == null || allowedTypesCondition.AllowedTypes.Count == 0) return null;

            // Tìm một slot hợp lệ và đang trống, hoặc slot đầu tiên nếu không có slot nào trống
            return container.EquipmentSlots.FirstOrDefault(s => allowedTypesCondition.AllowedTypes.Contains(s.SlotType) && s.IsEmpty)
                ?? container.EquipmentSlots.FirstOrDefault(s => allowedTypesCondition.AllowedTypes.Contains(s.SlotType));
        }

        // Các hàm xử lý Drag & Drop không thay đổi
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