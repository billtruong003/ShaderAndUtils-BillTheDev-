using UnityEngine;
using ModularInventory.Logic;

[RequireComponent(typeof(Collider))]
public sealed class PlayerInteraction : MonoBehaviour
{
    private InventoryContainer inventoryContainer;

    private void Awake()
    {
        inventoryContainer = GetComponentInParent<InventoryContainer>();
        if (inventoryContainer == null)
        {
            Debug.LogError("PlayerInteraction could not find an InventoryContainer in parent GameObjects!", this);
            enabled = false;
        }
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<WorldItem>(out var worldItem))
        {
            worldItem.OnInteract(inventoryContainer);
        }
    }
}