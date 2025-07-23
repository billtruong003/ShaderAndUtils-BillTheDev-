using UnityEngine;

namespace Orion
{
    public class PlayerSlideState : State
    {
        private Vector3 _slopeNormal;

        public PlayerSlideState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();
            player.IsOnSteepSlope(out _slopeNormal);
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();
            if (!player.IsOnSteepSlope(out _slopeNormal))
            {
                // Clean state transition without reflection
                stateMachine.ChangeState(player.GroundedState.IdleState);
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
            Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, _slopeNormal).normalized;
            float slideSpeed = player.GetWalkSpeed() * player.GetSlideSpeedMultiplier();

            player.Rigidbody.AddForce(slideDirection * slideSpeed, ForceMode.Acceleration);
        }
    }
}