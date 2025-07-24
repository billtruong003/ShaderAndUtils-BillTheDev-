using UnityEngine;
using System.Collections;

namespace Orion
{
    public sealed class ThirdPersonCameraController : MonoBehaviour
    {
        [Header("GENERAL SETTINGS")]
        [SerializeField] private Transform _followTarget;
        [SerializeField] private Camera _playerCamera;

        [Header("ROTATION & LOOK SETTINGS")]
        [SerializeField] private float _mouseSensitivity = 200f;
        [SerializeField] private Vector2 _verticalAngleLimits = new Vector2(-40f, 80f);
        [SerializeField] private float _rotationSmoothTime = 0.12f;
        [SerializeField] private float _lockOnSmoothTime = 0.08f;

        [Header("FRAMING & DISTANCE")]
        [SerializeField] private Vector3 _freeLookFraming = new Vector3(0f, 1.5f, 0f);
        [SerializeField] private float _freeLookDistance = 5.0f;
        [SerializeField] private Vector3 _lockOnFraming = new Vector3(0.5f, 1.2f, 0f);
        [SerializeField] private float _lockOnDistance = 4.0f;

        [Header("COLLISION HANDLING")]
        [SerializeField] private LayerMask _collisionLayers;
        [SerializeField] private float _cameraRadius = 0.2f;
        [SerializeField] private float _collisionSmoothTime = 0.1f;

        [Header("DYNAMIC FOV")]
        [SerializeField] private float _defaultFOV = 60f;
        [SerializeField] private float _sprintFOV = 70f;
        [SerializeField] private float _fovChangeDuration = 0.4f;

        private bool _isLockedOn;
        private Transform _lockOnTarget;

        private float _yaw;
        private float _pitch;

        private float _currentDistance;
        private float _distanceSmoothVelocity;
        private Vector3 _rotationSmoothVelocity;
        private Coroutine _fovCoroutine;

        private const string MouseXInput = "Mouse X";
        private const string MouseYInput = "Mouse Y";

        public void SetLockOnState(bool isLocked, Transform target)
        {
            _isLockedOn = isLocked;
            _lockOnTarget = target;
        }

        public void AdjustFieldOfView(float targetFov)
        {
            if (_playerCamera == null) return;
            if (_fovCoroutine != null) StopCoroutine(_fovCoroutine);
            _fovCoroutine = StartCoroutine(ChangeFovRoutine(targetFov));
        }

        public void ResetFieldOfView() => AdjustFieldOfView(_defaultFOV);
        public void SetSprintFieldOfView() => AdjustFieldOfView(_sprintFOV);

        private void Start()
        {
            if (!_followTarget || !TryInitializeCamera())
            {
                enabled = false;
                return;
            }

            InitializeCursor();
            InitializeCameraState();
        }

        private void LateUpdate()
        {
            if (!_followTarget) return;

            ProcessInput();

            CameraStateParameters stateParams = DetermineCameraStateParameters();
            Quaternion targetRotation = CalculateTargetRotation(stateParams);

            Quaternion finalRotation = SmoothRotation(targetRotation, stateParams.RotationSmoothTime);

            float finalDistance = ResolveCollisions(finalRotation, stateParams.Framing, stateParams.Distance);
            Vector3 finalPosition = CalculateFinalPosition(finalRotation, stateParams.Framing, finalDistance);

            transform.SetPositionAndRotation(finalPosition, finalRotation);
        }

        private bool TryInitializeCamera()
        {
            if (_playerCamera == null)
            {
                _playerCamera = Camera.main;
            }
            if (_playerCamera != null)
            {
                _playerCamera.fieldOfView = _defaultFOV;
                return true;
            }
            return false;
        }

        private void InitializeCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void InitializeCameraState()
        {
            _currentDistance = _freeLookDistance;
            Vector3 initialAngles = transform.eulerAngles;
            _yaw = initialAngles.y;
            _pitch = initialAngles.x;
        }

        private void ProcessInput()
        {
            if (_isLockedOn) return;

            float mouseX = Input.GetAxis(MouseXInput) * _mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis(MouseYInput) * _mouseSensitivity * Time.deltaTime;

            _yaw += mouseX;
            _pitch -= mouseY;
            _pitch = Mathf.Clamp(_pitch, _verticalAngleLimits.x, _verticalAngleLimits.y);
        }

        private CameraStateParameters DetermineCameraStateParameters()
        {
            bool canLockOn = _isLockedOn && _lockOnTarget != null;
            if (canLockOn)
            {
                return new CameraStateParameters(_lockOnDistance, _lockOnFraming, _lockOnSmoothTime);
            }
            return new CameraStateParameters(_freeLookDistance, _freeLookFraming, _rotationSmoothTime);
        }

        private Quaternion CalculateTargetRotation(CameraStateParameters stateParams)
        {
            bool canLockOn = _isLockedOn && _lockOnTarget != null;
            if (canLockOn)
            {
                Vector3 directionToTarget = (_lockOnTarget.position - _followTarget.position).normalized;
                return Quaternion.LookRotation(directionToTarget, Vector3.up);
            }

            return Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private Quaternion SmoothRotation(Quaternion targetRotation, float smoothTime)
        {
            return Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-smoothTime * 10f / Time.deltaTime));
        }

        private float ResolveCollisions(Quaternion currentRotation, Vector3 framing, float targetDistance)
        {
            Vector3 rayStartPoint = _followTarget.position + currentRotation * framing;
            Vector3 rayDirection = currentRotation * (-Vector3.forward);

            bool collisionDetected = Physics.SphereCast(
                rayStartPoint, _cameraRadius, rayDirection,
                out RaycastHit hit, targetDistance, _collisionLayers
            );

            float desiredDistance = collisionDetected ? hit.distance : targetDistance;

            _currentDistance = Mathf.SmoothDamp(
                _currentDistance, desiredDistance, ref _distanceSmoothVelocity, _collisionSmoothTime
            );

            return _currentDistance;
        }

        private Vector3 CalculateFinalPosition(Quaternion currentRotation, Vector3 framing, float distance)
        {
            Vector3 basePosition = _followTarget.position + currentRotation * framing;
            Vector3 offset = currentRotation * Vector3.forward * -distance;
            return basePosition + offset;
        }

        private IEnumerator ChangeFovRoutine(float targetFov)
        {
            float startFov = _playerCamera.fieldOfView;
            float timer = 0f;

            while (timer < _fovChangeDuration)
            {
                timer += Time.deltaTime;
                float progress = Mathf.Clamp01(timer / _fovChangeDuration);
                _playerCamera.fieldOfView = Mathf.Lerp(startFov, targetFov, progress);
                yield return null;
            }
        }

        private readonly struct CameraStateParameters
        {
            public readonly float Distance;
            public readonly Vector3 Framing;
            public readonly float RotationSmoothTime;

            public CameraStateParameters(float distance, Vector3 framing, float rotationSmoothTime)
            {
                Distance = distance;
                Framing = framing;
                RotationSmoothTime = rotationSmoothTime;
            }
        }
    }
}