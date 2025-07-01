using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_StatAllocatorPanel : MonoBehaviour
{
    public StatController playerStats;
    public TextMeshProUGUI availablePointsText;
    public string pointsTextFormat = "Points: {0}";

    // Kéo tất cả các nút cộng điểm vào đây trong Inspector
    public UI_StatAllocationButton[] allocationButtons;

    void OnEnable()
    {
        if (playerStats != null)
        {
            playerStats.OnStatPointsChanged += UpdatePanel;
            // Cập nhật trạng thái ban đầu khi panel được bật
            UpdatePanel(playerStats.StatPoints);
        }
    }

    void OnDisable()
    {
        if (playerStats != null)
        {
            playerStats.OnStatPointsChanged -= UpdatePanel;
        }
    }

    private void UpdatePanel(int availablePoints)
    {
        // Cập nhật Text hiển thị số điểm
        if (availablePointsText != null)
        {
            availablePointsText.text = string.Format(pointsTextFormat, availablePoints);
        }

        // Cập nhật trạng thái của tất cả các nút
        foreach (var btn in allocationButtons)
        {
            if (btn != null)
            {
                btn.UpdateInteractable(availablePoints);
            }
        }
    }
}