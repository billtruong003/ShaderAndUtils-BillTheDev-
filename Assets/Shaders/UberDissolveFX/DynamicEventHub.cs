using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BillTheDev.Anim
{
    public class DynamicAnimationEventHub : SerializedMonoBehaviour
    {
        [Title("Dynamic Animation Event Hub", "Ánh xạ từ một ID sự kiện sang một danh sách các hành động.")]
        [InfoBox("Mỗi 'Event ID' (Key) có thể chứa một danh sách nhiều hành động trong 'Hành Động Kích Hoạt' (Value). " +
                 "UnityEvent bản chất là một danh sách các hàm gọi.", InfoMessageType.None)]

        [SerializeField]
        [DictionaryDrawerSettings(KeyLabel = "Event ID", ValueLabel = "Hành Động Kích Hoạt (UnityEvent)", DisplayMode = DictionaryDisplayOptions.ExpandedFoldout)]
        private Dictionary<string, UnityEvent> eventHub = new Dictionary<string, UnityEvent>();

        public void Trigger(string eventID)
        {
            if (string.IsNullOrEmpty(eventID))
            {
                Debug.LogWarning($"[DynamicEventHub] Nhận được một Event ID rỗng trên GameObject '{gameObject.name}'. Vui lòng kiểm tra Animation Event.", this);
                return;
            }

            if (eventHub.TryGetValue(eventID, out UnityEvent actionsToTrigger))
            {
                actionsToTrigger?.Invoke();
            }
            else
            {
                Debug.LogWarning($"[DynamicEventHub] Không tìm thấy Event ID: '{eventID}' trong Hub trên GameObject '{gameObject.name}'.", this);
            }
        }
    }
}