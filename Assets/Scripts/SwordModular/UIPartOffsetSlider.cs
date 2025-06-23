using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIPartOffsetSlider : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private SwordPartType partType;
    [Header("UI Elements")]
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI valueText;
    [Header("Custom Progress Bar")]
    [SerializeField] private Image customProgressBarImage;

    private SwordAssembler swordAssembler;
    private Material progressBarMaterialInstance;

    public void Initialize(SwordAssembler assembler)
    {
        this.swordAssembler = assembler;
        if (slider == null) { enabled = false; return; }

        if (customProgressBarImage != null && customProgressBarImage.material != null)
        {
            progressBarMaterialInstance = new Material(customProgressBarImage.material);
            customProgressBarImage.material = progressBarMaterialInstance;
        }

        float initialValue = 0f;
        if (partType == SwordPartType.Blade) initialValue = swordAssembler.BladeOffsetValue;
        else if (partType == SwordPartType.Grip) initialValue = swordAssembler.GripOffsetValue;

        slider.value = initialValue;
        UpdateDisplay(initialValue);

        slider.onValueChanged.RemoveAllListeners(); // Xóa listener cũ để tránh trùng lặp
        slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void OnSliderValueChanged(float value)
    {
        if (swordAssembler == null) return;
        if (partType == SwordPartType.Blade) swordAssembler.SetBladeOffset(value);
        else if (partType == SwordPartType.Grip) swordAssembler.SetGripOffset(value);
        UpdateDisplay(value);
    }

    private void UpdateDisplay(float value)
    {
        if (valueText != null) valueText.text = $"{value:P0}";
        if (progressBarMaterialInstance != null) progressBarMaterialInstance.SetFloat("_Progress", value);
    }

    private void OnDestroy()
    {
        if (progressBarMaterialInstance != null) Destroy(progressBarMaterialInstance);
    }
}