using UnityEngine;

namespace Kaelia
{
    public class KaeliaCameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 focusPointOffset = new Vector3(0.7f, 1.6f, 0f);

        [SerializeField] private float rotationSpeed = 400f;
        [SerializeField] private float minVerticalAngle = -30f;
        [SerializeField] private float maxVerticalAngle = 75f;

        [SerializeField] private float zoomSpeed = 20f;
        [SerializeField] private float minZoomDistance = 1.5f;
        [SerializeField] private float maxZoomDistance = 10f;

        [SerializeField] private LayerMask collisionLayers;
        [SerializeField] private float cameraCollisionRadius = 0.2f;

        [SerializeField] private float focusPointSmoothTime = 0.1f;
        [SerializeField] private float rotationSmoothTime = 0.02f;
        [SerializeField] private float collisionSmoothTime = 0.1f;
        [SerializeField] private float tiltSmoothTime = 0.15f;

        private Vector3 focusPoint;
        private Vector3 focusPointVelocity;

        private float targetDistance;
        private float currentDistance;
        private float distanceVelocity;

        private Vector2 rotationInput;
        private float yaw;
        private float pitch;
        private float yawVelocity;
        private float pitchVelocity;

        private float targetTilt;
        private float currentTilt;
        private float tiltVelocity;

        private void Start()
        {
            if (IsTargetInvalid()) return;
            InitializeCameraState();
        }

        private void LateUpdate()
        {
            if (IsTargetInvalid()) return;

            HandleInput();
            UpdateCameraLogic();
        }

        public void UpdateWallRunTilt(float newTargetTilt)
        {
            targetTilt = newTargetTilt;
        }

        private void InitializeCameraState()
        {
            focusPoint = target.position + focusPointOffset;
            targetDistance = (minZoomDistance + maxZoomDistance) / 2f;
            currentDistance = targetDistance;

            yaw = target.eulerAngles.y;
            pitch = 20f;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private bool IsTargetInvalid()
        {
            if (target != null) return false;

            Debug.LogError($"Camera Target has not been assigned on {gameObject.name}. Disabling component.", this);
            enabled = false;
            return true;
        }

        private void HandleInput()
        {
            rotationInput.x = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            rotationInput.y = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;

            float scrollWheelInput = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scrollWheelInput) > 0.01f)
            {
                targetDistance -= scrollWheelInput * zoomSpeed;
                targetDistance = Mathf.Clamp(targetDistance, minZoomDistance, maxZoomDistance);
            }
        }

        private void UpdateCameraLogic()
        {
            Vector3 targetFocusPosition = target.position + focusPointOffset;
            focusPoint = Vector3.SmoothDamp(focusPoint, targetFocusPosition, ref focusPointVelocity, focusPointSmoothTime);

            yaw += rotationInput.x;
            pitch -= rotationInput.y;
            pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);

            float smoothedYaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, yaw, ref yawVelocity, rotationSmoothTime);
            float smoothedPitch = Mathf.SmoothDampAngle(transform.eulerAngles.x, pitch, ref pitchVelocity, rotationSmoothTime);
            currentTilt = Mathf.SmoothDamp(currentTilt, targetTilt, ref tiltVelocity, tiltSmoothTime);

            Quaternion finalRotation = Quaternion.Euler(smoothedPitch, smoothedYaw, currentTilt);

            float collisionAdjustedDistance = ResolveCollisionsAndGetDistance(finalRotation);
            currentDistance = Mathf.SmoothDamp(currentDistance, collisionAdjustedDistance, ref distanceVelocity, collisionSmoothTime);

            Vector3 finalPosition = focusPoint - (finalRotation * Vector3.forward * currentDistance);
            transform.SetPositionAndRotation(finalPosition, finalRotation);
        }

        private float ResolveCollisionsAndGetDistance(Quaternion rotation)
        {
            Vector3 direction = rotation * -Vector3.forward;
            if (Physics.SphereCast(focusPoint, cameraCollisionRadius, direction, out RaycastHit hit, targetDistance, collisionLayers))
            {
                return hit.distance;
            }
            return targetDistance;
        }
    }
}