// File: Assets/MyIndieGame/Scripts/Characters/CharacterSocketController.cs
using UnityEngine;
using System.Collections.Generic;



public class CharacterSocketController : MonoBehaviour
{
    [System.Serializable]
    public class SocketMapping
    {
        public EquipmentSlotType slotType;
        public Transform socketTransform;
    }

    [SerializeField]
    private List<SocketMapping> socketMappings;
    private Dictionary<EquipmentSlotType, Transform> socketDictionary;

    void Awake()
    {
        socketDictionary = new Dictionary<EquipmentSlotType, Transform>();
        foreach (var mapping in socketMappings)
        {
            if (mapping.socketTransform != null && !socketDictionary.ContainsKey(mapping.slotType))
            {
                socketDictionary.Add(mapping.slotType, mapping.socketTransform);
            }
        }
    }

    public Transform GetSocket(EquipmentSlotType slotType)
    {
        socketDictionary.TryGetValue(slotType, out Transform socket);
        return socket;
    }
}