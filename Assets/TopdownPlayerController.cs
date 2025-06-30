using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 2.0f; // Đặt giá trị này khớp với Threshold trong Animator
    public float runSpeed = 5.0f;  // Đặt giá trị này khớp với Threshold trong Animator
    public float rotationSpeed = 10f;

    [Header("Jumping & Gravity")]
    public float jumpHeight = 1.5f;
    public float gravity = -15.0f;

    // References
    [SerializeField] private CharacterController controller;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform camTransform;

    // Private variables
    private Vector3 playerVelocity;
    private bool isGrounded;

    void Update()
    {
        HandleMovementAndAnimation();
        HandleActions();
    }

    private void HandleMovementAndAnimation()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }

        // Lấy input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 moveInput = new Vector3(horizontal, 0f, vertical);

        float currentSpeed = 0f;

        if (moveInput.magnitude >= 0.1f)
        {
            // Tính toán hướng di chuyển dựa trên camera
            Vector3 camForward = camTransform.forward;
            Vector3 camRight = camTransform.right;
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();
            Vector3 moveDirection = (camForward * moveInput.z + camRight * moveInput.x).normalized;

            // Xoay nhân vật
            if (moveDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDirection), Time.deltaTime * rotationSpeed);
            }

            // Chọn tốc độ (đi bộ hay chạy)
            bool isRunning = Input.GetKey(KeyCode.LeftShift);
            currentSpeed = isRunning ? runSpeed : walkSpeed;

            // Di chuyển nhân vật
            controller.Move(moveDirection * currentSpeed * Time.deltaTime);
        }

        // --- PHẦN QUAN TRỌNG NHẤT ---
        // Cập nhật tham số "Speed" trong Animator
        // Dùng giá trị `currentSpeed` để Animator biết nên blend giữa idle, walk, hay run
        animator.SetFloat("Speed", currentSpeed, 0.1f, Time.deltaTime); // 0.1f là damp time để chuyển đổi mượt mà

        // Xử lý trọng lực
        playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }

    private void HandleActions()
    {
        // Xử lý nhảy
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetTrigger("Jump");
        }

        // Xử lý đấm
        if (Input.GetMouseButtonDown(0))
        {
            animator.SetTrigger("Punch");
        }
    }

    // Hàm này có thể được gọi từ bên ngoài
    public void TriggerKnockout()
    {
        animator.SetTrigger("Knockout");
        this.enabled = false; // Vô hiệu hóa điều khiển khi bị hạ gục
    }
}