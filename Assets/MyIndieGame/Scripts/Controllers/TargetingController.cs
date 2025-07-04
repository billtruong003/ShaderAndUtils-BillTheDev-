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
    // MỚI: Biến để điều chỉnh khoảng cách nhấc hồng tâm về phía camera
    [Tooltip("Khoảng cách để nhấc hồng tâm về phía camera, tránh bị khuất hoặc Z-fighting.")]
    [SerializeField] private float reticleCameraOffset = 0.5f;

    // --- Các biến nội bộ ---
    public LayerMask EnemyLayer => enemyLayer;
    public Transform CurrentTarget { get; private set; }
    private GameObject currentTargetReticleInstance;
    private List<Transform> potentialTargets = new List<Transform>();
    private Camera mainCamera; // MỚI: Cache camera để tăng hiệu suất

    private void Start()
    {
        if (targetReticlePrefab != null)
        {
            currentTargetReticleInstance = Instantiate(targetReticlePrefab);
            currentTargetReticleInstance.SetActive(false);
        }

        // MỚI: Tìm và cache camera chính khi bắt đầu
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("TargetingController: Không tìm thấy Camera được tag 'MainCamera'. Tính năng hồng tâm sẽ không hoạt động đúng.");
        }
    }

    // THAY ĐỔI: Chuyển logic cập nhật vị trí reticle sang LateUpdate
    private void LateUpdate()
    {
        // THAY ĐỔI: Toàn bộ logic bên trong đã được cập nhật
        if (CurrentTarget != null && currentTargetReticleInstance != null && mainCamera != null)
        {
            // 1. Vị trí gốc của hồng tâm (trên mục tiêu, có thể điều chỉnh độ cao cho phù hợp)
            Vector3 basePosition = CurrentTarget.position + Vector3.up * 0.1f;

            // 2. Tính toán hướng từ vị trí gốc của hồng tâm đến camera
            //    Vector này đã được chuẩn hóa (độ dài = 1)
            Vector3 directionToCamera = (mainCamera.transform.position - basePosition).normalized;

            // 3. Vị trí cuối cùng: Dịch chuyển hồng tâm từ vị trí gốc một khoảng về phía camera
            currentTargetReticleInstance.transform.position = basePosition + directionToCamera * reticleCameraOffset;

            // 4. (Giữ nguyên) Giả sử reticle có component để tự xoay mặt về camera (FaceCamera script)
            //    Nếu không, bạn có thể thêm dòng này để nó luôn nhìn vào camera:
            //    currentTargetReticleInstance.transform.rotation = Quaternion.LookRotation(currentTargetReticleInstance.transform.position - mainCamera.transform.position);
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
        else
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

        potentialTargets = potentialTargets.OrderBy(target =>
        {
            Vector3 directionToTarget = target.position - transform.position;
            return Vector3.Angle(transform.forward, directionToTarget);
        }).ThenBy(target => Vector3.Distance(transform.position, target.position)).ToList();

        Transform bestTarget = potentialTargets[0];

        Vector3 dirToBestTarget = (bestTarget.position - transform.position).normalized;
        float angleToBestTarget = Vector3.Angle(transform.forward, dirToBestTarget);

        if (angleToBestTarget <= targetingAngle / 2f)
        {
            SetTarget(bestTarget);
        }
        else
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, targetingRange);

        Vector3 forwardCone = Quaternion.Euler(0, -targetingAngle / 2, 0) * transform.forward;
        Vector3 backwardCone = Quaternion.Euler(0, targetingAngle / 2, 0) * transform.forward;
        Gizmos.color = new Color(1, 1, 0, 0.2f);
        Gizmos.DrawLine(transform.position, transform.position + forwardCone * targetingRange);
        Gizmos.DrawLine(transform.position, transform.position + backwardCone * targetingRange);
    }
}