using UnityEngine;

namespace Orion
{
    public sealed class ThirdPersonCameraController : MonoBehaviour
    {
        [Header("GENERAL SETTINGS")]
        [SerializeField] private Transform _followTarget;

        [Header("FREE LOOK SETTINGS")]
        [SerializeField] private Vector3 _freeLookFraming = new Vector3(0f, 1.5f, 0f);
        [SerializeField] private float _freeLookDistance = 5.0f;
        [SerializeField] private float _mouseSensitivity = 100f;
        [SerializeField] private Vector2 _verticalAngleLimits = new Vector2(-40f, 80f);
        [SerializeField] private float _freeLookRotationSmoothing = 0.1f;

        [Header("LOCK-ON SETTINGS")]
        [SerializeField] private Vector3 _lockOnFraming = new Vector3(0.5f, 1.2f, 0f);
        [SerializeField] private float _lockOnDistance = 4.0f;
        [SerializeField] private float _lockOnRotationSmoothing = 0.05f;

        [Header("COLLISION HANDLING")]
        [SerializeField] private LayerMask _collisionLayers;
        [SerializeField] private float _cameraRadius = 0.2f;
        [SerializeField] private float _collisionSmoothing = 0.1f;

        private bool _isLockedOn;
        private Transform _lockOnTarget;

        private float _yaw;
        private float _pitch;

        private Vector3 _currentRotationVelocity;
        private float _currentDistance;
        private float _distanceSmoothVelocity;

        private const string MouseXInput = "Mouse X";
        private const string MouseYInput = "Mouse Y";

        public void SetLockOnState(bool isLocked, Transform target)
        {
            _isLockedOn = isLocked;
            _lockOnTarget = target;
        }

        private void Start()
        {
            if (!_followTarget)
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

            CameraStateParameters stateParams = DetermineCameraStateParameters();
            Quaternion targetRotation = CalculateTargetRotation(stateParams);

            float finalDistance = ResolveCollisions(targetRotation, stateParams.Framing, stateParams.Distance);
            Vector3 finalPosition = CalculateFinalPosition(targetRotation, stateParams.Framing, finalDistance);

            transform.SetPositionAndRotation(finalPosition, targetRotation);
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
            _pitch = initialAngles.x;
            _yaw = initialAngles.y;
        }

        private CameraStateParameters DetermineCameraStateParameters()
        {
            bool canLockOn = _isLockedOn && _lockOnTarget != null;
            if (canLockOn)
            {
                return new CameraStateParameters(_lockOnDistance, _lockOnFraming);
            }

            return new CameraStateParameters(_freeLookDistance, _freeLookFraming);
        }

        private Quaternion CalculateTargetRotation(CameraStateParameters stateParams)
        {
            bool canLockOn = _isLockedOn && _lockOnTarget != null;
            if (canLockOn)
            {
                return ProcessLockOnRotation();
            }

            return ProcessFreeLookRotation();
        }

        private Quaternion ProcessFreeLookRotation()
        {
            float mouseX = Input.GetAxis(MouseXInput) * _mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis(MouseYInput) * _mouseSensitivity * Time.deltaTime;

            _yaw += mouseX;
            _pitch -= mouseY;
            _pitch = Mathf.Clamp(_pitch, _verticalAngleLimits.x, _verticalAngleLimits.y);

            Vector3 targetEulerAngles = new Vector3(_pitch, _yaw);
            Vector3 smoothedEulerAngles = Vector3.SmoothDamp(
                new Vector3(_pitch, _yaw),
                targetEulerAngles,
                ref _currentRotationVelocity,
                _freeLookRotationSmoothing
            );

            return Quaternion.Euler(smoothedEulerAngles);
        }

        private Quaternion ProcessLockOnRotation()
        {
            Vector3 directionToTarget = (_lockOnTarget.position - _followTarget.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

            return Quaternion.Slerp(transform.rotation, targetRotation, _lockOnRotationSmoothing * Time.deltaTime * 10f);
        }

        private float ResolveCollisions(Quaternion targetRotation, Vector3 framing, float targetDistance)
        {
            Vector3 rayStartPoint = _followTarget.position + targetRotation * framing;
            Vector3 rayDirection = targetRotation * -Vector3.forward;

            bool collisionDetected = Physics.SphereCast(
                rayStartPoint,
                _cameraRadius,
                rayDirection,
                out RaycastHit hit,
                targetDistance,
                _collisionLayers
            );

            float desiredDistance = collisionDetected ? hit.distance : targetDistance;

            _currentDistance = Mathf.SmoothDamp(
                _currentDistance,
                desiredDistance,
                ref _distanceSmoothVelocity,
                _collisionSmoothing
            );

            return _currentDistance;
        }

        private Vector3 CalculateFinalPosition(Quaternion targetRotation, Vector3 framing, float distance)
        {
            Vector3 basePosition = _followTarget.position + targetRotation * framing;
            Vector3 offset = targetRotation * (-Vector3.forward) * distance;

            return basePosition + offset;
        }

        private readonly struct CameraStateParameters
        {
            public readonly float Distance;
            public readonly Vector3 Framing;

            public CameraStateParameters(float distance, Vector3 framing)
            {
                Distance = distance;
                Framing = framing;
            }
        }
    }
}