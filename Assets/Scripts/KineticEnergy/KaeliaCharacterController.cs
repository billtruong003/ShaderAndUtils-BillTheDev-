using UnityEngine;
using Kaelia.Data;
using Kaelia.Player.States;
using StateSystem;
using Sirenix.OdinInspector;

namespace Kaelia.Player
{
    [SelectionBase]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(InputHandler))]
    public class PlayerController : MonoBehaviour
    {
        #region Core Dependencies
        [TitleGroup("Core Dependencies")]
        [Required]
        [InlineEditor(InlineEditorModes.GUIAndHeader, Expanded = true)]
        [SerializeField] private PlayerDataSO data;

        [TitleGroup("Core Dependencies")]
        [Required]
        [InlineEditor(InlineEditorModes.GUIOnly)]
        [SerializeField] private KeybindingSO keybindings;

        [TitleGroup("Core Dependencies")]
        [Required, SceneObjectsOnly]
        [SerializeField] private Transform cameraTransform;

        [TitleGroup("Core Dependencies")]
        [Required, SceneObjectsOnly]
        [SerializeField] private KaeliaCameraController cameraController;
        #endregion

        #region Runtime State & Debugging
        [TitleGroup("Runtime State", "Read-only values for debugging.")]
        [ProgressBar(0, 0.2f, r: 1, g: 0.6f, b: 0), ShowInInspector, ReadOnly]
        public float CoyoteTimeCounter { get; set; }

        [ProgressBar(0, 0.2f, r: 0, g: 0.7f, b: 1), ShowInInspector, ReadOnly]
        public float JumpBufferCounter { get; set; }

        [ShowInInspector, ReadOnly] public string CurrentStateName => MovementStateMachine?.CurrentState?.GetType().Name;
        [ShowInInspector, ReadOnly] public bool IsGrounded { get; private set; }
        [ShowInInspector, ReadOnly] public bool IsWallLeft { get; private set; }
        [ShowInInspector, ReadOnly] public bool IsWallRight { get; private set; }
        [ShowInInspector, ReadOnly] public bool IsWeaponDrawn { get; set; }
        [ShowInInspector, ReadOnly] public Vector3 CurrentVelocity => Application.isPlaying ? Rb.linearVelocity : Vector3.zero;
        #endregion

        #region Public Properties
        public PlayerDataSO Data => data;
        public Transform CameraTransform => cameraTransform;
        public KaeliaCameraController CameraController => cameraController;
        public StateMachine MovementStateMachine { get; private set; }
        public Vector3 MoveDirection { get; private set; }
        public float TurnSmoothVelocity { get; set; }
        public float OriginalColliderHeight { get; private set; }
        public Vector3 OriginalColliderCenter { get; private set; }
        public RaycastHit GroundHit { get; private set; }
        public Vector3 WallNormal => IsWallRight ? RightWallHit.normal : (IsWallLeft ? LeftWallHit.normal : Vector3.zero);
        public bool CanDash { get; set; } = true;
        public bool CanDoubleJump { get; set; }
        #endregion

        #region Cached Components
        [field: FoldoutGroup("Cached Components"), SerializeField, ReadOnly]
        public Rigidbody Rb { get; private set; }
        [field: FoldoutGroup("Cached Components"), SerializeField, ReadOnly]
        public CapsuleCollider PlayerCollider { get; private set; }
        [field: FoldoutGroup("Cached Components"), SerializeField, ReadOnly]
        public InputHandler Input { get; private set; }
        [field: FoldoutGroup("Cached Components"), SerializeField, ReadOnly]
        public Animator Animator { get; private set; }
        #endregion

        private RaycastHit LeftWallHit { get; set; }
        private RaycastHit RightWallHit { get; set; }

        private void Awake()
        {
            Rb = GetComponent<Rigidbody>();
            PlayerCollider = GetComponent<CapsuleCollider>();
            Input = GetComponent<InputHandler>();
            Animator = GetComponentInChildren<Animator>(true);

            Input.Initialize(keybindings);
            MovementStateMachine = new StateMachine();
        }

        private void Start()
        {
            Rb.freezeRotation = true;
            OriginalColliderHeight = PlayerCollider.height;
            OriginalColliderCenter = PlayerCollider.center;

            MovementStateMachine.Initialize(new Player.States.PlayerGroundedState(this, MovementStateMachine));
        }

        private void Update()
        {
            HandleTimers();
            UpdateAnimatorParameters();
            MovementStateMachine.CurrentState.LogicUpdate();
        }

        private void FixedUpdate()
        {
            HandleChecks();
            CalculateMoveDirection();
            MovementStateMachine.CurrentState.PhysicsUpdate();
        }

        private void HandleTimers()
        {
            if (CoyoteTimeCounter > 0) CoyoteTimeCounter -= Time.deltaTime;
            if (JumpBufferCounter > 0) JumpBufferCounter -= Time.deltaTime;
        }

        private void HandleChecks()
        {
            bool wasGrounded = IsGrounded;
            float sphereCastRadius = PlayerCollider.radius * 0.9f;
            float checkDistance = PlayerCollider.height / 2 - PlayerCollider.radius + data.GroundCheckDistance;
            IsGrounded = Physics.SphereCast(PlayerCollider.bounds.center, sphereCastRadius, Vector3.down, out RaycastHit hitInfo, checkDistance, data.GroundLayer);
            GroundHit = hitInfo;

            if (!wasGrounded && IsGrounded) CanDoubleJump = true;
            if (wasGrounded && !IsGrounded) CoyoteTimeCounter = data.CoyoteTime;

            IsWallRight = Physics.Raycast(transform.position, transform.right, out RaycastHit rightHit, data.WallCheckDistance, data.WallLayer);
            RightWallHit = rightHit;
            IsWallLeft = Physics.Raycast(transform.position, -transform.right, out RaycastHit leftHit, data.WallCheckDistance, data.WallLayer);
            LeftWallHit = leftHit;
        }

        private void CalculateMoveDirection()
        {
            Vector3 camFwd = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            MoveDirection = (camFwd * Input.Vertical + camRight * Input.Horizontal).normalized;
        }

        private void UpdateAnimatorParameters()
        {
            if (Animator == null) return;

            Animator.SetBool("IsGrounded", IsGrounded);
            Animator.SetBool("IsWeaponDrawn", IsWeaponDrawn);
            Vector3 flatVelocity = new Vector3(Rb.linearVelocity.x, 0, Rb.linearVelocity.z);
            Animator.SetFloat("MoveSpeed", flatVelocity.magnitude);
            Animator.SetFloat("VerticalVelocity", Rb.linearVelocity.y);
        }

        public void SetColliderHeight(float height, Vector3 center)
        {
            PlayerCollider.height = height;
            PlayerCollider.center = center;
        }

        public void ResetCollider()
        {
            PlayerCollider.height = OriginalColliderHeight;
            PlayerCollider.center = OriginalColliderCenter;
        }

        public void ChangeLayer(int newLayer) => gameObject.layer = newLayer;
    }
}