// File: Assets/MyIndieGame/Scripts/ModularInventory/Core/IItemAction.cs
using UnityEngine;
using ModularInventory.Logic;

namespace ModularInventory.Data.Actions
{
    public interface IItemAction
    {
        bool ExecuteAction(GameObject user, ItemStack sourceStack);
        string GetActionDescription();
    }
}