using UnityEngine;
using System.Collections;

namespace Kaelia
{
    public enum PlayerState { Idle, Walking, Running, Jumping, Dashing, Sliding, WallRunning }
    public enum SkillMoveType { None, Dash, Slide, WallRun }

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class KaeliaCharacterController : MonoBehaviour
    {
        public PlayerState CurrentState { get; private set; }
        public float CurrentKineticEnergy { get; private set; }
        public bool IsFlowStateActive => flowStateTimer > 0;
        public float MaxKineticEnergy => kineticEnergySettings.maxEnergy;
        public bool IsWallRunning => CurrentState == PlayerState.WallRunning;
        public Vector3 WallNormal { get; private set; }

        [Header("Dependencies")]
        [SerializeField] private KeybindingSO keybindings;
        [SerializeField] private Rigidbody rb;
        [SerializeField] private CapsuleCollider playerCollider;
        [SerializeField] private Transform orientation;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private KaeliaCameraController cameraController;

        [Header("Ground & Wall Detection")]
        [SerializeField] private LayerMask whatIsGround;
        [SerializeField] private LayerMask whatIsWall;
        [SerializeField] private float groundCheckDistance = 0.1f;
        [SerializeField] private float wallCheckDistance = 0.7f;
        [SerializeField] private float groundLinearDamping = 6f;

        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 7f;
        [SerializeField] private float runSpeed = 12f;
        [SerializeField] private float airMultiplier = 0.4f;
        [SerializeField] private float rotationSmoothTime = 0.1f;

        [Header("Jump Settings")]
        [SerializeField] private float jumpForce = 15f;
        [SerializeField] private float wallJumpUpForce = 12f;
        [SerializeField] private float wallJumpSideForce = 20f;
        [Tooltip("Phần trăm quán tính giữ lại khi nhảy khỏi tường (0.5 = 50%)")]
        [SerializeField, Range(0f, 1f)] private float wallJumpMomentumPreservation = 0.5f;

        [Header("Dash Settings")]
        [SerializeField] private float dashSpeed = 30f;
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private float dashCooldown = 1f;

        [Header("Slide Settings")]
        [SerializeField] private float slideStartBoost = 10f;
        [SerializeField] private float slideFriction = 0.985f;
        [SerializeField] private float slopeSlideMultiplier = 2f;
        [SerializeField] private float slideColliderHeight = 0.8f;
        [SerializeField] private float maxSlideSpeed = 25f;

        [Header("Wall Run Settings")]
        [SerializeField] private float wallRunSpeed = 15f;
        [SerializeField] private float wallStickForce = 100f;
        [SerializeField] private float wallRunGravity = 3f;
        [SerializeField] private float maxWallRunTime = 2.0f;

        [Header("Camera Effects")]
        [SerializeField] private float wallRunCameraTilt = 10f;

        [System.Serializable]
        public struct KineticEnergySettings { public float maxEnergy, energyDecayRate, walkGain, runGain, jumpGain, dashGain, slideGain, wallRunGain; }
        [SerializeField] private KineticEnergySettings kineticEnergySettings;

        [System.Serializable]
        public struct FlowStateSettings { public float duration, energyGainMultiplier; }
        [SerializeField] private FlowStateSettings flowStateSettings;

        private Vector3 moveDirection;
        private Vector2 moveInput;
        private bool isGrounded;
        private bool canDoubleJump;
        private bool canDash = true;
        private float originalColliderHeight;
        private Vector3 originalColliderCenter;
        private float dashCooldownTimer;
        private float wallRunTimer;
        private float flowStateTimer;
        private float turnSmoothVelocity;
        private SkillMoveType lastSkillMove = SkillMoveType.None;
        private RaycastHit leftWallHit, rightWallHit, groundHit;
        private bool isWallLeft, isWallRight;

        private void Start()
        {
            if (keybindings == null || cameraTransform == null || cameraController == null)
            {
                Debug.LogError("Một hoặc nhiều Dependencies chưa được gán! Vô hiệu hóa Controller.");
                enabled = false;
                return;
            }

            rb.freezeRotation = true;
            originalColliderHeight = playerCollider.height;
            originalColliderCenter = playerCollider.center;
        }

        private void Update()
        {
            HandleChecks();
            HandleInputAndRotation();
            HandleStateLogic();
            HandleTimers();
            HandleKineticEnergy();
            UpdateCameraController();
        }

        private void FixedUpdate()
        {
            ExecuteMovement();
        }

        private void HandleChecks()
        {
            isGrounded = Physics.Raycast(transform.position, Vector3.down, out groundHit, playerCollider.bounds.extents.y + groundCheckDistance, whatIsGround);
            isWallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallCheckDistance, whatIsWall);
            isWallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallCheckDistance, whatIsWall);
        }

        private void HandleInputAndRotation()
        {
            moveInput.x = Input.GetAxisRaw("Horizontal");
            moveInput.y = Input.GetAxisRaw("Vertical");

            Vector3 camFwd = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            moveDirection = (camFwd * moveInput.y + camRight * moveInput.x).normalized;

            if (moveDirection.sqrMagnitude > 0.01f && CurrentState != PlayerState.Sliding)
            {
                float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(orientation.eulerAngles.y, targetAngle, ref turnSmoothVelocity, rotationSmoothTime);
                orientation.rotation = Quaternion.Euler(0f, angle, 0f);
            }

            if (Input.GetKeyDown(keybindings.jumpKey)) HandleJumpInput();
            if (Input.GetKeyDown(keybindings.dashKey) && canDash) ChangeState(PlayerState.Dashing);
            if (Input.GetKeyDown(keybindings.slideKey) && isGrounded && rb.linearVelocity.magnitude > walkSpeed) ChangeState(PlayerState.Sliding);
            if (Input.GetKeyDown(keybindings.kineticPulseKey)) TryActivateKineticPulse();
        }

        private void HandleStateLogic()
        {
            if (CurrentState == PlayerState.Dashing) return;

            if (CanWallRun()) ChangeState(PlayerState.WallRunning);
            else if (CurrentState == PlayerState.WallRunning) ChangeState(PlayerState.Jumping);

            if (CurrentState == PlayerState.Sliding)
            {
                if (rb.linearVelocity.magnitude < walkSpeed || Input.GetKeyUp(keybindings.slideKey))
                    StopSlide();
            }
        }

        private void ExecuteMovement()
        {
            switch (CurrentState)
            {
                case PlayerState.Walking:
                case PlayerState.Running:
                case PlayerState.Jumping:
                    ApplyGroundAndAirMovement();
                    break;
                case PlayerState.Sliding:
                    ApplySlideMovement();
                    break;
                case PlayerState.WallRunning:
                    ApplyWallRunMovement();
                    break;
            }
        }

        private void ChangeState(PlayerState newState)
        {
            if (CurrentState == newState) return;

            OnStateExit(CurrentState);
            PlayerState oldState = CurrentState;
            CurrentState = newState;
            OnStateEnter(newState, oldState);
        }

        private void OnStateEnter(PlayerState state, PlayerState oldState)
        {
            switch (state)
            {
                case PlayerState.Dashing:
                    StartCoroutine(DashRoutine());
                    break;
                case PlayerState.Sliding:
                    InitiateSlide();
                    break;
                case PlayerState.WallRunning:
                    InitiateWallRun();
                    break;
                case PlayerState.Jumping:
                    if (oldState == PlayerState.WallRunning) break;
                    canDoubleJump = isGrounded;
                    break;
            }
        }

        private void OnStateExit(PlayerState state)
        {
            switch (state)
            {
                case PlayerState.Sliding:
                    playerCollider.height = originalColliderHeight;
                    playerCollider.center = originalColliderCenter;
                    break;
                case PlayerState.WallRunning:
                    rb.useGravity = true;
                    break;
            }
        }

        private void ApplyGroundAndAirMovement()
        {
            rb.useGravity = true;
            rb.linearDamping = isGrounded ? groundLinearDamping : 0;

            bool isRunning = Input.GetKey(keybindings.runKey);
            float currentSpeed = isRunning ? runSpeed : walkSpeed;
            float speed = isGrounded ? currentSpeed : currentSpeed * airMultiplier;

            if (moveDirection.sqrMagnitude > 0.1f)
                rb.AddForce(moveDirection * speed * 10f, ForceMode.Force);

            Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (flatVel.magnitude > currentSpeed && isGrounded)
            {
                Vector3 limitedVel = flatVel.normalized * currentSpeed;
                rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
            }
        }

        private void HandleJumpInput()
        {
            if (CurrentState == PlayerState.WallRunning)
            {
                WallJump();
                return;
            }

            if (isGrounded || canDoubleJump)
            {
                Jump();
            }
        }

        private void Jump()
        {
            canDoubleJump = isGrounded ? true : !canDoubleJump;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
            GainKineticEnergy(kineticEnergySettings.jumpGain);
            ChangeState(PlayerState.Jumping);
        }

        private void WallJump()
        {
            Vector3 wallNormal = isWallLeft ? rightWallHit.normal : leftWallHit.normal;
            Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);
            if (Vector3.Dot(orientation.forward, wallForward) < 0)
                wallForward = -wallForward;

            // Tính toán và bảo toàn quán tính
            float currentForwardMomentum = Vector3.Dot(rb.linearVelocity, wallForward);
            float preservedMomentum = currentForwardMomentum * wallJumpMomentumPreservation;

            // Reset vận tốc và áp dụng lực mới
            rb.linearVelocity = Vector3.zero;
            Vector3 forceToApply = (transform.up * wallJumpUpForce) + (wallNormal * wallJumpSideForce) + (wallForward * preservedMomentum);
            rb.AddForce(forceToApply, ForceMode.VelocityChange); // VelocityChange cho sự kiểm soát tuyệt đối

            ChangeState(PlayerState.Jumping);
        }

        private IEnumerator DashRoutine()
        {
            canDash = false;
            dashCooldownTimer = dashCooldown;
            GainKineticEnergy(kineticEnergySettings.dashGain);
            ActivateFlowState(SkillMoveType.Dash);

            Vector3 dashDirection = moveDirection.sqrMagnitude > 0.1f ? moveDirection : orientation.forward;

            rb.useGravity = false;
            rb.linearVelocity = dashDirection * dashSpeed;

            yield return new WaitForSeconds(dashDuration);

            rb.useGravity = true;
            if (CurrentState == PlayerState.Dashing) // Đảm bảo chỉ đổi state nếu vẫn đang dash
                ChangeState(isGrounded ? PlayerState.Walking : PlayerState.Jumping);
        }

        private void InitiateSlide()
        {
            playerCollider.height = slideColliderHeight;
            playerCollider.center = new Vector3(0, slideColliderHeight / 2, 0);
            rb.AddForce(orientation.forward * slideStartBoost, ForceMode.Impulse);
            ActivateFlowState(SkillMoveType.Slide);
        }

        private void ApplySlideMovement()
        {
            rb.AddForce(GetSlopeMoveDirection() * slopeSlideMultiplier, ForceMode.Force);
            rb.linearVelocity *= slideFriction;
            rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxSlideSpeed);
        }

        private void StopSlide()
        {
            ChangeState(PlayerState.Walking);
        }

        private Vector3 GetSlopeMoveDirection()
        {
            return Vector3.ProjectOnPlane(Vector3.down, groundHit.normal).normalized;
        }

        private bool CanWallRun()
        {
            return (isWallLeft || isWallRight) && !isGrounded && moveInput.y > 0 && CurrentState != PlayerState.WallRunning;
        }

        private void InitiateWallRun()
        {
            rb.useGravity = false;
            wallRunTimer = maxWallRunTime;
            canDoubleJump = true;
            ActivateFlowState(SkillMoveType.WallRun);

            // Chuẩn hóa tốc độ khi bắt đầu Wall Run
            Vector3 wallNormal = isWallRight ? rightWallHit.normal : leftWallHit.normal;
            Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);
            if (Vector3.Dot(orientation.forward, wallForward) < 0)
                wallForward = -wallForward;

            rb.linearVelocity = wallForward * wallRunSpeed;
        }

        private void ApplyWallRunMovement()
        {
            WallNormal = isWallRight ? rightWallHit.normal : leftWallHit.normal;
            Vector3 wallForward = Vector3.Cross(WallNormal, transform.up);
            if (Vector3.Dot(orientation.forward, wallForward) < 0)
                wallForward = -wallForward;

            // Logic duy trì tốc độ thay vì AddForce vô hạn
            Vector3 targetVelocity = wallForward * wallRunSpeed;
            Vector3 velocityChange = (targetVelocity - rb.linearVelocity);
            rb.AddForce(velocityChange, ForceMode.VelocityChange);

            rb.AddForce(-WallNormal * wallStickForce, ForceMode.Force);
            rb.AddForce(Vector3.down * wallRunGravity, ForceMode.Force);
        }

        private void UpdateCameraController()
        {
            float tilt = 0f;
            if (IsWallRunning)
            {
                float tiltDirection = Vector3.Dot(orientation.right, WallNormal) > 0 ? -1f : 1f;
                tilt = wallRunCameraTilt * tiltDirection;
            }
            cameraController.UpdateWallRunTilt(tilt);
        }

        // --- Các hàm Kinetic Energy, Timers, ... giữ nguyên ---
        #region Unchanged Utility Methods
        private void HandleKineticEnergy()
        {
            if (isGrounded && CurrentState != PlayerState.Sliding)
            {
                if (moveInput.magnitude < 0.1f)
                {
                    CurrentKineticEnergy -= kineticEnergySettings.energyDecayRate * Time.deltaTime;
                    if (CurrentState != PlayerState.Idle) ChangeState(PlayerState.Idle);
                }
                else
                {
                    bool isRunning = Input.GetKey(keybindings.runKey);
                    float gainRate = isRunning ? kineticEnergySettings.runGain : kineticEnergySettings.walkGain;
                    GainKineticEnergy(gainRate * Time.deltaTime);
                    ChangeState(isRunning ? PlayerState.Running : PlayerState.Walking);
                }
            }
            CurrentKineticEnergy = Mathf.Clamp(CurrentKineticEnergy, 0, MaxKineticEnergy);
        }

        private void GainKineticEnergy(float amount)
        {
            float finalAmount = IsFlowStateActive ? amount * flowStateSettings.energyGainMultiplier : amount;
            CurrentKineticEnergy += finalAmount;
        }

        private void ActivateFlowState(SkillMoveType currentMove)
        {
            if (currentMove != SkillMoveType.None && currentMove != lastSkillMove)
                flowStateTimer = flowStateSettings.duration;
            lastSkillMove = currentMove;
        }

        private void TryActivateKineticPulse()
        {
            if (CurrentKineticEnergy >= MaxKineticEnergy)
            {
                Debug.Log("KINETIC PULSE ACTIVATED!");
                CurrentKineticEnergy = 0;
            }
        }

        private void HandleTimers()
        {
            if (dashCooldownTimer > 0)
            {
                dashCooldownTimer -= Time.deltaTime;
                if (dashCooldownTimer <= 0) canDash = true;
            }

            if (flowStateTimer > 0)
                flowStateTimer -= Time.deltaTime;
            else
                lastSkillMove = SkillMoveType.None;

            if (wallRunTimer > 0 && CurrentState == PlayerState.WallRunning)
            {
                wallRunTimer -= Time.deltaTime;
                if (wallRunTimer <= 0) ChangeState(PlayerState.Jumping);
            }
        }
        #endregion
    }
}