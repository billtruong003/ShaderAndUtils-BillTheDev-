using UnityEngine;

namespace Orion
{
    public class PlayerIdleState : State
    {
        public PlayerIdleState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
        {
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();
            if (player.Input.MoveInput != Vector2.zero)
            {
                if (player.Input.SprintIsHeld)
                {
                    stateMachine.ChangeState(player.GroundedState.RunState);
                }
                else
                {
                    stateMachine.ChangeState(player.GroundedState.WalkState);
                }
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
            player.ApplyAirResistance(player.IdleFriction);
        }
    }
}