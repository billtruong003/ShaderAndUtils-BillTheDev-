using UnityEngine;

public class WorldItem : MonoBehaviour
{
    [Tooltip("Mã định danh trong ItemDatabase")]
    public string ItemID;
    public int Quantity = 1;

    // Hàm này có thể được gọi bởi một script PlayerInteraction
    public void OnInteract(InventoryController inventory)
    {
        if (inventory.AddItem(ItemID, Quantity))
        {
            // Nếu nhặt thành công, xóa object khỏi thế giới
            Destroy(gameObject);
        }
        else
        {
            // Có thể hiển thị thông báo "Kho đồ đã đầy!"
            Debug.Log("Could not pick up item, inventory full.");
        }
    }
}