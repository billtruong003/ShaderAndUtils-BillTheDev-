// File: Assets/Shaders/VAT/FinalizeForm/FishBoidsController.cs
using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Random = UnityEngine.Random;
using ReadOnly = Unity.Collections.ReadOnlyAttribute;

namespace OptimizeVariousVAT
{
    [BurstCompile]
    public class FishBoidsController : MonoBehaviour
    {
        #region Structs
        [BurstCompile]
        private struct AgentData
        {
            public float3 position;
            public quaternion rotation;
            public float3 velocity;
            public float3 acceleration;

            public float maxSpeed;
            public float perceptionRadius;
            public float avoidanceRadius;
            public int typeIndex;
        }

        [BurstCompile]
        private struct ObstacleData
        {
            public float3 position;
            public float radius;
        }
        #endregion

        #region Public Properties for UI Control
        public int AgentCount { get => _agentCount; set => _agentCount = Mathf.Clamp(value, 1, MaxAgentCount); }
        public int MaxAgentCount => 50000;
        public Vector3 SpawnBounds { get => _spawnBounds; set => _spawnBounds = value; }
        public float NeighborRadius { get => _neighborRadius; set => _neighborRadius = value; }
        public float MinSpeed { get => _minSpeed; set => _minSpeed = value; }
        public float MaxSpeed { get => _maxSpeed; set => _maxSpeed = value; }
        public float MaxSteerForce { get => _maxSteerForce; set => _maxSteerForce = value; }
        public float SeparationWeight { get => _separationWeight; set => _separationWeight = value; }
        public float AlignmentWeight { get => _alignmentWeight; set => _alignmentWeight = value; }
        public float CohesionWeight { get => _cohesionWeight; set => _cohesionWeight = value; }
        public float BoundsWeight { get => _boundsWeight; set => _boundsWeight = value; }
        public float TargetWeight { get => _targetWeight; set => _targetWeight = value; }
        public float ObstacleAvoidanceWeight { get => _obstacleAvoidanceWeight; set => _obstacleAvoidanceWeight = value; }
        public float WanderWeight { get => _wanderWeight; set => _wanderWeight = value; }
        #endregion

        #region Inspector Fields
        [TitleGroup("Spawning & Bounds")]
        [SerializeField, Range(1, 50000)] private int _agentCount = 1000;
        [SerializeField, Required] private VAT_InstanceManager _instanceManager;
        [SerializeField] private Vector3 _spawnBounds = new Vector3(100, 50, 100);
        [SerializeField] private Vector3 _modelRotationOffset = new Vector3(0, -90, 0);

        [TitleGroup("Obstacles")]
        [SerializeField] private LayerMask _obstacleLayer;
        [SerializeField, Range(1f, 20f)] private float _obstacleDetectionRadius = 10f;

        [TitleGroup("Performance")]
        [SerializeField, ReadOnly] private SimulationMode _simulationMode = SimulationMode.JobSystem;
        private enum SimulationMode { JobSystem }

        [TitleGroup("Boids Behavior - Main")]
        [SerializeField, Range(1f, 20f)] private float _neighborRadius = 7f;
        [SerializeField, Range(0.1f, 10f)] private float _minSpeed = 2f;
        [SerializeField, Range(1f, 30f)] private float _maxSpeed = 12f;
        [SerializeField, Range(1f, 50f)] private float _maxSteerForce = 25f;

        [TitleGroup("Boids Behavior - Weights")]
        [SerializeField, Range(0f, 10f)] private float _separationWeight = 3.5f;
        [SerializeField, Range(0f, 10f)] private float _alignmentWeight = 2.0f;
        [SerializeField, Range(0f, 10f)] private float _cohesionWeight = 2.5f;
        [SerializeField, Range(0f, 10f)] private float _wanderWeight = 1.0f;
        [SerializeField, Range(0f, 10f)] private float _boundsWeight = 4.0f;
        [SerializeField, Range(0f, 10f)] private float _obstacleAvoidanceWeight = 5.0f;
        [SerializeField, Range(0f, 10f)] private float _targetWeight = 1.0f;

        [TitleGroup("Animation Control")]
        [SerializeField, Required] private string _swimClip = "swim";
        [SerializeField, Range(0.1f, 1f)] private float _animationFadeDuration = 0.5f;

        [TitleGroup("Target")]
        [SerializeField] private Transform _target;
        #endregion

        #region Private Fields
        private readonly List<VAT_BoidsAgent> _agentComponents = new List<VAT_BoidsAgent>();
        private NativeArray<AgentData> _jobAgents;
        private NativeArray<ObstacleData> _jobObstacles;
        private NativeParallelHashMap<int, int> _gridMap;
        private NativeArray<int> _gridNext;
        private JobHandle _simulationJobHandle;
        private float3 _spawnCenter;
        private quaternion _rotationOffsetQuaternion;
        private readonly Collider[] _obstacleBuffer = new Collider[100];
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            _spawnCenter = transform.position;
            _rotationOffsetQuaternion = quaternion.Euler(math.radians(_modelRotationOffset));
            InitializeSimulation();
        }

        private void Update()
        {
            _simulationJobHandle.Complete();
            ApplyTransforms();
            ScheduleNextSimulation(Time.deltaTime);
        }

        private void OnDestroy()
        {
            _simulationJobHandle.Complete();
            CleanupNativeArrays();
            CleanupAgentObjects();
        }
        #endregion

        #region Initialization and Cleanup
        [Button(ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
        private void RespawnAgents()
        {
            CleanupAndReinitialize();
        }

        private void InitializeSimulation()
        {
            if (!ValidateDependencies()) return;
            UpdateObstacles();
            InitializeNativeArrays();
            SpawnAgents();
        }

        private void CleanupAndReinitialize()
        {
            _simulationJobHandle.Complete();
            CleanupNativeArrays();
            CleanupAgentObjects();
            _rotationOffsetQuaternion = quaternion.Euler(math.radians(_modelRotationOffset));
            InitializeSimulation();
        }

        private bool ValidateDependencies() => _instanceManager != null && _instanceManager.IsInitialized && _instanceManager.GetAgentTypeCount() > 0;

        private void UpdateObstacles()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, _obstacleDetectionRadius, _obstacleBuffer, _obstacleLayer);

            if (_jobObstacles.IsCreated) _jobObstacles.Dispose();
            _jobObstacles = new NativeArray<ObstacleData>(count, Allocator.Persistent);

            for (int i = 0; i < count; i++)
            {
                var col = _obstacleBuffer[i];
                _jobObstacles[i] = new ObstacleData
                {
                    position = col.transform.position,
                    radius = col.bounds.extents.x
                };
            }
        }

        private void InitializeNativeArrays()
        {
            _jobAgents = new NativeArray<AgentData>(_agentCount, Allocator.Persistent);
            _gridMap = new NativeParallelHashMap<int, int>(_agentCount * 2, Allocator.Persistent);
            _gridNext = new NativeArray<int>(_agentCount, Allocator.Persistent);
        }

        private void CleanupNativeArrays()
        {
            if (_jobAgents.IsCreated) _jobAgents.Dispose();
            if (_jobObstacles.IsCreated) _jobObstacles.Dispose();
            if (_gridMap.IsCreated) _gridMap.Dispose();
            if (_gridNext.IsCreated) _gridNext.Dispose();
        }

        private void CleanupAgentObjects()
        {
            foreach (var agent in _agentComponents)
            {
                if (agent != null) Destroy(agent.gameObject);
            }
            _agentComponents.Clear();
        }

        private void SpawnAgents()
        {
            int agentTypeCount = _instanceManager.GetAgentTypeCount();
            for (int i = 0; i < _agentCount; i++)
            {
                int typeIndex = Random.Range(0, agentTypeCount);
                var prefab = _instanceManager.GetAgentPrefab(typeIndex);
                var position = GetRandomPositionInBounds();
                var rotation = Random.rotation;

                var agentInstance = Instantiate(prefab, position, rotation, transform);
                var agentComponent = agentInstance.GetComponent<VAT_BoidsAgent>();

                agentComponent.Initialize(_instanceManager);
                agentComponent.AgentTypeIndex = typeIndex;
                agentInstance.gameObject.SetActive(true);
                agentComponent.CrossFade(_swimClip, _animationFadeDuration);
                _agentComponents.Add(agentComponent);

                _jobAgents[i] = new AgentData
                {
                    position = position,
                    rotation = rotation,
                    velocity = math.forward(rotation) * Random.Range(_minSpeed, _maxSpeed),
                    maxSpeed = Random.Range(_minSpeed, _maxSpeed),
                    perceptionRadius = Random.Range(_neighborRadius * 0.8f, _neighborRadius * 1.2f),
                    avoidanceRadius = Random.Range(_neighborRadius * 0.4f, _neighborRadius * 0.6f),
                    typeIndex = typeIndex
                };
            }
        }
        #endregion

        #region Simulation Logic
        private void ScheduleNextSimulation(float deltaTime)
        {
            var populateGridJob = new PopulateGridJob
            {
                agents = _jobAgents.AsReadOnly(),
                gridMap = _gridMap,
                gridNext = _gridNext,
                inverseCellSize = 1f / _neighborRadius
            };
            var populateHandle = populateGridJob.Schedule();

            var simulationJob = new SimulationJob
            {
                agents = _jobAgents,
                obstacles = _jobObstacles.AsReadOnly(),
                gridMap = _gridMap.AsReadOnly(),
                gridNext = _gridNext.AsReadOnly(),
                deltaTime = deltaTime,
                minSpeed = _minSpeed,
                maxSteerForce = _maxSteerForce,
                separationWeight = _separationWeight,
                alignmentWeight = _alignmentWeight,
                cohesionWeight = _cohesionWeight,
                wanderWeight = _wanderWeight,
                obstacleAvoidanceWeight = _obstacleAvoidanceWeight,
                boundsWeight = _boundsWeight,
                targetWeight = _targetWeight,
                spawnCenter = _spawnCenter,
                spawnBounds = _spawnBounds,
                inverseCellSize = 1f / _neighborRadius,
                targetPosition = _target != null ? (float3)_target.position : new float3(float.PositiveInfinity),
                seed = (uint)Random.Range(1, int.MaxValue)
            };

            _simulationJobHandle = simulationJob.Schedule(_agentCount, 64, populateHandle);
            JobHandle.ScheduleBatchedJobs();
        }

        private void ApplyTransforms()
        {
            for (int i = 0; i < _agentCount; i++)
            {
                var agentTransform = _agentComponents[i].transform;
                var agentData = _jobAgents[i];

                agentTransform.SetPositionAndRotation(agentData.position, math.mul(agentData.rotation, _rotationOffsetQuaternion));
            }
        }
        #endregion

        #region Helper Methods & Gizmos
        private float3 GetRandomPositionInBounds()
        {
            return _spawnCenter + new float3(
                Random.Range(-_spawnBounds.x * 0.5f, _spawnBounds.x * 0.5f),
                Random.Range(-_spawnBounds.y * 0.5f, _spawnBounds.y * 0.5f),
                Random.Range(-_spawnBounds.z * 0.5f, _spawnBounds.z * 0.5f));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.5f);
            Gizmos.DrawWireCube(transform.position, _spawnBounds);

            if (_jobObstacles.IsCreated)
            {
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
                foreach (var obstacle in _jobObstacles)
                {
                    Gizmos.DrawSphere(obstacle.position, obstacle.radius);
                }
            }
        }
        #endregion

        #region Jobs
        [BurstCompile]
        private struct PopulateGridJob : IJob
        {
            [ReadOnly] public NativeArray<AgentData>.ReadOnly agents;
            public NativeParallelHashMap<int, int> gridMap;
            [WriteOnly] public NativeArray<int> gridNext;
            public float inverseCellSize;
            private const int NO_AGENT = -1;

            private int GetCellHash(float3 pos)
            {
                return (int)(math.floor(pos.x * inverseCellSize) * 73856093) ^
                       (int)(math.floor(pos.y * inverseCellSize) * 19349663) ^
                       (int)(math.floor(pos.z * inverseCellSize) * 83492791);
            }

            public void Execute()
            {
                gridMap.Clear();
                for (int i = 0; i < agents.Length; i++)
                {
                    var hash = GetCellHash(agents[i].position);
                    if (gridMap.TryGetValue(hash, out int head))
                    {
                        gridNext[i] = head;
                    }
                    else
                    {
                        gridNext[i] = NO_AGENT;
                    }
                    gridMap[hash] = i;
                }
            }
        }

        [BurstCompile]
        private struct SimulationJob : IJobParallelFor
        {
            private const int NO_AGENT = -1;

            public NativeArray<AgentData> agents;
            [ReadOnly] public NativeArray<ObstacleData>.ReadOnly obstacles;
            [ReadOnly] public NativeParallelHashMap<int, int>.ReadOnly gridMap;
            [ReadOnly] public NativeArray<int>.ReadOnly gridNext;

            public float deltaTime;
            public float minSpeed;
            public float maxSteerForce;
            public float separationWeight;
            public float alignmentWeight;
            public float cohesionWeight;
            public float wanderWeight;
            public float obstacleAvoidanceWeight;
            public float boundsWeight;
            public float targetWeight;
            public float3 spawnCenter;
            public float3 spawnBounds;
            public float inverseCellSize;
            public float3 targetPosition;
            public uint seed;

            public void Execute(int index)
            {
                var agent = agents[index];
                var random = new Unity.Mathematics.Random(seed + (uint)index);
                agent.acceleration = float3.zero;

                // --- Calculate Boids Forces ---
                var forces = CalculateBoidsForces(index, ref agent);

                // --- Calculate Other Forces ---
                if (boundsWeight > 0) forces += CalculateBoundsForce(ref agent) * boundsWeight;
                if (obstacleAvoidanceWeight > 0) forces += CalculateObstacleAvoidanceForce(ref agent) * obstacleAvoidanceWeight;
                if (targetWeight > 0 && targetPosition.x != float.PositiveInfinity) forces += Seek(agent.position, targetPosition, agent.velocity, agent.maxSpeed) * targetWeight;
                if (wanderWeight > 0) forces += Wander(ref agent, ref random) * wanderWeight;

                // --- Apply Forces & Integrate ---
                agent.acceleration += forces;
                agent.velocity += agent.acceleration * deltaTime;

                float speed = math.length(agent.velocity);
                float3 dir = agent.velocity / speed;
                speed = math.clamp(speed, minSpeed, agent.maxSpeed);
                agent.velocity = dir * speed;

                agent.position += agent.velocity * deltaTime;
                agent.rotation = quaternion.LookRotationSafe(dir, math.up());

                agents[index] = agent;
            }

            private float3 CalculateBoidsForces(int index, ref AgentData agent)
            {
                float3 separationSum = float3.zero;
                float3 alignmentSum = float3.zero;
                float3 cohesionSum = float3.zero;
                int neighborsCount = 0;
                int avoidanceCount = 0;

                int3 gridPos = (int3)math.floor(agent.position * inverseCellSize);

                for (int x = -1; x <= 1; x++)
                    for (int y = -1; y <= 1; y++)
                        for (int z = -1; z <= 1; z++)
                        {
                            int3 cell = gridPos + new int3(x, y, z);
                            var hash = (cell.x * 73856093) ^ (cell.y * 19349663) ^ (cell.z * 83492791);
                            if (gridMap.TryGetValue(hash, out int current))
                            {
                                while (current != NO_AGENT)
                                {
                                    if (index != current)
                                    {
                                        var neighbor = agents[current];
                                        float distSqr = math.distancesq(agent.position, neighbor.position);
                                        if (distSqr < agent.perceptionRadius * agent.perceptionRadius)
                                        {
                                            if (distSqr < agent.avoidanceRadius * agent.avoidanceRadius)
                                            {
                                                separationSum += (agent.position - neighbor.position) / distSqr;
                                                avoidanceCount++;
                                            }
                                            alignmentSum += neighbor.velocity;
                                            cohesionSum += neighbor.position;
                                            neighborsCount++;
                                        }
                                    }
                                    current = gridNext[current];
                                }
                            }
                        }

                float3 totalForce = float3.zero;
                if (neighborsCount > 0)
                {
                    if (avoidanceCount > 0 && separationWeight > 0) totalForce += Steer(separationSum / avoidanceCount, agent.velocity, agent.maxSpeed) * separationWeight;
                    if (alignmentWeight > 0) totalForce += Steer(alignmentSum / neighborsCount, agent.velocity, agent.maxSpeed) * alignmentWeight;
                    if (cohesionWeight > 0) totalForce += Seek(agent.position, cohesionSum / neighborsCount, agent.velocity, agent.maxSpeed) * cohesionWeight;
                }
                return totalForce;
            }

            private float3 CalculateBoundsForce(ref AgentData agent)
            {
                float3 halfBounds = spawnBounds * 0.5f;
                float3 offset = agent.position - spawnCenter;
                float3 desired = agent.velocity;
                const float turnMargin = 0.9f;

                if (math.abs(offset.x) > halfBounds.x * turnMargin) desired.x = -math.sign(offset.x) * agent.maxSpeed;
                if (math.abs(offset.y) > halfBounds.y * turnMargin) desired.y = -math.sign(offset.y) * agent.maxSpeed;
                if (math.abs(offset.z) > halfBounds.z * turnMargin) desired.z = -math.sign(offset.z) * agent.maxSpeed;

                return Steer(desired, agent.velocity, agent.maxSpeed);
            }

            private float3 CalculateObstacleAvoidanceForce(ref AgentData agent)
            {
                float3 avoidanceForce = float3.zero;
                float3 ahead = agent.position + math.normalize(agent.velocity) * agent.avoidanceRadius;

                for (int i = 0; i < obstacles.Length; i++)
                {
                    float distSqr = math.distancesq(ahead, obstacles[i].position);
                    if (distSqr < (obstacles[i].radius + agent.avoidanceRadius) * (obstacles[i].radius + agent.avoidanceRadius))
                    {
                        float3 away = ahead - obstacles[i].position;
                        avoidanceForce += math.normalize(away) * agent.maxSpeed;
                    }
                }
                return Steer(avoidanceForce, agent.velocity, agent.maxSpeed);
            }

            private float3 Wander(ref AgentData agent, ref Unity.Mathematics.Random random)
            {
                float3 wanderCircleCenter = agent.position + math.normalize(agent.velocity) * 2f;
                float3 randomPoint = random.NextFloat3Direction() * 1.5f;
                float3 target = wanderCircleCenter + randomPoint;
                return Seek(agent.position, target, agent.velocity, agent.maxSpeed);
            }

            private float3 Seek(float3 currentPos, float3 targetPos, float3 currentVel, float maxSpeed)
            {
                float3 desired = math.normalize(targetPos - currentPos) * maxSpeed;
                return Steer(desired, currentVel, maxSpeed);
            }

            private float3 Steer(float3 desired, float3 currentVelocity, float maxSpeed)
            {
                float3 steer = desired - currentVelocity;
                if (math.lengthsq(steer) > maxSteerForce * maxSteerForce)
                {
                    steer = math.normalize(steer) * maxSteerForce;
                }
                return steer;
            }
        }
        #endregion
    }
}