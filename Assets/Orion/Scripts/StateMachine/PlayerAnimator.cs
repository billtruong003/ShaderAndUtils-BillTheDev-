using UnityEngine;

namespace Orion
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimationController : MonoBehaviour
    {
        private Animator _animator;
        private PlayerController _playerController;

        private readonly int _isGroundedHash = Animator.StringToHash("isGrounded");
        private readonly int _isWallRunningHash = Animator.StringToHash("isWallRunning");
        private readonly int _isWallOnRightHash = Animator.StringToHash("isWallOnRight");
        private readonly int _isClimbingLedgeHash = Animator.StringToHash("isClimbingLedge");
        private readonly int _speedHash = Animator.StringToHash("speed");
        private readonly int _verticalVelocityHash = Animator.StringToHash("verticalVelocity");
        private readonly int _jumpHash = Animator.StringToHash("jump");
        private readonly int _dashHash = Animator.StringToHash("dash");

        public void Initialize(PlayerController playerController)
        {
            _playerController = playerController;
            _animator = GetComponent<Animator>();
        }

        private void Update()
        {
            if (_playerController == null) return;

            UpdateLocomotionParameters();
        }

        private void UpdateLocomotionParameters()
        {
            Vector3 horizontalVelocity = new Vector3(_playerController.CurrentVelocity.x, 0, _playerController.CurrentVelocity.z);
            _animator.SetFloat(_speedHash, horizontalVelocity.magnitude);
            _animator.SetFloat(_verticalVelocityHash, _playerController.CurrentVelocity.y);
        }

        public void SetGrounded(bool value) => _animator.SetBool(_isGroundedHash, value);
        public void SetWallRunning(bool isRunning, bool isWallOnRight)
        {
            _animator.SetBool(_isWallRunningHash, isRunning);
            if (isRunning)
            {
                _animator.SetBool(_isWallOnRightHash, isWallOnRight);
            }
        }
        public void SetClimbingLedge(bool value) => _animator.SetBool(_isClimbingLedgeHash, value);
        public void TriggerJump() => _animator.SetTrigger(_jumpHash);
        public void TriggerDash() => _animator.SetTrigger(_dashHash);
    }
}