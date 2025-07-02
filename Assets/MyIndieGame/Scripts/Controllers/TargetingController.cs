using UnityEngine;
using System.Linq;

public class TargetingController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Phạm vi tối đa để tìm và khóa mục tiêu.")]
    [SerializeField] private float targetingRange = 15f;
    [Tooltip("Layer của các đối tượng được coi là kẻ địch. Rất quan trọng!")]
    [SerializeField] private LayerMask enemyLayer;

    // Tương lai có thể dùng để hiển thị icon trên đầu mục tiêu
    // [Header("Feedback")]
    // [SerializeField] private GameObject targetIconPrefab;
    // private GameObject currentTargetIcon;

    public Transform CurrentTarget { get; private set; }
    private Transform mainCameraTransform;

    private void Start()
    {
        // Lấy transform của camera để tính toán
        mainCameraTransform = Camera.main.transform;
    }

    /// <summary>
    /// Hàm chính để xử lý việc khóa/hủy khóa mục tiêu.
    /// Sẽ được gọi từ PlayerStateMachine khi có Input.
    /// </summary>
    public void HandleTargeting()
    {
        if (CurrentTarget != null)
        {
            // Nếu đã có mục tiêu, hủy nó đi
            ClearTarget();
        }
        else
        {
            // Nếu chưa có, tìm một mục tiêu mới
            FindAndSetTarget();
        }
    }

    /// <summary>
    /// Được gọi mỗi frame từ PlayerStateMachine để kiểm tra xem mục tiêu có còn hợp lệ không.
    /// </summary>
    public void ValidateTarget()
    {
        if (CurrentTarget == null) return;

        // Tính khoảng cách tới mục tiêu
        float distance = Vector3.Distance(transform.position, CurrentTarget.position);

        // Nếu mục tiêu chạy quá xa (cho thêm 20% khoảng đệm) hoặc bị hủy (chết), hủy target
        if (distance > targetingRange * 1.2f || !CurrentTarget.gameObject.activeInHierarchy)
        {
            ClearTarget();
        }
    }

    private void FindAndSetTarget()
    {
        // Tìm tất cả các collider trong phạm vi thuộc layer Enemy
        Collider[] enemies = Physics.OverlapSphere(transform.position, targetingRange, enemyLayer);

        Transform bestTarget = null;
        float closestDotProduct = -1f; // Giá trị càng gần 1, mục tiêu càng ở gần tâm màn hình

        foreach (var enemyCollider in enemies)
        {
            Vector3 directionToEnemy = (enemyCollider.transform.position - mainCameraTransform.position).normalized;

            // Tính toán dot product để xem mục tiêu có ở trước mặt camera không
            float dot = Vector3.Dot(mainCameraTransform.forward, directionToEnemy);

            // Chỉ xem xét các mục tiêu trong một hình nón phía trước camera (dot > 0.7 tương đương góc khoảng 45 độ)
            if (dot > 0.7f)
            {
                // Tìm mục tiêu có dot product lớn nhất (gần tâm màn hình nhất)
                if (dot > closestDotProduct)
                {
                    closestDotProduct = dot;
                    bestTarget = enemyCollider.transform;
                }
            }
        }

        // Nếu tìm thấy mục tiêu phù hợp, khóa nó lại
        if (bestTarget != null)
        {
            SetTarget(bestTarget);
        }
    }

    private void SetTarget(Transform newTarget)
    {
        CurrentTarget = newTarget;
        // TƯƠNG LAI: Kích hoạt icon trên đầu mục tiêu tại đây
        // if (currentTargetIcon != null) currentTargetIcon.SetActive(true);
    }

    private void ClearTarget()
    {
        CurrentTarget = null;
        // TƯƠNG LAI: Tắt icon trên đầu mục tiêu tại đây
        // if (currentTargetIcon != null) currentTargetIcon.SetActive(false);
    }
}