using UnityEngine;

namespace Orion
{
    public class PlayerWalkState : PlayerGroundedMovementState
    {
        protected override float TargetSpeed => player.WalkSpeed;

        public PlayerWalkState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
        {
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            if (player.Input.SprintIsHeld && player.Input.MoveInput != Vector2.zero)
            {
                stateMachine.ChangeState(player.GroundedState.RunState);
                return;
            }

            if (player.Input.MoveInput == Vector2.zero)
            {
                stateMachine.ChangeState(player.GroundedState.IdleState);
                return;
            }

            if (player.IsOnSteepSlope())
            {
                stateMachine.ChangeState(player.GroundedState.SlopeSlideState);
                return;
            }
        }
    }
}