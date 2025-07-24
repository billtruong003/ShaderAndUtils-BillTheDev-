using UnityEngine;

namespace Orion
{
    [RequireComponent(typeof(InputHandler), typeof(PlayerController))]
    public sealed class PlayerOrientationController : MonoBehaviour
    {
        [Header("Dependencies")]
        [Tooltip("The camera transform used to determine the forward direction for movement.")]
        [SerializeField] private Transform _cameraTransform;

        private PlayerController _playerController;
        private InputHandler _inputHandler;
        private Rigidbody _rigidbody;
        private Transform _playerTransform;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
            _inputHandler = GetComponent<InputHandler>();
            _rigidbody = GetComponent<Rigidbody>();
            _playerTransform = transform;

            if (_cameraTransform == null)
            {
                enabled = false;
            }
        }

        private void FixedUpdate()
        {
            HandleRotation();
        }

        private void HandleRotation()
        {
            if (_playerController.LockOrientation)
            {
                return;
            }

            if (_inputHandler.MoveInput == Vector2.zero)
            {
                return;
            }

            Vector3 moveDirection = CalculateMoveDirection();
            if (moveDirection == Vector3.zero)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            Quaternion newRotation = Quaternion.Slerp(_rigidbody.rotation, targetRotation, DetermineRotationSpeed() * Time.fixedDeltaTime);

            _rigidbody.MoveRotation(newRotation);
        }

        private float DetermineRotationSpeed()
        {
            return _playerController.IsGrounded
                ? _playerController.GroundedRotationSpeed
                : _playerController.AirborneRotationSpeed;
        }

        private Vector3 CalculateMoveDirection()
        {
            Vector3 cameraForward = _cameraTransform.forward;
            Vector3 cameraRight = _cameraTransform.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;

            return (cameraForward.normalized * _inputHandler.MoveInput.y + cameraRight.normalized * _inputHandler.MoveInput.x).normalized;
        }
    }
}