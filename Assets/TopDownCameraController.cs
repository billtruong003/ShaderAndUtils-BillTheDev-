using UnityEngine;

public class TopDownCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Settings")]
    public Vector3 offset = new Vector3(0f, 10f, -8f);
    [Range(0.01f, 1.0f)]
    public float smoothSpeed = 0.125f;

    [Header("Camera Control")]
    public float rotationSpeed = 5.0f;
    public float returnToDefaultSpeed = 2.0f;

    // --- PHẦN MỚI THÊM VÀO ---
    [Header("Zoom Settings")]
    [Tooltip("Tốc độ zoom của camera")]
    public float zoomSpeed = 4.0f;
    [Tooltip("Khoảng cách zoom gần nhất")]
    public float minZoom = 5.0f;
    [Tooltip("Khoảng cách zoom xa nhất")]
    public float maxZoom = 15.0f;
    // -------------------------

    private Vector3 initialOffset;
    private float currentZoom; // Biến để lưu trữ khoảng cách zoom hiện tại

    void Start()
    {
        if (target == null) { Debug.LogError("Chưa gán Target cho Camera!"); return; }

        // Lưu lại offset ban đầu để quay về
        initialOffset = offset;

        // Khởi tạo giá trị zoom ban đầu từ độ lớn của vector offset
        currentZoom = offset.magnitude;
    }

    void LateUpdate()
    {
        if (target == null) return;
        HandleCameraControl(); // Hàm này giờ sẽ bao gồm cả logic zoom
        FollowTarget();
    }

    private void HandleCameraControl()
    {
        // --- LOGIC ZOOM MỚI ---
        // Lấy input từ con lăn chuột (trục Y)
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        // Điều chỉnh giá trị zoom hiện tại
        // Nhân với -1 để lăn lên thì zoom vào, lăn xuống thì zoom ra (hành vi thông thường)
        currentZoom -= scroll * zoomSpeed;

        // Giới hạn giá trị zoom trong khoảng min và max
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
        // -------------------------

        // Xoay camera tự do bằng chuột phải
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
            Quaternion camTurnAngle = Quaternion.AngleAxis(mouseX, Vector3.up);
            offset = camTurnAngle * offset;
        }
        else // Tự động quay về góc nhìn mặc định khi di chuyển
        {
            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");
            if (Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f)
            {
                // Thay vì Slerp về initialOffset cố định, chúng ta Slerp về hướng của initialOffset
                // nhưng vẫn giữ nguyên khoảng cách zoom hiện tại.
                Vector3 targetOffsetDirection = initialOffset.normalized;
                offset = Vector3.Slerp(offset.normalized, targetOffsetDirection, Time.deltaTime * returnToDefaultSpeed) * currentZoom;
            }
        }

        // Luôn cập nhật độ dài của vector offset theo giá trị zoom hiện tại
        // Điều này đảm bảo zoom hoạt động ngay cả khi đang xoay camera
        offset = offset.normalized * currentZoom;
    }

    private void FollowTarget()
    {
        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
        transform.LookAt(target);
    }
}