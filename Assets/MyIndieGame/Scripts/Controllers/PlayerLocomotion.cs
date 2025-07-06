// File: Assets/MyIndieGame/Scripts/Controllers/PlayerLocomotion.cs
using UnityEngine;
using System; // <-- THÊM MỚI
using System.Collections; // <-- THÊM MỚI

[RequireComponent(typeof(CharacterController))]
public class PlayerLocomotion : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerAnimator playerAnimator;

    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 7f;
    [SerializeField] private float airControlSpeed = 5f;

    [Header("Rotation & Gravity")]
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private float gravity = -20.0f;

    [Header("Physics & Feel Improvements")]
    [SerializeField] private float movementSmoothTime = 0.1f;
    [SerializeField] private float slopeSlideSpeed = 8f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private int maxJumps = 2;

    [Header("Ground Check Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 1.1f;

    private CharacterController controller;
    private Transform camTransform;

    private Vector3 playerVelocity;
    private Vector3 moveDampVelocity;
    private float coyoteTimeCounter;
    private bool isCurrentlyGrounded;
    private Vector3 groundNormal;
    private int jumpsLeft;

    public float CurrentSpeed => new Vector3(playerVelocity.x, 0, playerVelocity.z).magnitude;
    public Vector3 PlayerVelocity => playerVelocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        camTransform = Camera.main.transform;
        if (playerAnimator == null) playerAnimator = GetComponent<PlayerAnimator>();
        jumpsLeft = maxJumps;
    }

    void Update()
    {
        PerformGroundCheck();
        HandleGravity();
    }

    private void PerformGroundCheck()
    {
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            if (!isCurrentlyGrounded)
            {
                groundNormal = hit.normal;
                jumpsLeft = maxJumps;
            }
            isCurrentlyGrounded = true;
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            isCurrentlyGrounded = false;
            coyoteTimeCounter -= Time.deltaTime;
        }
    }

    public bool IsGrounded()
    {
        return isCurrentlyGrounded || controller.isGrounded;
    }

    public bool PerformJump(float jumpHeight, float doubleJumpHeight, Vector2 moveInput, out bool isDoubleJump)
    {
        isDoubleJump = false;

        if (coyoteTimeCounter > 0f)
        {
            jumpsLeft = maxJumps - 1;
            coyoteTimeCounter = 0f;
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            return true;
        }

        if (jumpsLeft > 0)
        {
            isDoubleJump = true;
            jumpsLeft--;

            Vector3 moveDirection = CalculateCameraRelativeMoveDirection(moveInput);
            playerVelocity.x = moveDirection.x * airControlSpeed;
            playerVelocity.z = moveDirection.z * airControlSpeed;
            playerVelocity.y = Mathf.Sqrt(doubleJumpHeight * -2f * gravity);

            HandleRotation(moveDirection, null);
            return true;
        }

        return false;
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

    public void HandleGroundedMovement(Vector2 moveInput, bool isRunning, Transform target)
    {
        float targetMaxSpeed = isRunning ? runSpeed : walkSpeed;
        Vector3 targetMoveVector = CalculateCameraRelativeMoveDirection(moveInput) * targetMaxSpeed;

        Vector3 horizontalVelocity = new Vector3(playerVelocity.x, 0, playerVelocity.z);
        horizontalVelocity = Vector3.SmoothDamp(horizontalVelocity, targetMoveVector, ref moveDampVelocity, movementSmoothTime);
        playerVelocity.x = horizontalVelocity.x;
        playerVelocity.z = horizontalVelocity.z;

        Vector3 finalMove = HandleSlopeSlide(playerVelocity);

        UpdateAnimatorParameters(moveInput, target, CalculateCameraRelativeMoveDirection(moveInput));
        HandleRotation(targetMoveVector, target);
        controller.Move(finalMove * Time.deltaTime);
    }

    private Vector3 HandleSlopeSlide(Vector3 currentVelocity)
    {
        if (!isCurrentlyGrounded) return currentVelocity;

        float slopeAngle = Vector3.Angle(Vector3.up, groundNormal);
        if (slopeAngle > controller.slopeLimit)
        {
            Vector3 slopeDirection = Vector3.ProjectOnPlane(new Vector3(currentVelocity.x, 0, currentVelocity.z), groundNormal);
            Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized * slopeSlideSpeed;
            return new Vector3(slopeDirection.x + slideDirection.x, currentVelocity.y, slopeDirection.z + slideDirection.z);
        }
        return currentVelocity;
    }

    public void HandleAirborneMovement(Vector2 moveInput, Transform target)
    {
        Vector3 moveDirection = CalculateCameraRelativeMoveDirection(moveInput);

        playerVelocity.x = Mathf.Lerp(playerVelocity.x, moveDirection.x * airControlSpeed, Time.deltaTime * airControlSpeed * 0.5f);
        playerVelocity.z = Mathf.Lerp(playerVelocity.z, moveDirection.z * airControlSpeed, Time.deltaTime * airControlSpeed * 0.5f);

        if (target != null)
        {
            Vector3 worldMoveDirection = new Vector3(playerVelocity.x, 0, playerVelocity.z);
            Vector3 localMoveDirection = transform.InverseTransformDirection(worldMoveDirection);
            playerAnimator.UpdateMoveSpeed(localMoveDirection.z, localMoveDirection.x);
        }

        HandleRotation(moveDirection, target);
        controller.Move(playerVelocity * Time.deltaTime);
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
        else if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
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

    private void UpdateAnimatorParameters(Vector2 moveInput, Transform target, Vector3 worldMoveDirection)
    {
        if (target != null)
        {
            Vector3 localMoveDirection = transform.InverseTransformDirection(worldMoveDirection);
            playerAnimator.UpdateMoveSpeed(localMoveDirection.z, localMoveDirection.x);
        }
        else
        {
            float currentPhysicalSpeed = new Vector3(playerVelocity.x, 0, playerVelocity.z).magnitude;
            playerAnimator.UpdateMoveSpeed(currentPhysicalSpeed, 0f);
        }
    }

    public void HandleDash(float dashSpeed)
    {
        controller.Move(transform.forward * dashSpeed * Time.deltaTime);
    }

    public void HandleAttackMovement(float speed)
    {
        controller.Move(transform.forward * speed * Time.deltaTime);
    }

    public void ForceLookAtTarget(Transform target)
    {
        if (target == null) return;
        Vector3 dir = (target.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
    }

    public void ForceLookAtDirection(Vector3 direction)
    {
        direction.y = 0;
        if (direction != Vector3.zero) transform.rotation = Quaternion.LookRotation(direction);
    }

    // --- THÊM MỚI: Hệ thống Lunge thông minh ---
    public void PerformLunge(Transform target, float idealRange, float duration)
    {
        StartCoroutine(LungeCoroutine(target, idealRange, duration));
    }
    private IEnumerator LungeCoroutine(Transform target, float idealRange, float duration)
    {
        if (target == null || duration <= 0)
        {
            yield break;
        }

        Vector3 startPosition = transform.position;
        float timer = 0f;

        while (timer < duration)
        {
            // Luôn tính toán lại vị trí đích để xử lý trường hợp mục tiêu di chuyển
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            directionToTarget.y = 0;
            Vector3 targetPosition = target.position - directionToTarget * idealRange;
            targetPosition.y = transform.position.y; // Giữ nguyên độ cao của người chơi

            // Xoay người về phía mục tiêu trong khi lướt
            if (directionToTarget != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed * 3f);
            }

            // Di chuyển mượt mà
            float progress = Mathf.SmoothStep(0, 1, timer / duration);
            Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, progress);
            controller.Move(newPosition - transform.position);

            timer += Time.deltaTime;
            yield return null;
        }

        // Đảm bảo xoay mặt đúng hướng khi kết thúc
        ForceLookAtTarget(target);
    }

    // --- KẾT THÚC THÊM MỚI ---
}