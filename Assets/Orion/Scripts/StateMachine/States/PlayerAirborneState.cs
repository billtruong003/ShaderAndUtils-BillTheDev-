using UnityEngine;

namespace Orion
{
    public abstract class PlayerAirborneState : State
    {
        protected PlayerAirborneState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
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

            if (player.CurrentVelocity.y <= 0f && TryDetectLedge(out LedgeData ledgeData))
            {
                player.LedgeClimbState.SetLedgeData(ledgeData);
                stateMachine.ChangeState(player.LedgeClimbState);
                return;
            }

            if (CheckForWallRun())
            {
                stateMachine.ChangeState(player.WallRunState);
                return;
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
            ApplyGravity();
            ApplyAirControl();
        }

        protected virtual void ApplyGravity()
        {
            float gravity = Physics.gravity.y * player.GravityMultiplier;
            player.AddForce(new Vector3(0, gravity, 0), ForceMode.Acceleration);
        }

        protected void ApplyAirControl()
        {
            Vector3 moveDirection = MovementUtilities.GetCameraRelativeMoveDirection(player.PlayerCameraTransform, player.Input.MoveInput);
            if (moveDirection == Vector3.zero) return;

            Vector3 currentHorizontalVelocity = new Vector3(player.CurrentVelocity.x, 0, player.CurrentVelocity.z);
            Vector3 targetVelocity = moveDirection * player.RunSpeed;

            Vector3 velocityChange = targetVelocity - currentHorizontalVelocity;
            float acceleration = player.AirAcceleration;

            Vector3 requiredForce = Vector3.ClampMagnitude(velocityChange, player.RunSpeed) * acceleration;
            player.AddForce(requiredForce, ForceMode.Acceleration);
        }

        private bool CheckForWallRun()
        {
            if (player.Input.MoveInput == Vector2.zero || player.CurrentVelocity.y > 0) return false;

            Vector3 right = player.transform.right;
            right.y = 0f;

            return Physics.Raycast(player.transform.position, right, 1f, player.WallRunLayer) ||
                   Physics.Raycast(player.transform.position, -right, 1f, player.WallRunLayer);
        }

        private bool TryDetectLedge(out LedgeData ledgeData)
        {
            ledgeData = new LedgeData();
            if (player.CurrentVelocity.y > 0) return false;

            Vector3 forwardRayOrigin = player.transform.position + player.LedgeDetectForwardOffset;
            Vector3 forwardDirection = player.transform.forward;

            if (Physics.Raycast(forwardRayOrigin, forwardDirection, out RaycastHit wallHit, player.LedgeDetectForwardDistance, player.LedgeLayer))
            {
                Vector3 downRayOrigin = wallHit.point + (forwardDirection * 0.1f) + (Vector3.up * player.LedgeDetectDownDistance);
                if (Physics.Raycast(downRayOrigin, Vector3.down, out RaycastHit surfaceHit, player.LedgeDetectDownDistance, player.LedgeLayer))
                {
                    ledgeData.SurfacePoint = surfaceHit.point;
                    ledgeData.WallNormal = wallHit.normal;
                    return true;
                }
            }

            return false;
        }
    }
}