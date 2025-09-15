using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Random = UnityEngine.Random;
using System.Collections;

namespace OptimizeVariousVAT
{
    public class CrowdController : MonoBehaviour
    {
        public enum UpdateMode { EveryFrame, ViaCoroutine }
        public enum SimulationMode { JobSystem, Managed }

        [BurstCompile]
        private struct AgentData
        {
            public float3 position;
            public float3 velocity;
            public float3 acceleration;
            public float3 targetPosition;
            public AgentBehaviorState currentState;
            public float stateTimer;
            public int typeIndex;
        }

        private enum AgentBehaviorState { Idle, Walking, Running }

        [TitleGroup("Performance & Mode")]
        [SerializeField] private int targetFrame = 240;
        [SerializeField] private UpdateMode _updateMode = UpdateMode.EveryFrame;
        [SerializeField] private SimulationMode _simulationMode = SimulationMode.JobSystem;
        [SerializeField, ShowIf("_updateMode", UpdateMode.ViaCoroutine), Range(0.01f, 1f)]
        private float _coroutineUpdateInterval = 0.033f;

        [TitleGroup("Spawning")]
        [SerializeField, Range(1, 50000)] private int _initialAgentCount = 500;
        [SerializeField, Required] private VAT_InstanceManager _instanceManager;
        [SerializeField] private Vector3 _spawnBounds = new Vector3(100, 0, 100);

        [TitleGroup("Movement Settings")]
        [SerializeField] private float _walkSpeed = 3.5f;
        [SerializeField] private float _runSpeed = 8.0f;
        [SerializeField, Range(1f, 100f)] private float _maxSteerForce = 30f;
        [SerializeField, Range(0.1f, 5f)] private float _destinationReachedThreshold = 1f;

        [TitleGroup("Behavior Settings")]
        [SerializeField, Range(0f, 50f)] private float _separationRadius = 3f;
        [SerializeField, Range(0f, 10f)] private float _separationWeight = 2.5f;
        [SerializeField, Range(0f, 10f)] private float _goalSeekingWeight = 1f;

        [TitleGroup("State Durations")]
        [SerializeField, MinMaxSlider(1f, 20f, true)] private Vector2 _idleDurationRange = new Vector2(2f, 8f);
        [SerializeField, Range(0f, 1f)] private float _chanceToRunInsteadOfWalk = 0.15f;

        [TitleGroup("Animation Control")]
        [SerializeField, Required] private string _idleClip = "idle";
        [SerializeField, Required] private string _walkClip = "walk";
        [SerializeField, Required] private string _runClip = "run";
        [SerializeField, Range(0.1f, 1f)] private float _animationFadeDuration = 0.3f;

        public int CurrentAgentCount => _activeAgentCount;
        public int MaxAgentCount => 50000;
        public UpdateMode CurrentUpdateMode
        {
            get => _updateMode;
            set { if (_updateMode == value) return; OnDisable(); _updateMode = value; OnEnable(); }
        }
        public SimulationMode CurrentSimulationMode
        {
            get => _simulationMode;
            set { if (_simulationMode == value) return; _simulationJobHandle.Complete(); _simulationMode = value; ApplyAgentCount(CurrentAgentCount, true); }
        }
        public float CoroutineUpdateInterval { get => _coroutineUpdateInterval; set => _coroutineUpdateInterval = value; }
        public Vector3 SpawnBounds { get => _spawnBounds; set => _spawnBounds = value; }
        public float WalkSpeed { get => _walkSpeed; set => _walkSpeed = value; }
        public float RunSpeed { get => _runSpeed; set => _runSpeed = value; }
        public float DestinationReachedThreshold { get => _destinationReachedThreshold; set => _destinationReachedThreshold = value; }

        private readonly List<VAT_BoidsAgent> _agentPool = new List<VAT_BoidsAgent>();
        private int _activeAgentCount = 0;
        private float _planeYPosition;
        private Coroutine _simulationCoroutine;

        private List<AgentData> _managedAgentsBufferA;
        private List<AgentData> _managedAgentsBufferB;
        private List<AgentData> _currentManagedAgents;
        private List<AgentData> _nextManagedAgents;
        private SpatialGridManaged _managedGrid;

        private NativeArray<AgentData> _jobAgents;
        private NativeParallelHashMap<int, int> _gridMap;
        private NativeArray<int> _gridNext;
        private JobHandle _simulationJobHandle;

        private WaitForSeconds _coroutineWait;
        private float _lastCoroutineInterval;

        private void Awake()
        {
            Application.targetFrameRate = targetFrame;
        }

        private void Start()
        {
            _planeYPosition = transform.position.y;
            StartCoroutine(InitializeCrowd());
        }

        private IEnumerator InitializeCrowd()
        {
            while (_instanceManager == null || !_instanceManager.IsInitialized)
            {
                yield return null;
            }
            ApplyAgentCount(_initialAgentCount, true);
        }

        private void OnEnable()
        {
            if (Application.isPlaying && _updateMode == UpdateMode.ViaCoroutine && _simulationCoroutine == null)
            {
                _simulationCoroutine = StartCoroutine(SimulationCoroutine());
            }
        }

        private void OnDisable()
        {
            if (_simulationCoroutine != null)
            {
                StopCoroutine(_simulationCoroutine);
                _simulationCoroutine = null;
            }
            _simulationJobHandle.Complete();
        }

        private void Update()
        {
            if (_updateMode == UpdateMode.EveryFrame)
            {
                RunSimulationStep(Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            _simulationJobHandle.Complete();
            DisposeNativeArrays();
            DestroyAgentPool();
        }

        [Button(ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
        public void RespawnActiveAgents()
        {
            _simulationJobHandle.Complete();
            ApplyAgentCount(_activeAgentCount, true);
        }

        public void ApplyAgentCount(int newCount, bool forceReset = false)
        {
            if (_instanceManager == null || _instanceManager.GetAgentTypeCount() == 0) return;

            int newAgentCount = Mathf.Clamp(newCount, 0, MaxAgentCount);
            int previousAgentCount = _activeAgentCount;
            _activeAgentCount = newAgentCount;

            if (_simulationMode == SimulationMode.JobSystem)
            {
                EnsureNativeArrayCapacity(_activeAgentCount);
            }
            else
            {
                EnsureManagedBufferCapacity(_activeAgentCount);
            }

            EnsurePoolCapacity(_activeAgentCount);

            for (int i = 0; i < _activeAgentCount; i++)
            {
                if (forceReset || i >= previousAgentCount || !_agentPool[i].gameObject.activeSelf)
                {
                    ResetAgent(i);
                }
            }

            for (int i = _activeAgentCount; i < previousAgentCount; i++)
            {
                _agentPool[i].gameObject.SetActive(false);
            }

            if (_simulationMode == SimulationMode.Managed)
            {
                _currentManagedAgents.RemoveRange(_activeAgentCount, _currentManagedAgents.Count - _activeAgentCount);
            }
        }

        private void EnsureManagedBufferCapacity(int capacity)
        {
            _managedGrid ??= new SpatialGridManaged(_separationRadius, MaxAgentCount);

            if (_managedAgentsBufferA == null || _managedAgentsBufferA.Capacity < capacity)
            {
                _managedAgentsBufferA = new List<AgentData>(capacity);
                _managedAgentsBufferB = new List<AgentData>(capacity);
                _currentManagedAgents = _managedAgentsBufferA;
                _nextManagedAgents = _managedAgentsBufferB;
            }
        }

        private void EnsurePoolCapacity(int capacity)
        {
            int agentTypeCount = _instanceManager.GetAgentTypeCount();
            while (_agentPool.Count < capacity)
            {
                int randomTypeIndex = Random.Range(0, agentTypeCount);
                VAT_BoidsAgent prefab = _instanceManager.GetAgentPrefab(randomTypeIndex);
                var newAgentGO = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
                var agentComponent = newAgentGO.GetComponent<VAT_BoidsAgent>();
                agentComponent.Initialize(_instanceManager);
                agentComponent.AgentTypeIndex = randomTypeIndex;
                newAgentGO.gameObject.SetActive(false);
                _agentPool.Add(agentComponent);
            }
        }

        private void ResetAgent(int agentIndex)
        {
            var agentComponent = _agentPool[agentIndex];
            Vector3 randomPos = GetRandomPositionInBounds();
            agentComponent.transform.position = randomPos;
            agentComponent.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
            agentComponent.gameObject.SetActive(true);

            var agentData = new AgentData
            {
                position = randomPos,
                velocity = float3.zero,
                typeIndex = agentComponent.AgentTypeIndex
            };
            TransitionToNewState(ref agentData, agentComponent);

            if (_simulationMode == SimulationMode.JobSystem)
            {
                _jobAgents[agentIndex] = agentData;
            }
            else
            {
                if (agentIndex < _currentManagedAgents.Count) _currentManagedAgents[agentIndex] = agentData;
                else _currentManagedAgents.Add(agentData);
            }
        }

        private void DestroyAgentPool()
        {
            foreach (var agentComponent in _agentPool)
            {
                if (agentComponent != null) Destroy(agentComponent.gameObject);
            }
            _agentPool.Clear();
        }

        private void DisposeNativeArrays()
        {
            if (_jobAgents.IsCreated) _jobAgents.Dispose();
            if (_gridMap.IsCreated) _gridMap.Dispose();
            if (_gridNext.IsCreated) _gridNext.Dispose();
        }

        private void EnsureNativeArrayCapacity(int capacity)
        {
            if (_jobAgents.IsCreated && _jobAgents.Length >= capacity) return;

            _simulationJobHandle.Complete();
            DisposeNativeArrays();

            if (capacity <= 0) return;

            _jobAgents = new NativeArray<AgentData>(capacity, Allocator.Persistent);
            _gridMap = new NativeParallelHashMap<int, int>(capacity, Allocator.Persistent);
            _gridNext = new NativeArray<int>(capacity, Allocator.Persistent);
        }

        private IEnumerator SimulationCoroutine()
        {
            while (true)
            {
                if (_activeAgentCount > 0)
                {
                    if (_simulationMode == SimulationMode.JobSystem)
                    {
                        ApplyJobSystemResults(_coroutineUpdateInterval);
                        _simulationJobHandle = ScheduleJobSystemSimulation(_coroutineUpdateInterval);
                    }
                    else
                    {
                        UpdateManagedSimulation(_coroutineUpdateInterval);
                    }
                }

                if (_coroutineWait == null || !Mathf.Approximately(_lastCoroutineInterval, _coroutineUpdateInterval))
                {
                    _lastCoroutineInterval = _coroutineUpdateInterval;
                    _coroutineWait = new WaitForSeconds(_coroutineUpdateInterval);
                }
                yield return _coroutineWait;
            }
        }

        private void RunSimulationStep(float deltaTime)
        {
            if (_activeAgentCount == 0) return;

            if (_simulationMode == SimulationMode.JobSystem)
            {
                _simulationJobHandle.Complete();
                ApplyJobSystemResults(deltaTime);
                _simulationJobHandle = ScheduleJobSystemSimulation(deltaTime);
            }
            else
            {
                UpdateManagedSimulation(deltaTime);
            }
        }

        #region Managed Mode Simulation

        private class SpatialGridManaged
        {
            private const int HASH_P1 = 73856093;
            private const int HASH_P2 = 19349663;
            private const int NO_AGENT = -1;

            private readonly float _inverseCellSize;
            private readonly List<int> _queryResultCache = new List<int>(100);
            private readonly Dictionary<int, int> _gridBuckets;
            private readonly int[] _nextAgentIndices;

            public SpatialGridManaged(float radius, int maxCapacity)
            {
                _inverseCellSize = 1f / radius;
                _nextAgentIndices = new int[maxCapacity];
                _gridBuckets = new Dictionary<int, int>(maxCapacity);
            }

            public void ClearAndBuild(IReadOnlyList<AgentData> agents, int agentCount)
            {
                _gridBuckets.Clear();
                for (int i = 0; i < agentCount; i++)
                {
                    var hash = GetCellHash(agents[i].position);
                    if (_gridBuckets.TryGetValue(hash, out int headIndex))
                    {
                        _nextAgentIndices[i] = headIndex;
                    }
                    else
                    {
                        _nextAgentIndices[i] = NO_AGENT;
                    }
                    _gridBuckets[hash] = i;
                }
            }

            private int GetCellHash(float3 position) => (int)(position.x * _inverseCellSize) * HASH_P1 ^ (int)(position.z * _inverseCellSize) * HASH_P2;

            public List<int> Query(float3 position)
            {
                _queryResultCache.Clear();
                int centerX = (int)(position.x * _inverseCellSize);
                int centerZ = (int)(position.z * _inverseCellSize);

                for (int x = -1; x <= 1; x++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        var hash = (centerX + x) * HASH_P1 ^ (centerZ + z) * HASH_P2;
                        if (_gridBuckets.TryGetValue(hash, out int agentIndex))
                        {
                            while (agentIndex != NO_AGENT)
                            {
                                _queryResultCache.Add(agentIndex);
                                agentIndex = _nextAgentIndices[agentIndex];
                            }
                        }
                    }
                }
                return _queryResultCache;
            }
        }

        private void UpdateManagedSimulation(float deltaTime)
        {
            _managedGrid.ClearAndBuild(_currentManagedAgents, _activeAgentCount);
            _nextManagedAgents.Clear();

            for (int i = 0; i < _activeAgentCount; i++)
            {
                var data = _currentManagedAgents[i];
                UpdateAgentState(ref data, i, deltaTime);
                UpdateAgentPhysics(ref data, deltaTime);
                _nextManagedAgents.Add(data);
                ApplyTransform(data, i);
            }

            var temp = _currentManagedAgents;
            _currentManagedAgents = _nextManagedAgents;
            _nextManagedAgents = temp;
        }

        private void UpdateAgentState(ref AgentData data, int agentIndex, float deltaTime)
        {
            data.acceleration = float3.zero;

            if (data.currentState == AgentBehaviorState.Idle)
            {
                data.stateTimer -= deltaTime;
                data.velocity = math.lerp(data.velocity, float3.zero, deltaTime * 5f);
                if (data.stateTimer <= 0)
                {
                    TransitionToNewState(ref data, _agentPool[agentIndex]);
                }
            }
            else
            {
                if (math.distancesq(data.targetPosition, data.position) < _destinationReachedThreshold * _destinationReachedThreshold)
                {
                    TransitionToNewState(ref data, _agentPool[agentIndex]);
                    return;
                }

                float3 goalForce = Steer(data.targetPosition - data.position, ref data);
                float3 separationForce = CalculateManagedSeparationForce(ref data, agentIndex);
                data.acceleration += goalForce * _goalSeekingWeight;
                data.acceleration += separationForce * _separationWeight;
            }
        }

        private float3 CalculateManagedSeparationForce(ref AgentData data, int agentIndex)
        {
            float3 force = float3.zero;
            int neighborsCount = 0;
            float separationSqr = _separationRadius * _separationRadius;

            List<int> neighbors = _managedGrid.Query(data.position);
            foreach (int j in neighbors)
            {
                if (agentIndex == j) continue;

                float3 offset = _currentManagedAgents[j].position - data.position;
                float sqrDistance = math.lengthsq(offset);
                if (sqrDistance > 0 && sqrDistance < separationSqr)
                {
                    force -= math.normalize(offset) / math.max(math.sqrt(sqrDistance), 0.001f);
                    neighborsCount++;
                }
            }
            return neighborsCount > 0 ? Steer(force / neighborsCount, ref data) : float3.zero;
        }

        #endregion

        #region Job System Simulation

        private JobHandle ScheduleJobSystemSimulation(float deltaTime)
        {
            var populateGridJob = new PopulateGridJob
            {
                agents = _jobAgents.AsReadOnly(),
                gridMap = _gridMap,
                gridNext = _gridNext,
                inverseCellSize = 1f / _separationRadius
            };
            var populateHandle = populateGridJob.Schedule();

            var simulationJob = new SimulationJob
            {
                agents = _jobAgents,
                gridMap = _gridMap.AsReadOnly(),
                gridNext = _gridNext.AsReadOnly(),
                separationRadiusSqr = _separationRadius * _separationRadius,
                separationWeight = _separationWeight,
                goalSeekingWeight = _goalSeekingWeight,
                walkSpeed = _walkSpeed,
                runSpeed = _runSpeed,
                maxSteerForce = _maxSteerForce,
                destinationReachedThresholdSqr = _destinationReachedThreshold * _destinationReachedThreshold,
                inverseCellSize = 1f / _separationRadius
            };
            var simulationHandle = simulationJob.Schedule(_activeAgentCount, 64, populateHandle);

            var integrationJob = new IntegrationJob
            {
                agents = _jobAgents,
                deltaTime = deltaTime,
                walkSpeed = _walkSpeed,
                runSpeed = _runSpeed,
                planeYPosition = _planeYPosition
            };

            return integrationJob.Schedule(_activeAgentCount, 64, simulationHandle);
        }

        private void ApplyJobSystemResults(float deltaTime)
        {
            _simulationJobHandle.Complete();

            for (int i = 0; i < _activeAgentCount; i++)
            {
                var data = _jobAgents[i];
                UpdateAgentStateFromJob(ref data, i, deltaTime);
                ApplyTransform(data, i);
                _jobAgents[i] = data;
            }
        }

        private void UpdateAgentStateFromJob(ref AgentData data, int index, float deltaTime)
        {
            if (data.currentState == AgentBehaviorState.Idle)
            {
                data.stateTimer -= deltaTime;
                if (data.stateTimer <= 0) TransitionToNewState(ref data, _agentPool[index]);
            }
            else
            {
                if (math.distancesq(data.targetPosition, data.position) < _destinationReachedThreshold * _destinationReachedThreshold)
                {
                    TransitionToNewState(ref data, _agentPool[index]);
                }
            }
        }

        [BurstCompile]
        private struct PopulateGridJob : IJob
        {
            private const int HASH_P1 = 73856093;
            private const int HASH_P2 = 19349663;
            private const int NO_AGENT = -1;

            [Unity.Collections.ReadOnly] public NativeArray<AgentData>.ReadOnly agents;
            [WriteOnly] public NativeParallelHashMap<int, int> gridMap;
            [WriteOnly] public NativeArray<int> gridNext;
            public float inverseCellSize;

            private int GetCellHash(float3 pos) => (int)(math.floor(pos.x * inverseCellSize) * HASH_P1) ^ (int)(math.floor(pos.z * inverseCellSize) * HASH_P2);

            public void Execute()
            {
                gridMap.Clear();
                for (int i = 0; i < agents.Length; i++)
                {
                    var hash = GetCellHash(agents[i].position);
                    if (gridMap.TryGetValue(hash, out int headIndex))
                    {
                        gridNext[i] = headIndex;
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
            private const int HASH_P1 = 73856093;
            private const int HASH_P2 = 19349663;
            private const int NO_AGENT = -1;

            public NativeArray<AgentData> agents;
            [Unity.Collections.ReadOnly] public NativeParallelHashMap<int, int>.ReadOnly gridMap;
            [Unity.Collections.ReadOnly] public NativeArray<int>.ReadOnly gridNext;
            public float separationRadiusSqr;
            public float separationWeight;
            public float goalSeekingWeight;
            public float walkSpeed;
            public float runSpeed;
            public float maxSteerForce;
            public float destinationReachedThresholdSqr;
            public float inverseCellSize;

            public void Execute(int index)
            {
                var agent = agents[index];
                agent.acceleration = float3.zero;

                if (agent.currentState == AgentBehaviorState.Idle || math.distancesq(agent.targetPosition, agent.position) < destinationReachedThresholdSqr)
                {
                    agents[index] = agent;
                    return;
                }

                float speed = agent.currentState == AgentBehaviorState.Running ? runSpeed : walkSpeed;
                float3 goalForce = Steer(agent.targetPosition - agent.position, agent.velocity, speed);
                float3 separationForce = CalculateSeparationForce(index, ref agent, speed);

                agent.acceleration += goalForce * goalSeekingWeight;
                agent.acceleration += separationForce * separationWeight;
                agents[index] = agent;
            }

            private float3 CalculateSeparationForce(int index, ref AgentData agent, float speed)
            {
                float3 force = float3.zero;
                int neighborsCount = 0;
                int centerX = (int)math.floor(agent.position.x * inverseCellSize);
                int centerZ = (int)math.floor(agent.position.z * inverseCellSize);

                for (int x = -1; x <= 1; x++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        var hash = (centerX + x) * HASH_P1 ^ (centerZ + z) * HASH_P2;
                        if (gridMap.TryGetValue(hash, out int current))
                        {
                            while (current != NO_AGENT)
                            {
                                if (index != current)
                                {
                                    float3 offset = agents[current].position - agent.position;
                                    float sqrDist = math.lengthsq(offset);
                                    if (sqrDist > 0.001f && sqrDist < separationRadiusSqr)
                                    {
                                        force -= math.normalize(offset) / math.sqrt(sqrDist);
                                        neighborsCount++;
                                    }
                                }
                                current = gridNext[current];
                            }
                        }
                    }
                }

                return neighborsCount > 0 ? Steer(force / neighborsCount, agent.velocity, speed) : float3.zero;
            }

            private float3 Steer(float3 targetDirection, float3 currentVelocity, float speed)
            {
                float3 desired = math.normalize(targetDirection) * speed;
                float3 steer = desired - currentVelocity;
                if (math.lengthsq(steer) > maxSteerForce * maxSteerForce)
                {
                    steer = math.normalize(steer) * maxSteerForce;
                }
                return steer;
            }
        }

        [BurstCompile]
        private struct IntegrationJob : IJobParallelFor
        {
            public NativeArray<AgentData> agents;
            public float deltaTime;
            public float walkSpeed;
            public float runSpeed;
            public float planeYPosition;

            public void Execute(int index)
            {
                var agent = agents[index];

                if (agent.currentState == AgentBehaviorState.Idle)
                {
                    agent.velocity = math.lerp(agent.velocity, float3.zero, deltaTime * 5f);
                }
                else
                {
                    float targetSpeed = agent.currentState == AgentBehaviorState.Running ? runSpeed : walkSpeed;
                    agent.velocity += agent.acceleration * deltaTime;
                    if (math.lengthsq(agent.velocity) > targetSpeed * targetSpeed)
                    {
                        agent.velocity = math.normalize(agent.velocity) * targetSpeed;
                    }
                }

                agent.position += agent.velocity * deltaTime;
                agent.position.y = planeYPosition;
                agent.velocity.y = 0;
                agents[index] = agent;
            }
        }
        #endregion

        #region Common Logic and Helpers
        private void UpdateAgentPhysics(ref AgentData data, float deltaTime)
        {
            if (data.currentState == AgentBehaviorState.Idle)
            {
                data.velocity = math.lerp(data.velocity, float3.zero, deltaTime * 5f);
            }
            else
            {
                float targetSpeed = data.currentState == AgentBehaviorState.Running ? _runSpeed : _walkSpeed;
                data.velocity += data.acceleration * deltaTime;
                if (math.lengthsq(data.velocity) > targetSpeed * targetSpeed)
                {
                    data.velocity = math.normalize(data.velocity) * targetSpeed;
                }
            }

            data.position += data.velocity * deltaTime;
            data.position.y = _planeYPosition;
            data.velocity.y = 0;
        }

        private void ApplyTransform(AgentData data, int index)
        {
            var t = _agentPool[index].transform;
            t.position = data.position;
            if (math.lengthsq(data.velocity) > 0.01f)
            {
                t.rotation = Quaternion.LookRotation(data.velocity);
            }
        }

        private void TransitionToNewState(ref AgentData data, VAT_BoidsAgent agentComponent)
        {
            if (data.currentState != AgentBehaviorState.Idle)
            {
                data.currentState = AgentBehaviorState.Idle;
                data.stateTimer = Random.Range(_idleDurationRange.x, _idleDurationRange.y);
                agentComponent.CrossFade(_idleClip, _animationFadeDuration);
            }
            else
            {
                data.targetPosition = GetRandomPositionInBounds();
                bool shouldRun = Random.value < _chanceToRunInsteadOfWalk;
                data.currentState = shouldRun ? AgentBehaviorState.Running : AgentBehaviorState.Walking;
                string clip = shouldRun ? _runClip : _walkClip;
                agentComponent.CrossFade(clip, _animationFadeDuration);
            }
        }

        private float3 Steer(float3 targetDirection, ref AgentData data)
        {
            float speed = data.currentState == AgentBehaviorState.Running ? _runSpeed : _walkSpeed;
            float3 desired = math.normalize(targetDirection) * speed;
            float3 steer = desired - data.velocity;
            if (math.lengthsq(steer) > _maxSteerForce * _maxSteerForce)
            {
                steer = math.normalize(steer) * _maxSteerForce;
            }
            return steer;
        }

        private float3 GetRandomPositionInBounds() => (float3)transform.position + new float3(
                Random.Range(-_spawnBounds.x * 0.5f, _spawnBounds.x * 0.5f), 0,
                Random.Range(-_spawnBounds.z * 0.5f, _spawnBounds.z * 0.5f));

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.5f, 0.4f);
            Gizmos.DrawWireCube(transform.position, new Vector3(_spawnBounds.x, 0.1f, _spawnBounds.z));
        }
        #endregion
    }
}