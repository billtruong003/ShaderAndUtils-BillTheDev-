using UnityEngine;
using Sirenix.OdinInspector;

namespace FPS
{
    public class PlayerStateMachine : MonoBehaviour
    {
        [Title("Dependencies")]
        [SerializeField, Required] private PlayerAnimationController animationController;
        [SerializeField, Required] private GroundChecker groundChecker;
        [SerializeField, Required] private WallRunChecker wallRunChecker;
        [SerializeField, Required] private Rigidbody rb;

        [field: ShowInInspector, ReadOnly]
        public PlayerState CurrentState { get; private set; }

        private PlayerState previousState;

        public void DetermineCurrentState(Vector2 moveInput, bool isSprinting, bool isCrouching)
        {
            PlayerState newState;
            bool isMoving = moveInput.magnitude > 0.1f;

            // Ưu tiên cao nhất: Wall Running
            if (wallRunChecker.CanWallRun && !groundChecker.IsGrounded)
            {
                newState = PlayerState.WallRunning;
            }
            // Ưu tiên 2: Trượt (khi đang chạy và nhấn crouch)
            else if (isCrouching && isSprinting && isMoving && groundChecker.IsGrounded)
            {
                newState = PlayerState.Sliding;
            }
            // Ưu tiên 3: Trên không
            else if (!groundChecker.IsGrounded)
            {
                newState = rb.linearVelocity.y > 0.1f ? PlayerState.Jumping : PlayerState.Falling;
            }
            // Ưu tiên 4: Cúi
            else if (isCrouching)
            {
                newState = PlayerState.Crouching;
            }
            // Ưu tiên 5: Chạy
            else if (isMoving && isSprinting)
            {
                newState = PlayerState.Running;
            }
            // Ưu tiên 6: Đi bộ
            else if (isMoving)
            {
                newState = PlayerState.Walking;
            }
            // Mặc định: Đứng yên
            else
            {
                newState = PlayerState.Idle;
            }

            SetState(newState);
        }

        private void SetState(PlayerState newState)
        {
            if (CurrentState == newState) return;

            previousState = CurrentState;
            CurrentState = newState;
            animationController.OnStateChanged(CurrentState, previousState);
        }

        // Được gọi từ PlayerMovement khi nhảy khỏi tường
        public void ExitWallRun()
        {
            SetState(PlayerState.Falling);
        }
    }
}