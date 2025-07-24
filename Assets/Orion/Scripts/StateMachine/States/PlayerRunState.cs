using UnityEngine;

namespace Orion
{
    public class PlayerRunState : State
    {
        public PlayerRunState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
        {
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();
            if (!player.Input.SprintIsHeld)
            {
                stateMachine.ChangeState(player.GroundedState.WalkState);
                return;
            }

            if (player.Input.MoveInput == Vector2.zero)
            {
                stateMachine.ChangeState(player.GroundedState.IdleState);
                return;
            }

            if (player.IsOnSteepSlope(out _))
            {
                stateMachine.ChangeState(player.GroundedState.SlideState);
                return;
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
            MovePlayer();
        }

        private void MovePlayer()
        {
            Vector3 moveDirection = GetCameraRelativeMoveDirection();
            float targetSpeed = player.GetRunSpeed();
            Vector3 targetVelocity = moveDirection * targetSpeed;

            Vector3 currentHorizontalVelocity = new Vector3(player.Rigidbody.linearVelocity.x, 0, player.Rigidbody.linearVelocity.z);

            Vector3 velocityChange = targetVelocity - currentHorizontalVelocity;
            float acceleration = player.GetMovementAcceleration();

            player.Rigidbody.AddForce(velocityChange * acceleration, ForceMode.Acceleration);
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
    }
}