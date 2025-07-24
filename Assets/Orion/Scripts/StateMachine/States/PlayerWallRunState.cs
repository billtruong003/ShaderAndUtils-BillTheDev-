using UnityEngine;

namespace Orion
{
    public class PlayerWallRunState : State
    {
        private float _wallRunTimer;
        private Vector3 _wallNormal;
        private Vector3 _wallForward;
        private bool _isWallOnRight;

        public PlayerWallRunState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();
            _wallRunTimer = player.GetMaxWallRunTime();
            player.SetVelocity(new Vector3(player.CurrentVelocity.x, 0, player.CurrentVelocity.z));
            FindWall();
            player.AnimationController.SetWallRunning(true, _isWallOnRight);
        }

        public override void Exit()
        {
            base.Exit();
            player.AnimationController.SetWallRunning(false, _isWallOnRight);
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();
            _wallRunTimer -= Time.deltaTime;

            if (_wallRunTimer <= 0 || !IsWallNearby() || player.IsGrounded())
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

            Vector3 velocity = _wallForward * player.GetWallRunSpeed();
            velocity.y = player.Rigidbody.linearVelocity.y;
            player.Rigidbody.linearVelocity = velocity;

            float gravity = Physics.gravity.y * player.GetWallRunGravityMultiplier();
            player.Rigidbody.AddForce(new Vector3(0, gravity, 0), ForceMode.Acceleration);
        }

        private void FindWall()
        {
            Vector3 right = player.PlayerCameraTransform.right;
            right.y = 0;

            if (Physics.Raycast(player.transform.position, right, out RaycastHit rightHit, 1f, player.GetWallRunLayer()))
            {
                _wallNormal = rightHit.normal;
                _isWallOnRight = true;
            }
            else if (Physics.Raycast(player.transform.position, -right, out RaycastHit leftHit, 1f, player.GetWallRunLayer()))
            {
                _wallNormal = leftHit.normal;
                _isWallOnRight = false;
            }

            _wallForward = Vector3.Cross(_wallNormal, Vector3.up).normalized;

            Vector3 cameraForward = player.PlayerCameraTransform.forward;
            cameraForward.y = 0;
            if (Vector3.Dot(cameraForward, _wallForward) < 0)
            {
                _wallForward = -_wallForward;
            }
        }

        private bool IsWallNearby()
        {
            return Physics.Raycast(player.transform.position, -_wallNormal, 1.2f, player.GetWallRunLayer());
        }

        private void PerformWallJump()
        {
            player.Input.UseJumpInput();

            Vector3 wallJumpForce = player.GetWallJumpForce();
            Vector3 finalForce = _wallNormal * wallJumpForce.x + Vector3.up * wallJumpForce.y;

            player.SetVelocity(finalForce);
            stateMachine.ChangeState(player.JumpState);
        }
    }
}