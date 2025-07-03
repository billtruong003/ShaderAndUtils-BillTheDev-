using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class TargetingController : MonoBehaviour
{
    [Header("Top-Down Settings")]
    [Tooltip("Phạm vi tối đa để tìm và khóa mục tiêu.")]
    [SerializeField] private float targetingRange = 10f;
    [Tooltip("Góc của hình nón phía trước nhân vật để tìm kiếm mục tiêu. 90 = hình quạt 180 độ.")]
    [SerializeField] private float targetingAngle = 90f;
    [Tooltip("Layer của các đối tượng được coi là kẻ địch.")]
    [SerializeField] private LayerMask enemyLayer;

    [Header("UI & Input")]
    [Tooltip("Kéo UI World-Space Canvas Prefab dùng làm hồng tâm khóa mục tiêu vào đây.")]
    [SerializeField] private GameObject targetReticlePrefab;

    // --- Các biến nội bộ ---
    public LayerMask EnemyLayer => enemyLayer;
    public Transform CurrentTarget { get; private set; }
    private GameObject currentTargetReticleInstance; // Instance của hồng tâm
    private List<Transform> potentialTargets = new List<Transform>();

    private void Start()
    {
        if (targetReticlePrefab != null)
        {
            // Tạo sẵn một instance và tắt nó đi để dùng sau
            currentTargetReticleInstance = Instantiate(targetReticlePrefab);
            currentTargetReticleInstance.SetActive(false);
        }
    }

    // Chuyển logic cập nhật vị trí reticle sang LateUpdate để nó chạy sau khi mọi di chuyển đã hoàn tất
    private void LateUpdate()
    {
        if (CurrentTarget != null && currentTargetReticleInstance != null)
        {
            // Reticle sẽ di chuyển theo mục tiêu
            // Giả sử reticle có component để tự xoay mặt về camera (FaceCamera script)
            currentTargetReticleInstance.transform.position = CurrentTarget.position + Vector3.up * 0.1f; // Điều chỉnh độ cao cho phù hợp
        }
    }

    /// <summary>
    /// Hàm chính được gọi từ Input để khóa hoặc hủy khóa mục tiêu.
    /// </summary>
    public void HandleTargeting()
    {
        if (CurrentTarget != null)
        {
            ClearTarget();
        }
        else
        {
            FindAndSetTarget();
        }
    }

    /// <summary>
    /// Hàm được gọi từ Input để chuyển sang mục tiêu tiếp theo.
    /// Ví dụ: Gán cho phím Tab.
    /// </summary>
    public void SwitchToNextTarget()
    {
        if (potentialTargets.Count <= 1) return;

        int currentIndex = potentialTargets.IndexOf(CurrentTarget);
        if (currentIndex != -1)
        {
            int nextIndex = (currentIndex + 1) % potentialTargets.Count;
            SetTarget(potentialTargets[nextIndex]);
        }
        else // Nếu vì lý do nào đó mục tiêu hiện tại không có trong danh sách, chỉ cần chọn mục tiêu đầu tiên
        {
            SetTarget(potentialTargets[0]);
        }
    }

    /// <summary>
    /// Kiểm tra xem mục tiêu có còn hợp lệ không (còn sống, trong tầm).
    /// </summary>
    public void ValidateTarget()
    {
        if (CurrentTarget == null) return;

        float distance = Vector3.Distance(transform.position, CurrentTarget.position);

        // Hủy khóa nếu mục tiêu chết hoặc chạy quá xa
        if (!CurrentTarget.gameObject.activeInHierarchy || distance > targetingRange * 1.2f)
        {
            ClearTarget();
        }
    }

    /// <summary>
    /// Tìm tất cả các kẻ địch trong tầm và chọn ra kẻ địch tốt nhất để khóa.
    /// </summary>
    private void FindAndSetTarget()
    {
        potentialTargets.Clear();
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, targetingRange, enemyLayer);

        foreach (var enemyCollider in enemiesInRange)
        {
            potentialTargets.Add(enemyCollider.transform);
        }

        if (potentialTargets.Count == 0) return;

        // Sắp xếp danh sách mục tiêu tiềm năng: ưu tiên góc nhỏ nhất, sau đó đến khoảng cách gần nhất
        potentialTargets = potentialTargets.OrderBy(target =>
        {
            Vector3 directionToTarget = target.position - transform.position;
            return Vector3.Angle(transform.forward, directionToTarget);
        }).ThenBy(target => Vector3.Distance(transform.position, target.position)).ToList();

        // Chọn mục tiêu đầu tiên trong danh sách đã sắp xếp (gần tâm phía trước nhất)
        Transform bestTarget = potentialTargets[0];

        // Kiểm tra xem mục tiêu tốt nhất có nằm trong hình nón phía trước không
        Vector3 dirToBestTarget = (bestTarget.position - transform.position).normalized;
        float angleToBestTarget = Vector3.Angle(transform.forward, dirToBestTarget);

        if (angleToBestTarget <= targetingAngle / 2f)
        {
            SetTarget(bestTarget);
        }
        else // Nếu không có ai trong hình nón, chỉ cần chọn kẻ địch gần nhất
        {
            bestTarget = potentialTargets.OrderBy(t => Vector3.Distance(transform.position, t.position)).FirstOrDefault();
            if (bestTarget != null)
            {
                SetTarget(bestTarget);
            }
        }
    }

    private void SetTarget(Transform newTarget)
    {
        CurrentTarget = newTarget;
        if (currentTargetReticleInstance != null)
        {
            currentTargetReticleInstance.SetActive(true);
        }
    }

    private void ClearTarget()
    {
        CurrentTarget = null;
        if (currentTargetReticleInstance != null)
        {
            currentTargetReticleInstance.SetActive(false);
        }
    }

    // Optional: Vẽ Gizmos để debug trong Editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, targetingRange);

        // Vẽ hình nón
        Vector3 forwardCone = Quaternion.Euler(0, -targetingAngle / 2, 0) * transform.forward;
        Vector3 backwardCone = Quaternion.Euler(0, targetingAngle / 2, 0) * transform.forward;
        Gizmos.color = new Color(1, 1, 0, 0.2f);
        Gizmos.DrawLine(transform.position, transform.position + forwardCone * targetingRange);
        Gizmos.DrawLine(transform.position, transform.position + backwardCone * targetingRange);
    }
}