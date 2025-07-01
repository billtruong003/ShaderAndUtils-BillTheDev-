[System.Serializable]
public class InventorySlot
{
    public ItemDefinition item; // Tham chiếu đến định nghĩa vật phẩm trong Database
    public int quantity;

    public InventorySlot()
    {
        item = null;
        quantity = 0;
    }

    public InventorySlot(ItemDefinition item, int quantity)
    {
        this.item = item;
        this.quantity = quantity;
    }

    public void Clear()
    {
        item = null;
        quantity = 0;
    }

    public bool IsEmpty()
    {
        return item == null;
    }
}