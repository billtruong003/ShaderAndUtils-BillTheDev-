using UnityEngine;
using System.Collections.Generic;

namespace ZombieAI
{
    public class ZombiePoolManager : MonoBehaviour
    {
        public static ZombiePoolManager Instance { get; private set; }

        private Dictionary<string, Queue<GameObject>> _poolDictionary;
        private Dictionary<string, GameObject> _prefabDictionary;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                _poolDictionary = new Dictionary<string, Queue<GameObject>>();
                _prefabDictionary = new Dictionary<string, GameObject>();
            }
        }

        public void InitializePools(List<ZombieDirector.ZombieSpawnInfo> zombieTypes, int initialSize)
        {
            foreach (var type in zombieTypes)
            {
                string poolKey = type.ZombiePrefab.name;
                if (_poolDictionary.ContainsKey(poolKey)) continue;

                _prefabDictionary[poolKey] = type.ZombiePrefab;

                Queue<GameObject> objectPool = new Queue<GameObject>();
                for (int i = 0; i < initialSize; i++)
                {
                    objectPool.Enqueue(CreateNewZombieInstance(poolKey));
                }
                _poolDictionary.Add(poolKey, objectPool);
            }
        }

        public GameObject SpawnFromPool(string prefabName, Vector3 position, Quaternion rotation)
        {
            if (!_poolDictionary.ContainsKey(prefabName))
            {
                Debug.LogError($"Pool with name '{prefabName}' doesn't exist.");
                return null;
            }

            Queue<GameObject> poolQueue = _poolDictionary[prefabName];
            GameObject objectToSpawn;

            if (poolQueue.Count > 0)
            {
                objectToSpawn = poolQueue.Dequeue();
            }
            else
            {
                Debug.LogWarning($"Pool '{prefabName}' ran out of objects. Dynamically creating a new one.");
                objectToSpawn = CreateNewZombieInstance(prefabName, false);
            }

            objectToSpawn.transform.position = position;
            objectToSpawn.transform.rotation = rotation;
            objectToSpawn.SetActive(true);

            return objectToSpawn;
        }

        public void ReturnToPool(GameObject objectToReturn)
        {
            string prefabName = objectToReturn.name;
            if (!_poolDictionary.ContainsKey(prefabName))
            {
                Debug.LogWarning($"Pool with name '{prefabName}' doesn't exist. Destroying object.");
                Destroy(objectToReturn);
                return;
            }

            objectToReturn.SetActive(false);
            _poolDictionary[prefabName].Enqueue(objectToReturn);
        }

        private GameObject CreateNewZombieInstance(string prefabName, bool initiallyInactive = true)
        {
            GameObject prefab = _prefabDictionary[prefabName];
            GameObject newInstance = Instantiate(prefab);
            newInstance.name = prefabName;
            if (initiallyInactive)
            {
                newInstance.SetActive(false);
            }
            return newInstance;
        }
    }
}