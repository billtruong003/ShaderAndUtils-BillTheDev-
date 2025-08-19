using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DynamicAnimationEventHub : MonoBehaviour
{
    [System.Serializable]
    public struct EventMapping
    {
        [field: SerializeField] public string EventID { get; private set; }
        [field: SerializeField] public UnityEvent ActionsToTrigger { get; private set; }
    }

    [Title("Dynamic Animation Event Hub")]
    [InfoBox("Ánh xạ ID sự kiện tới các hành động. Sử dụng List<struct> để tương thích hoàn toàn với Prefab của Unity.")]
    [SerializeField]
    [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "EventID")]
    private List<EventMapping> eventMappings = new List<EventMapping>();

    private readonly Dictionary<string, UnityEvent> _runtimeEventHub = new Dictionary<string, UnityEvent>();

    private void Awake()
    {
        InitializeFromInspector();
    }

    private void InitializeFromInspector()
    {
        _runtimeEventHub.Clear();
        foreach (var mapping in eventMappings)
        {
            if (string.IsNullOrEmpty(mapping.EventID))
            {
                Debug.LogWarning($"[DynamicEventHub] Phát hiện một EventID rỗng trong cấu hình trên GameObject '{gameObject.name}'.", this);
                continue;
            }

            if (_runtimeEventHub.ContainsKey(mapping.EventID))
            {
                Debug.LogWarning($"[DynamicEventHub] EventID '{mapping.EventID}' bị trùng lặp trên GameObject '{gameObject.name}'. Chỉ mục đầu tiên sẽ được sử dụng.", this);
                continue;
            }

            _runtimeEventHub.Add(mapping.EventID, mapping.ActionsToTrigger);
        }
    }

    public void Trigger(string eventID)
    {
        if (string.IsNullOrEmpty(eventID))
        {
            Debug.LogWarning($"[DynamicEventHub] Nhận được một EventID rỗng trên GameObject '{gameObject.name}'.", this);
            return;
        }

        if (_runtimeEventHub.TryGetValue(eventID, out UnityEvent actionsToTrigger))
        {
            actionsToTrigger?.Invoke();
        }
        else
        {
            Debug.LogWarning($"[DynamicEventHub] Không tìm thấy EventID: '{eventID}' trong Hub trên GameObject '{gameObject.name}'.", this);
        }
    }

    // ------------------------------------------------------------------------------------
    // Public Runtime API - Các API hỗ trợ quản lý Event từ script khác
    // ------------------------------------------------------------------------------------

    public bool HasEvent(string eventID)
    {
        if (string.IsNullOrEmpty(eventID)) return false;
        return _runtimeEventHub.ContainsKey(eventID);
    }

    public bool AddListener(string eventID, UnityAction call)
    {
        if (string.IsNullOrEmpty(eventID) || call == null) return false;

        if (!_runtimeEventHub.TryGetValue(eventID, out UnityEvent actionsToTrigger))
        {
            actionsToTrigger = new UnityEvent();
            _runtimeEventHub.Add(eventID, actionsToTrigger);
        }

        actionsToTrigger.AddListener(call);
        return true;
    }

    public bool RemoveListener(string eventID, UnityAction call)
    {
        if (string.IsNullOrEmpty(eventID) || call == null) return false;

        if (_runtimeEventHub.TryGetValue(eventID, out UnityEvent actionsToTrigger))
        {
            actionsToTrigger.RemoveListener(call);
            return true;
        }

        return false;
    }

    public bool RemoveAllListeners(string eventID)
    {
        if (string.IsNullOrEmpty(eventID)) return false;

        if (_runtimeEventHub.TryGetValue(eventID, out UnityEvent actionsToTrigger))
        {
            actionsToTrigger.RemoveAllListeners();
            return true;
        }

        return false;
    }
}