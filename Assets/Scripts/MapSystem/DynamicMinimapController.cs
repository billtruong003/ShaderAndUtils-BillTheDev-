using UnityEngine;

/// <summary>
/// Điều khiển một camera orthographic để tạo minimap động.
/// Camera này sẽ đi theo người chơi và có thể zoom.
/// </summary>
public class DynamicMinimapController : MonoBehaviour
{
    [Header("Đối tượng cần theo dõi")]
    [Tooltip("Đối tượng mà minimap sẽ lấy làm trung tâm, thường là người chơi.")]
    public Transform playerTransform;

    [Header("Camera của Minimap")]
    [Tooltip("Camera orthographic được dùng để render minimap.")]
    public Camera minimapCamera;

    [Header("Cài đặt Camera")]
    [Tooltip("Độ cao cố định của camera so với mặt đất.")]
    public float cameraHeight = 100f;

    [Tooltip("Kiểu xoay của Minimap. True: Xoay theo người chơi. False: Hướng Bắc cố định.")]
    public bool rotateWithPlayer = true;

    [Header("Cài đặt Zoom")]
    [Tooltip("Mức zoom của minimap. Giá trị càng nhỏ, minimap càng được phóng to.")]
    [Range(10f, 200f)]
    public float zoomLevel = 50f;

    void Start()
    {
        // Kiểm tra xem các đối tượng cần thiết đã được gán chưa
        if (playerTransform == null || minimapCamera == null)
        {
            Debug.LogError("Dynamic Minimap Controller chưa được thiết lập! Vui lòng gán Player Transform và Minimap Camera trong Inspector.");
            this.enabled = false; // Vô hiệu hóa script này nếu thiếu thiết lập
            return;
        }

        // Đảm bảo camera của minimap luôn ở chế độ orthographic
        minimapCamera.orthographic = true;
    }

    // Sử dụng LateUpdate để đảm bảo vị trí người chơi đã được cập nhật trong frame này
    void LateUpdate()
    {
        if (playerTransform == null) return;

        // --- CẬP NHẬT VỊ TRÍ ---
        // Lấy vị trí của người chơi và đặt vị trí camera theo trục X, Z của người chơi
        Vector3 newPosition = playerTransform.position;
        newPosition.y = cameraHeight; // Giữ nguyên độ cao của camera
        minimapCamera.transform.position = newPosition;

        // --- CẬP NHẬT XOAY ---
        if (rotateWithPlayer)
        {
            // Minimap xoay theo hướng nhìn của người chơi
            // Lấy góc quay Y của người chơi và áp dụng cho camera, trong khi giữ góc X là 90 độ.
            minimapCamera.transform.rotation = Quaternion.Euler(90f, playerTransform.eulerAngles.y, 0f);
        }
        else
        {
            // Minimap cố định, hướng Bắc luôn ở trên
            minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        // --- CẬP NHẬT ZOOM ---
        // Áp dụng mức zoom vào thuộc tính Orthographic Size của camera
        minimapCamera.orthographicSize = zoomLevel;
    }

    /// <summary>
    /// Hàm public để các script khác có thể điều khiển zoom
    /// </summary>
    public void SetZoom(float newZoomLevel)
    {
        // Giới hạn giá trị zoom trong một khoảng hợp lý
        this.zoomLevel = Mathf.Clamp(newZoomLevel, 10f, 200f);
    }
}