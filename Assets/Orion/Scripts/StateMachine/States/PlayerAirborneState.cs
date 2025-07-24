using UnityEngine;

namespace Orion
{
    public abstract class PlayerAirborneBaseState : State
    {
        protected PlayerAirborneBaseState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
        {
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            if (player.Input.DashWasPressed)
            {
                stateMachine.ChangeState(player.DashState);
                return;
            }

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
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
            ApplyGravity();
            ApplyAirControl();
        }

        protected void ApplyGravity()
        {
            float gravity = Physics.gravity.y * player.GetGravityMultiplier();
            player.Rigidbody.AddForce(new Vector3(0, gravity, 0), ForceMode.Acceleration);
        }

        protected void ApplyAirControl()
        {
            Vector3 moveDirection = GetCameraRelativeMoveDirection();
            float airControl = player.GetAirControlFactor();
            float acceleration = player.GetMovementAcceleration();

            player.Rigidbody.AddForce(moveDirection * airControl * acceleration, ForceMode.Acceleration);
        }

        protected Vector3 GetCameraRelativeMoveDirection()
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

            Vector3 intendedDirection = GetCameraRelativeMoveDirection();
            if (Physics.Raycast(player.transform.position, intendedDirection, 1f, player.GetWallRunLayer()))
            {
                return true;
            }

            Vector3 right = player.PlayerCameraTransform.right;
            right.y = 0;
            return Physics.Raycast(player.transform.position, right, 1f, player.GetWallRunLayer()) ||
                   Physics.Raycast(player.transform.position, -right, 1f, player.GetWallRunLayer());
        }

        private bool CheckForLedge()
        {
            Vector3 worldLedgeDetectPoint = player.transform.TransformPoint(player.GetLedgeDetectOffset());

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