using UnityEngine;

namespace BillTheDev.Anim
{
    [RequireComponent(typeof(Animator))]
    [DisallowMultipleComponent]
    public class AnimationEventProxy : MonoBehaviour
    {
        private MasterEventHub _masterHub;

        private void Awake()
        {
            // Tự động tìm MasterEventHub ở cấp cha, đảm bảo kiến trúc luôn đúng
            _masterHub = GetComponentInParent<MasterEventHub>();
            if (_masterHub == null)
            {
                // Cung cấp lỗi rõ ràng nếu cấu trúc bị sai, giúp debug dễ dàng
                Debug.LogError($"[AnimationEventProxy] trên '{gameObject.name}' không thể tìm thấy 'MasterEventHub' ở bất kỳ đối tượng cha nào. " +
                               $"Hãy chắc chắn rằng có một MasterEventHub component trên GameObject parent gốc.", this);
            }
        }

        // Hàm này sẽ được gọi trực tiếp từ Animation Event
        public void ProxyTrigger(string eventID)
        {
            // Kiểm tra một lần nữa để tránh lỗi runtime nếu cấu trúc bị thay đổi
            if (_masterHub == null) return;

            // Chuyển tiếp event ID lên cho Hub xử lý
            _masterHub.Trigger(eventID);
        }
    }
}