using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Random = UnityEngine.Random;

namespace OptimizeVariousVAT
{
    public class CrowdController_Optimized : MonoBehaviour
    {
        #region Enums and Structsx

        public enum UpdateMode
        {
            EveryFrame,
            ViaCoroutine
        }

        public enum SimulationMode
        {
            Managed,
            JobSystem
        }

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

        #endregion

        #region Inspector Fields

        [TitleGroup("Performance & Mode")]
        [SerializeField] private UpdateMode _updateMode = UpdateMode.EveryFrame;
        [SerializeField] private SimulationMode _simulationMode = SimulationMode.JobSystem;
        [SerializeField, ShowIf("_updateMode", UpdateMode.ViaCoroutine)]
        [Range(0.01f, 1f)] private float _coroutineUpdateInterval = 0.033f;

        [TitleGroup("Spawning")]
        [SerializeField, Range(1, 50000)] private int _agentCount = 500;
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

        #endregion

        #region Private Fields

        private readonly List<VAT_BoidsAgent> _agentComponents = new List<VAT_BoidsAgent>();
        private float _planeYPosition;

        // Managed Mode Fields
        private readonly List<AgentData> _managedAgents = new List<AgentData>();
        private SpatialGrid2D _managedGrid;

        // Job System Fields
        private NativeArray<AgentData> _jobAgents;
        private NativeParallelHashMap<int, int> _gridMap;
        private Unity.Collections.NativeArray<int> _gridCells;
        private Unity.Collections.NativeArray<int> _gridNext;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _planeYPosition = this.transform.position.y;
            SpawnAgents();

            if (_updateMode == UpdateMode.ViaCoroutine)
            {
                StartCoroutine(SimulationCoroutine());
            }
        }

        private void Update()
        {
            if (_updateMode == UpdateMode.EveryFrame)
            {
                RunSimulation(Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            if (_simulationMode == SimulationMode.JobSystem && _jobAgents.IsCreated)
            {
                _jobAgents.Dispose();
                _gridMap.Dispose();
                _gridCells.Dispose();
                _gridNext.Dispose();
            }
            CleanupAgents();
        }

        #endregion

        #region Agent Spawning and Cleanup

        [Button(ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
        private void RespawnAgents()
        {
            CleanupAgents();
            SpawnAgents();
        }

        private void SpawnAgents()
        {
            if (_instanceManager == null || _instanceManager.GetAgentTypeCount() == 0)
            {
                return;
            }

            if (_simulationMode == SimulationMode.JobSystem)
            {
                InitializeJobSystemData();
            }
            else
            {
                _managedGrid = new SpatialGrid2D(_separationRadius);
            }

            int agentTypeCount = _instanceManager.GetAgentTypeCount();

            for (int i = 0; i < _agentCount; i++)
            {
                Vector3 randomPos = GetRandomPositionInBounds();
                int randomTypeIndex = Random.Range(0, agentTypeCount);
                VAT_BoidsAgent prefab = _instanceManager.GetAgentPrefab(randomTypeIndex);

                var newAgentGO = Instantiate(prefab, randomPos, Quaternion.Euler(0, Random.Range(0, 360f), 0));
                newAgentGO.transform.SetParent(this.transform);

                var agentComponent = newAgentGO.GetComponent<VAT_BoidsAgent>();
                agentComponent.AgentTypeIndex = randomTypeIndex;
                newAgentGO.gameObject.SetActive(true);
                _agentComponents.Add(agentComponent);

                var agentData = new AgentData
                {
                    position = randomPos,
                    velocity = float3.zero,
                    typeIndex = randomTypeIndex
                };
                TransitionToNewState(ref agentData, agentComponent);

                if (_simulationMode == SimulationMode.JobSystem)
                {
                    _jobAgents[i] = agentData;
                }
                else
                {
                    _managedAgents.Add(agentData);
                }
            }
        }

        private void CleanupAgents()
        {
            foreach (var agentComponent in _agentComponents)
            {
                if (agentComponent != null) Destroy(agentComponent.gameObject);
            }
            _agentComponents.Clear();

            if (_simulationMode == SimulationMode.JobSystem)
            {
                if (_jobAgents.IsCreated) _jobAgents.Dispose();
                if (_gridMap.IsCreated) _gridMap.Dispose();
                if (_gridCells.IsCreated) _gridCells.Dispose();
                if (_gridNext.IsCreated) _gridNext.Dispose();
            }
            else
            {
                _managedAgents.Clear();
            }
        }

        private void InitializeJobSystemData()
        {
            _jobAgents = new NativeArray<AgentData>(_agentCount, Allocator.Persistent);
            _gridMap = new NativeParallelHashMap<int, int>(_agentCount, Allocator.Persistent);
            _gridCells = new NativeArray<int>(_agentCount, Allocator.Persistent);
            _gridNext = new NativeArray<int>(_agentCount, Allocator.Persistent);
        }

        #endregion

        #region Simulation Logic

        private System.Collections.IEnumerator SimulationCoroutine()
        {
            var wait = new WaitForSeconds(_coroutineUpdateInterval);
            while (true)
            {
                RunSimulation(_coroutineUpdateInterval);
                yield return wait;
            }
        }

        private void RunSimulation(float deltaTime)
        {
            if (_simulationMode == SimulationMode.JobSystem)
            {
                UpdateJobSystemSimulation(deltaTime);
            }
            else
            {
                UpdateManagedSimulation(deltaTime);
            }
        }

        #endregion

        #region Managed Mode Simulation

        private class SpatialGrid2D
        {
            private readonly Dictionary<int, List<int>> _cells = new Dictionary<int, List<int>>(5000);
            private readonly float _inverseCellSize;
            private readonly List<int> _queryResult = new List<int>(50);

            public SpatialGrid2D(float radius) => _inverseCellSize = 1f / radius;
            public void Clear() => _cells.Clear();

            private int GetCellHash(float3 position)
            {
                int x = (int)(position.x * _inverseCellSize);
                int z = (int)(position.z * _inverseCellSize);
                return (x * 73856093) ^ (z * 19349663);
            }

            public void Add(float3 position, int agentIndex)
            {
                int hash = GetCellHash(position);
                if (!_cells.TryGetValue(hash, out var cell))
                {
                    cell = new List<int>(10);
                    _cells[hash] = cell;
                }
                cell.Add(agentIndex);
            }

            public List<int> Query(float3 position, float radius)
            {
                _queryResult.Clear();
                int centerX = (int)(position.x * _inverseCellSize);
                int centerZ = (int)(position.z * _inverseCellSize);

                for (int x = -1; x <= 1; x++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        var queryPos = new float3((centerX + x) / _inverseCellSize, 0, (centerZ + z) / _inverseCellSize);
                        if (_cells.TryGetValue(GetCellHash(queryPos), out var cell))
                        {
                            _queryResult.AddRange(cell);
                        }
                    }
                }
                return _queryResult;
            }
        }

        private void UpdateManagedSimulation(float deltaTime)
        {
            _managedGrid.Clear();
            for (int i = 0; i < _managedAgents.Count; i++)
            {
                _managedGrid.Add(_managedAgents[i].position, i);
            }

            for (int i = 0; i < _managedAgents.Count; i++)
            {
                var data = _managedAgents[i];
                UpdateAgentState(ref data, i, deltaTime);
                UpdateAgentPhysics(ref data, deltaTime);
                _managedAgents[i] = data;
                ApplyTransform(data, i);
            }
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
                    TransitionToNewState(ref data, _agentComponents[agentIndex]);
                }
            }
            else // Walking or Running
            {
                if (math.distancesq(data.targetPosition, data.position) < _destinationReachedThreshold * _destinationReachedThreshold)
                {
                    TransitionToNewState(ref data, _agentComponents[agentIndex]);
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

            List<int> neighbors = _managedGrid.Query(data.position, _separationRadius);
            foreach (int j in neighbors)
            {
                if (agentIndex == j) continue;

                float3 offset = _managedAgents[j].position - data.position;
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

        private void UpdateJobSystemSimulation(float deltaTime)
        {
            var populateGridJob = new PopulateGridJob
            {
                agents = _jobAgents.AsReadOnly(),
                gridMap = _gridMap,
                gridCells = _gridCells,
                gridNext = _gridNext,
                inverseCellSize = 1f / _separationRadius
            };
            var populateHandle = populateGridJob.Schedule();

            var simulationJob = new SimulationJob
            {
                agents = _jobAgents,
                gridMap = _gridMap.AsReadOnly(),
                gridCells = _gridCells,
                gridNext = _gridNext,
                separationRadiusSqr = _separationRadius * _separationRadius,
                separationWeight = _separationWeight,
                goalSeekingWeight = _goalSeekingWeight,
                walkSpeed = _walkSpeed,
                runSpeed = _runSpeed,
                maxSteerForce = _maxSteerForce,
                destinationReachedThresholdSqr = _destinationReachedThreshold * _destinationReachedThreshold,
                inverseCellSize = 1f / _separationRadius
            };
            var simulationHandle = simulationJob.Schedule(_agentCount, 64, populateHandle);

            var integrationJob = new IntegrationJob
            {
                agents = _jobAgents,
                deltaTime = deltaTime,
                walkSpeed = _walkSpeed,
                runSpeed = _runSpeed,
                planeYPosition = _planeYPosition
            };
            var integrationHandle = integrationJob.Schedule(_agentCount, 64, simulationHandle);

            integrationHandle.Complete();

            // Sync state and transforms back to managed world
            for (int i = 0; i < _agentCount; i++)
            {
                var data = _jobAgents[i];
                UpdateAgentStateFromJob(ref data, i, deltaTime);
                ApplyTransform(data, i);
                _jobAgents[i] = data; // Write back state changes
            }
        }

        private void UpdateAgentStateFromJob(ref AgentData data, int index, float deltaTime)
        {
            if (data.currentState == AgentBehaviorState.Idle)
            {
                data.stateTimer -= deltaTime;
                if (data.stateTimer <= 0) TransitionToNewState(ref data, _agentComponents[index]);
            }
            else
            {
                if (math.distancesq(data.targetPosition, data.position) < _destinationReachedThreshold * _destinationReachedThreshold)
                {
                    TransitionToNewState(ref data, _agentComponents[index]);
                }
            }
        }

        [BurstCompile]
        private struct PopulateGridJob : IJob
        {
            [Unity.Collections.ReadOnly] public NativeArray<AgentData>.ReadOnly agents;

            [WriteOnly] public NativeParallelHashMap<int, int> gridMap;
            [WriteOnly] public NativeArray<int> gridCells;
            [WriteOnly] public NativeArray<int> gridNext;

            public float inverseCellSize;

            private int GetCellHash(float3 pos)
            {
                int x = (int)(pos.x * inverseCellSize);
                int z = (int)(pos.z * inverseCellSize);
                return (x * 73856093) ^ (z * 19349663);
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
                        gridNext[i] = -1;
                    }
                    gridMap[hash] = i;
                    gridCells[i] = i;
                }
            }
        }

        [BurstCompile]
        private struct SimulationJob : IJobParallelFor
        {
            public NativeArray<AgentData> agents;

            [Unity.Collections.ReadOnly] public NativeParallelHashMap<int, int>.ReadOnly gridMap;
            [Unity.Collections.ReadOnly] public NativeArray<int> gridCells;
            [Unity.Collections.ReadOnly] public NativeArray<int> gridNext;

            public float separationRadiusSqr;
            public float separationWeight;
            public float goalSeekingWeight;
            public float walkSpeed;
            public float runSpeed;
            public float maxSteerForce;
            public float destinationReachedThresholdSqr;
            public float inverseCellSize;

            private int GetCellHash(float3 pos)
            {
                int x = (int)(pos.x * inverseCellSize);
                int z = (int)(pos.z * inverseCellSize);
                return (x * 73856093) ^ (z * 19349663);
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

            public void Execute(int index)
            {
                var agent = agents[index];
                agent.acceleration = float3.zero;

                if (agent.currentState == AgentBehaviorState.Idle)
                {
                    agents[index] = agent;
                    return;
                }

                if (math.distancesq(agent.targetPosition, agent.position) < destinationReachedThresholdSqr)
                {
                    agents[index] = agent;
                    return;
                }

                float speed = agent.currentState == AgentBehaviorState.Running ? runSpeed : walkSpeed;
                float3 goalForce = Steer(agent.targetPosition - agent.position, agent.velocity, speed);

                // Separation Force
                float3 separationForce = float3.zero;
                int neighborsCount = 0;

                int centerX = (int)(agent.position.x * inverseCellSize);
                int centerZ = (int)(agent.position.z * inverseCellSize);

                for (int x = -1; x <= 1; x++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        var queryPos = new float3((centerX + x) / inverseCellSize, 0, (centerZ + z) / inverseCellSize);
                        if (gridMap.TryGetValue(GetCellHash(queryPos), out int head))
                        {
                            int current = head;
                            while (current != -1)
                            {
                                int neighborIndex = gridCells[current];
                                if (index != neighborIndex)
                                {
                                    float3 offset = agents[neighborIndex].position - agent.position;
                                    float sqrDist = math.lengthsq(offset);
                                    if (sqrDist > 0 && sqrDist < separationRadiusSqr)
                                    {
                                        separationForce -= math.normalize(offset) / math.max(math.sqrt(sqrDist), 0.001f);
                                        neighborsCount++;
                                    }
                                }
                                current = gridNext[current];
                            }
                        }
                    }
                }

                if (neighborsCount > 0)
                {
                    separationForce = Steer(separationForce / neighborsCount, agent.velocity, speed);
                }

                agent.acceleration += goalForce * goalSeekingWeight;
                agent.acceleration += separationForce * separationWeight;

                agents[index] = agent;
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

                float targetSpeed = agent.currentState == AgentBehaviorState.Idle ? 0 : (agent.currentState == AgentBehaviorState.Running ? runSpeed : walkSpeed);

                if (agent.currentState == AgentBehaviorState.Idle)
                {
                    agent.velocity = math.lerp(agent.velocity, float3.zero, deltaTime * 5f);
                }

                agent.velocity += agent.acceleration * deltaTime;
                if (math.lengthsq(agent.velocity) > targetSpeed * targetSpeed)
                {
                    agent.velocity = math.normalize(agent.velocity) * targetSpeed;
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
            float targetSpeed = data.currentState == AgentBehaviorState.Running ? _runSpeed : _walkSpeed;
            if (data.currentState == AgentBehaviorState.Idle) targetSpeed = 0;

            data.velocity += data.acceleration * deltaTime;
            if (math.lengthsq(data.velocity) > targetSpeed * targetSpeed)
            {
                data.velocity = math.normalize(data.velocity) * targetSpeed;
            }

            data.position += data.velocity * deltaTime;
            data.position.y = _planeYPosition;
            data.velocity.y = 0;
        }

        private void ApplyTransform(AgentData data, int index)
        {
            var t = _agentComponents[index].transform;
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
                data.targetPosition = (float3)GetRandomPositionInBounds();
                data.currentState = Random.value < _chanceToRunInsteadOfWalk ? AgentBehaviorState.Running : AgentBehaviorState.Walking;
                string clip = data.currentState == AgentBehaviorState.Running ? _runClip : _walkClip;
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

        private Vector3 GetRandomPositionInBounds()
        {
            return this.transform.position + new Vector3(
                Random.Range(-_spawnBounds.x / 2f, _spawnBounds.x / 2f), 0,
                Random.Range(-_spawnBounds.z / 2f, _spawnBounds.z / 2f));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.5f, 0.4f);
            Gizmos.DrawWireCube(transform.position, new Vector3(_spawnBounds.x, 0.1f, _spawnBounds.z));
        }

        #endregion
    }
}