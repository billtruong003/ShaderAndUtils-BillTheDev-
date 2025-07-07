// File: Assets/MyIndieGame/Scripts/Controllers/DirectionVisualController.cs
using UnityEngine;

/// <summary>
/// Quản lý các yếu tố hình ảnh phụ trợ cho việc điều khiển của người chơi,
/// bao gồm mũi tên chỉ hướng và đường chỉ báo vị trí đáp đất khi nhảy.
/// Script này độc lập và chỉ đọc dữ liệu từ các controller khác để hoạt động.
/// </summary>
[AddComponentMenu("My Indie Game/Controllers/Direction Visual Controller")]
public sealed class DirectionVisualController : MonoBehaviour
{
    [Header("Core Dependencies")]
    [Tooltip("Tham chiếu đến InputHandler để lấy dữ liệu di chuyển của người chơi.")]
    [SerializeField] private InputHandler inputHandler;
    [Tooltip("Tham chiếu đến PlayerLocomotion để kiểm tra trạng thái đang ở trên không hay mặt đất.")]
    [SerializeField] private PlayerLocomotion playerLocomotion;

    [Header("Direction Arrow Settings")]
    [Tooltip("Transform của đối tượng mũi tên sẽ xoay theo hướng input.")]
    [SerializeField] private Transform directionArrowTransform;
    [Tooltip("Tốc độ xoay của mũi tên để tạo cảm giác mượt mà.")]
    [SerializeField] private float arrowRotationSpeed = 25f;

    [Header("Jump Trajectory Settings")]
    [Tooltip("Line Renderer để vẽ đường thẳng từ người chơi xuống mặt đất.")]
    [SerializeField] private LineRenderer trajectoryLine;
    [Tooltip("Đối tượng hình tròn hoặc decal sẽ xuất hiện tại điểm đáp đất dự kiến.")]
    [SerializeField] private Transform groundLandingIndicator;
    [Tooltip("Điểm bắt đầu của Line Renderer, thường là một xương ở hông hoặc gốc của nhân vật.")]
    [SerializeField] private Transform lineStartPivot;
    [Tooltip("Layer được định nghĩa là 'mặt đất' để Raycast có thể va chạm.")]
    [SerializeField] private LayerMask groundLayer;
    [Tooltip("Khoảng cách tối đa mà Raycast sẽ dò tìm mặt đất.")]
    [SerializeField] private float raycastMaxDistance = 100f;
    [Tooltip("Một khoảng nhỏ để nâng điểm gốc của Raycast, tránh trường hợp nó bắt đầu bên trong collider.")]
    [SerializeField] private float raycastOriginVerticalOffset = 0.1f;

    private Camera mainCamera;
    private bool areTrajectoryVisualsActive = false;

    private void Awake()
    {
        ValidateDependencies();
        mainCamera = Camera.main;
    }

    private void Start()
    {
        InitializeVisualsState();
    }

    private void Update()
    {
        HandleArrowRotation();
        HandleJumpTrajectoryVisuals();
    }

    private void ValidateDependencies()
    {
        if (inputHandler == null || playerLocomotion == null)
        {
            Debug.LogError($"[{nameof(DirectionVisualController)}] - Core Dependencies (InputHandler, PlayerLocomotion) must be assigned.", this);
            enabled = false;
            return;
        }

        if (directionArrowTransform == null)
        {
            Debug.LogWarning($"[{nameof(DirectionVisualController)}] - Direction Arrow Transform is not assigned. This feature will be disabled.", this);
        }

        if (trajectoryLine == null || groundLandingIndicator == null || lineStartPivot == null)
        {
            Debug.LogWarning($"[{nameof(DirectionVisualController)}] - One or more Jump Trajectory components are not assigned. This feature will be disabled.", this);
        }
    }

    private void InitializeVisualsState()
    {
        if (trajectoryLine != null) trajectoryLine.gameObject.SetActive(false);
        if (groundLandingIndicator != null) groundLandingIndicator.gameObject.SetActive(false);
        areTrajectoryVisualsActive = false;
    }

    private void HandleArrowRotation()
    {
        if (directionArrowTransform == null) return;

        Vector2 moveInput = inputHandler.MoveInput;

        // Chỉ xử lý khi có input để tránh mũi tên reset về hướng mặc định
        if (moveInput.sqrMagnitude < 0.01f) return;

        Vector3 worldSpaceDirection = CalculateWorldSpaceInputDirection(moveInput);

        if (worldSpaceDirection == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(worldSpaceDirection, Vector3.up);
        directionArrowTransform.rotation = Quaternion.Slerp(
            directionArrowTransform.rotation,
            targetRotation,
            Time.deltaTime * arrowRotationSpeed
        );
    }

    private void HandleJumpTrajectoryVisuals()
    {
        if (trajectoryLine == null || groundLandingIndicator == null) return;

        bool isAirborne = !playerLocomotion.IsGrounded();

        UpdateTrajectoryVisualsActivation(isAirborne);

        if (isAirborne)
        {
            UpdateTrajectoryPositions();
        }
    }

    private void UpdateTrajectoryVisualsActivation(bool shouldBeActive)
    {
        if (areTrajectoryVisualsActive == shouldBeActive) return;

        trajectoryLine.gameObject.SetActive(shouldBeActive);
        // Ground indicator chỉ bật khi raycast trúng, nên ta tắt nó ở đây
        // và sẽ bật lại trong UpdateTrajectoryPositions nếu cần.
        if (!shouldBeActive)
        {
            groundLandingIndicator.gameObject.SetActive(false);
        }
        areTrajectoryVisualsActive = shouldBeActive;
    }

    private void UpdateTrajectoryPositions()
    {
        Vector3 rayOrigin = lineStartPivot.position + Vector3.up * raycastOriginVerticalOffset;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastMaxDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            trajectoryLine.SetPosition(0, lineStartPivot.position);
            trajectoryLine.SetPosition(1, hit.point);

            groundLandingIndicator.position = hit.point;
            if (!groundLandingIndicator.gameObject.activeSelf)
            {
                groundLandingIndicator.gameObject.SetActive(true);
            }
        }
        else
        {
            // Nếu không tìm thấy mặt đất (ví dụ: đang rơi xuống vực)
            // thì ẩn các visual đi.
            trajectoryLine.gameObject.SetActive(false);
            groundLandingIndicator.gameObject.SetActive(false);
        }
    }

    private Vector3 CalculateWorldSpaceInputDirection(Vector2 moveInput)
    {
        if (mainCamera == null) return Vector3.forward;

        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight = mainCamera.transform.right;

        camForward.y = 0;
        camRight.y = 0;

        return (camForward.normalized * moveInput.y + camRight.normalized * moveInput.x).normalized;
    }
}