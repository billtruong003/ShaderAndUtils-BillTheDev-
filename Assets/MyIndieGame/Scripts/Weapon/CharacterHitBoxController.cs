using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CharacterHitboxController : MonoBehaviour
{
    [System.Serializable]
    public class BodyPartMapping
    {
        public BodyPart BodyPart;
        public Transform HitboxTransform;
    }

    [SerializeField] private List<BodyPartMapping> bodyPartMappings;
    private Dictionary<BodyPart, List<Transform>> hitboxDictionary;

    void Awake()
    {
        hitboxDictionary = new Dictionary<BodyPart, List<Transform>>();
        foreach (BodyPart part in System.Enum.GetValues(typeof(BodyPart)))
        {
            hitboxDictionary.Add(part, new List<Transform>());
        }

        foreach (var mapping in bodyPartMappings)
        {
            if (mapping.HitboxTransform != null)
            {
                hitboxDictionary[mapping.BodyPart].Add(mapping.HitboxTransform);
            }
        }
    }

    public List<Transform> GetHitboxes(BodyPart bodyPart)
    {
        hitboxDictionary.TryGetValue(bodyPart, out List<Transform> hitboxes);
        return hitboxes ?? new List<Transform>();
    }
}