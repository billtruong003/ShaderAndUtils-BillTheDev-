using UnityEngine;

namespace Orion
{
    public class PlayerSlopeSlideState : State
    {
        private Vector3 _slopeNormal;

        public PlayerSlopeSlideState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();
            _slopeNormal = player.GetGroundNormal();
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();
            if (!player.IsOnSteepSlope())
            {
                stateMachine.ChangeState(player.GroundedState.IdleState);
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
            Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, _slopeNormal).normalized;
            float slideSpeed = player.RunSpeed * player.SlopeSlideSpeedMultiplier;

            player.Rigidbody.AddForce(slideDirection * slideSpeed, ForceMode.Acceleration);
        }
    }
}