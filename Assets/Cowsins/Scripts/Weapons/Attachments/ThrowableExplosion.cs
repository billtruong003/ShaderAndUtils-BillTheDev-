using UnityEngine;

namespace cowsins
{
    // Đảm bảo prefab có Rigidbody
    [RequireComponent(typeof(Rigidbody))]
    public class ThrowableExplosion : MonoBehaviour
    {
        [Header("Explosion Settings")]
        [SerializeField, Min(0)] private float delay = 3f;
        [SerializeField, Min(0)] private float explosionRadius = 5f;
        [SerializeField, Min(0)] private float explosionForce = 700f;
        [SerializeField, Min(0)] private float damage = 100f;

        [Header("Effects")]
        [SerializeField] private GameObject explosionVFX;

        private bool hasExploded = false;

        private void Start()
        {
            // Bắt đầu đếm ngược để phát nổ ngay khi được tạo ra
            Invoke(nameof(Explode), delay);
        }

        private void Explode()
        {
            if (hasExploded) return;
            hasExploded = true;

            // Tạo hiệu ứng cháy nổ
            if (explosionVFX != null)
            {
                // Sử dụng PoolManager nếu có thể để tối ưu
                Instantiate(explosionVFX, transform.position, transform.rotation);
            }

            // Lấy tất cả các đối tượng trong bán kính vụ nổ
            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

            foreach (Collider nearbyObject in colliders)
            {
                // Gây sát thương cho các đối tượng có thể nhận sát thương
                IDamageable damageable = nearbyObject.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    // Logic tính toán sát thương giảm dần theo khoảng cách có thể thêm ở đây
                    damageable.Damage(damage, false);
                }

                // Tác động lực đẩy vật lý
                Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
                }
            }

            // Hủy đối tượng lựu đạn sau khi đã nổ
            Destroy(gameObject);
        }

        // Tùy chọn: Cho phép nổ ngay khi va chạm với một số bề mặt nhất định
        private void OnCollisionEnter(Collision collision)
        {
            // Ví dụ: Nổ ngay khi chạm vào kẻ địch
            // if (collision.gameObject.CompareTag("Enemy"))
            // {
            //     CancelInvoke(nameof(Explode));
            //     Explode();
            // }
        }
    }
}