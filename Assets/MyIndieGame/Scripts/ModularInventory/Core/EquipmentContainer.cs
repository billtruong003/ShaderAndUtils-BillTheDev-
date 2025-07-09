// File: Assets/MyIndieGame/Scripts/ModularInventory/Core/EquipmentContainer.cs (PHIÊN BẢN NÂNG CẤP)
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using ModularInventory.Logic;
using ModularInventory.Data;

[RequireComponent(typeof(EquipmentManager))]
public class EquipmentContainer : MonoBehaviour
{
    [Title("Equipment Slots")]
    [InfoBox("Defines the character's available equipment slots.")]
    [TableList(AlwaysExpanded = true)]
    public List<EquipmentSlot> EquipmentSlots;

    private EquipmentManager equipmentManager;

    private void Awake()
    {
        equipmentManager = GetComponent<EquipmentManager>();
        if (EquipmentSlots == null || EquipmentSlots.Count == 0)
        {
            SetupDefaultSlots();
        }
    }

    private void OnEnable()
    {
        foreach (var slot in EquipmentSlots)
        {
            slot.OnEquipmentChanged += HandleEquipmentChange;
        }
    }

    private void OnDisable()
    {
        foreach (var slot in EquipmentSlots)
        {
            slot.OnEquipmentChanged -= HandleEquipmentChange;
        }
    }

    [Button("Setup Default Slots"), PropertyOrder(-1)]
    private void SetupDefaultSlots()
    {
        EquipmentSlots = new List<EquipmentSlot>
        {
            new EquipmentSlot(EquipmentSlotType.MainHand), new EquipmentSlot(EquipmentSlotType.OffHand),
            new EquipmentSlot(EquipmentSlotType.Head), new EquipmentSlot(EquipmentSlotType.Chest),
            new EquipmentSlot(EquipmentSlotType.Legs), new EquipmentSlot(EquipmentSlotType.Feet),
        };
    }

    private void HandleEquipmentChange(EquipmentSlot changedSlot)
    {
        // Chỉ quan tâm đến vũ khí chính để thay đổi logic combat
        if (changedSlot.SlotType == EquipmentSlotType.MainHand)
        {
            var itemDef = changedSlot.EquippedItemStack?.Definition as EquippableItemDefinition;
            // Dòng log này cực kỳ quan trọng để debug
            Debug.Log($"<color=cyan>[EquipmentContainer]</color> MainHand changed. Notifying EquipmentManager with Weapon: {(itemDef != null ? itemDef.WeaponData.name : "Unarmed")}");
            equipmentManager.EquipWeapon(itemDef?.WeaponData);
        }
    }

    public bool TryEquipItem(ItemStack itemToEquip, EquipmentSlot targetSlot, out ItemStack previousItem)
    {
        previousItem = null;
        if (itemToEquip == null || itemToEquip.IsEmpty || !(itemToEquip.Definition is EquippableItemDefinition equippableDef))
        {
            Debug.LogError("[EquipmentContainer] Attempted to equip an invalid item.", this);
            return false;
        }

        // VÒNG LẶP KIỂM TRA ĐIỀU KIỆN - NÂNG CẤP VỚI LOG RÕ RÀNG
        foreach (var condition in equippableDef.EquipConditions)
        {
            if (!condition.CheckCondition(gameObject, itemToEquip, targetSlot))
            {
                // Đây là dòng log quan trọng nhất để bạn tìm lỗi
                Debug.LogWarning($"<color=yellow>Equip Failed for '{equippableDef.DisplayName}':</color> {condition.GetFailureMessage()}", this);
                return false;
            }
        }

        // Nếu tất cả điều kiện đều qua, tiến hành trang bị
        Debug.Log($"<color=green>[EquipmentContainer]</color> All conditions met for '{equippableDef.DisplayName}'. Equipping to slot '{targetSlot.SlotType}'.");
        previousItem = targetSlot.EquippedItemStack;
        targetSlot.EquipItem(itemToEquip);
        return true;
    }
}