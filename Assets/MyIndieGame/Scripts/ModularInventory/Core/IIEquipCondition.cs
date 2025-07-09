// File: Assets/MyIndieGame/Scripts/ModularInventory/Core/IIEquipCondition.cs
using UnityEngine;
using ModularInventory.Logic;

namespace ModularInventory.Data.Conditions
{
    public interface IEquipCondition
    {
        bool CheckCondition(GameObject user, ItemStack itemToEquip, EquipmentSlot targetSlot);
        string GetFailureMessage();
    }
}