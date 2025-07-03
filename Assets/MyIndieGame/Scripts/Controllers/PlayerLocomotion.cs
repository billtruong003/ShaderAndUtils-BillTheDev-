// File: Assets/MyIndieGame/Scripts/Controllers/PlayerLocomotion.cs
// Phiên bản nâng cấp cuối cùng, hỗ trợ hai chế độ di chuyển riêng biệt
// và cập nhật Animator tương ứng cho từng chế độ.

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerLocomotion : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Kéo Player Animator vào đây để cập nhật các tham số animation.")]
    [SerializeField] private PlayerAnimator playerAnimator;

    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 7f;
    [SerializeField] private float airControlSpeed = 5f;

    [Header("Rotation & Gravity")]
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private float gravity = -20.0f;

    // Các biến nội bộ
    private CharacterController controller;
    private Transform camTransform;
    private Vector3 playerVelocity;
    private float currentPhysicalSpeed; // Tốc độ vật lý thực tế của nhân vật

    public Vector3 PlayerVelocity => playerVelocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        camTransform = Camera.main.transform;

        if (playerAnimator == null)
        {
            // Cố gắng tự tìm nếu chưa được gán
            playerAnimator = GetComponent<PlayerAnimator>();
            if (playerAnimator == null)
            {
                Debug.LogError("PlayerAnimator is not assigned in PlayerLocomotion and could not be found on the same GameObject!", this);
            }
        }
    }

    // Hàm chính, đóng vai trò điều phối logic di chuyển trên mặt đất
    public void HandleGroundedMovement(Vector2 moveInput, bool isRunning, Transform target)
    {
        // Tính toán tốc độ vật lý dựa trên input và trạng thái chạy
        currentPhysicalSpeed = moveInput.magnitude < 0.1f ? 0f : (isRunning ? runSpeed : walkSpeed);
        
        Vector3 moveDirection;

        if (target != null)
        {
            // --- Chế độ Khóa Mục Tiêu (Lock-on) ---
            moveDirection = CalculateTargetRelativeMoveDirection(moveInput, target);
            // Cập nhật Animator với input X, Y để blend animation 8 hướng (Strafe)
            playerAnimator.UpdateMoveSpeed(moveInput.y, moveInput.x);
        }
        else
        {
            // --- Chế độ Tự Do (Free-form) ---
            moveDirection = CalculateCameraRelativeMoveDirection(moveInput);
            // Cập nhật Animator chỉ với tốc độ tổng (tiến/lùi), vì nhân vật luôn hướng về phía trước
            playerAnimator.UpdateMoveSpeed(moveInput.magnitude, 0f);
        }

        // Áp dụng di chuyển vật lý
        controller.Move(moveDirection * currentPhysicalSpeed * Time.deltaTime);

        // Áp dụng trọng lực
        HandleGravity();
        controller.Move(playerVelocity * Time.deltaTime);

        // Xử lý xoay người
        HandleRotation(moveDirection, target);
    }

    // Di chuyển khi đang ở trên không
    public void HandleAirborneMovement(Vector2 moveInput, Transform target)
    {
        Vector3 moveDirection;
        if (target != null)
        {
            moveDirection = CalculateTargetRelativeMoveDirection(moveInput, target);
        }
        else
        {
            moveDirection = CalculateCameraRelativeMoveDirection(moveInput);
        }
        
        controller.Move(moveDirection * airControlSpeed * Time.deltaTime);
        HandleGravity();
        controller.Move(playerVelocity * Time.deltaTime);

        // Khi trên không, chỉ cho phép xoay người khi không có mục tiêu
        if (target == null)
        {
            HandleRotation(moveDirection, null);
        }
    }

    // Xử lý xoay người cho cả hai chế độ
    public void HandleRotation(Vector3 moveDirection, Transform target)
    {
        // Ưu tiên 1: Luôn xoay về phía mục tiêu nếu có
        if (target != null)
        {
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            directionToTarget.y = 0;
            if (directionToTarget != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed * 2f);
            }
        }
        // Ưu tiên 2: Nếu không có mục tiêu, xoay theo hướng di chuyển
        else if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    private void HandleGravity()
    {
        if (controller.isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f; // Một lực nhỏ để giữ nhân vật trên mặt đất
        }
        playerVelocity.y += gravity * Time.deltaTime;
    }

    // Tính hướng di chuyển tương đối so với camera (cho chế độ Tự do)
    private Vector3 CalculateCameraRelativeMoveDirection(Vector2 moveInput)
    {
        Vector3 camForward = camTransform.forward;
        Vector3 camRight = camTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        return (camForward.normalized * moveInput.y + camRight.normalized * moveInput.x).normalized;
    }

    // Tính hướng di chuyển tương đối so với mục tiêu (cho chế độ Strafe)
    private Vector3 CalculateTargetRelativeMoveDirection(Vector2 moveInput, Transform target)
    {
        Vector3 forward = (target.position - transform.position).normalized;
        forward.y = 0;
        // Vector3 right = Vector3.Cross(Vector3.up, forward).normalized; // Cách cũ
        Vector3 right = new Vector3(forward.z, 0, -forward.x); // Cách tính vector vuông góc hiệu quả hơn
        return (forward * moveInput.y + right * moveInput.x).normalized;
    }
    
    // --- Các hàm hành động (không thay đổi) ---

    public void HandleJump(float jumpHeight)
    {
        if (IsGrounded())
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    public void HandleDash(float dashSpeed)
    {
        // Dash sẽ luôn đi về phía trước của nhân vật tại thời điểm đó
        Vector3 dashVelocity = transform.forward * dashSpeed;
        controller.Move(dashVelocity * Time.deltaTime);
    }
    
    public void HandleAttackMovement(float speed)
    {
        // Di chuyển tới trước khi tấn công
        Vector3 forwardVelocity = transform.forward * speed;
        controller.Move(forwardVelocity * Time.deltaTime);
    }

    public void ForceLookAtTarget(Transform target)
    {
        if (target == null) return;
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        directionToTarget.y = 0;
        if (directionToTarget != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(directionToTarget);
        }
    }

    public void ForceLookAtDirection(Vector3 direction)
    {
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    public bool IsGrounded() => controller.isGrounded;

    // Hàm GetCurrentSpeed được giữ lại theo yêu cầu
    // Nó trả về tốc độ vật lý, hữu ích cho Blend Tree 1D ở chế độ Tự do
    public float GetCurrentSpeed()
    {
        return currentPhysicalSpeed;
    }
}