using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIPartRotateSlider : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private SwordPartType partType;
    [Header("UI Elements")]
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI valueText;
    [Header("Custom Progress Bar")]
    [SerializeField] private Image customProgressBarImage;
    [SerializeField] private Color progressBarColor = Color.cyan;

    private SwordAssembler swordAssembler;
    private Material progressBarMaterialInstance;

    public void Initialize(SwordAssembler assembler)
    {
        this.swordAssembler = assembler;
        if (slider == null) { enabled = false; return; }

        if (customProgressBarImage != null && customProgressBarImage.material != null)
        {
            if (progressBarMaterialInstance == null)
                progressBarMaterialInstance = new Material(customProgressBarImage.material);
            customProgressBarImage.material = progressBarMaterialInstance;
            progressBarMaterialInstance.SetColor("_ColorStart", progressBarColor);
            progressBarMaterialInstance.SetColor("_ColorEnd", progressBarColor);
        }

        slider.value = swordAssembler.GetPartRotation(partType);
        UpdateDisplay(slider.value);

        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void OnSliderValueChanged(float value)
    {
        if (swordAssembler == null) return;
        switch (partType)
        {
            case SwordPartType.Blade: swordAssembler.SetBladeRotation(value); break;
            case SwordPartType.Hilt: swordAssembler.SetHiltRotation(value); break;
            case SwordPartType.Grip: swordAssembler.SetGripRotation(value); break;
        }
        UpdateDisplay(value);
    }

    private void UpdateDisplay(float value)
    {
        if (valueText != null) valueText.text = $"{value:F0}°";
        if (progressBarMaterialInstance != null)
        {
            // --- LOGIC MỚI: Hiển thị độ lớn của góc xoay ---
            // Giá trị tuyệt đối của góc xoay, chia cho 180 để ra khoảng [0, 1]
            // Điều này thể hiện "mức độ xoay" so với vị trí 0.
            float normalizedValue = Mathf.Abs(value) / 180f;
            progressBarMaterialInstance.SetFloat("_Progress", normalizedValue);
        }
    }

    private void OnDestroy()
    {
        if (progressBarMaterialInstance != null) Destroy(progressBarMaterialInstance);
    }
}