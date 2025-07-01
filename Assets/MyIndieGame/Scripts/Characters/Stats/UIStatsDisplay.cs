using TMPro;
using UnityEngine;

public class UI_StatDisplay : MonoBehaviour
{
    public StatController playerStats; // Kéo StatController của Player vào đây
    public StatType statToDisplay; // Chọn chỉ số muốn hiển thị trong Inspector

    private TextMeshProUGUI text;

    void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        // Đăng ký lắng nghe sự kiện khi UI được bật
        if (playerStats != null)
        {
            playerStats.OnStatChanged += UpdateStatText;
        }
        // Cập nhật giá trị lần đầu
        UpdateStatText(statToDisplay, playerStats.GetStatValue(statToDisplay));
    }

    void OnDisable()
    {
        // Hủy đăng ký khi UI bị tắt để tránh lỗi
        if (playerStats != null)
        {
            playerStats.OnStatChanged -= UpdateStatText;
        }
    }

    private void UpdateStatText(StatType type, float value)
    {
        // Chỉ cập nhật nếu đúng là chỉ số mà UI này đang hiển thị
        if (type == statToDisplay)
        {
            text.text = $"{type}: {value}";
        }
    }
}