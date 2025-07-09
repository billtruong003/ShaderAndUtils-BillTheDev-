using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace ModularInventory.UI
{
    public class DragDropController : MonoBehaviour
    {
        [Required][SerializeField] private Image draggedItemIcon;
        [HideInInspector] public InventorySlotView SourceSlotView;
        public static DragDropController Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
            draggedItemIcon.enabled = false;
            draggedItemIcon.raycastTarget = false;
        }

        public void StartDrag(InventorySlotView sourceSlot)
        {
            SourceSlotView = sourceSlot;
            draggedItemIcon.sprite = sourceSlot.SlotIcon.sprite;
            draggedItemIcon.enabled = true;
            UpdateIconPosition();
        }

        public void EndDrag()
        {
            SourceSlotView = null;
            draggedItemIcon.enabled = false;
        }

        private void Update()
        {
            if (draggedItemIcon.enabled) UpdateIconPosition();
        }

        private void UpdateIconPosition() => draggedItemIcon.transform.position = Input.mousePosition;
    }
}