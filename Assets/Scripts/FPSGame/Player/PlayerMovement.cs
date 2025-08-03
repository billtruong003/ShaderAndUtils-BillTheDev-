using UnityEngine;
using Sirenix.OdinInspector;

namespace FPS
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerMovement : MonoBehaviour
    {
        [Title("Dependencies")]
        [SerializeField, Required] private Rigidbody rb;
        [SerializeField, Required] private Transform orientation;
        [SerializeField, Required] private Transform playerModel; // Transform của model player để xoay
        [SerializeField, Required] private PlayerStateMachine stateMachine;
        [SerializeField, Required] private GroundChecker groundChecker;
        [SerializeField, Required] private WallRunChecker wallRunChecker;

        [Title("Movement Parameters")]
        [SerializeField] private float walkSpeed = 7f;
        [SerializeField] private float runSpeed = 10f;
        [SerializeField] private float airMultiplier = 0.4f;
        [SerializeField, Range(0, 1)] private float movementSmoothing = 0.1f;

        [Title("Jumping")]
        [SerializeField] private float jumpForce = 12f;

        [Title("Sliding")]
        [SerializeField] private float slideSpeed = 15f;
        [SerializeField] private float slideCounterMovement = 0.2f;

        [Title("Wall Running")]
        [SerializeField] private float wallRunForce = 150f;
        [SerializeField] private float wallJumpUpForce = 8f;
        [SerializeField] private float wallJumpSideForce = 12f;

        [Title("Drag")]
        [SerializeField] private float groundDrag = 6f;
        [SerializeField] private float airDrag = 2f;

        private Vector3 moveDirection;
        private Vector3 targetVelocity;

        private void OnValidate()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            rb.freezeRotation = true;
        }

        public void HandleMovement(Vector2 input, bool isSprinting, bool isJumping, bool isCrouching)
        {
            HandleDrag();
            moveDirection = orientation.forward * input.y + orientation.right * input.x;

            switch (stateMachine.CurrentState)
            {
                case PlayerState.Walking:
                    MoveOnGround(walkSpeed);
                    break;
                case PlayerState.Running:
                    MoveOnGround(runSpeed);
                    break;
                case PlayerState.Sliding:
                    Slide();
                    break;
                case PlayerState.WallRunning:
                    WallRun();
                    break;
                case PlayerState.Jumping:
                case PlayerState.Falling:
                    MoveInAir();
                    break;
            }

            if (isJumping) RequestJump();
        }

        private void MoveOnGround(float targetSpeed)
        {
            rb.AddForce(moveDirection.normalized * targetSpeed * 10f, ForceMode.Force);
            LimitSpeed(targetSpeed);
        }

        private void MoveInAir()
        {
            rb.AddForce(moveDirection.normalized * walkSpeed * 10f * airMultiplier, ForceMode.Force);
        }

        private void Slide()
        {
            rb.AddForce(moveDirection.normalized * slideSpeed, ForceMode.Force);
            // Thêm lực cản để slide không kéo dài mãi mãi
            rb.AddForce(-rb.linearVelocity.normalized * slideCounterMovement, ForceMode.VelocityChange);
            LimitSpeed(slideSpeed);
        }

        private void WallRun()
        {
            rb.useGravity = false;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            Vector3 wallNormal = wallRunChecker.IsOnLeftWall ? wallRunChecker.LeftWallHit.normal : wallRunChecker.RightWallHit.normal;
            Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

            // Đảo hướng nếu cần để luôn chạy về phía trước
            if ((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude)
            {
                wallForward = -wallForward;
            }

            // Lực đẩy về phía trước và lực ép vào tường
            rb.AddForce(wallForward * wallRunForce, ForceMode.Force);
        }

        private void RequestJump()
        {
            if (groundChecker.IsGrounded)
            {
                Jump(transform.up, jumpForce);
            }
            else if (stateMachine.CurrentState == PlayerState.WallRunning)
            {
                Vector3 wallNormal = wallRunChecker.IsOnLeftWall ? wallRunChecker.LeftWallHit.normal : wallRunChecker.RightWallHit.normal;
                Vector3 jumpDirection = transform.up * wallJumpUpForce + wallNormal * wallJumpSideForce;
                Jump(jumpDirection, 1f, ForceMode.VelocityChange); // Dùng velocity change để có cú bật tức thì
                stateMachine.ExitWallRun();
            }
        }

        private void Jump(Vector3 direction, float force, ForceMode mode = ForceMode.Impulse)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z); // Reset y velocity
            rb.AddForce(direction * force, mode);
        }

        private void HandleDrag()
        {
            if (stateMachine.CurrentState == PlayerState.WallRunning)
            {
                rb.useGravity = false;
                rb.linearDamping = 0;
            }
            else if (groundChecker.IsGrounded)
            {
                rb.useGravity = true;
                rb.linearDamping = groundDrag;
            }
            else
            {
                rb.useGravity = true;
                rb.linearDamping = airDrag;
            }
        }

        private void LimitSpeed(float maxSpeed)
        {
            Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (flatVel.magnitude > maxSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * maxSpeed;
                rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
            }
        }
    }
}