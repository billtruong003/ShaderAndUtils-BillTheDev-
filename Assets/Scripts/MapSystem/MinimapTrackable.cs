// File: MinimapTrackable.cs
using UnityEngine;
using Utils.Bill.InspectorCustom; // Import thư viện tiện ích của bạn

public class MinimapTrackable : MonoBehaviour
{
    // Không còn bất kỳ biến static nào ở đây

    [CustomHeader("Data Source", "Cấu hình dữ liệu cho đối tượng này từ Database.", "#AEEA00")]
    [Tooltip("Chọn loại đối tượng này từ danh sách đã định nghĩa trong Database.")]
    public TrackableType type;
    [Tooltip("Kéo ScriptableObject Database chứa dữ liệu icon vào đây.")]
    public TrackableDatabase database;

    [CustomHeader("System Connection", "Tham chiếu đến hệ thống minimap. Sử dụng nút bên dưới để gán tự động.", "#00B8D4")]
    [ReadOnly("#37474F")] // Đánh dấu chỉ đọc để người dùng biết nên dùng nút bấm
    public MinimapController controller;

    // Các thuộc tính này sẽ được tự động điền từ Database, không cần hiện trên Inspector
    [HideInInspector] public Sprite iconSprite;
    [HideInInspector] public Color iconColor;
    [HideInInspector] public float iconSize;

    void Awake()
    {
        // Lấy dữ liệu từ database
        if (database == null)
        {
            Debug.LogError($"MinimapTrackable trên đối tượng '{gameObject.name}' chưa được gán Database!", this);
            return;
        }

        TrackableTypeData data = database.GetData(type);
        if (data != null)
        {
            this.iconSprite = data.iconSprite;
            this.iconColor = data.iconColor;
            this.iconSize = data.iconSize;
        }
    }

    void OnEnable()
    {
        // Chỉ đăng ký nếu đã có tham chiếu đến controller
        if (controller != null)
        {
            MinimapController.AddTrackable(this);
        }
    }

    void OnDisable()
    {
        // Chỉ hủy đăng ký nếu đã có tham chiếu
        // Kiểm tra controller != null để tránh lỗi khi thoát game hoặc scene
        if (controller != null)
        {
            MinimapController.RemoveTrackable(this);
        }
    }

    [CustomButton("Find & Assign Controller", "Tự động tìm MinimapController trong Scene và gán vào trường bên trên.", "#FFAB00")]
    private void FindAndAssignController()
    {
        // Tìm đối tượng MinimapController DUY NHẤT trong scene đang mở
        MinimapController foundController = FindObjectOfType<MinimapController>();

        if (foundController != null)
        {
            // Gán tham chiếu
            this.controller = foundController;

            // Đánh dấu đối tượng này đã bị thay đổi để Unity lưu lại (quan trọng cho Prefab)
            UnityEditor.EditorUtility.SetDirty(this);

            Debug.Log($"[Minimap] Đã gán thành công '{foundController.gameObject.name}' cho '{this.gameObject.name}'.", this.gameObject);
        }
        else
        {
            Debug.LogError("[Minimap] Không tìm thấy đối tượng nào có script MinimapController trong Scene!", this.gameObject);
        }
    }
}