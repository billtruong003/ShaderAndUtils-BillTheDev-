// File: Assets/MyIndieGame/Scripts/Controllers/PlayerLocomotion.cs
// PHIÊN BẢN CẢI TIẾN VẬT LÝ - GIỮ NGUYÊN 100% API GỐC

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerLocomotion : MonoBehaviour
{
    // === CÁC BIẾN GỐC CỦA BẠN ===
    [Header("Dependencies")]
    [SerializeField] private PlayerAnimator playerAnimator;

    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 7f;
    [SerializeField] private float airControlSpeed = 5f;

    [Header("Rotation & Gravity")]
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private float gravity = -20.0f;

    // === CÁC CẢI TIẾN VỀ VẬT LÝ (THÊM VÀO) ===
    [Header("Physics & Feel Improvements")]
    [Tooltip("Thời gian để đạt tốc độ tối đa. Tạo cảm giác quán tính.")]
    [SerializeField] private float movementSmoothTime = 0.1f;
    [Tooltip("Tốc độ nhân vật trượt khi đứng trên dốc quá nghiêng.")]
    [SerializeField] private float slopeSlideSpeed = 8f;
    [Tooltip("Thời gian (giây) người chơi vẫn có thể nhảy sau khi rời khỏi mặt đất.")]
    [SerializeField] private float coyoteTime = 0.15f;

    // THAY ĐỔI: Chuyển GroundCheck sang Raycast để xử lý dốc
    [Header("Ground Check Settings")]
    [SerializeField] private LayerMask groundLayer;
    [Tooltip("Khoảng cách Raycast xuống để kiểm tra đất.")]
    [SerializeField] private float groundCheckDistance = 1.1f;

    // === CÁC BIẾN NỘI BỘ ===
    private CharacterController controller;
    private Transform camTransform;

    // Biến nội bộ gốc
    private Vector3 playerVelocity;

    // Biến nội bộ mới cho các cải tiến
    private Vector3 currentMoveVector;
    private Vector3 moveDampVelocity;
    private float coyoteTimeCounter;
    private bool isCurrentlyGrounded;
    private Vector3 groundNormal;

    // THAY ĐỔI: Dùng controller.velocity để lấy tốc độ thực tế, chính xác hơn
    public float CurrentSpeed => new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;
    public Vector3 PlayerVelocity => playerVelocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        camTransform = Camera.main.transform;
        if (playerAnimator == null) playerAnimator = GetComponent<PlayerAnimator>();
    }

    void Update()
    {
        // Chạy kiểm tra mặt đất và trọng lực mỗi frame để ổn định hơn
        PerformGroundCheck();
        HandleGravity();
    }

    private void PerformGroundCheck()
    {
        // Dùng Raycast thay cho SphereCast để lấy được "groundNormal"
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            isCurrentlyGrounded = true;
            groundNormal = hit.normal;
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            isCurrentlyGrounded = false;
            coyoteTimeCounter -= Time.deltaTime;
        }
    }

    // GIỮ NGUYÊN API: Hàm IsGrounded() vẫn tồn tại và hoạt động đúng
    public bool IsGrounded()
    {
        // Trả về trạng thái đã được tính toán trong PerformGroundCheck()
        return isCurrentlyGrounded;
    }

    // GIỮ NGUYÊN API: HandleGroundedMovement
    public void HandleGroundedMovement(Vector2 moveInput, bool isRunning, Transform target)
    {
        // 1. Tính toán vector di chuyển mục tiêu
        float targetMaxSpeed = isRunning ? runSpeed : walkSpeed;
        Vector3 targetMoveVector = CalculateCameraRelativeMoveDirection(moveInput) * targetMaxSpeed;

        // 2. CẢI TIẾN: Làm mượt di chuyển để tạo quán tính
        currentMoveVector = Vector3.SmoothDamp(currentMoveVector, targetMoveVector, ref moveDampVelocity, movementSmoothTime);

        // 3. CẢI TIẾN: Xử lý trượt dốc
        Vector3 finalMove = HandleSlopeSlide(currentMoveVector);

        // 4. CẬP NHẬT ANIMATOR (Logic không đổi)
        UpdateAnimatorParameters(moveInput, target, CalculateCameraRelativeMoveDirection(moveInput));

        // 5. XOAY (Logic không đổi)
        HandleRotation(targetMoveVector, target); // Dùng vector mục tiêu để xoay ngay cả khi đang đứng yên

        // 6. CẢI TIẾN: Chỉ gọi controller.Move MỘT LẦN với tất cả các lực
        controller.Move((finalMove + playerVelocity) * Time.deltaTime);
    }

    private Vector3 HandleSlopeSlide(Vector3 moveDirection)
    {
        if (!isCurrentlyGrounded) return moveDirection;

        float slopeAngle = Vector3.Angle(Vector3.up, groundNormal);
        if (slopeAngle > controller.slopeLimit)
        {
            Vector3 slopeSlideDirection = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
            return moveDirection + (slopeSlideDirection * slopeSlideSpeed);
        }
        return moveDirection;
    }

    // GIỮ NGUYÊN API: HandleAirborneMovement
    public void HandleAirborneMovement(Vector2 moveInput, Transform target)
    {
        Vector3 moveDirection = CalculateCameraRelativeMoveDirection(moveInput);

        // CẬP NHẬT ANIMATOR (Logic không đổi)
        if (target != null)
        {
            Vector3 localMoveDirection = transform.InverseTransformDirection(moveDirection);
            playerAnimator.UpdateMoveSpeed(localMoveDirection.z, localMoveDirection.x);
        }

        // XOAY (Logic không đổi)
        HandleRotation(moveDirection, target);

        // CẢI TIẾN: Chỉ gọi controller.Move MỘT LẦN
        controller.Move((moveDirection * airControlSpeed + playerVelocity) * Time.deltaTime);
    }

    // GIỮ NGUYÊN API: HandleJump
    public void HandleJump(float jumpHeight)
    {
        // CẢI TIẾN: Dùng coyote time thay vì IsGrounded()
        if (coyoteTimeCounter > 0f)
        {
            coyoteTimeCounter = 0f; // Ngăn nhảy 2 lần
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    /// <summary>
    /// THÊM MỚI (TÙY CHỌN): Hàm để cắt ngắn cú nhảy (gọi khi thả nút nhảy).
    /// Đây là hàm mới, không ảnh hưởng đến code cũ.
    /// </summary>
    public void CutJump()
    {
        if (playerVelocity.y > 0)
        {
            playerVelocity.y *= 0.5f;
        }
    }

    // ==========================================================
    // CÁC HÀM CÒN LẠI GIỮ NGUYÊN, KHÔNG THAY ĐỔI
    // ==========================================================

    private void UpdateAnimatorParameters(Vector2 moveInput, Transform target, Vector3 worldMoveDirection)
    {
        // Logic animation của bạn được giữ nguyên 100%
        if (target != null)
        {
            Vector3 localMoveDirection = transform.InverseTransformDirection(worldMoveDirection);
            playerAnimator.UpdateMoveSpeed(localMoveDirection.z, localMoveDirection.x);
        }
        else
        {
            // Cải tiến nhỏ: dùng tốc độ đã làm mượt để animation đồng bộ với di chuyển
            float currentPhysicalSpeed = currentMoveVector.magnitude;
            playerAnimator.UpdateMoveSpeed(currentPhysicalSpeed, 0f);
        }
    }

    public void HandleRotation(Vector3 moveDirection, Transform target)
    {
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
        else if (moveDirection.magnitude > 0.1f) // Thêm ngưỡng nhỏ để không bị giật khi thả tay
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    private void HandleGravity()
    {
        if (IsGrounded() && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }
        else
        {
            playerVelocity.y += gravity * Time.deltaTime;
        }
    }

    private Vector3 CalculateCameraRelativeMoveDirection(Vector2 moveInput)
    {
        Vector3 camForward = camTransform.forward;
        Vector3 camRight = camTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        return (camForward.normalized * moveInput.y + camRight.normalized * moveInput.x).normalized;
    }

    public void HandleDash(float dashSpeed) { controller.Move(transform.forward * dashSpeed * Time.deltaTime); }
    public void HandleAttackMovement(float speed) { controller.Move(transform.forward * speed * Time.deltaTime); }
    public void ForceLookAtTarget(Transform target) { if (target == null) return; Vector3 dir = (target.position - transform.position).normalized; dir.y = 0; if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir); }
    public void ForceLookAtDirection(Vector3 direction) { direction.y = 0; if (direction != Vector3.zero) transform.rotation = Quaternion.LookRotation(direction); }
    private void OnDrawGizmosSelected() { Gizmos.color = Color.cyan; Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, transform.position + Vector3.up * 0.1f + Vector3.down * groundCheckDistance); }
}