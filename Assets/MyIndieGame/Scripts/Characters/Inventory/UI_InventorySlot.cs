using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_InventorySlot : MonoBehaviour
{

    public Image icon;
    public TextMeshProUGUI quantityText;

    public void UpdateSlot(InventorySlot slot)
    {
        if (slot.IsEmpty())
        {
            icon.enabled = false;
            quantityText.enabled = false;
        }
        else
        {
            icon.enabled = true;
            icon.sprite = slot.item.Icon;
            quantityText.enabled = slot.quantity > 1;
            quantityText.text = slot.quantity.ToString();
        }
    }
}