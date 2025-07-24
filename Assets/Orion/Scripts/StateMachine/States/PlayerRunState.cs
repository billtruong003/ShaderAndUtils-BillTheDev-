using UnityEngine;

namespace Orion
{
    public class PlayerRunState : PlayerGroundedMovementState
    {
        protected override float TargetSpeed => player.RunSpeed;

        public PlayerRunState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();
            player.CameraController.SetSprintFieldOfView();
        }

        public override void Exit()
        {
            base.Exit();
            player.CameraController.ResetFieldOfView();
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

            if (player.IsOnSteepSlope())
            {
                stateMachine.ChangeState(player.GroundedState.SlopeSlideState);
                return;
            }

            if (player.Input.CrouchIsHeld)
            {
                stateMachine.ChangeState(player.GroundedState.CrouchState);
                return;
            }
        }
    }
}