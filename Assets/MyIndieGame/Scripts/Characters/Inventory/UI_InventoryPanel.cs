using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_InventoryPanel : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Tham chiếu đến Inventory Controller của người chơi.")]
    public InventoryController inventory;

    [Header("UI Setup")]
    [Tooltip("Transform cha chứa tất cả các UI Slot. Script sẽ tự động tìm các slot bên trong.")]
    public Transform slotContainer;

    private List<UI_InventorySlot> uiSlots = new List<UI_InventorySlot>();
    private bool isInitialized = false; // Thêm cờ để biết đã khởi tạo chưa

    void Awake()
    {
        InitializeSlots();
    }

    void OnEnable()
    {
        if (!isInitialized)
        {
            // Nếu vì lý do nào đó Awake chưa chạy, hãy thử khởi tạo lại
            InitializeSlots();
        }

        // Kiểm tra an toàn trước khi đăng ký sự kiện
        if (inventory != null)
        {
            inventory.OnInventoryChanged += Redraw;
            Redraw();
        }
    }

    void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= Redraw;
        }
    }

    private void InitializeSlots()
    {
        // Thêm các bước kiểm tra quan trọng
        if (inventory == null)
        {
            Debug.LogError($"[UI_InventoryPanel] Lỗi: InventoryController chưa được gán trên GameObject '{this.gameObject.name}'! Vui lòng kéo Player vào Inspector.", this.gameObject);
            isInitialized = false;
            return; // Dừng lại nếu chưa gán
        }
        if (slotContainer == null)
        {
            Debug.LogError($"[UI_InventoryPanel] Lỗi: Slot Container chưa được gán trên GameObject '{this.gameObject.name}'!", this.gameObject);
            isInitialized = false;
            return;
        }

        uiSlots.Clear();
        foreach (UI_InventorySlot slot in slotContainer.GetComponentsInChildren<UI_InventorySlot>())
        {
            uiSlots.Add(slot);
            int slotIndex = uiSlots.Count - 1;
            Button slotButton = slot.GetComponent<Button>();
            if (slotButton != null)
            {
                slotButton.onClick.RemoveAllListeners();
                slotButton.onClick.AddListener(() => OnSlotClicked(slotIndex));
            }
        }
        isInitialized = true;
    }

    private void Redraw()
    {
        // Thêm kiểm tra an toàn ở đây nữa!
        if (!isInitialized || inventory.slots == null)
        {
            return; // Không làm gì cả nếu chưa sẵn sàng
        }

        for (int i = 0; i < uiSlots.Count; i++)
        {
            // Dòng 82 của bạn có lẽ là dòng này
            if (i < inventory.slots.Count)
            {
                uiSlots[i].gameObject.SetActive(true);
                uiSlots[i].UpdateSlot(inventory.slots[i]);
            }
            else
            {
                uiSlots[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnSlotClicked(int slotIndex)
    {
        if (!isInitialized || slotIndex >= inventory.slots.Count) return;

        Debug.Log($"Clicked on slot {slotIndex}");

        if (!inventory.slots[slotIndex].IsEmpty())
        {
            inventory.UseItem(slotIndex);
        }
    }
}