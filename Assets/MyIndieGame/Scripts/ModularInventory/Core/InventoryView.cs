// File: Assets/MyIndieGame/Scripts/ModularInventory/Core/InventoryView.cs (PHIÊN BẢN SỬA LỖI)
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using ModularInventory.Logic;

namespace ModularInventory.UI
{
    public class InventoryView : MonoBehaviour
    {
        [Title("References")]
        [Required][SerializeField] private InventoryContainer targetContainer;

        [Title("UI Prefabs & Parents")]
        [Required][SerializeField] private GameObject slotViewPrefab;
        [Required][SerializeField] private Transform slotContainerTransform;

        private readonly List<InventorySlotView> instantiatedSlotViews = new List<InventorySlotView>();

        private void OnEnable()
        {
            if (targetContainer != null)
            {
                targetContainer.OnInventoryUpdated += RefreshView;
                RefreshView(targetContainer);
            }
        }

        private void OnDisable()
        {
            if (targetContainer != null)
            {
                targetContainer.OnInventoryUpdated -= RefreshView;
            }
        }

        private void RefreshView(InventoryContainer container)
        {
            if (container == null || slotViewPrefab == null) return;

            CreateOrDestroySlotViews(container.Size);

            // Truy cập Slots thông qua property đã được bảo vệ
            IReadOnlyList<InventorySlot> containerSlots = container.Slots;

            // Kiểm tra an toàn một lần nữa để chắc chắn
            if (instantiatedSlotViews.Count != containerSlots.Count)
            {
                Debug.LogError("Mismatch between UI slots and data slots. Aborting refresh.", this);
                return;
            }

            for (int i = 0; i < container.Size; i++)
            {
                instantiatedSlotViews[i].Bind(containerSlots[i], container, i);
            }
        }

        private void CreateOrDestroySlotViews(int requiredSize)
        {
            // Hủy các view thừa
            while (instantiatedSlotViews.Count > requiredSize)
            {
                var viewToRemove = instantiatedSlotViews[instantiatedSlotViews.Count - 1];
                instantiatedSlotViews.RemoveAt(instantiatedSlotViews.Count - 1);
                // Dùng DestroyImmediate nếu bạn cần nó biến mất ngay lập tức trong editor,
                // nhưng Destroy là an toàn hơn trong runtime.
                if (viewToRemove != null) Destroy(viewToRemove.gameObject);
            }

            // Tạo các view còn thiếu
            while (instantiatedSlotViews.Count < requiredSize)
            {
                var newSlotObject = Instantiate(slotViewPrefab, slotContainerTransform);
                if (newSlotObject.TryGetComponent<InventorySlotView>(out var slotView))
                {
                    instantiatedSlotViews.Add(slotView);
                }
                else
                {
                    Debug.LogError($"Prefab '{slotViewPrefab.name}' is missing the InventorySlotView component.", slotViewPrefab);
                    break; // Thoát vòng lặp để tránh lỗi vô hạn
                }
            }
        }
    }
}