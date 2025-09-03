using UnityEngine;
using StateSystem;

namespace Kaelia.Player.States
{
    public class PlayerWallRunState : PlayerBaseState
    {
        private float wallRunTimer;

        public PlayerWallRunState(PlayerController controller, StateMachine stateMachine) : base(controller, stateMachine) { }

        public override void Enter()
        {
            Controller.Animator.SetBool("IsWallRunning", true);
            wallRunTimer = Data.MaxWallRunTime;
            Controller.Rb.useGravity = false;
            Controller.Rb.linearVelocity = new Vector3(Controller.Rb.linearVelocity.x, 0, Controller.Rb.linearVelocity.z);
            Controller.CanDoubleJump = true;
        }

        public override void Exit()
        {
            Controller.Animator.SetBool("IsWallRunning", false);
            Controller.Rb.useGravity = true;
            Controller.CameraController.UpdateWallRunTilt(0f);
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();
            wallRunTimer -= Time.deltaTime;
            UpdateCameraTilt();

            bool noWall = !Controller.IsWallLeft && !Controller.IsWallRight;
            bool stopRunning = Controller.Input.Vertical <= 0;

            if (wallRunTimer <= 0 || noWall || stopRunning)
            {
                StateMachine.ChangeState(new PlayerAirborneState(Controller, StateMachine));
                return;
            }

            if (Controller.Input.JumpDown)
            {
                PerformWallJump();
                StateMachine.ChangeState(new PlayerAirborneState(Controller, StateMachine));
                return;
            }
        }

        public override void PhysicsUpdate()
        {
            ApplyWallRunMovement();
        }

        private void ApplyWallRunMovement()
        {
            Vector3 desiredForward = Vector3.ProjectOnPlane(Controller.CameraTransform.forward, Controller.WallNormal).normalized;
            Vector3 targetVelocity = desiredForward * Data.WallRunSpeed;

            Controller.Rb.linearVelocity = Vector3.Lerp(Controller.Rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 10f);

            Controller.Rb.AddForce(-Controller.WallNormal * Data.WallStickForce, ForceMode.Force);
            Controller.Rb.AddForce(Vector3.down * Data.WallRunGravity, ForceMode.Force);
        }

        private void PerformWallJump()
        {
            Vector3 forceToApply = Controller.transform.up * Data.WallJumpUpForce + Controller.WallNormal * Data.WallJumpSideForce;
            Controller.Rb.linearVelocity = Vector3.zero;
            Controller.Rb.AddForce(forceToApply, ForceMode.Impulse);
        }

        private void UpdateCameraTilt()
        {
            float tiltDirection = Controller.IsWallRight ? 1f : -1f;
            float tilt = Data.WallRunCameraTilt * tiltDirection;
            Controller.CameraController.UpdateWallRunTilt(tilt);
        }
    }
}