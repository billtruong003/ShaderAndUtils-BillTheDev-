using UnityEngine;

namespace Orion
{
    public class PlayerJumpState : PlayerAirborneBaseState
    {
        public PlayerJumpState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();
            PerformJump();
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            if (player.Rigidbody.linearVelocity.y < 0f)
            {
                stateMachine.ChangeState(player.FallState);
                return;
            }
        }

        private void PerformJump()
        {
            player.AnimationController.TriggerJump();
            player.Input.UseJumpInput();

            player.CoyoteTimeCounter = 0f;
            player.JumpBufferCounter = 0f;

            float jumpForce = player.GetJumpForce();
            player.SetVelocity(new Vector3(player.CurrentVelocity.x, jumpForce, player.CurrentVelocity.z));
        }
    }
}