using UnityEngine;

namespace Orion
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(InputHandler))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Core References")]
        public InputHandler Input;
        public Transform PlayerCameraTransform;
        public PlayerAnimationController AnimationController { get; private set; }
        public Rigidbody Rigidbody { get; private set; }
        public CapsuleCollider CapsuleCollider { get; private set; }

        [Header("Movement Stats")]
        [SerializeField] private float _walkSpeed = 5.0f;
        [SerializeField] private float _runSpeed = 8.0f;
        [SerializeField] private float _slideSpeedMultiplier = 1.5f;
        [SerializeField] private float _movementAcceleration = 50.0f;
        [SerializeField] private float _maxSlopeAngle = 45.0f;

        [Header("Dash Stats")]
        [SerializeField] private float _dashForce = 25.0f;
        [SerializeField] private float _dashDuration = 0.2f;

        [Header("Jump Stats")]
        [SerializeField] private float _jumpForce = 10.0f;
        [SerializeField] private float _coyoteTime = 0.1f;
        [SerializeField] private float _jumpBufferTime = 0.1f;

        [Header("Airborne Stats")]
        [SerializeField] private float _airControlFactor = 0.5f;
        [SerializeField] private float _gravityMultiplier = 2.5f;

        [Header("Wall Run Stats")]
        [SerializeField] private float _wallRunSpeed = 7.0f;
        [SerializeField] private float _wallRunGravityMultiplier = 0.5f;
        [SerializeField] private float _maxWallRunTime = 2.0f;
        [SerializeField] private Vector3 _wallJumpForce = new Vector3(5f, 8f, 0f);
        [SerializeField] private LayerMask _wallRunLayer;

        [Header("Ledge Detection")]
        [SerializeField] private Vector3 _ledgeDetectOffset = new Vector3(0, 1.8f, 0.6f);
        [SerializeField] private float _ledgeDetectRadius = 0.2f;
        [SerializeField] private LayerMask _ledgeLayer;

        public StateMachine MovementStateMachine { get; private set; }
        public PlayerGroundedState GroundedState { get; private set; }
        public PlayerJumpState JumpState { get; private set; }
        public PlayerFallState FallState { get; private set; }
        public PlayerWallRunState WallRunState { get; private set; }
        public PlayerLedgeClimbState LedgeClimbState { get; private set; }
        public PlayerDashState DashState { get; private set; }

        public Vector3 CurrentVelocity { get; private set; }
        public float CoyoteTimeCounter { get; set; }
        public float JumpBufferCounter { get; set; }

        private void Awake()
        {
            Input = GetComponent<InputHandler>();
            Rigidbody = GetComponent<Rigidbody>();
            CapsuleCollider = GetComponent<CapsuleCollider>();
            AnimationController = GetComponentInChildren<PlayerAnimationController>();

            MovementStateMachine = new StateMachine();

            GroundedState = new PlayerGroundedState(this, MovementStateMachine);
            JumpState = new PlayerJumpState(this, MovementStateMachine);
            FallState = new PlayerFallState(this, MovementStateMachine);
            WallRunState = new PlayerWallRunState(this, MovementStateMachine);
            LedgeClimbState = new PlayerLedgeClimbState(this, MovementStateMachine);
            DashState = new PlayerDashState(this, MovementStateMachine);
        }

        private void Start()
        {
            AnimationController.Initialize(this);
            MovementStateMachine.Initialize(GroundedState);
        }

        private void Update()
        {
            UpdateTimers();
            MovementStateMachine.CurrentState.LogicUpdate();
            CurrentVelocity = Rigidbody.linearVelocity;
        }

        private void FixedUpdate()
        {
            MovementStateMachine.CurrentState.PhysicsUpdate();
        }

        private void UpdateTimers()
        {
            CoyoteTimeCounter -= Time.deltaTime;
            JumpBufferCounter -= Time.deltaTime;
        }

        public void SetVelocity(Vector3 newVelocity)
        {
            Rigidbody.linearVelocity = newVelocity;
            CurrentVelocity = newVelocity;
        }

        public void ApplyAirResistance(float resistance)
        {
            if (Mathf.Abs(CurrentVelocity.x) > 0.01f || Mathf.Abs(CurrentVelocity.z) > 0.01f)
            {
                var horizontalVelocity = new Vector3(CurrentVelocity.x, 0, CurrentVelocity.z);
                Rigidbody.AddForce(-horizontalVelocity * resistance, ForceMode.Acceleration);
            }
        }

        public bool IsGrounded()
        {
            return Physics.Raycast(transform.position, Vector3.down, CapsuleCollider.height * 0.5f + 0.1f);
        }

        public bool IsOnSteepSlope(out Vector3 slopeNormal)
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, CapsuleCollider.height * 0.5f + 0.2f))
            {
                slopeNormal = hit.normal;
                float slopeAngle = Vector3.Angle(Vector3.up, hit.normal);
                return slopeAngle > _maxSlopeAngle;
            }
            slopeNormal = Vector3.up;
            return false;
        }

        public float GetWalkSpeed() => _walkSpeed;
        public float GetRunSpeed() => _runSpeed;
        public float GetSlideSpeedMultiplier() => _slideSpeedMultiplier;
        public float GetMovementAcceleration() => _movementAcceleration;
        public float GetDashForce() => _dashForce;
        public float GetDashDuration() => _dashDuration;
        public float GetJumpForce() => _jumpForce;
        public float GetCoyoteTime() => _coyoteTime;
        public float GetJumpBufferTime() => _jumpBufferTime;
        public float GetAirControlFactor() => _airControlFactor;
        public float GetGravityMultiplier() => _gravityMultiplier;
        public float GetWallRunSpeed() => _wallRunSpeed;
        public float GetWallRunGravityMultiplier() => _wallRunGravityMultiplier;
        public float GetMaxWallRunTime() => _maxWallRunTime;
        public Vector3 GetWallJumpForce() => _wallJumpForce;
        public LayerMask GetWallRunLayer() => _wallRunLayer;
        public Vector3 GetLedgeDetectOffset() => _ledgeDetectOffset;
        public float GetLedgeDetectRadius() => _ledgeDetectRadius;
        public LayerMask GetLedgeLayer() => _ledgeLayer;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Vector3 worldLedgeDetectPoint = transform.TransformPoint(_ledgeDetectOffset);
            Gizmos.DrawWireSphere(worldLedgeDetectPoint, _ledgeDetectRadius);
        }
    }
}