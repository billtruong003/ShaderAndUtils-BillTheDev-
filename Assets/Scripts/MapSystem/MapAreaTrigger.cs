using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MapAreaTrigger : MonoBehaviour
{
    [Header("Target Area")]
    [Tooltip("Tên của khu vực bản đồ sẽ được kích hoạt khi người chơi đi vào.")]
    public string targetAreaName;

    [Header("Required Components")]
    [Tooltip("Kéo đối tượng chứa script MinimapController vào đây.")]
    public MinimapController minimapController;

    private void Awake()
    {
        // Đảm bảo collider là một trigger
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem đối tượng va chạm có phải là người chơi không
        if (other.CompareTag("Player"))
        {
            if (minimapController != null)
            {
                minimapController.SwitchMapArea(targetAreaName);
            }
            else
            {
                Debug.LogError($"MapAreaTrigger trên đối tượng '{gameObject.name}' chưa được gán MinimapController.");
            }
        }
    }
}