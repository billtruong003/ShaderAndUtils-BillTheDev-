using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIPartSelector : MonoBehaviour
{
    [Header("Part Configuration")]
    [SerializeField] private SwordPartType partType;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI partNameText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;

    private SwordAssembler swordAssembler;
    private bool isInitialized = false;

    public void Initialize(SwordAssembler assembler)
    {
        this.swordAssembler = assembler;

        // --- TỐI ƯU HÓA: Xóa listener cũ để tránh đăng ký trùng lặp ---
        nextButton.onClick.RemoveAllListeners();
        prevButton.onClick.RemoveAllListeners();

        nextButton.onClick.AddListener(() => ChangeOption(1));
        prevButton.onClick.AddListener(() => ChangeOption(-1));
        nextButton.onClick.AddListener(() => AnimateButton(nextButton));
        prevButton.onClick.AddListener(() => AnimateButton(prevButton));

        isInitialized = true;
        UpdateDisplay(false);
    }

    private void ChangeOption(int direction)
    {
        if (!isInitialized) return;
        int currentIndex = swordAssembler.GetPartIndex(partType);
        int count = swordAssembler.GetPartCount(partType);
        int totalOptions = count + 1;
        int logicalIndex = (currentIndex + 1 + direction + totalOptions) % totalOptions;
        int newIndex = logicalIndex - 1;
        swordAssembler.SetPart(partType, newIndex);
        UpdateDisplay(true);
    }

    public void UpdateDisplay(bool withAnimation)
    {
        if (!isInitialized) return;
        int currentIndex = swordAssembler.GetPartIndex(partType);
        string newName = (currentIndex == -1)
            ? "None"
            : swordAssembler.GetPartDefinition(partType, currentIndex)?.Mesh.name.Replace("_", " ") ?? "Invalid";

        if (withAnimation && partNameText != null)
        {
            LeanTween.scale(partNameText.gameObject, Vector3.zero, 0.15f)
                     .setEase(LeanTweenType.easeInBack)
                     .setOnComplete(() =>
                     {
                         partNameText.text = newName;
                         LeanTween.scale(partNameText.gameObject, Vector3.one, 0.15f)
                                  .setEase(LeanTweenType.easeOutBack);
                     });
        }
        else if (partNameText != null)
        {
            partNameText.text = newName;
        }
    }

    private void AnimateButton(Button button)
    {
        LeanTween.scale(button.gameObject, Vector3.one * 0.9f, 0.1f).setEasePunch();
    }
}