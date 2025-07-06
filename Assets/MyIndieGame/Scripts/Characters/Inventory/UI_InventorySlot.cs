using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class UI_InventorySlot : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI quantityText;

    private InventoryController inventoryController;
    private int slotIndex;

    public void Initialize(InventoryController controller, int index)
    {
        inventoryController = controller;
        slotIndex = index;
        GetComponent<Button>().onClick.AddListener(OnSlotClicked);
    }

    public void UpdateSlot(InventorySlot slot)
    {
        bool isEmpty = slot.IsEmpty();
        icon.gameObject.SetActive(!isEmpty);
        quantityText.gameObject.SetActive(!isEmpty && slot.quantity > 1);

        if (!isEmpty)
        {
            icon.sprite = slot.item.Icon;
            quantityText.text = slot.quantity.ToString();
        }
    }

    private void OnSlotClicked()
    {
        inventoryController?.UseItem(slotIndex);
    }
}