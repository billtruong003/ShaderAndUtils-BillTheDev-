using UnityEngine;

namespace Orion
{
    public class PlayerActiveSlideState : State
    {
        private float _slideTimer;
        private Vector3 _slideDirection;

        public PlayerActiveSlideState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();
            player.LockOrientation = true;
            player.AnimationController.TriggerSlide();
            player.CameraController.AdjustFieldOfView(player.DashFOV);
            player.SetColliderHeight(player.CrouchColliderHeight);

            _slideTimer = player.SlideDuration;
            _slideDirection = player.transform.forward;

            player.Rigidbody.AddForce(_slideDirection * player.SlideForce, ForceMode.VelocityChange);
        }

        public override void Exit()
        {
            base.Exit();
            player.LockOrientation = false;
            player.CameraController.ResetFieldOfView();
            if (player.CanStandUp())
            {
                player.SetColliderHeight(player.DefaultColliderHeight);
            }
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();
            _slideTimer -= Time.deltaTime;

            if (player.Input.JumpWasPressed)
            {
                PerformSlideJump();
                return;
            }

            if (_slideTimer <= 0f || player.CurrentVelocity.sqrMagnitude < 1f)
            {
                if (player.Input.CrouchIsHeld)
                {
                    stateMachine.ChangeState(player.GroundedState);
                    player.GroundedState.CrouchState.Enter();
                }
                else
                {
                    stateMachine.ChangeState(player.GroundedState);
                }
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
            player.ApplyAirResistance(player.SlideFriction);
        }

        private void PerformSlideJump()
        {
            player.Input.UseJumpInput();
            stateMachine.ChangeState(player.JumpState);
        }
    }
}