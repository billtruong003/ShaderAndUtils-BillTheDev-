// File: AdvancedTopDownCameraControllerWith3Modes.cs
// PHIÊN BẢN SỬA LỖI GIẬT CAMERA KHI NHẢY

using UnityEngine;

public class AdvancedTopDownCameraControllerWith3Modes : MonoBehaviour
{
    private enum CameraState { TopDown, OverTheShoulder, FaceToFace }
    private CameraState currentState;

    [Header("Target Settings")]
    public Transform target;

    [Header("Camera Framing")]
    public Vector3 topDownOffset = new Vector3(0f, 10f, -8f);
    // Tách riêng tốc độ làm mượt cho di chuyển ngang và dọc
    [Range(0f, 1f)]
    public float horizontalSmoothTime = 0.1f;
    [Range(0f, 1f)]
    public float verticalSmoothTime = 0.05f; // Làm mượt trục Y nhanh hơn

    [Header("Top-Down Control")]
    public float rotationSpeed = 5.0f;
    public float returnToDefaultSpeed = 2.0f;
    public bool autoReturnOnMove = true;

    [Header("Zoom Settings")]
    public float zoomSpeed = 10.0f;
    public float minZoom = 1.5f;
    public float maxZoom = 15.0f;

    [Header("Over-the-Shoulder View Settings")]
    public bool enableShoulderView = true;
    public float shoulderViewThreshold = 6.0f;
    public Vector3 shoulderOffset = new Vector3(0.7f, 1.6f, -2.0f);

    [Header("Face-to-Face View Settings")]
    public bool enableFaceView = true;
    public float faceViewThreshold = 3.0f;
    public Vector3 faceOffset = new Vector3(0f, 1.7f, 1.5f);
    public Vector3 faceLookAtOffset = new Vector3(0f, 1.7f, 0f);

    [Header("General Settings")]
    public float viewChangeSpeed = 5.0f;
    public bool enableCollision = true;
    public LayerMask collisionLayers;
    public float collisionPadding = 0.2f;

    private Vector3 initialTopDownOffset;
    private Vector3 currentTopDownOffset;
    private float currentZoom;
    private Vector3 currentVelocity = Vector3.zero; // Vẫn cần cho SmoothDamp

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

        if (enableFaceView && currentZoom <= faceViewThreshold) currentState = CameraState.FaceToFace;
        else if (enableShoulderView && currentZoom <= shoulderViewThreshold) currentState = CameraState.OverTheShoulder;
        else currentState = CameraState.TopDown;
    }

    // ===================================================================
    // === HÀM FOLLOW ĐÃ ĐƯỢC SỬA LẠI ĐỂ TÁCH BIỆT TRỤC Y =================
    // ===================================================================
    private void FollowAndLookAtTarget()
    {
        Vector3 targetOffset;
        Quaternion targetRotation;

        switch (currentState)
        {
            case CameraState.TopDown:
                HandleTopDownRotation();
                targetOffset = currentTopDownOffset;
                targetRotation = Quaternion.LookRotation(target.position - (target.position + targetOffset));
                break;
            case CameraState.OverTheShoulder:
                targetOffset = target.TransformDirection(shoulderOffset);
                targetRotation = target.rotation;
                break;
            case CameraState.FaceToFace:
                targetOffset = target.TransformDirection(faceOffset);
                Vector3 lookAtPoint = target.position + faceLookAtOffset;
                targetRotation = Quaternion.LookRotation(lookAtPoint - (target.position + targetOffset));
                break;
            default:
                targetOffset = currentTopDownOffset;
                targetRotation = Quaternion.LookRotation(target.position - (target.position + targetOffset));
                break;
        }

        Vector3 desiredPosition = target.position + targetOffset;

        if (enableCollision)
        {
            RaycastHit hit;
            Vector3 collisionCheckOrigin = target.position + faceLookAtOffset;
            if (Physics.Linecast(collisionCheckOrigin, desiredPosition, out hit, collisionLayers))
            {
                desiredPosition = hit.point + hit.normal * collisionPadding;
            }
        }

        // --- LOGIC MỚI: TÁCH BIỆT VIỆC LÀM MƯỢT ---

        // 1. Cập nhật vị trí X và Z một cách mượt mà
        Vector3 smoothedHorizontalPosition = Vector3.SmoothDamp(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(desiredPosition.x, 0, desiredPosition.z),
            ref currentVelocity,
            horizontalSmoothTime);

        // 2. Cập nhật vị trí Y gần như tức thời (smooth time rất nhỏ)
        float smoothedY = Mathf.Lerp(transform.position.y, desiredPosition.y, 1f - verticalSmoothTime);

        // 3. Gộp lại thành vị trí cuối cùng
        transform.position = new Vector3(smoothedHorizontalPosition.x, smoothedY, smoothedHorizontalPosition.z);

        // Cập nhật xoay
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