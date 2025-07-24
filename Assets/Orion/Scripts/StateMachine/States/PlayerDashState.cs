using UnityEngine;

namespace Orion
{
    public class PlayerDashState : State
    {
        private float _dashTimer;
        private Vector3 _dashMomentum;

        public PlayerDashState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();
            player.LockOrientation = true;
            player.AnimationController.TriggerDash();
            player.Input.UseDashInput();
            _dashTimer = player.DashDuration;
            player.Rigidbody.useGravity = false;
            PerformDash();
        }

        public override void Exit()
        {
            base.Exit();
            player.LockOrientation = false;
            player.Rigidbody.useGravity = true;
            player.AddForce(_dashMomentum, ForceMode.VelocityChange);
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();
            _dashTimer -= Time.deltaTime;
            if (_dashTimer <= 0f)
            {
                if (player.IsGrounded)
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
            float dashForce = player.DashForce;

            player.SetVelocity(dashDirection * dashForce);
            _dashMomentum = player.CurrentVelocity * player.DashEndMomentumDampening;
        }

        private Vector3 GetDashDirection()
        {
            Vector3 inputDirection = MovementUtilities.GetCameraRelativeMoveDirection(player.PlayerCameraTransform, player.Input.MoveInput);
            if (inputDirection != Vector3.zero)
            {
                return inputDirection;
            }

            Vector3 playerForward = player.transform.forward;
            playerForward.y = 0;
            return playerForward.normalized;
        }
    }
}