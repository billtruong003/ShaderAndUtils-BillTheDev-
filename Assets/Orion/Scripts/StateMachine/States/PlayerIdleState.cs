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
                // Clean state transition without reflection
                stateMachine.ChangeState(player.GroundedState.WalkState);
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
            // Apply drag to stop the player
            player.ApplyAirResistance(5f);
        }
    }
}