using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class UI_StatDisplay : MonoBehaviour
{
    [Tooltip("Kéo đối tượng Player có StatController vào đây.")]
    public StatController playerStats;

    [Tooltip("Chọn chỉ số mà Text này sẽ hiển thị.")]
    public StatType statToDisplay;

    [Tooltip("Định dạng chuỗi hiển thị. {0} là tên chỉ số, {1} là giá trị.")]
    public string displayTextFormat = "{0}: {1}";

    private TextMeshProUGUI textComponent;

    void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        if (playerStats != null)
        {
            // Đăng ký lắng nghe sự kiện khi đối tượng được kích hoạt
            playerStats.OnStatChanged += UpdateStatValue;
            // Cập nhật giá trị ban đầu ngay lập tức
            UpdateStatValue(statToDisplay, playerStats.GetStatValue(statToDisplay));
        }
        else
        {
            Debug.LogWarning($"UI_StatDisplay trên đối tượng '{gameObject.name}' chưa được gán Player Stats.", this);
            textComponent.text = $"{statToDisplay}: N/A";
        }
    }

    void OnDisable()
    {
        // Hủy đăng ký sự kiện để tránh lỗi khi đối tượng bị vô hiệu hóa hoặc hủy
        if (playerStats != null)
        {
            playerStats.OnStatChanged -= UpdateStatValue;
        }
    }

    /// <summary>
    /// Được gọi bởi sự kiện OnStatChanged từ StatController.
    /// </summary>
    private void UpdateStatValue(StatType type, float value)
    {
        // Chỉ cập nhật text nếu sự kiện được phát ra đúng cho chỉ số mà component này đang theo dõi
        if (type == statToDisplay)
        {
            // FUTURE: Có thể thêm một hệ thống localization để dịch tên chỉ số 'type.ToString()'
            // FUTURE: Có thể format số 'value' (ví dụ: làm tròn, thêm dấu %) bằng cách thay đổi displayTextFormat
            // Ví dụ format: "{0}: {1:F1}" sẽ hiển thị 1 chữ số thập phân
            textComponent.text = string.Format(displayTextFormat, type.ToString(), value);
        }
    }
}