using UnityEngine;

public class AdvancedTopDownCameraControllerWith3Modes : MonoBehaviour
{
    // --- NÂNG CẤP: Thêm trạng thái FaceToFace ---
    private enum CameraState { TopDown, OverTheShoulder, FaceToFace }
    private CameraState currentState;

    [Header("Target Settings")]
    public Transform target;

    [Header("Camera Framing")]
    public Vector3 topDownOffset = new Vector3(0f, 10f, -8f);
    [Range(0.01f, 1.0f)]
    public float smoothSpeed = 0.125f;

    [Header("Top-Down Control")]
    public float rotationSpeed = 5.0f;
    public float returnToDefaultSpeed = 2.0f;
    public bool autoReturnOnMove = true;

    [Header("Zoom Settings")]
    public float zoomSpeed = 10.0f;
    public float minZoom = 1.5f; // Giảm minZoom để cho phép zoom sát mặt
    public float maxZoom = 15.0f;

    [Header("Over-the-Shoulder View Settings")]
    public bool enableShoulderView = true;
    [Tooltip("Ngưỡng zoom để kích hoạt góc nhìn ngang vai.")]
    public float shoulderViewThreshold = 6.0f;
    [Tooltip("Vị trí của camera so với nhân vật ở chế độ ngang vai.")]
    public Vector3 shoulderOffset = new Vector3(0.7f, 1.6f, -2.0f);

    // --- NÂNG CẤP MỚI: Cài đặt cho góc nhìn Chính diện ---
    [Header("Face-to-Face View Settings")]
    public bool enableFaceView = true;
    [Tooltip("Ngưỡng zoom để kích hoạt góc nhìn chính diện (phải nhỏ hơn Shoulder Threshold).")]
    public float faceViewThreshold = 3.0f;
    [Tooltip("Vị trí của camera ở phía trước nhân vật.")]
    public Vector3 faceOffset = new Vector3(0f, 1.7f, 1.5f); // Y: chiều cao ngang mắt, Z: khoảng cách phía trước
    [Tooltip("Điểm camera sẽ nhìn vào trên nhân vật (ví dụ: đầu).")]
    public Vector3 faceLookAtOffset = new Vector3(0f, 1.7f, 0f);
    // --------------------------------------------------------

    [Header("General Settings")]
    [Tooltip("Tốc độ chuyển đổi giữa các chế độ nhìn.")]
    public float viewChangeSpeed = 5.0f;
    public bool enableCollision = true;
    public LayerMask collisionLayers;
    public float collisionPadding = 0.2f;

    // Private variables
    private Vector3 initialTopDownOffset;
    private Vector3 currentTopDownOffset; // Dùng để xoay ở chế độ TopDown
    private float currentZoom;
    private Vector3 currentVelocity = Vector3.zero;

    void Start()
    {
        if (target == null) { Debug.LogError("Chưa gán Target cho Camera!"); return; }

        initialTopDownOffset = topDownOffset;
        currentTopDownOffset = topDownOffset;
        currentZoom = topDownOffset.magnitude;
        currentState = CameraState.TopDown;
    }

    void LateUpdate()
    {
        if (target == null) return;

        HandleZoomAndStateChange();
        FollowAndLookAtTarget();
    }

    private void HandleZoomAndStateChange()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        currentZoom -= scroll * zoomSpeed;
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

        // --- Logic chuyển đổi giữa 3 trạng thái ---
        // Ưu tiên kiểm tra trạng thái gần nhất trước (FaceToFace)
        if (enableFaceView && currentZoom <= faceViewThreshold)
        {
            currentState = CameraState.FaceToFace;
        }
        else if (enableShoulderView && currentZoom <= shoulderViewThreshold)
        {
            currentState = CameraState.OverTheShoulder;
        }
        else
        {
            currentState = CameraState.TopDown;
        }
    }

    private void FollowAndLookAtTarget()
    {
        Vector3 targetOffset;
        Quaternion targetRotation;

        // --- Sử dụng Switch-Case cho 3 trạng thái rõ ràng hơn ---
        switch (currentState)
        {
            case CameraState.TopDown:
                HandleTopDownRotation();
                targetOffset = currentTopDownOffset;
                targetRotation = Quaternion.LookRotation(target.position - transform.position);
                break;

            case CameraState.OverTheShoulder:
                targetOffset = target.TransformDirection(shoulderOffset);
                targetRotation = target.rotation; // Nhìn về phía trước cùng nhân vật
                break;

            case CameraState.FaceToFace:
                targetOffset = target.TransformDirection(faceOffset);
                Vector3 lookAtPoint = target.position + faceLookAtOffset;
                targetRotation = Quaternion.LookRotation(lookAtPoint - transform.position); // Nhìn vào mặt nhân vật
                break;

            default: // Phòng trường hợp lỗi
                targetOffset = currentTopDownOffset;
                targetRotation = Quaternion.LookRotation(target.position - transform.position);
                break;
        }

        Vector3 desiredPosition = target.position + targetOffset;

        if (enableCollision)
        {
            RaycastHit hit;
            // Bắn tia từ điểm nhìn (đầu nhân vật) tới camera để va chạm chính xác hơn
            Vector3 collisionCheckOrigin = target.position + faceLookAtOffset;
            if (Physics.Linecast(collisionCheckOrigin, desiredPosition, out hit, collisionLayers))
            {
                desiredPosition = hit.point + hit.normal * collisionPadding;
            }
        }

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * viewChangeSpeed);
    }

    private void HandleTopDownRotation()
    {
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
            Quaternion camTurnAngle = Quaternion.AngleAxis(mouseX, Vector3.up);
            currentTopDownOffset = camTurnAngle * currentTopDownOffset;
        }
        else if (autoReturnOnMove && (Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f))
        {
            Vector3 targetDir = initialTopDownOffset.normalized;
            currentTopDownOffset = Vector3.Slerp(currentTopDownOffset.normalized, targetDir, Time.deltaTime * returnToDefaultSpeed) * currentZoom;
        }

        currentTopDownOffset = currentTopDownOffset.normalized * currentZoom;
    }
}