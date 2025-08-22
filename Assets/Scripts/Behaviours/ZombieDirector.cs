using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ZombieAI
{
    public class AdvancedZombieDirector : MonoBehaviour
    {
        #region Director Singleton
        public static AdvancedZombieDirector Instance { get; private set; }
        #endregion

        #region Nested Classes & Enums
        public enum SpawnMode
        {
            AlwaysActive,
            OnPlayerProximity
        }

        [System.Serializable]
        public class ZombieSpawnInfo
        {
            [AssetsOnly, Required]
            public GameObject ZombiePrefab;
            [Range(1, 100)] public int SpawnWeight = 50;
        }

        [System.Serializable]
        public class SpawnZone
        {
            // SỬA LỖI ODIN: Bỏ FoldoutGroup và thay bằng TitleGroup để cấu trúc rõ ràng hơn.
            // TitleGroup vừa tạo ra một group có viền, vừa có tiêu đề.
            [TitleGroup("$ZoneName", boldTitle: true, horizontalLine: false)]
            [HideLabel, Tooltip("Tên định danh cho Zone này.")]
            public string ZoneName = "New Spawn Zone";

            [TitleGroup("$ZoneName/A. Settings")]
            public SpawnMode SpawnActivationMode = SpawnMode.OnPlayerProximity;

            [TitleGroup("$ZoneName/A. Settings")]
            [ShowIf("SpawnActivationMode", SpawnMode.OnPlayerProximity)]
            [Range(10f, 200f)] public float ActivationRadius = 50f;

            [TitleGroup("$ZoneName/A. Settings")]
            [ShowIf("SpawnActivationMode", SpawnMode.OnPlayerProximity)]
            [ValidateInput("ValidateDeactivationRadius", "Deactivation Radius phải lớn hơn hoặc bằng Activation Radius.")]
            [Range(15f, 250f)] public float DeactivationRadius = 70f;

            [TitleGroup("$ZoneName/B. Population")]
            [Range(1, 100)] public int MaxZombies = 15;

            [TitleGroup("$ZoneName/B. Population")]
            [Range(0.5f, 30f)] public float SpawnInterval = 3f;

            [TitleGroup("$ZoneName/C. Configuration")]
            [Required("Zone phải có một Transform làm tâm điểm.")]
            public Transform ZoneCenter;

            [TitleGroup("$ZoneName/C. Configuration")]
            [Required, MinValue(1)]
            public List<Transform> SpawnPoints;

            [TitleGroup("$ZoneName/D. Zombie Types")]
            [ListDrawerSettings(Expanded = true), Required, MinValue(1)]
            public List<ZombieSpawnInfo> ZombieTypes;

            [HideInInspector] public List<Zombie> ActiveZombies = new List<Zombie>();
            [HideInInspector] public float SpawnTimer;
            [HideInInspector] public int TotalSpawnWeight;

            public void Initialize()
            {
                TotalSpawnWeight = ZombieTypes.Sum(z => z.SpawnWeight);
                ActiveZombies.Clear();
            }

            public GameObject GetRandomZombiePrefab()
            {
                if (TotalSpawnWeight <= 0) return null;
                int randomWeight = Random.Range(0, TotalSpawnWeight);
                int currentWeight = 0;
                foreach (var zombieType in ZombieTypes)
                {
                    currentWeight += zombieType.SpawnWeight;
                    if (randomWeight < currentWeight)
                    {
                        return zombieType.ZombiePrefab;
                    }
                }
                return ZombieTypes.FirstOrDefault()?.ZombiePrefab;
            }

#if UNITY_EDITOR
            private bool ValidateDeactivationRadius(float radius)
            {
                return radius >= this.ActivationRadius;
            }
#endif
        }
        #endregion

        [Title("Director Configuration")]
        [SerializeField, Required] private Transform playerTransform;
        [SerializeField, Range(0.2f, 2f)] private float managementTickRate = 1.0f;
        [SerializeField] private int initialPoolSizePerType = 10;

        [Title("Spawn Zones")]
        [ListDrawerSettings(OnBeginListElementGUI = "BeginDrawListElement", OnEndListElementGUI = "EndDrawListElement")]
        [SerializeField] private List<SpawnZone> spawnZones;

        [Title("Debug Settings")]
        [SerializeField] private bool enableGizmos = true;

#if UNITY_EDITOR
        #region Odin Helper Methods

        private void BeginDrawListElement(int index)
        {
            Sirenix.Utilities.Editor.SirenixEditorGUI.BeginBox(this.spawnZones[index].ZoneName);
        }

        private void EndDrawListElement(int index)
        {
            Sirenix.Utilities.Editor.SirenixEditorGUI.EndBox();
        }
        #endregion
#endif
        // ... (Giữ nguyên các hàm còn lại: Awake, InitializeDirector, Start, v.v.)
        // Mã nguồn các hàm logic không thay đổi

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeDirector();
        }

        private void InitializeDirector()
        {
            if (playerTransform == null)
            {
                var playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null) playerTransform = playerObject.transform;
            }

            foreach (var zone in spawnZones)
            {
                zone.Initialize();
            }
        }

        private void Start()
        {
            if (playerTransform == null)
            {
                Debug.LogError("Player Transform not found. Zombie Director will not function.");
                return;
            }
            if (spawnZones == null || spawnZones.Count == 0)
            {
                Debug.LogError("No Spawn Zones configured. Zombie Director will not function.");
                return;
            }

            InitializePools();
            StartCoroutine(ManageAllZonesCoroutine());
        }

        private void InitializePools()
        {
            var allPrefabs = spawnZones
                .SelectMany(zone => zone.ZombieTypes)
                .Select(type => type.ZombiePrefab)
                .Where(prefab => prefab != null)
                .Distinct()
                .ToList();

            ZombiePoolManager.Instance.CreatePools(allPrefabs, initialPoolSizePerType);
        }

        private IEnumerator ManageAllZonesCoroutine()
        {
            var wait = new WaitForSeconds(managementTickRate);
            while (true)
            {
                foreach (var zone in spawnZones)
                {
                    ProcessZone(zone);
                }
                yield return wait;
            }
        }

        private void ProcessZone(SpawnZone zone)
        {
            bool isZoneActive = IsZoneActive(zone);

            if (!isZoneActive)
            {
                DeactivateAndCullZone(zone);
                return;
            }

            zone.SpawnTimer += managementTickRate;
            if (zone.SpawnTimer >= zone.SpawnInterval && zone.ActiveZombies.Count < zone.MaxZombies)
            {
                zone.SpawnTimer = 0f;
                SpawnZombieInZone(zone);
            }
        }

        public bool IsZoneActive(SpawnZone zone)
        {
            if (zone.SpawnActivationMode == SpawnMode.AlwaysActive) return true;
            if (zone.ZoneCenter == null || playerTransform == null) return false;

            float distanceToPlayerSqr = (playerTransform.position - zone.ZoneCenter.position).sqrMagnitude;

            if (zone.ActiveZombies.Any())
            {
                return distanceToPlayerSqr < zone.DeactivationRadius * zone.DeactivationRadius;
            }

            return distanceToPlayerSqr < zone.ActivationRadius * zone.ActivationRadius;
        }

        private void DeactivateAndCullZone(SpawnZone zone)
        {
            if (zone.ActiveZombies.Count == 0) return;

            var zombiesToReturn = new List<Zombie>(zone.ActiveZombies);
            foreach (var zombie in zombiesToReturn)
            {
                ZombiePoolManager.Instance.ReturnToPool(zombie.gameObject, zombie.OriginalPrefab);
            }
            zone.ActiveZombies.Clear();
        }

        private void SpawnZombieInZone(SpawnZone zone)
        {
            if (zone.SpawnPoints == null || zone.SpawnPoints.Count == 0) return;

            Transform spawnPoint = zone.SpawnPoints[Random.Range(0, zone.SpawnPoints.Count)];
            GameObject prefabToSpawn = zone.GetRandomZombiePrefab();

            if (prefabToSpawn == null || spawnPoint == null) return;

            GameObject zombieInstance = ZombiePoolManager.Instance.SpawnFromPool(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
            if (zombieInstance == null) return;

            var zombieAI = zombieInstance.GetComponent<Zombie>();
            if (zombieAI != null)
            {
                zombieAI.Setup(playerTransform, this, prefabToSpawn, zone);
                zone.ActiveZombies.Add(zombieAI);
            }
        }

        public void OnZombieDied(Zombie zombie, SpawnZone zone)
        {
            if (zone != null && zone.ActiveZombies.Contains(zombie))
            {
                zone.ActiveZombies.Remove(zombie);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!enableGizmos || spawnZones == null) return;

            foreach (var zone in spawnZones)
            {
                if (zone == null || zone.ZoneCenter == null) continue;

                Vector3 center = zone.ZoneCenter.position;
                bool isCurrentlyActive = Application.isPlaying && IsZoneActive(zone);
                Color baseColor = isCurrentlyActive ? new Color(0.1f, 1f, 0.1f) : new Color(1f, 0.5f, 0f);

                if (zone.SpawnActivationMode == SpawnMode.OnPlayerProximity)
                {
                    Handles.color = new Color(1f, 0.2f, 0.2f, 0.1f);
                    Handles.DrawSolidDisc(center, Vector3.up, zone.DeactivationRadius);
                    Handles.color = new Color(1f, 0.2f, 0.2f, 0.7f);
                    Handles.DrawWireDisc(center, Vector3.up, zone.DeactivationRadius);

                    Handles.color = new Color(0.2f, 1f, 0.2f, 0.15f);
                    Handles.DrawSolidDisc(center, Vector3.up, zone.ActivationRadius);
                    Handles.color = new Color(0.2f, 1f, 0.2f, 0.8f);
                    Handles.DrawWireDisc(center, Vector3.up, zone.ActivationRadius);
                }

                Gizmos.color = baseColor;
                if (zone.SpawnPoints != null)
                {
                    foreach (var point in zone.SpawnPoints)
                    {
                        if (point == null) continue;
                        Gizmos.DrawCube(point.position, Vector3.one * 0.5f);
                        Gizmos.DrawLine(center, point.position);
                    }
                }

                string statusText = "";
                if (Application.isPlaying)
                {
                    statusText = isCurrentlyActive
                        ? $"<color=green>ACTIVE</color>\nZombies: {zone.ActiveZombies.Count} / {zone.MaxZombies}"
                        : $"<color=orange>INACTIVE</color>";
                }

                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.white;
                style.alignment = TextAnchor.MiddleCenter;
                style.fontStyle = FontStyle.Bold;
                style.richText = true;

                string label = $"-- {zone.ZoneName} --\n{statusText}";
                Handles.Label(center + Vector3.up * 2f, label, style);
            }
        }
#endif
    }
}