// File: Assets/MyIndieGame/Scripts/Characters/CharacterSocketController.cs
using UnityEngine;
using System.Collections.Generic;



public class CharacterSocketController : MonoBehaviour
{
    [System.Serializable]
    public class SocketMapping
    {
        public CharacterSocketType slotType;
        public Transform socketTransform;
    }

    [SerializeField]
    private List<SocketMapping> socketMappings;
    private Dictionary<CharacterSocketType, Transform> socketDictionary;

    void Awake()
    {
        socketDictionary = new Dictionary<CharacterSocketType, Transform>();
        foreach (var mapping in socketMappings)
        {
            if (mapping.socketTransform != null && !socketDictionary.ContainsKey(mapping.slotType))
            {
                socketDictionary.Add(mapping.slotType, mapping.socketTransform);
            }
        }
    }

    public Transform GetSocket(CharacterSocketType slotType)
    {
        socketDictionary.TryGetValue(slotType, out Transform socket);
        return socket;
    }
}