using UnityEngine;

namespace Orion
{
    public class PlayerAnimationController : MonoBehaviour
    {
        private Animator _animator;
        private PlayerController _playerController;

        private static readonly int IsGroundedHash = Animator.StringToHash("isGrounded");
        private static readonly int IsWallRunningHash = Animator.StringToHash("isWallRunning");
        private static readonly int IsWallOnRightHash = Animator.StringToHash("isWallOnRight");
        private static readonly int IsClimbingLedgeHash = Animator.StringToHash("isClimbingLedge");
        private static readonly int IsCrouchingHash = Animator.StringToHash("isCrouching");
        private static readonly int SpeedHash = Animator.StringToHash("speed");
        private static readonly int VerticalVelocityHash = Animator.StringToHash("verticalVelocity");
        private static readonly int JumpHash = Animator.StringToHash("jump");
        private static readonly int DashHash = Animator.StringToHash("dash");
        private static readonly int SlideHash = Animator.StringToHash("slide");

        private void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
            _playerController = GetComponentInParent<PlayerController>();

            if (_playerController == null || _animator == null)
            {
                enabled = false;
            }
        }

        private void FixedUpdate()
        {
            UpdateAnimationParameters();
        }

        private void UpdateAnimationParameters()
        {
            UpdateNormalizedHorizontalSpeed();
            UpdateVerticalVelocity();
        }

        private void UpdateNormalizedHorizontalSpeed()
        {
            Vector3 horizontalVelocity = new Vector3(_playerController.CurrentVelocity.x, 0f, _playerController.CurrentVelocity.z);
            float normalizedSpeed = horizontalVelocity.magnitude / _playerController.RunSpeed;
            _animator.SetFloat(SpeedHash, Mathf.Clamp01(normalizedSpeed));
        }

        private void UpdateVerticalVelocity()
        {
            _animator.SetFloat(VerticalVelocityHash, _playerController.CurrentVelocity.y);
        }

        public void SetGrounded(bool isGrounded) => _animator.SetBool(IsGroundedHash, isGrounded);

        public void SetCrouching(bool isCrouching) => _animator.SetBool(IsCrouchingHash, isCrouching);

        public void SetWallRunning(bool isRunning, bool isWallOnRight)
        {
            _animator.SetBool(IsWallRunningHash, isRunning);
            if (isRunning)
            {
                _animator.SetBool(IsWallOnRightHash, isWallOnRight);
            }
        }

        public void SetClimbingLedge(bool isClimbing) => _animator.SetBool(IsClimbingLedgeHash, isClimbing);
        public void TriggerJump() => _animator.SetTrigger(JumpHash);
        public void TriggerDash() => _animator.SetTrigger(DashHash);
        public void TriggerSlide() => _animator.SetTrigger(SlideHash);
    }
}