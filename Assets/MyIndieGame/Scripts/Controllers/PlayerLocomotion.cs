using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerLocomotion : MonoBehaviour
{
    [Header("Settings")]
    public float rotationSpeed = 15f;
    public float gravity = -20.0f;

    private CharacterController controller;
    private Transform camTransform;
    private Vector3 playerVelocity;

    // --- THAY ĐỔI Ở ĐÂY ---
    // Tạo một thuộc tính công khai để các script khác có thể đọc giá trị của playerVelocity
    // Dấu "=>" là một cách viết tắt cho một property chỉ có getter.
    public Vector3 PlayerVelocity => playerVelocity;

    // Stat-driven values
    private float currentSpeed;
    private float walkSpeed = 3f;
    private float runSpeed = 7f;

    // ... (phần còn lại của file giữ nguyên không đổi) ...
    // ...
    void Awake()
    {
        controller = GetComponent<CharacterController>();
        camTransform = Camera.main.transform;
    }

    public void HandleGroundedMovement(Vector2 moveInput, bool isRunning)
    {
        HandleGravity();

        if (moveInput.magnitude < 0.1f)
        {
            currentSpeed = 0f;
            return;
        }

        Vector3 moveDirection = CalculateMoveDirection(moveInput);
        RotatePlayer(moveDirection);

        currentSpeed = isRunning ? runSpeed : walkSpeed;
        controller.Move(moveDirection * currentSpeed * Time.deltaTime);
    }

    public void HandleAirborneMovement()
    {
        HandleGravity();
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
        controller.Move(dashDirection * dashSpeed * Time.deltaTime);
    }

    private void HandleGravity()
    {
        if (IsGrounded() && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }
        playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
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
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDirection), Time.deltaTime * rotationSpeed);
        }
    }

    public bool IsFalling()
    {
        return !controller.isGrounded && playerVelocity.y < 0f;
    }

    public bool IsGrounded() => controller.isGrounded;
    public float GetCurrentSpeed() => currentSpeed;

    public void UpdateMovementSpeeds(float newWalkSpeed, float newRunSpeed)
    {
        walkSpeed = newWalkSpeed;
        runSpeed = newRunSpeed;
    }
}