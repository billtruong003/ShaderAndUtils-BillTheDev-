using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UILightingSlider : MonoBehaviour
{
    [Header("Core References")]
    [Tooltip("Gán GameObject chứa script LightingController vào đây.")]
    [SerializeField] private LightingController lightingController;

    [Header("UI Elements")]
    [Tooltip("Gán Slider component vào đây.")]
    [SerializeField] private Slider slider;
    [Tooltip("(Tùy chọn) Text để hiển thị giá trị góc xoay.")]
    [SerializeField] private TextMeshProUGUI valueText;

    // --- LOGIC MỚI ĐƯỢC THÊM VÀO ---
    [Header("Custom Progress Bar")]
    [Tooltip("(Tùy chọn) Gán Image component có Material là ProgressBarURP.")]
    [SerializeField] private Image customProgressBarImage;

    private Material progressBarMaterialInstance; // Material riêng cho Image này

    void Start()
    {
        if (lightingController == null || slider == null)
        {
            Debug.LogError("Chưa gán đủ các tham chiếu (Lighting Controller hoặc Slider) cho UILightingSlider!", this);
            enabled = false;
            return;
        }

        // --- LOGIC MỚI: TẠO INSTANCE CHO MATERIAL ---
        if (customProgressBarImage != null && customProgressBarImage.material != null)
        {
            progressBarMaterialInstance = new Material(customProgressBarImage.material);
            customProgressBarImage.material = progressBarMaterialInstance;
        }

        // Cấu hình slider
        slider.minValue = 0;
        slider.maxValue = 360;

        // Đặt giá trị ban đầu cho slider
        slider.value = lightingController.GetCurrentLightRotation();
        UpdateDisplay(slider.value);

        // Thêm listener
        slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void OnSliderValueChanged(float value)
    {
        if (lightingController == null) return;

        lightingController.SetLightRotation(value);
        UpdateDisplay(value);
    }

    private void UpdateDisplay(float value)
    {
        // Cập nhật text
        if (valueText != null)
        {
            valueText.text = $"Light: {value:F0}°";
        }

        // --- LOGIC MỚI: CẬP NHẬT PROGRESS BAR ---
        if (progressBarMaterialInstance != null)
        {
            // Chuẩn hóa giá trị từ khoảng [0, 360] về khoảng [0, 1]
            // Công thức: (value - min) / (max - min) = value / 360
            float normalizedValue = value / 360f;
            progressBarMaterialInstance.SetFloat("_Progress", normalizedValue);
        }
    }

    // --- LOGIC MỚI: DỌN DẸP MATERIAL ---
    private void OnDestroy()
    {
        if (progressBarMaterialInstance != null)
        {
            Destroy(progressBarMaterialInstance);
        }
    }
}