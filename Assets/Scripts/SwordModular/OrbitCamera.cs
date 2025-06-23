using UnityEngine;
using Sirenix.OdinInspector; // Sử dụng Odin để làm Inspector đẹp hơn

[AddComponentMenu("Camera/Orbit Camera Controller")]
public class OrbitCameraController : MonoBehaviour
{
    [Title("Target Settings")]
    [Tooltip("Đối tượng mà camera sẽ xoay quanh.")]
    [Required] // Đánh dấu là trường bắt buộc
    public Transform target;

    [Tooltip("Điểm lệch so với tâm của target. Hữu ích để nâng tâm camera lên phần thân/đầu nhân vật.")]
    public Vector3 targetOffset = Vector3.zero;

    [Header("Orbit Settings")]
    [Tooltip("Tốc độ xoay ngang.")]
    public float xSpeed = 120.0f;
    [Tooltip("Tốc độ xoay dọc.")]
    public float ySpeed = 120.0f;
    [MinMaxSlider(-90, 90, true)] // Dùng slider của Odin
    [Tooltip("Góc xoay dọc tối thiểu và tối đa.")]
    public Vector2 yAngleLimits = new Vector2(-20, 80);
    [Tooltip("Độ mượt khi xoay (giá trị nhỏ hơn sẽ mượt hơn).")]
    [Range(0.01f, 1f)]
    public float rotationDampening = 0.1f;

    [Header("Zoom Settings")]
    [Tooltip("Khoảng cách ban đầu tới mục tiêu.")]
    public float distance = 5.0f;
    [MinMaxSlider(0, 200f, true)]
    [Tooltip("Khoảng cách zoom gần nhất và xa nhất.")]
    public Vector2 distanceLimits = new Vector2(1.5f, 15f);
    [Tooltip("Tốc độ zoom.")]
    public float zoomSpeed = 20.0f;
    [Tooltip("Độ mượt khi zoom (giá trị nhỏ hơn sẽ mượt hơn).")]
    [Range(0.01f, 1f)]
    public float zoomDampening = 0.1f;

    [Header("Collision")]
    [Tooltip("Layer mà camera sẽ va chạm.")]
    public LayerMask collisionLayers = 1; // Mặc định là Layer "Default"
    [Tooltip("Khoảng đệm giữa camera và vật cản.")]
    public float collisionPadding = 0.3f;

    private float x = 0.0f;
    private float y = 0.0f;
    private float currentDistance;
    private float desiredDistance;
    private Vector3 currentRotation;
    private Vector3 desiredRotation;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
        currentDistance = desiredDistance = distance;

        // Đảm bảo không có lỗi khi khởi động nếu target chưa được gán
        if (target == null)
        {
            Debug.LogError("Chưa gán Target cho Orbit Camera Controller.", this);
            this.enabled = false;
        }
    }

    // Sử dụng FixedUpdate để xử lý input và tính toán logic, 
    // LateUpdate để áp dụng vị trí cuối cùng, giúp tránh giật hình.
    void Update()
    {
        if (target == null) return;

        HandleInput();
    }

    void LateUpdate()
    {
        if (target == null) return;

        UpdateTransform();
    }

    private void HandleInput()
    {
        // --- Xử lý Input ---
        // Chỉ xoay khi nhấn chuột phải (hoặc nút chuột giữa)
        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
            y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
        }

        // Giới hạn góc xoay dọc
        y = ClampAngle(y, yAngleLimits.x, yAngleLimits.y);

        // Lấy input từ con lăn chuột để zoom
        desiredDistance -= Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
        desiredDistance = Mathf.Clamp(desiredDistance, distanceLimits.x, distanceLimits.y);
    }

    private void UpdateTransform()
    {
        // --- Tính toán vị trí và xoay ---
        // Tính toán điểm mục tiêu thực tế (có offset)
        Vector3 targetPosition = target.position + targetOffset;

        // Làm mượt chuyển động
        // Thay vì dùng Time.deltaTime / dampening, dùng Mathf.SmoothDamp để có kết quả ổn định hơn
        currentDistance = Mathf.Lerp(currentDistance, desiredDistance, zoomDampening);

        desiredRotation = new Vector3(y, x);
        currentRotation = Vector3.Lerp(currentRotation, desiredRotation, rotationDampening);

        Quaternion rotation = Quaternion.Euler(currentRotation.x, currentRotation.y, 0);
        Vector3 desiredPosition = targetPosition - (rotation * Vector3.forward * currentDistance);

        // --- Xử lý Va chạm ---
        RaycastHit hit;
        // Bắn một tia từ điểm mục tiêu ra vị trí camera mong muốn
        if (Physics.Linecast(targetPosition, desiredPosition, out hit, collisionLayers, QueryTriggerInteraction.Ignore))
        {
            // Nếu có va chạm, di chuyển camera đến điểm va chạm + một khoảng đệm
            // Điều chỉnh khoảng cách hiện tại thay vì vị trí trực tiếp để tránh giật khi hết va chạm
            float collisionDistance = Vector3.Distance(targetPosition, hit.point) - collisionPadding;
            desiredPosition = targetPosition - (rotation * Vector3.forward * collisionDistance);
        }

        // Áp dụng vị trí và xoay cho camera
        transform.rotation = rotation;
        transform.position = desiredPosition;
    }

    // Hàm trợ giúp để giới hạn góc
    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F) angle += 360F;
        if (angle > 360F) angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }

    // Vẽ Gizmos trong Scene view để dễ dàng hình dung
    private void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.yellow;
            // Vẽ một hình cầu tại điểm mục tiêu thực tế
            Gizmos.DrawWireSphere(target.position + targetOffset, 0.2f);

            Gizmos.color = Color.cyan;
            // Vẽ một đường thẳng từ mục tiêu đến camera
            Gizmos.DrawLine(target.position + targetOffset, transform.position);
        }
    }
}