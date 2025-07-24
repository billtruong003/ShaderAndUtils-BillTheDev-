using UnityEngine;

namespace Orion
{
    public class PlayerFallState : PlayerAirborneBaseState
    {
        public PlayerFallState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
        {
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            if (player.IsGrounded())
            {
                stateMachine.ChangeState(player.GroundedState);
            }
        }
    }
}