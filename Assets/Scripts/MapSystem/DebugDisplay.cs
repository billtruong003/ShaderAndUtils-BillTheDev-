using UnityEngine;
using TMPro;

/// <summary>
/// Gắn vào một GameObject có TextMeshPro. Chỉ có một nhiệm vụ:
/// nhận một chuỗi và hiển thị nó.
/// </summary>
public class DebugDisplay : MonoBehaviour
{
    public static DebugDisplay Instance { get; private set; }
    [SerializeField] private TextMeshPro textComponent;

    void Awake()
    {
        // Thiết lập Singleton
        if (Instance != null && Instance != this) { Destroy(this.gameObject); }
        else { Instance = this; }

    }

    /// <summary>
    /// Hiển thị một chuỗi thông tin "snapshot" lên màn hình.
    /// </summary>
    public void Show(string information)
    {
        if (textComponent != null)
        {
            textComponent.text = information;
        }
    }
}