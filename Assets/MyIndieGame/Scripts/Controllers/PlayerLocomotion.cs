using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerLocomotion : MonoBehaviour
{
    [Header("Settings")]
    public float airControlSpeed = 5f;
    public float rotationSpeed = 15f;
    public float gravity = -20.0f;

    private CharacterController controller;
    private Transform camTransform;
    private Vector3 playerVelocity;

    public Vector3 PlayerVelocity => playerVelocity;

    private float currentSpeed;
    private float walkSpeed = 3f;
    private float runSpeed = 7f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        camTransform = Camera.main.transform;
    }

    // --- SỬA ĐỔI HÀM NÀY ĐỂ NHẬN THAM SỐ THỨ 3 ---
    public void HandleGroundedMovement(Vector2 moveInput, bool isRunning, Transform target)
    {
        if (moveInput.magnitude < 0.1f)
        {
            currentSpeed = 0f;
        }
        else
        {
            currentSpeed = isRunning ? runSpeed : walkSpeed;
        }

        Vector3 moveDirection = CalculateMoveDirection(moveInput);
        Vector3 horizontalVelocity = moveDirection * currentSpeed;

        HandleGravity();

        Vector3 finalVelocity = horizontalVelocity;
        finalVelocity.y = playerVelocity.y;

        // Gọi hàm xoay người đã có sẵn, truyền vào cả input và target
        HandleRotation(moveInput, target);

        controller.Move(finalVelocity * Time.deltaTime);
    }

    public void HandleAttackMovement(float speed)
    {
        Vector3 forwardVelocity = transform.forward * speed;
        HandleGravity();
        forwardVelocity.y = playerVelocity.y;
        controller.Move(forwardVelocity * Time.deltaTime);
    }

    // --- SỬA ĐỔI HÀM NÀY ĐỂ NHẬN THAM SỐ THỨ 2 ---
    public void HandleAirborneMovement(Vector2 moveInput, Transform target)
    {
        Vector3 moveDirection = CalculateMoveDirection(moveInput);
        Vector3 horizontalVelocity = moveDirection * airControlSpeed;

        HandleGravity();

        Vector3 finalVelocity = horizontalVelocity;
        finalVelocity.y = playerVelocity.y;

        // Gọi hàm xoay người đã có sẵn, truyền vào cả input và target
        HandleRotation(moveInput, target);

        controller.Move(finalVelocity * Time.deltaTime);
    }

    public void HandleJump(float jumpHeight)
    {
        if (IsGrounded())
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    public void HandleDash(float dashSpeed)
    {
        Vector3 dashDirection = transform.forward;
        Vector3 dashVelocity = dashDirection * dashSpeed;
        dashVelocity.y = playerVelocity.y;
        controller.Move(dashVelocity * Time.deltaTime);
    }

    private void HandleGravity()
    {
        if (IsGrounded() && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }

        playerVelocity.y += gravity * Time.deltaTime;
    }

    private Vector3 CalculateMoveDirection(Vector2 moveInput)
    {
        Vector3 camForward = camTransform.forward;
        Vector3 camRight = camTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        return (camForward.normalized * moveInput.y + camRight.normalized * moveInput.x).normalized;
    }

    private void RotatePlayer(Vector3 moveDirection)
    {
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    public void HandleRotation(Vector2 moveInput, Transform target)
    {
        // Ưu tiên xoay theo mục tiêu nếu có
        if (target != null)
        {
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            directionToTarget.y = 0;
            if (directionToTarget != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed * 2f); // Xoay nhanh hơn khi có target
            }
        }
        // Nếu không có mục tiêu, xoay theo hướng di chuyển của người chơi
        else
        {
            Vector3 moveDirection = CalculateMoveDirection(moveInput);
            if (moveDirection != Vector3.zero)
            {
                RotatePlayer(moveDirection);
            }
        }
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

    public bool IsGrounded() => controller.isGrounded;

    public float GetCurrentSpeed() => currentSpeed;

    public void UpdateMovementSpeeds(float newWalkSpeed, float newRunSpeed)
    {
        walkSpeed = newWalkSpeed;
        runSpeed = newRunSpeed;
    }
}