using UnityEngine;

namespace Orion
{
    public class PlayerWallRunState : State
    {
        private float _wallRunTimer;
        private Vector3 _wallNormal;
        private Vector3 _wallForward;
        private bool _isWallOnRight;
        public bool IsWallOnRight => _isWallOnRight;

        public PlayerWallRunState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();
            player.LockOrientation = true;
            _wallRunTimer = player.MaxWallRunTime;
            player.SetVelocityY(0);
            FindWall();
            player.AnimationController.SetWallRunning(true, _isWallOnRight);
        }

        public override void Exit()
        {
            base.Exit();
            player.LockOrientation = false;
            player.AnimationController.SetWallRunning(false, _isWallOnRight);
            player.Rigidbody.useGravity = true;

            if (stateMachine.CurrentState != player.JumpState)
            {
                Vector3 exitMomentum = _wallForward * player.WallExitForwardMomentum;
                Vector3 pushOffForce = _wallNormal * player.WallExitPushForce;
                player.AddForce(exitMomentum + pushOffForce, ForceMode.VelocityChange);
            }
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();
            _wallRunTimer -= Time.deltaTime;

            if (_wallRunTimer <= 0f || !IsWallNearby() || player.IsGrounded)
            {
                stateMachine.ChangeState(player.FallState);
                return;
            }

            if (player.Input.JumpWasPressed)
            {
                PerformWallJump();
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
            ApplyWallRunForces();
        }

        private void ApplyWallRunForces()
        {
            player.Rigidbody.useGravity = false;

            Vector3 targetVelocity = _wallForward * player.WallRunSpeed;
            Vector3 currentHorizontalVelocity = new Vector3(player.CurrentVelocity.x, 0, player.CurrentVelocity.z);
            Vector3 velocityChange = targetVelocity - currentHorizontalVelocity;

            player.AddForce(velocityChange, ForceMode.VelocityChange);

            float timerRatio = _wallRunTimer / player.MaxWallRunTime;
            float dynamicUpwardForce = Mathf.Lerp(0f, player.WallRunUpwardForce, timerRatio * timerRatio);

            player.AddForce(Vector3.up * dynamicUpwardForce, ForceMode.Force);
        }

        private void FindWall()
        {
            Vector3 right = player.transform.right;
            right.y = 0;

            if (Physics.Raycast(player.transform.position, right, out RaycastHit rightHit, 1f, player.WallRunLayer))
            {
                _wallNormal = rightHit.normal;
                _isWallOnRight = true;
            }
            else if (Physics.Raycast(player.transform.position, -right, out RaycastHit leftHit, 1f, player.WallRunLayer))
            {
                _wallNormal = leftHit.normal;
                _isWallOnRight = false;
            }

            UpdateWallForwardDirection();
        }

        private void UpdateWallForwardDirection()
        {
            _wallForward = Vector3.Cross(_wallNormal, Vector3.up).normalized;

            Vector3 cameraForward = player.PlayerCameraTransform.forward;
            cameraForward.y = 0;
            if (Vector3.Dot(cameraForward.normalized, _wallForward) < 0)
            {
                _wallForward = -_wallForward;
            }
        }

        private bool IsWallNearby()
        {
            return Physics.Raycast(player.transform.position, -_wallNormal, 1.2f, player.WallRunLayer);
        }

        private void PerformWallJump()
        {
            player.Input.UseJumpInput();

            Vector3 wallJumpForce = player.WallJumpForce;

            Vector3 lateralForce = _wallNormal * wallJumpForce.x;
            Vector3 upwardForce = Vector3.up * wallJumpForce.y;
            Vector3 forwardForce = _wallForward * wallJumpForce.z;

            player.SetVelocity(lateralForce + upwardForce + forwardForce);
            stateMachine.ChangeState(player.JumpState);
        }
    }
}