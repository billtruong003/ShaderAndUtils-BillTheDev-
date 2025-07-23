using UnityEngine;

public enum NotificationType { Info, Success, Warning, Error }

public interface IUserNotifier
{
    void ShowNotification(string message, NotificationType type = NotificationType.Info, float duration = 3f);
}

public class DebugLogNotifier : MonoBehaviour, IUserNotifier
{
    public static DebugLogNotifier Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ShowNotification(string message, NotificationType type = NotificationType.Info, float duration = 3f)
    {
        string formattedMessage = $"[UI NOTIFICATION] ({type}): {message}";
        switch (type)
        {
            case NotificationType.Error:
                Debug.LogError(formattedMessage);
                break;
            case NotificationType.Warning:
                Debug.LogWarning(formattedMessage);
                break;
            default:
                Debug.Log(formattedMessage);
                break;
        }
    }
}
