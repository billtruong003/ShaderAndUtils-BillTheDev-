using UnityEngine;

namespace Orion
{
    public abstract class PlayerGroundedMovementState : State
    {
        protected abstract float TargetSpeed { get; }

        protected PlayerGroundedMovementState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
        {
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
            MovePlayer();
        }

        private void MovePlayer()
        {
            Vector3 moveDirection = MovementUtilities.GetCameraRelativeMoveDirection(player.PlayerCameraTransform, player.Input.MoveInput);
            Vector3 targetVelocity = moveDirection * TargetSpeed;

            Vector3 currentHorizontalVelocity = new Vector3(player.Rigidbody.linearVelocity.x, 0, player.Rigidbody.linearVelocity.z);
            Vector3 velocityChange = targetVelocity - currentHorizontalVelocity;

            player.Rigidbody.AddForce(velocityChange * player.MovementAcceleration, ForceMode.Acceleration);
        }
    }
}