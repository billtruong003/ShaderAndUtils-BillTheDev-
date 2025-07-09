using UnityEngine;
using Sirenix.OdinInspector;
using ModularInventory.Data;
using ModularInventory.Logic;

public sealed class WorldItem : MonoBehaviour
{
    [AssetsOnly, Required]
    public ItemDefinition Item;

    [Range(1, 999)]
    public int Quantity = 1;

    public void OnInteract(InventoryContainer inventory)
    {
        if (inventory == null || this.Item == null) return;

        if (inventory.TryAddItem(this.Item, this.Quantity))
        {
            Destroy(gameObject);
        }
    }
}