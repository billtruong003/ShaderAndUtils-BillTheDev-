using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ZombieAI
{
    public class ZombieDirector : MonoBehaviour
    {
        [System.Serializable]
        public class ZombieSpawnInfo
        {
            public GameObject ZombiePrefab;
            [Range(1, 100)] public int SpawnWeight = 50;
        }

        [Header("Spawning Configuration")]
        [SerializeField] private List<ZombieSpawnInfo> zombieTypes;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float spawnInterval = 5f;
        [SerializeField] private int maxZombies = 30;

        [Header("Object Pooling")]
        [Tooltip("Số lượng của mỗi loại Zombie sẽ được tạo sẵn khi bắt đầu game.")]
        [SerializeField] private int poolInitialSize = 10;

        [Header("Target")]
        [SerializeField] private Transform playerTransform;

        private int _currentZombieCount = 0;
        private bool _isDirectorReady = false;
        private int _totalSpawnWeight;

        private void Awake()
        {
            ValidateSettings();
            CalculateTotalWeight();
        }

        private void Start()
        {
            if (_isDirectorReady)
            {
                // Yêu cầu Pool Manager tạo sẵn các Zombie khi game bắt đầu
                ZombiePoolManager.Instance.InitializePools(zombieTypes, poolInitialSize);
                StartCoroutine(SpawnHorde());
            }
        }

        private void ValidateSettings()
        {
            if (playerTransform == null)
            {
                var playerObject = GameObject.FindGameObjectWithTag("Player");
                playerTransform = playerObject != null ? playerObject.transform : null;
            }

            if (playerTransform == null)
            {
                Debug.LogError("Player Transform not found. Director is disabled.", this);
                return;
            }

            if (zombieTypes == null || zombieTypes.Count == 0)
            {
                Debug.LogError("Zombie Types list is empty. Director is disabled.", this);
                return;
            }

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogError("Spawn Points are not assigned. Director is disabled.", this);
                return;
            }

            _isDirectorReady = true;
        }

        private void CalculateTotalWeight()
        {
            _totalSpawnWeight = 0;
            foreach (var zombieType in zombieTypes)
            {
                _totalSpawnWeight += zombieType.SpawnWeight;
            }
        }

        private IEnumerator SpawnHorde()
        {
            while (true)
            {
                yield return new WaitForSeconds(spawnInterval);
                if (_currentZombieCount < maxZombies)
                {
                    SpawnZombie();
                }
            }
        }

        private void SpawnZombie()
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject prefabToSpawn = GetRandomZombiePrefab();

            // Lấy Zombie từ Pool thay vì Instantiate
            GameObject zombieInstance = ZombiePoolManager.Instance.SpawnFromPool(prefabToSpawn.name, spawnPoint.position, spawnPoint.rotation);

            // Nếu pool rỗng hoặc có lỗi, không thực hiện tiếp
            if (zombieInstance == null) return;

            var zombieAI = zombieInstance.GetComponent<Zombie>();
            if (zombieAI != null)
            {
                // Reset lại trạng thái của Zombie để tái sử dụng
                zombieAI.Initialize(playerTransform, this);
                zombieAI.SetAnchorPoint(spawnPoint.position);
            }

            _currentZombieCount++;
        }

        private GameObject GetRandomZombiePrefab()
        {
            int randomWeight = Random.Range(0, _totalSpawnWeight);
            int currentWeight = 0;

            foreach (var zombieType in zombieTypes)
            {
                currentWeight += zombieType.SpawnWeight;
                if (randomWeight < currentWeight)
                {
                    return zombieType.ZombiePrefab;
                }
            }
            // Fallback an toàn, trả về loại đầu tiên nếu có lỗi
            return zombieTypes[0].ZombiePrefab;
        }

        public void OnZombieDied()
        {
            _currentZombieCount = Mathf.Max(0, _currentZombieCount - 1);
        }
    }
}