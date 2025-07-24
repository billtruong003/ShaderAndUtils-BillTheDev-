using UnityEngine;

namespace Orion
{
    public class PlayerCrouchState : PlayerGroundedMovementState
    {
        protected override float TargetSpeed => player.CrouchSpeed;

        public PlayerCrouchState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();
            player.AnimationController.SetCrouching(true);
            player.SetColliderHeight(player.CrouchColliderHeight);
        }

        public override void Exit()
        {
            base.Exit();
            if (player.CanStandUp())
            {
                player.AnimationController.SetCrouching(false);
                player.SetColliderHeight(player.DefaultColliderHeight);
            }
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();
            if (!player.Input.CrouchIsHeld && player.CanStandUp())
            {
                if (player.Input.MoveInput != Vector2.zero)
                {
                    stateMachine.ChangeState(player.GroundedState.WalkState);
                }
                else
                {
                    stateMachine.ChangeState(player.GroundedState.IdleState);
                }
            }
        }
    }
}