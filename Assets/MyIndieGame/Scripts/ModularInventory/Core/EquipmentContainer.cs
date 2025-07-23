using UnityEngine;
using System.Collections.Generic;
using ModularInventory.Logic;
using ModularInventory.Data;
using Sirenix.OdinInspector;

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
        if (changedSlot.SlotType == EquipmentSlotType.MainHand)
        {
            var itemDef = changedSlot.EquippedItemStack?.Definition as EquippableItemDefinition;
            equipmentManager.EquipWeapon(itemDef?.WeaponData);
        }
    }

    public bool TryEquipItem(ItemStack itemToEquip, EquipmentSlot targetSlot, out ItemStack previousItem, out string failureMessage)
    {
        previousItem = null;
        failureMessage = "Unknown error.";

        if (itemToEquip == null || itemToEquip.IsEmpty || !(itemToEquip.Definition is EquippableItemDefinition equippableDef))
        {
            failureMessage = "Item is not equippable.";
            return false;
        }

        if (targetSlot == null)
        {
            failureMessage = "No target slot specified.";
            return false;
        }

        foreach (var condition in equippableDef.EquipConditions)
        {
            if (!condition.CheckCondition(gameObject, itemToEquip, targetSlot))
            {
                failureMessage = condition.GetFailureMessage();
                return false;
            }
        }

        previousItem = targetSlot.EquippedItemStack;
        targetSlot.EquipItem(itemToEquip);
        failureMessage = string.Empty;
        return true;
    }
}