using UnityEngine;
using System.Collections.Generic;

namespace ZombieAI
{
    public class ZombiePoolManager : MonoBehaviour
    {
        // Singleton Pattern
        public static ZombiePoolManager Instance { get; private set; }

        private Dictionary<string, Queue<GameObject>> _poolDictionary;

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
            }
        }

        // ZombieDirector sẽ gọi hàm này lúc khởi động
        public void InitializePools(List<ZombieDirector.ZombieSpawnInfo> zombieTypes, int initialSize)
        {
            foreach (var type in zombieTypes)
            {
                if (!_poolDictionary.ContainsKey(type.ZombiePrefab.name))
                {
                    Queue<GameObject> objectPool = new Queue<GameObject>();
                    for (int i = 0; i < initialSize; i++)
                    {
                        GameObject obj = Instantiate(type.ZombiePrefab);
                        obj.name = type.ZombiePrefab.name; // Gán tên để nhận dạng
                        obj.SetActive(false);
                        objectPool.Enqueue(obj);
                    }
                    _poolDictionary.Add(type.ZombiePrefab.name, objectPool);
                }
            }
        }

        public GameObject SpawnFromPool(string prefabName, Vector3 position, Quaternion rotation)
        {
            if (!_poolDictionary.ContainsKey(prefabName))
            {
                Debug.LogWarning($"Pool with name '{prefabName}' doesn't exist.");
                return null;
            }

            Queue<GameObject> poolQueue = _poolDictionary[prefabName];

            // Nếu hết đối tượng trong pool, tạo mới (để tránh lỗi)
            if (poolQueue.Count == 0)
            {
                // Tìm lại prefab gốc để instantiate
                // Đây là một cách đơn giản, cách tốt hơn là lưu trữ prefab gốc trong một dictionary khác.
                // Tuy nhiên, với hệ thống hiện tại, việc này không xảy ra thường xuyên.
                Debug.LogWarning($"Pool '{prefabName}' ran out of objects. Instantiating a new one.");
                // For now, we will not dynamically grow the pool to keep it simple.
                // A better implementation would find the original prefab and instantiate it.
                return null;
            }

            GameObject objectToSpawn = poolQueue.Dequeue();

            objectToSpawn.SetActive(true);
            objectToSpawn.transform.position = position;
            objectToSpawn.transform.rotation = rotation;

            return objectToSpawn;
        }

        public void ReturnToPool(string prefabName, GameObject objectToReturn)
        {
            if (!_poolDictionary.ContainsKey(prefabName))
            {
                Debug.LogWarning($"Pool with name '{prefabName}' doesn't exist. Destroying object.");
                Destroy(objectToReturn);
                return;
            }

            objectToReturn.SetActive(false);
            _poolDictionary[prefabName].Enqueue(objectToReturn);
        }
    }
}