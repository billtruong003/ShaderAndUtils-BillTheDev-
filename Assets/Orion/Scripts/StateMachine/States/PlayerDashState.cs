using UnityEngine;

namespace Orion
{
    public class PlayerDashState : State
    {
        private float _dashTimer;

        public PlayerDashState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();
            player.AnimationController.TriggerDash();
            player.Input.UseDashInput();
            _dashTimer = player.GetDashDuration();
            player.Rigidbody.useGravity = false;
            PerformDash();
        }

        public override void Exit()
        {
            base.Exit();
            player.Rigidbody.useGravity = true;
            player.SetVelocity(new Vector3(player.CurrentVelocity.x, 0, player.CurrentVelocity.z));
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();
            _dashTimer -= Time.deltaTime;
            if (_dashTimer <= 0f)
            {
                if (player.IsGrounded())
                {
                    stateMachine.ChangeState(player.GroundedState);
                }
                else
                {
                    stateMachine.ChangeState(player.FallState);
                }
            }
        }

        private void PerformDash()
        {
            Vector3 dashDirection = GetDashDirection();
            float dashForce = player.GetDashForce();
            player.SetVelocity(dashDirection * dashForce);
        }

        private Vector3 GetDashDirection()
        {
            Vector3 cameraForward = player.PlayerCameraTransform.forward;
            Vector3 cameraRight = player.PlayerCameraTransform.right;
            cameraForward.y = 0;
            cameraRight.y = 0;

            Vector3 moveDirection = (cameraForward.normalized * player.Input.MoveInput.y + cameraRight.normalized * player.Input.MoveInput.x).normalized;

            if (moveDirection == Vector3.zero)
            {
                cameraForward = player.PlayerCameraTransform.forward;
                cameraForward.y = 0;
                moveDirection = cameraForward.normalized;
            }

            return moveDirection;
        }
    }
}