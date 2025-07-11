using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

// Định nghĩa một UnityEvent tùy chỉnh để có thể gán các hàm nghe sự kiện từ Inspector.
// Event này sẽ trả về chỉ số (int) của ô được chọn.
[System.Serializable]
public class WheelSelectionEvent : UnityEvent<int> { }

/// <summary>
/// Quản lý toàn bộ logic cho một bánh xe lựa chọn (selection wheel).
/// Bao gồm:
/// - Xử lý input từ chuột để xác định ô được chọn.
/// - Gửi thông tin highlight vào shader.
/// - Sắp xếp layout cho các đối tượng UI (icon) vào từng ô.
/// - Hiển thị icon được chọn ở trung tâm.
/// - Kích hoạt sự kiện khi người dùng xác nhận lựa chọn.
/// </summary>
public class SelectionWheelController : MonoBehaviour
{
    [Header("Wheel Configuration")]
    [Tooltip("Số lượng ô trên bánh xe. Giá trị này PHẢI GIỐNG HỆT với thuộc tính 'Segments' trong Material của bánh xe.")]
    [SerializeField] private int numberOfSegments = 8;

    [Header("Layout Settings")]
    [Tooltip("Danh sách các đối tượng UI (ví dụ: Image, Text) cần được sắp xếp vào các ô.")]
    [SerializeField] private List<RectTransform> segmentObjects;

    [Tooltip("Khoảng cách từ tâm bánh xe đến các đối tượng UI trong các ô.")]
    [SerializeField] private float layoutRadius = 150f;

    [Tooltip("Nếu được chọn, các đối tượng sẽ xoay theo hướng của lát cắt. Nếu không, chúng sẽ luôn đứng thẳng.")]
    [SerializeField] private bool rotateObjectsWithSegments = false;

    [Header("Center Display")]
    [Tooltip("Đối tượng Image ở trung tâm dùng để hiển thị icon của ô đang được chọn.")]
    [SerializeField] private Image centerIconDisplay;

    [Header("References")]
    [Tooltip("Kéo đối tượng Image chính của bánh xe vào đây. Nếu để trống, script sẽ tự tìm.")]
    [SerializeField] private Image wheelImage;
    private Material wheelMaterial; // Instance của material để không ảnh hưởng đến asset gốc
    private RectTransform rectTransform;

    [Header("Output & Events")]
    [Tooltip("Sự kiện được kích hoạt khi người dùng click chuột để chọn một ô. Trả về chỉ số của ô đó.")]
    public WheelSelectionEvent onSegmentSelected;

    [SerializeField, Tooltip("Chỉ số của ô đang được trỏ vào (-1 là không có). Chỉ để theo dõi.")]
    private int currentSelectedSegment = -1;


    // Hàm này được Unity gọi trong Editor mỗi khi một giá trị trong Inspector thay đổi.
    // Rất hữu ích để xem trước layout mà không cần phải chạy game.
    private void OnValidate()
    {
        // Tự động tìm Image component nếu chưa được gán
        if (wheelImage == null)
        {
            wheelImage = GetComponent<Image>();
        }
        // Cập nhật lại layout ngay trong Editor
        UpdateLayout();
    }

    void Start()
    {
        if (wheelImage == null)
        {
            wheelImage = GetComponent<Image>();
        }

        // Quan trọng: Sử dụng materialForRendering để tạo một instance của material.
        // Điều này đảm bảo rằng việc thay đổi thuộc tính của material này chỉ ảnh hưởng đến đối tượng này,
        // không làm thay đổi file Material gốc trong Project.
        wheelMaterial = wheelImage.materialForRendering;
        rectTransform = wheelImage.rectTransform;

        // Đồng bộ số lượng ô giữa script và shader khi bắt đầu
        wheelMaterial.SetInt("_Segments", numberOfSegments);

        // Cập nhật layout và trạng thái ban đầu của icon trung tâm
        UpdateLayout();
        UpdateCenterIcon();
    }

    void Update()
    {
        // Liên tục kiểm tra vị trí chuột để cập nhật highlight
        HandleMouseInput();
    }

    /// <summary>
    /// Tính toán và áp dụng vị trí/góc xoay cho tất cả các đối tượng trong danh sách segmentObjects.
    /// </summary>
    private void UpdateLayout()
    {
        if (segmentObjects == null || segmentObjects.Count == 0 || numberOfSegments == 0)
        {
            return;
        }

        float angleStep = 360f / numberOfSegments;

        for (int i = 0; i < segmentObjects.Count; i++)
        {
            if (segmentObjects[i] == null) continue;

            // Tính góc cho tâm của lát cắt thứ i (đơn vị: độ)
            float currentAngleDegrees = angleStep * i;

            // Chuyển đổi góc sang Radian để sử dụng trong các hàm lượng giác (Cos, Sin)
            float angleRad = currentAngleDegrees * Mathf.Deg2Rad;

            // Tính toán vị trí X và Y trên một vòng tròn với bán kính là layoutRadius
            float xPos = Mathf.Cos(angleRad) * layoutRadius;
            float yPos = Mathf.Sin(angleRad) * layoutRadius;

            // Áp dụng vị trí đã tính toán
            segmentObjects[i].anchoredPosition = new Vector2(xPos, yPos);

            // Xử lý việc xoay đối tượng (nếu được chọn)
            if (rotateObjectsWithSegments)
            {
                // Xoay đối tượng để nó hướng ra ngoài từ tâm
                segmentObjects[i].localRotation = Quaternion.Euler(0, 0, currentAngleDegrees);
            }
            else
            {
                // Giữ cho đối tượng luôn đứng thẳng
                segmentObjects[i].localRotation = Quaternion.identity;
            }
        }
    }

    /// <summary>
    /// Xử lý toàn bộ logic liên quan đến input từ chuột.
    /// </summary>
    private void HandleMouseInput()
    {
        // Chuyển đổi vị trí con trỏ chuột trên màn hình thành tọa độ cục bộ bên trong RectTransform của bánh xe
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            Input.mousePosition,
            null, // Camera của canvas (null cho chế độ Screen Space - Overlay)
            out localPoint
        );

        // Chuẩn hóa tọa độ về khoảng [-0.5, 0.5] để tương thích với UV trong shader
        Vector2 normalizedPoint = new Vector2(
            localPoint.x / rectTransform.sizeDelta.x,
            localPoint.y / rectTransform.sizeDelta.y
        );

        float distance = normalizedPoint.magnitude;
        int segmentIndex = -1;

        // Chỉ xử lý nếu chuột nằm trong bán kính ngoài của bánh xe (0.5 theo UV)
        if (distance <= 0.5f)
        {
            // Tính góc của điểm so với tâm (Atan2 nhận y trước, x sau)
            float angleRad = Mathf.Atan2(normalizedPoint.y, normalizedPoint.x);
            // Chuẩn hóa góc về khoảng [0, 1]
            float angleNormalized = (angleRad / (2 * Mathf.PI)) + 0.5f;

            // Từ góc chuẩn hóa, tính ra chỉ số của ô mà chuột đang trỏ vào
            segmentIndex = Mathf.FloorToInt(angleNormalized * numberOfSegments);
        }

        // Nếu ô được chọn thay đổi so với frame trước -> cần cập nhật
        if (segmentIndex != currentSelectedSegment)
        {
            currentSelectedSegment = segmentIndex;
            // Gửi chỉ số của ô mới được chọn vào shader để nó tự xử lý highlight
            wheelMaterial.SetInt("_SelectedSegment", currentSelectedSegment);
            // Cập nhật lại icon ở trung tâm
            UpdateCenterIcon();
        }

        // Nếu người dùng click chuột trái và đang chọn một ô hợp lệ
        if (Input.GetMouseButtonDown(0) && currentSelectedSegment != -1)
        {
            Debug.Log("Đã chọn ô số: " + currentSelectedSegment);
            // Kích hoạt sự kiện onSegmentSelected và truyền đi chỉ số của ô đã chọn
            onSegmentSelected.Invoke(currentSelectedSegment);
        }
    }

    /// <summary>
    /// Cập nhật sprite và màu sắc cho icon hiển thị ở trung tâm.
    /// </summary>
    private void UpdateCenterIcon()
    {
        if (centerIconDisplay == null) return;

        // Nếu có một ô đang được chọn (chỉ số khác -1) và ô đó có tồn tại trong danh sách
        if (currentSelectedSegment != -1 && segmentObjects.Count > currentSelectedSegment && segmentObjects[currentSelectedSegment] != null)
        {
            // Lấy component Image từ đối tượng trong lát cắt
            Image sourceIcon = segmentObjects[currentSelectedSegment].GetComponent<Image>();
            if (sourceIcon != null && sourceIcon.sprite != null)
            {
                // Bật và gán sprite/màu cho icon trung tâm
                centerIconDisplay.enabled = true;
                centerIconDisplay.sprite = sourceIcon.sprite;
                centerIconDisplay.color = sourceIcon.color;
            }
            else
            {
                // Nếu ô đó không có Image hoặc sprite, ẩn icon trung tâm đi
                centerIconDisplay.enabled = false;
            }
        }
        else // Nếu không có ô nào được chọn (chuột ở ngoài bánh xe)
        {
            // Ẩn icon trung tâm
            centerIconDisplay.enabled = false;
        }
    }
}