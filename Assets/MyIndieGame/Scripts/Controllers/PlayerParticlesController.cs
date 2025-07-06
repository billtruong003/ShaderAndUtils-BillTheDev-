// File: Assets/MyIndieGame/Scripts/Controllers/PlayerParticlesController.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerParticlesController : MonoBehaviour
{
    public enum PlayerParticleType
    {
        Jump,
        Impact,
        Slice,
        Shoot
    }

    [System.Serializable]
    public class ParticlePool
    {
        public PlayerParticleType type;
        public GameObject particlePrefab;
        public int poolSize = 5;
        public Transform container;
        public Vector3 positionOffset; // <-- ĐÃ THÊM
    }

    [Header("Particle Pools")]
    [SerializeField] private List<ParticlePool> particlePools;

    [Header("Special Particles (Controlled by Update)")]
    [Tooltip("Hiệu ứng bụi liên tục dưới chân khi chạy. Sẽ được quản lý riêng.")]
    [SerializeField] private ParticleSystem dustTrailParticle;

    [Header("Dependencies")]
    [SerializeField] private PlayerLocomotion playerLocomotion;
    [SerializeField] private CharacterController characterController;

    private Dictionary<PlayerParticleType, Queue<ParticleSystem>> _poolDictionary;
    private Dictionary<ParticleSystem, PlayerParticleType> _particleTypeMap;

    private void Awake()
    {
        InitializePools();
    }

    private void InitializePools()
    {
        _poolDictionary = new Dictionary<PlayerParticleType, Queue<ParticleSystem>>();
        _particleTypeMap = new Dictionary<ParticleSystem, PlayerParticleType>();

        foreach (var pool in particlePools)
        {
            if (pool.particlePrefab == null)
            {
                Debug.LogWarning($"Particle prefab for type '{pool.type}' is not assigned.");
                continue;
            }

            if (pool.container == null)
            {
                pool.container = new GameObject($"{pool.type} Particle Container").transform;
                pool.container.SetParent(this.transform);
            }

            Queue<ParticleSystem> objectPool = new Queue<ParticleSystem>();
            for (int i = 0; i < pool.poolSize; i++)
            {
                ParticleSystem ps = CreateNewParticleForPool(pool.type, pool.particlePrefab, pool.container);
                objectPool.Enqueue(ps);
            }
            _poolDictionary[pool.type] = objectPool;
        }

        if (dustTrailParticle != null)
        {
            var emission = dustTrailParticle.emission;
            emission.enabled = false;
        }
    }

    private ParticleSystem CreateNewParticleForPool(PlayerParticleType type, GameObject prefab, Transform container)
    {
        GameObject obj = Instantiate(prefab, container);
        ParticleSystem ps = obj.GetComponent<ParticleSystem>();

        _particleTypeMap[ps] = type;

        var main = ps.main;
        main.stopAction = ParticleSystemStopAction.Callback;

        var trigger = obj.AddComponent<ParticleSystemCallbacks>();
        trigger.OnParticleSystemStoppedEvent += ReturnToPool;

        obj.SetActive(false);
        return ps;
    }

    private void Update()
    {
        HandleDustTrail();
    }

    public void PlayParticle(PlayerParticleType type, Vector3 position, Quaternion? rotation = null)
    {
        if (!_poolDictionary.ContainsKey(type)) return;

        Queue<ParticleSystem> currentPool = _poolDictionary[type];
        ParticleSystem particleToPlay;
        var poolConfig = particlePools.Find(p => p.type == type);
        if (poolConfig == null) return;

        if (currentPool.Count > 0)
        {
            particleToPlay = currentPool.Dequeue();
        }
        else
        {
            Debug.LogWarning($"Pool for particle type '{type}' was empty. A new instance was created.");
            particleToPlay = CreateNewParticleForPool(type, poolConfig.particlePrefab, poolConfig.container);
        }

        Quaternion finalRotation = rotation ?? Quaternion.identity;
        Vector3 finalPosition = position + finalRotation * poolConfig.positionOffset;

        particleToPlay.transform.SetPositionAndRotation(finalPosition, finalRotation);
        particleToPlay.gameObject.SetActive(true);
        particleToPlay.Play();
    }

    private void ReturnToPool(ParticleSystem particleSystem)
    {
        particleSystem.gameObject.SetActive(false);
        if (_particleTypeMap.TryGetValue(particleSystem, out PlayerParticleType type))
        {
            _poolDictionary[type].Enqueue(particleSystem);
        }
    }

    private void HandleDustTrail()
    {
        if (dustTrailParticle == null) return;

        bool shouldShowDust = playerLocomotion.IsGrounded() && characterController.velocity.sqrMagnitude > 0.1f;
        var emission = dustTrailParticle.emission;

        if (shouldShowDust != emission.enabled)
        {
            emission.enabled = shouldShowDust;
        }
    }

    private class ParticleSystemCallbacks : MonoBehaviour
    {
        public System.Action<ParticleSystem> OnParticleSystemStoppedEvent;
        private ParticleSystem _particleSystem;

        private void Awake() => _particleSystem = GetComponent<ParticleSystem>();
        private void OnParticleSystemStopped() => OnParticleSystemStoppedEvent?.Invoke(_particleSystem);
    }
}