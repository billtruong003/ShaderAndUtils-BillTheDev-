using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UI_StatAllocationButton : MonoBehaviour
{
    public StatController playerStats;
    public StatType statToAllocate;

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(AllocatePoint);
    }

    private void AllocatePoint()
    {
        if (playerStats != null)
        {
            playerStats.DistributeStatPoint(statToAllocate);
        }
    }

    // Phương thức này sẽ được gọi từ bên ngoài để bật/tắt nút
    public void UpdateInteractable(int availablePoints)
    {
        button.interactable = availablePoints > 0;
    }
}