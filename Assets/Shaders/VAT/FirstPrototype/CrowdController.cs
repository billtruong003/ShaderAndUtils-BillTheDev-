using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public class CrowdController : MonoBehaviour
{
    private class SpatialGrid2D
    {
        private readonly Dictionary<int, List<int>> _cells = new Dictionary<int, List<int>>(5000);
        private readonly float _cellSize;
        private readonly float _inverseCellSize;
        private readonly List<int> _queryResult = new List<int>(50);

        public SpatialGrid2D(float radius)
        {
            _cellSize = radius;
            _inverseCellSize = 1f / _cellSize;
        }

        public void Clear() => _cells.Clear();

        private int GetCellHash(Vector3 position)
        {
            const int prime1 = 73856093;
            const int prime2 = 19349663;
            int x = Mathf.FloorToInt(position.x * _inverseCellSize);
            int z = Mathf.FloorToInt(position.z * _inverseCellSize);
            return (x * prime1) ^ (z * prime2);
        }

        public void Add(Vector3 position, int agentIndex)
        {
            int hash = GetCellHash(position);
            if (!_cells.TryGetValue(hash, out var cell))
            {
                cell = new List<int>(10);
                _cells[hash] = cell;
            }
            cell.Add(agentIndex);
        }

        public List<int> Query(Vector3 position)
        {
            _queryResult.Clear();
            int centerX = Mathf.FloorToInt(position.x * _inverseCellSize);
            int centerZ = Mathf.FloorToInt(position.z * _inverseCellSize);

            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    var queryPos = new Vector3(
                        (centerX + x) * _cellSize, 0, (centerZ + z) * _cellSize
                    );
                    if (_cells.TryGetValue(GetCellHash(queryPos), out var cell))
                    {
                        _queryResult.AddRange(cell);
                    }
                }
            }
            return _queryResult;
        }
    }

    private enum AgentBehaviorState { Idle, Walking, Running }
    private struct CrowdAgentData
    {
        public VAT_BoidsAgent agentComponent;
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 acceleration;
        public Vector3 targetPosition;
        public AgentBehaviorState currentState;
        public float stateTimer;
    }

    [TitleGroup("Spawning")]
    [SerializeField, Range(1, 25000)] private int _agentCount = 500;
    [SerializeField, Required] private VAT_BoidsAgent _agentPrefab;
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

    private List<CrowdAgentData> _agents = new List<CrowdAgentData>();
    private float _planeYPosition;
    private SpatialGrid2D _grid;

    private void Start()
    {
        _planeYPosition = this.transform.position.y;
        _grid = new SpatialGrid2D(_separationRadius);
        SpawnAgents();
    }

    private void Update()
    {
        PopulateGrid();
        UpdateCrowdSimulation(Time.deltaTime);
    }

    [Button(ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
    private void RespawnAgents()
    {
        foreach (var agentData in _agents)
        {
            if (agentData.agentComponent != null) Destroy(agentData.agentComponent.gameObject);
        }
        _agents.Clear();
        SpawnAgents();
    }

    private void SpawnAgents()
    {
        for (int i = 0; i < _agentCount; i++)
        {
            Vector3 randomPos = this.transform.position + new Vector3(
                Random.Range(-_spawnBounds.x / 2f, _spawnBounds.x / 2f),
                0,
                Random.Range(-_spawnBounds.z / 2f, _spawnBounds.z / 2f)
            );
            var newAgentGO = Instantiate(_agentPrefab, randomPos, Quaternion.Euler(0, Random.Range(0, 360f), 0));
            newAgentGO.transform.SetParent(this.transform);
            var agentData = new CrowdAgentData
            {
                agentComponent = newAgentGO,
                position = randomPos,
                velocity = Vector3.zero
            };
            TransitionToNewState(ref agentData);
            _agents.Add(agentData);
        }
    }

    private void PopulateGrid()
    {
        _grid.Clear();
        for (int i = 0; i < _agents.Count; i++)
        {
            _grid.Add(_agents[i].position, i);
        }
    }

    private void UpdateCrowdSimulation(float deltaTime)
    {
        for (int i = 0; i < _agents.Count; i++)
        {
            var data = _agents[i];
            data.acceleration = Vector3.zero;

            switch (data.currentState)
            {
                case AgentBehaviorState.Idle: UpdateIdleState(ref data, deltaTime); break;
                case AgentBehaviorState.Walking:
                case AgentBehaviorState.Running: UpdateMovementState(ref data, i); break;
            }

            float targetSpeed = data.currentState == AgentBehaviorState.Running ? _runSpeed : _walkSpeed;
            if (data.currentState == AgentBehaviorState.Idle) targetSpeed = 0;

            data.velocity += data.acceleration * deltaTime;
            data.velocity = Vector3.ClampMagnitude(data.velocity, targetSpeed);
            data.position += data.velocity * deltaTime;
            data.position.y = _planeYPosition;
            data.velocity.y = 0;

            data.agentComponent.transform.position = data.position;
            if (data.velocity.sqrMagnitude > 0.01f)
            {
                data.agentComponent.transform.rotation = Quaternion.LookRotation(data.velocity);
            }
            _agents[i] = data;
        }
    }

    private void UpdateIdleState(ref CrowdAgentData data, float deltaTime)
    {
        data.stateTimer -= deltaTime;
        data.velocity = Vector3.Lerp(data.velocity, Vector3.zero, deltaTime * 5f);
        if (data.stateTimer <= 0)
        {
            TransitionToNewState(ref data);
        }
    }

    private void UpdateMovementState(ref CrowdAgentData data, int agentIndex)
    {
        if ((data.targetPosition - data.position).sqrMagnitude < _destinationReachedThreshold * _destinationReachedThreshold)
        {
            TransitionToNewState(ref data);
            return;
        }

        Vector3 goalForce = Steer(data.targetPosition - data.position, ref data);
        Vector3 separationForce = CalculateSeparationForce(ref data, agentIndex);

        data.acceleration += goalForce * _goalSeekingWeight;
        data.acceleration += separationForce * _separationWeight;
    }

    private Vector3 CalculateSeparationForce(ref CrowdAgentData data, int agentIndex)
    {
        Vector3 force = Vector3.zero;
        int neighborsCount = 0;
        float separationSqr = _separationRadius * _separationRadius;

        List<int> neighbors = _grid.Query(data.position);
        foreach (int j in neighbors)
        {
            if (agentIndex == j) continue;
            Vector3 offset = _agents[j].position - data.position;
            float sqrDistance = offset.sqrMagnitude;
            if (sqrDistance > 0 && sqrDistance < separationSqr)
            {
                force -= offset.normalized / Mathf.Max(Mathf.Sqrt(sqrDistance), 0.001f);
                neighborsCount++;
            }
        }
        return neighborsCount > 0 ? Steer(force / neighborsCount, ref data) : Vector3.zero;
    }

    private void TransitionToNewState(ref CrowdAgentData data)
    {
        if (data.currentState != AgentBehaviorState.Idle)
        {
            data.currentState = AgentBehaviorState.Idle;
            data.stateTimer = Random.Range(_idleDurationRange.x, _idleDurationRange.y);
            data.agentComponent.CrossFade(_idleClip, _animationFadeDuration);
        }
        else
        {
            data.targetPosition = this.transform.position + new Vector3(
                Random.Range(-_spawnBounds.x / 2f, _spawnBounds.x / 2f), 0,
                Random.Range(-_spawnBounds.z / 2f, _spawnBounds.z / 2f));
            data.currentState = Random.value < _chanceToRunInsteadOfWalk ? AgentBehaviorState.Running : AgentBehaviorState.Walking;
            data.agentComponent.CrossFade(data.currentState == AgentBehaviorState.Running ? _runClip : _walkClip, _animationFadeDuration);
        }
    }

    private Vector3 Steer(Vector3 targetDirection, ref CrowdAgentData data)
    {
        float speed = data.currentState == AgentBehaviorState.Running ? _runSpeed : _walkSpeed;
        Vector3 desired = targetDirection.normalized * speed;
        Vector3 steer = desired - data.velocity;
        return Vector3.ClampMagnitude(steer, _maxSteerForce);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.5f, 0.4f);
        Gizmos.DrawWireCube(transform.position, new Vector3(_spawnBounds.x, 0.1f, _spawnBounds.z));
    }
}