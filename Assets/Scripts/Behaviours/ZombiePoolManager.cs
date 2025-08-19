using UnityEngine;
using System.Collections.Generic;

namespace ZombieAI
{
    public class ZombiePoolManager : MonoBehaviour
    {
        public static ZombiePoolManager Instance { get; private set; }

        private Dictionary<GameObject, Queue<GameObject>> _poolDictionary;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                _poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();
            }
        }

        public void CreatePools(List<GameObject> prefabs, int initialSize)
        {
            foreach (var prefab in prefabs)
            {
                if (_poolDictionary.ContainsKey(prefab)) continue;

                var objectPool = new Queue<GameObject>();
                for (int i = 0; i < initialSize; i++)
                {
                    objectPool.Enqueue(CreateNewInstance(prefab));
                }
                _poolDictionary.Add(prefab, objectPool);
            }
        }

        public GameObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!_poolDictionary.ContainsKey(prefab))
            {
                Debug.LogError($"Pool for prefab '{prefab.name}' doesn't exist.");
                return null;
            }

            Queue<GameObject> poolQueue = _poolDictionary[prefab];
            GameObject objectToSpawn;

            if (poolQueue.Count > 0)
            {
                objectToSpawn = poolQueue.Dequeue();
            }
            else
            {
                Debug.LogWarning($"Pool for '{prefab.name}' is empty. Creating a new instance dynamically.");
                objectToSpawn = CreateNewInstance(prefab, false);
            }

            objectToSpawn.transform.position = position;
            objectToSpawn.transform.rotation = rotation;
            objectToSpawn.SetActive(true);

            return objectToSpawn;
        }

        public void ReturnToPool(GameObject objectToReturn, GameObject originalPrefab)
        {
            if (originalPrefab == null || !_poolDictionary.ContainsKey(originalPrefab))
            {
                Debug.LogWarning($"Pool for '{objectToReturn.name}' doesn't exist. Destroying object.");
                Destroy(objectToReturn);
                return;
            }

            objectToReturn.SetActive(false);
            _poolDictionary[originalPrefab].Enqueue(objectToReturn);
        }

        private GameObject CreateNewInstance(GameObject prefab, bool initiallyInactive = true)
        {
            GameObject newInstance = Instantiate(prefab, transform); // Parent to manager for scene clarity
            if (initiallyInactive)
            {
                newInstance.SetActive(false);
            }
            return newInstance;
        }
    }
}