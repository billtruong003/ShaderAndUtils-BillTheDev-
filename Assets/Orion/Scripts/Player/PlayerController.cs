using UnityEngine;

namespace Orion
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(InputHandler))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Core References")]
        public InputHandler Input;
        public Transform PlayerCameraTransform;
        public ThirdPersonCameraController CameraController;
        public PlayerAnimationController AnimationController { get; private set; }
        public Rigidbody Rigidbody { get; private set; }
        public CapsuleCollider CapsuleCollider { get; private set; }

        [Header("Effects")]
        [SerializeField] private ParticleSystem _landingParticles;

        [Header("Movement Stats")]
        [field: SerializeField] public float WalkSpeed { get; private set; } = 5.0f;
        [field: SerializeField] public float RunSpeed { get; private set; } = 8.0f;
        [field: SerializeField] public float MovementAcceleration { get; private set; } = 50.0f;
        [field: SerializeField] public float MaxSlopeAngle { get; private set; } = 45.0f;

        [Header("Rotation Stats")]
        [field: SerializeField] public float GroundedRotationSpeed { get; private set; } = 15f;
        [field: SerializeField] public float AirborneRotationSpeed { get; private set; } = 5f;

        [Header("Ground Detection")]
        [field: SerializeField] public LayerMask GroundLayer { get; private set; }
        [field: SerializeField] public float GroundCheckDistance { get; private set; } = 0.2f;
        [Tooltip("The amount of time the character remains in the 'grounded' state after losing contact with the ground. Prevents animation jitter on uneven surfaces.")]
        [field: SerializeField] public float GroundedLingerTime { get; private set; } = 0.1f;

        [Header("Crouch & Slide Stats")]
        [field: SerializeField] public float CrouchSpeed { get; private set; } = 3.0f;
        [field: SerializeField] public float CrouchColliderHeight { get; private set; } = 1.0f;
        [field: SerializeField] public float SlideForce { get; private set; } = 15.0f;
        [field: SerializeField] public float SlideDuration { get; private set; } = 0.7f;
        [field: SerializeField] public float SlideFriction { get; private set; } = 15f;
        [field: SerializeField] public float SlopeSlideSpeedMultiplier { get; private set; } = 1.5f;

        [Header("Dash Stats")]
        [field: SerializeField] public float DashForce { get; private set; } = 25.0f;
        [field: SerializeField] public float DashDuration { get; private set; } = 0.2f;
        [field: SerializeField, Range(0f, 1f)] public float DashEndMomentumDampening { get; private set; } = 0.5f;
        [field: SerializeField] public float DashFOV { get; private set; } = 80f;

        [Header("Jump Stats")]
        [field: SerializeField] public float JumpForce { get; private set; } = 10.0f;
        [field: SerializeField] public float CoyoteTime { get; private set; } = 0.1f;
        [field: SerializeField] public float JumpBufferTime { get; private set; } = 0.1f;

        [Header("Airborne Stats")]
        [field: SerializeField] public float AirAcceleration { get; private set; } = 25.0f;
        [field: SerializeField] public float GravityMultiplier { get; private set; } = 2.5f;
        [field: SerializeField, Range(0f, 1f)] public float LandingHorizontalDampening { get; private set; } = 0.5f;
        [field: SerializeField] public float IdleFriction { get; private set; } = 5f;

        [Header("Wall Run Stats")]
        [field: SerializeField] public float WallRunSpeed { get; private set; } = 7.0f;
        [field: SerializeField] public float WallRunUpwardForce { get; private set; } = 20.0f;
        [field: SerializeField] public float MaxWallRunTime { get; private set; } = 2.0f;
        [field: SerializeField] public Vector3 WallJumpForce { get; private set; } = new Vector3(8f, 10f, 3f);
        [field: SerializeField] public float WallExitForwardMomentum { get; private set; } = 5.0f;
        [field: SerializeField] public float WallExitPushForce { get; private set; } = 3.0f;
        [field: SerializeField] public LayerMask WallRunLayer { get; private set; }

        [Header("Ledge Climb Stats")]
        [field: SerializeField] public Vector3 LedgeDetectForwardOffset { get; private set; } = new Vector3(0, 1.2f, 0);
        [field: SerializeField] public float LedgeDetectForwardDistance { get; private set; } = 0.8f;
        [field: SerializeField] public float LedgeDetectDownDistance { get; private set; } = 1.5f;
        [field: SerializeField] public float LedgeClimbDuration { get; private set; } = 0.6f;
        [field: SerializeField] public Vector3 LedgeClimbStandPositionOffset { get; private set; } = new Vector3(0, 0, 0.3f);
        [field: SerializeField] public LayerMask LedgeLayer { get; private set; }

        public StateMachine MovementStateMachine { get; private set; }
        public PlayerGroundedState GroundedState { get; private set; }
        public PlayerJumpState JumpState { get; private set; }
        public PlayerFallState FallState { get; private set; }
        public PlayerWallRunState WallRunState { get; private set; }
        public PlayerLedgeClimbState LedgeClimbState { get; private set; }
        public PlayerDashState DashState { get; private set; }
        public PlayerActiveSlideState ActiveSlideState { get; private set; }

        public bool LockOrientation { get; set; }
        public Vector3 CurrentVelocity => Rigidbody.linearVelocity;
        public float CoyoteTimeCounter { get; set; }
        public float JumpBufferCounter { get; set; }
        public bool IsGrounded { get; private set; }
        public float DefaultColliderHeight { get; private set; }

        private Vector3 _groundHitNormal;
        private float _groundedLingerTimer;

        private void Awake()
        {
            InitializeComponents();
            InitializeStateMachine();
        }

        private void Start()
        {
            MovementStateMachine.Initialize(GroundedState);
        }

        private void Update()
        {
            UpdateTimers();
            MovementStateMachine.CurrentState.LogicUpdate();
        }

        private void FixedUpdate()
        {
            CheckGroundedStatus();
            MovementStateMachine.CurrentState.PhysicsUpdate();
        }

        private void InitializeComponents()
        {
            Input = GetComponent<InputHandler>();
            Rigidbody = GetComponent<Rigidbody>();
            CapsuleCollider = GetComponent<CapsuleCollider>();
            AnimationController = GetComponentInChildren<PlayerAnimationController>();
            DefaultColliderHeight = CapsuleCollider.height;
        }

        private void InitializeStateMachine()
        {
            MovementStateMachine = new StateMachine();
            GroundedState = new PlayerGroundedState(this, MovementStateMachine);
            JumpState = new PlayerJumpState(this, MovementStateMachine);
            FallState = new PlayerFallState(this, MovementStateMachine);
            WallRunState = new PlayerWallRunState(this, MovementStateMachine);
            LedgeClimbState = new PlayerLedgeClimbState(this, MovementStateMachine);
            DashState = new PlayerDashState(this, MovementStateMachine);
            ActiveSlideState = new PlayerActiveSlideState(this, MovementStateMachine);
        }

        // >>> THAY ĐỔI LOGIC <<<
        // Logic quản lý timer đã được làm lại cho chính xác.
        private void UpdateTimers()
        {
            // Jump Buffer luôn được đếm ngược.
            JumpBufferCounter -= Time.deltaTime;

            // Coyote Time chỉ đếm ngược khi người chơi ở trên không.
            // Khi ở dưới đất, nó luôn được nạp đầy.
            if (IsGrounded)
            {
                CoyoteTimeCounter = CoyoteTime;
            }
            else
            {
                CoyoteTimeCounter -= Time.deltaTime;
            }
        }

        private void HandleLanding(float previousYVelocity)
        {
            if (_landingParticles && previousYVelocity < -2f)
            {
                _landingParticles.Play();
            }

            Vector3 horizontalVelocity = new Vector3(CurrentVelocity.x, 0, CurrentVelocity.z);
            Rigidbody.AddForce(-horizontalVelocity * LandingHorizontalDampening, ForceMode.Impulse);
        }

        public void SetVelocity(Vector3 newVelocity)
        {
            Rigidbody.linearVelocity = newVelocity;
        }

        public void SetVelocityY(float yVelocity)
        {
            Rigidbody.linearVelocity = new Vector3(CurrentVelocity.x, yVelocity, CurrentVelocity.z);
        }

        public void AddForce(Vector3 force, ForceMode mode)
        {
            Rigidbody.AddForce(force, mode);
        }

        public void ApplyAirResistance(float resistance)
        {
            if (CurrentVelocity.sqrMagnitude < 0.01f) return;

            var horizontalVelocity = new Vector3(CurrentVelocity.x, 0, CurrentVelocity.z);
            Rigidbody.AddForce(-horizontalVelocity * resistance, ForceMode.Acceleration);
        }

        public void SetColliderHeight(float newHeight)
        {
            Vector3 center = CapsuleCollider.center;
            center.y = newHeight / 2f;
            CapsuleCollider.height = newHeight;
            CapsuleCollider.center = center;
        }

        public bool CanStandUp()
        {
            float castRadius = CapsuleCollider.radius;
            float castDistance = DefaultColliderHeight - CrouchColliderHeight;
            Vector3 castOrigin = transform.position + new Vector3(0, castRadius, 0);
            return !Physics.SphereCast(castOrigin, castRadius, Vector3.up, out _, castDistance);
        }

        private void CheckGroundedStatus()
        {
            bool previouslyGrounded = IsGrounded;
            float previousYVelocity = CurrentVelocity.y;

            Vector3 castCenter = CapsuleCollider.bounds.center;
            float castRadius = CapsuleCollider.radius * 0.9f;
            float castDistance = (CapsuleCollider.height / 2f) - castRadius + GroundCheckDistance;

            bool isHittingGround = Physics.SphereCast(castCenter, castRadius, Vector3.down, out RaycastHit hitInfo, castDistance, GroundLayer);

            if (isHittingGround)
            {
                _groundedLingerTimer = GroundedLingerTime;
                IsGrounded = true;
                _groundHitNormal = hitInfo.normal;
            }
            else
            {
                _groundedLingerTimer -= Time.fixedDeltaTime;
                if (_groundedLingerTimer <= 0f)
                {
                    IsGrounded = false;
                    _groundHitNormal = Vector3.up;
                }
            }

            if (!previouslyGrounded && IsGrounded)
            {
                HandleLanding(previousYVelocity);
            }
        }

        public bool IsOnSteepSlope()
        {
            if (!IsGrounded) return false;
            float slopeAngle = Vector3.Angle(Vector3.up, _groundHitNormal);
            return slopeAngle > MaxSlopeAngle;
        }

        public Vector3 GetGroundNormal() => _groundHitNormal;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 castCenter = (Application.isPlaying) ? CapsuleCollider.bounds.center : transform.position + CapsuleCollider.center;
            float castRadius = (Application.isPlaying) ? CapsuleCollider.radius * 0.9f : GetComponent<CapsuleCollider>().radius * 0.9f;
            float castDistance = (Application.isPlaying) ? (CapsuleCollider.height / 2f) - castRadius + GroundCheckDistance : (GetComponent<CapsuleCollider>().height / 2f) - castRadius + GroundCheckDistance;

            Gizmos.DrawWireSphere(castCenter, castRadius);
            Gizmos.DrawWireSphere(castCenter + Vector3.down * castDistance, castRadius);

            Gizmos.color = Color.cyan;
            Vector3 forwardRayOrigin = transform.position + LedgeDetectForwardOffset;
            Vector3 forwardRayEnd = forwardRayOrigin + transform.forward * LedgeDetectForwardDistance;
            Gizmos.DrawLine(forwardRayOrigin, forwardRayEnd);
        }
    }
}