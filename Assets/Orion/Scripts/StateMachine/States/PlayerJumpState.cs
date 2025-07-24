using UnityEngine;

namespace Orion
{
    public class PlayerJumpState : PlayerAirborneState
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

            if (player.CurrentVelocity.y < 0f)
            {
                stateMachine.ChangeState(player.FallState);
                return;
            }
        }

        protected override void ApplyGravity()
        {
            float baseGravity = Physics.gravity.y * player.GravityMultiplier;
            float finalGravity = player.Input.JumpIsHeld ? baseGravity : baseGravity * 1.5f;

            player.AddForce(new Vector3(0, finalGravity, 0), ForceMode.Acceleration);
        }

        private void PerformJump()
        {
            player.AnimationController.TriggerJump();
            player.Input.UseJumpInput();

            player.CoyoteTimeCounter = 0f;
            player.JumpBufferCounter = 0f;

            player.SetVelocityY(player.JumpForce);
        }
    }
}