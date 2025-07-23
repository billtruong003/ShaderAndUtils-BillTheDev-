using UnityEngine;

namespace Orion
{
    public class PlayerAirborneState : State
    {
        private bool _hasPerformedJump;

        public PlayerAirborneState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();
            _hasPerformedJump = false;
            PerformJump();
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            if (CheckForWallRun())
            {
                stateMachine.ChangeState(player.WallRunState);
                return;
            }

            if (CheckForLedge())
            {
                stateMachine.ChangeState(player.LedgeClimbState);
                return;
            }

            if (player.IsGrounded() && player.Rigidbody.linearVelocity.y < 0.01f)
            {
                stateMachine.ChangeState(player.GroundedState);
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
            ApplyGravity();
            ApplyAirControl();
        }

        private void PerformJump()
        {
            if (player.CoyoteTimeCounter > 0f && player.JumpBufferCounter > 0f && !_hasPerformedJump)
            {
                // *** SỬA LỖI TẠI ĐÂY ***
                // Gọi `UseJumpInput` từ `player.Input` thay vì `player`
                player.Input.UseJumpInput();

                player.CoyoteTimeCounter = 0f;
                player.JumpBufferCounter = 0f;

                float jumpForce = player.GetJumpForce();
                player.SetVelocity(new Vector3(player.CurrentVelocity.x, jumpForce, player.CurrentVelocity.z));
                _hasPerformedJump = true;
            }
        }

        private void ApplyGravity()
        {
            float gravity = Physics.gravity.y * player.GetGravityMultiplier();
            player.Rigidbody.AddForce(new Vector3(0, gravity, 0), ForceMode.Acceleration);
        }

        private void ApplyAirControl()
        {
            Vector3 moveDirection = GetCameraRelativeMoveDirection();
            float airControl = player.GetAirControlFactor();
            float acceleration = player.GetMovementAcceleration();

            player.Rigidbody.AddForce(moveDirection * airControl * acceleration, ForceMode.Acceleration);
        }

        private Vector3 GetCameraRelativeMoveDirection()
        {
            Vector3 forward = player.PlayerCameraTransform.forward;
            Vector3 right = player.PlayerCameraTransform.right;

            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            return (forward * player.Input.MoveInput.y + right * player.Input.MoveInput.x).normalized;
        }

        private bool CheckForWallRun()
        {
            if (player.Input.MoveInput == Vector2.zero || player.CurrentVelocity.y <= 0) return false;

            // Use camera's forward to determine wall direction, not player model's
            Vector3 intendedDirection = GetCameraRelativeMoveDirection();
            if (Physics.Raycast(player.transform.position, intendedDirection, 1f, player.GetWallRunLayer()))
            {
                return true;
            }

            // Check left/right relative to camera
            Vector3 right = player.PlayerCameraTransform.right;
            right.y = 0;
            if (Physics.Raycast(player.transform.position, right, 1f, player.GetWallRunLayer()) ||
                Physics.Raycast(player.transform.position, -right, 1f, player.GetWallRunLayer()))
            {
                return true;
            }

            return false;
        }

        private bool CheckForLedge()
        {
            Vector3 worldLedgeDetectPoint = player.transform.TransformPoint(player.GetLedgeDetectOffset());

            // Check if we are moving downwards to prevent grabbing a ledge while moving up
            if (player.CurrentVelocity.y > 0) return false;

            if (Physics.CheckSphere(worldLedgeDetectPoint, player.GetLedgeDetectRadius(), player.GetLedgeLayer()))
            {
                if (!Physics.Raycast(worldLedgeDetectPoint, Vector3.up, player.CapsuleCollider.height))
                {
                    return true;
                }
            }
            return false;
        }
    }
}