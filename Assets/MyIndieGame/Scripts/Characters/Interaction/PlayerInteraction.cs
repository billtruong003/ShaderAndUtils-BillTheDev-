using UnityEngine;
public class PlayerInteraction : MonoBehaviour
{
    private InventoryController inventory;

    void Awake()
    {
        inventory = GetComponent<InventoryController>();
    }

    // Ví dụ dùng Trigger
    void OnTriggerEnter(Collider other)
    {
        WorldItem worldItem = other.GetComponent<WorldItem>();
        if (worldItem != null)
        {
            worldItem.OnInteract(inventory);
        }
    }
}