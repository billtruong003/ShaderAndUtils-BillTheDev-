using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BillTheDev.Anim
{
    [DisallowMultipleComponent]
    public class MasterEventHub : SerializedMonoBehaviour
    {
        [Title("Master Animation Event Hub", "Ánh xạ ID sự kiện tới các hành động. Được kích hoạt bởi AnimationEventProxy từ các Animator ở child.")]
        [InfoBox("Cấu hình tất cả các phản ứng sự kiện tại đây. " +
                 "Mỗi 'Event ID' sẽ được gọi từ AnimationEventProxy trên đối tượng con chứa Animator.", InfoMessageType.None)]

        [SerializeField]
        [DictionaryDrawerSettings(KeyLabel = "Event ID", ValueLabel = "Hành Động Kích Hoạt (UnityEvent)", DisplayMode = DictionaryDisplayOptions.ExpandedFoldout)]
        private Dictionary<string, UnityEvent> _eventRegistry = new Dictionary<string, UnityEvent>();

        public void Trigger(string eventID)
        {
            if (string.IsNullOrEmpty(eventID))
            {
                Debug.LogWarning($"[MasterEventHub] Nhận được một Event ID rỗng trên '{gameObject.name}'.", this);
                return;
            }

            if (_eventRegistry.TryGetValue(eventID, out UnityEvent actionsToTrigger))
            {
                actionsToTrigger?.Invoke();
            }
            else
            {
                Debug.LogWarning($"[MasterEventHub] Không tìm thấy Event ID '{eventID}' đã đăng ký trong Hub trên '{gameObject.name}'.", this);
            }
        }
    }
}