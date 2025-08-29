using UnityEngine;

namespace Kaelia
{
    public class KaeliaCameraController : MonoBehaviour
    {
        [Header("Target & Focus")]
        [Tooltip("Đối tượng mà camera sẽ theo dõi, thường là nhân vật người chơi.")]
        [SerializeField] private Transform target;

        [Tooltip("Điểm lệch của camera so với mục tiêu. Giúp định vị camera qua vai hoặc cao hơn đầu.")]
        [SerializeField] private Vector3 focusPointOffset = new Vector3(0.7f, 1.6f, 0f);

        [Header("Rotation & Look")]
        [Tooltip("Tốc độ xoay camera theo di chuyển của chuột.")]
        [SerializeField] private float rotationSpeed = 400f;
        [Tooltip("Góc xoay dọc tối thiểu (nhìn xuống).")]
        [SerializeField] private float minVerticalAngle = -30f;
        [Tooltip("Góc xoay dọc tối đa (nhìn lên).")]
        [SerializeField] private float maxVerticalAngle = 75f;

        [Header("Zoom")]
        [Tooltip("Tốc độ thu phóng camera bằng con lăn chuột.")]
        [SerializeField] private float zoomSpeed = 20f;
        [Tooltip("Khoảng cách gần nhất camera có thể thu vào.")]
        [SerializeField] private float minZoomDistance = 1.5f;
        [Tooltip("Khoảng cách xa nhất camera có thể phóng ra.")]
        [SerializeField] private float maxZoomDistance = 10f;

        [Header("Collision")]
        [Tooltip("Các layer mà camera sẽ coi là vật cản để tránh xuyên qua.")]
        [SerializeField] private LayerMask collisionLayers;
        [Tooltip("Bán kính của camera khi kiểm tra va chạm, giúp camera không xuyên qua các góc tường mỏng.")]
        [SerializeField] private float cameraCollisionRadius = 0.2f;

        [Header("Smoothing (Quan trọng nhất)")]
        [Tooltip("Độ mượt khi điểm tập trung của camera bám theo nhân vật. Giá trị càng lớn, camera càng 'trôi' theo sau.")]
        [SerializeField] private float focusPointSmoothTime = 0.1f;
        [Tooltip("Độ mượt khi camera xoay theo chuột. Giá trị nhỏ để phản ứng nhạy nhưng vẫn khử giật.")]
        [SerializeField] private float rotationSmoothTime = 0.02f;
        [Tooltip("Độ mượt khi camera lùi ra sau khi hết va chạm. Tạo cảm giác giảm chấn, tránh giật.")]
        [SerializeField] private float collisionSmoothTime = 0.1f;
        [Tooltip("Độ mượt khi camera nghiêng lúc wall-run.")]
        [SerializeField] private float tiltSmoothTime = 0.15f;

        // Trạng thái nội bộ của camera
        private Vector3 _focusPoint;
        private Vector3 _focusPointVelocity;

        private float _targetDistance;
        private float _currentDistance;
        private float _distanceVelocity;

        private Vector2 _rotationInput;
        private float _yaw; // Xoay ngang
        private float _pitch; // Xoay dọc
        private float _yawVelocity;
        private float _pitchVelocity;

        private float _targetTilt;
        private float _currentTilt;
        private float _tiltVelocity;

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

        private void InitializeCameraState()
        {
            _focusPoint = target.position + focusPointOffset;
            _targetDistance = (minZoomDistance + maxZoomDistance) / 2f;
            _currentDistance = _targetDistance;

            _yaw = target.eulerAngles.y;
            _pitch = 20f;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private bool IsTargetInvalid()
        {
            if (target != null) return false;

            Debug.LogError("Camera Target chưa được gán! Vui lòng gán Target trong Inspector.", this);
            enabled = false;
            return true;
        }

        private void HandleInput()
        {
            _rotationInput.x = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            _rotationInput.y = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;

            float scrollWheelInput = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scrollWheelInput) > 0.01f)
            {
                _targetDistance -= scrollWheelInput * zoomSpeed;
                _targetDistance = Mathf.Clamp(_targetDistance, minZoomDistance, maxZoomDistance);
            }
        }

        private void UpdateCameraLogic()
        {
            // 1. Cập nhật điểm tập trung (Focus Point) một cách mượt mà
            Vector3 targetFocusPosition = target.position + focusPointOffset;
            _focusPoint = Vector3.SmoothDamp(_focusPoint, targetFocusPosition, ref _focusPointVelocity, focusPointSmoothTime);

            // 2. Cập nhật góc xoay mong muốn từ input
            _yaw += _rotationInput.x;
            _pitch -= _rotationInput.y;
            _pitch = Mathf.Clamp(_pitch, minVerticalAngle, maxVerticalAngle);

            // 3. Xoay camera một cách mượt mà
            float smoothedYaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, _yaw, ref _yawVelocity, rotationSmoothTime);
            float smoothedPitch = Mathf.SmoothDampAngle(transform.eulerAngles.x, _pitch, ref _pitchVelocity, rotationSmoothTime);
            float smoothedTilt = Mathf.SmoothDamp(_currentTilt, _targetTilt, ref _tiltVelocity, tiltSmoothTime);

            Quaternion finalRotation = Quaternion.Euler(smoothedPitch, smoothedYaw, smoothedTilt);

            // 4. Xử lý va chạm và khoảng cách
            float collisionAdjustedDistance = ResolveCollisionsAndGetDistance(finalRotation);
            _currentDistance = Mathf.SmoothDamp(_currentDistance, collisionAdjustedDistance, ref _distanceVelocity, collisionSmoothTime);

            // 5. Áp dụng vị trí và góc xoay cuối cùng
            Vector3 finalPosition = _focusPoint - (finalRotation * Vector3.forward * _currentDistance);
            transform.SetPositionAndRotation(finalPosition, finalRotation);
        }

        private float ResolveCollisionsAndGetDistance(Quaternion rotation)
        {
            Vector3 direction = rotation * -Vector3.forward;
            if (Physics.SphereCast(_focusPoint, cameraCollisionRadius, direction, out RaycastHit hit, _targetDistance, collisionLayers))
            {
                // Nếu có va chạm, khoảng cách mong muốn là khoảng cách đến điểm va chạm
                return hit.distance;
            }

            // Nếu không có va chạm, quay về khoảng cách người dùng mong muốn
            return _targetDistance;
        }

        // Phương thức này được gọi từ Character Controller để cập nhật độ nghiêng
        public void UpdateWallRunTilt(float targetTilt)
        {
            _targetTilt = targetTilt;
        }
    }
}