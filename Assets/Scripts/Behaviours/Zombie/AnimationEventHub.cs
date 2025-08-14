using UnityEngine;
using UnityEngine.Events;

namespace BillTheDev.Animation
{
    /// <summary>
    /// Một lớp trung gian nhận sự kiện từ các clip Animation và phát chúng
    /// dưới dạng UnityEvents. Điều này giúp tách biệt Animator khỏi logic game.
    /// Bất kỳ script nào cũng có thể lắng nghe các sự kiện này mà không cần
    /// Animator phải biết về chúng.
    /// </summary>
    public class AnimationEventHub : MonoBehaviour
    {
        // === CÁC SỰ KIỆN CÔNG KHAI ĐỂ CÁC SCRIPT KHÁC LẮNG NGHE ===

        [Header("Combat Events")]
        public UnityEvent OnDealDamage;      // Sự kiện tại thời điểm animation gây sát thương
        public UnityEvent OnAttackFinished;  // Sự kiện khi animation tấn công kết thúc
        public UnityEvent OnAttackStarted;   // Tùy chọn: Sự kiện khi animation tấn công bắt đầu

        [Header("Movement Events")]
        public UnityEvent OnFootstepLeft;
        public UnityEvent OnFootstepRight;

        [Header("General Events")]
        public UnityEvent OnAnimationCompleted; // Sự kiện chung cho bất kỳ animation nào kết thúc


        // === CÁC HÀM CÔNG KHAI ĐỂ GỌI TỪ ANIMATION EVENT ===
        // Người làm animation sẽ gọi các hàm này từ cửa sổ Animation.

        public void TriggerDealDamage()
        {
            OnDealDamage?.Invoke();
        }

        public void TriggerAttackFinished()
        {
            OnAttackFinished?.Invoke();
        }

        public void TriggerAttackStarted()
        {
            OnAttackStarted?.Invoke();
        }

        public void TriggerFootstepLeft()
        {
            OnFootstepLeft?.Invoke();
        }

        public void TriggerFootstepRight()
        {
            OnFootstepRight?.Invoke();
        }

        public void TriggerAnimationCompleted()
        {
            OnAnimationCompleted?.Invoke();
        }
    }
}