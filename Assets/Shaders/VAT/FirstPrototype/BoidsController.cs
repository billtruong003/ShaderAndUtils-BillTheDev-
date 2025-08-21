using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public class BoidsController : MonoBehaviour
{
    private class SpatialGrid
    {
        private readonly Dictionary<int, List<int>> _cells = new Dictionary<int, List<int>>(10000);
        private readonly float _cellSize;
        private readonly float _inverseCellSize;
        private readonly List<int> _queryResult = new List<int>(100);

        public SpatialGrid(float perceptionRadius)
        {
            _cellSize = perceptionRadius;
            _inverseCellSize = 1f / _cellSize;
        }

        public void Clear() => _cells.Clear();

        private int GetCellHash(Vector3 position)
        {
            const int prime1 = 73856093;
            const int prime2 = 19349663;
            const int prime3 = 83492791;
            int x = Mathf.FloorToInt(position.x * _inverseCellSize);
            int y = Mathf.FloorToInt(position.y * _inverseCellSize);
            int z = Mathf.FloorToInt(position.z * _inverseCellSize);
            return (x * prime1) ^ (y * prime2) ^ (z * prime3);
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
            int centerY = Mathf.FloorToInt(position.y * _inverseCellSize);
            int centerZ = Mathf.FloorToInt(position.z * _inverseCellSize);

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        var queryPos = new Vector3(
                            (centerX + x) * _cellSize,
                            (centerY + y) * _cellSize,
                            (centerZ + z) * _cellSize
                        );
                        if (_cells.TryGetValue(GetCellHash(queryPos), out var cell))
                        {
                            _queryResult.AddRange(cell);
                        }
                    }
                }
            }
            return _queryResult;
        }
    }

    private struct BoidAgentData
    {
        public VAT_BoidsAgent agentComponent;
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 acceleration;
    }

    [TitleGroup("Spawning")]
    [SerializeField, Range(1, 25000)]
    private int _agentCount = 500;
    [SerializeField, Required]
    private VAT_BoidsAgent _agentPrefab;
    [SerializeField]
    private Vector3 _spawnBounds = new Vector3(100, 50, 100);

    [TitleGroup("Movement Settings")]
    [SerializeField, MinMaxSlider(1f, 100f, true)]
    private Vector2 _speedRange = new Vector2(10f, 30f);
    [SerializeField, Range(1f, 200f)]
    private float _maxSteerForce = 50f;

    [TitleGroup("Boids Behavior")]
    [SerializeField, Range(0f, 50f)]
    private float _perceptionRadius = 15f;
    [SerializeField, Range(0f, 20f)]
    private float _avoidanceRadius = 5f;

    [TitleGroup("Behavior Weights")]
    [SerializeField, Range(0f, 5f)] private float _cohesionWeight = 1f;
    [SerializeField, Range(0f, 5f)] private float _alignmentWeight = 1f;
    [SerializeField, Range(0f, 5f)] private float _separationWeight = 2f;
    [SerializeField, Range(0f, 5f)] private float _boundsWeight = 1.5f;

    [TitleGroup("Animation Control (Velocity Based)")]
    [SerializeField, Required] private string _idleClip = "idle";
    [SerializeField, Required] private string _walkClip = "walk";
    [SerializeField, Required] private string _runClip = "run";
    [SerializeField, Required] private string _sprintClip = "sprint";
    [SerializeField] private float _walkSpeedThreshold = 5f;
    [SerializeField] private float _runSpeedThreshold = 15f;
    [SerializeField] private float _sprintSpeedThreshold = 25f;
    [SerializeField, Range(0.1f, 1f)] private float _animationFadeDuration = 0.3f;

    private List<BoidAgentData> _agents = new List<BoidAgentData>();
    private SpatialGrid _grid;

    private void Start()
    {
        _grid = new SpatialGrid(_perceptionRadius);
        SpawnAgents();
    }

    private void Update()
    {
        PopulateGrid();
        CalculateBoidsForces();
        UpdateAgentMovementAndAnimation(Time.deltaTime);
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
                Random.Range(-_spawnBounds.y / 2f, _spawnBounds.y / 2f),
                Random.Range(-_spawnBounds.z / 2f, _spawnBounds.z / 2f)
            );
            var newAgentGO = Instantiate(_agentPrefab, randomPos, Random.rotation);
            newAgentGO.transform.SetParent(this.transform);
            _agents.Add(new BoidAgentData
            {
                agentComponent = newAgentGO,
                position = randomPos,
                velocity = newAgentGO.transform.forward * Random.Range(_speedRange.x, _speedRange.y)
            });
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

    private void CalculateBoidsForces()
    {
        float perceptionSqr = _perceptionRadius * _perceptionRadius;
        float avoidanceSqr = _avoidanceRadius * _avoidanceRadius;

        for (int i = 0; i < _agents.Count; i++)
        {
            var data = _agents[i];
            data.acceleration = Vector3.zero;
            Vector3 separation = Vector3.zero, alignment = Vector3.zero, cohesion = Vector3.zero;
            int separationCount = 0, alignmentCohesionCount = 0;

            List<int> neighbors = _grid.Query(data.position);
            foreach (int j in neighbors)
            {
                if (i == j) continue;
                Vector3 offset = _agents[j].position - data.position;
                float sqrDist = offset.sqrMagnitude;

                if (sqrDist < perceptionSqr)
                {
                    if (sqrDist < avoidanceSqr)
                    {
                        separation -= offset.normalized / Mathf.Max(Mathf.Sqrt(sqrDist), 0.001f);
                        separationCount++;
                    }
                    alignment += _agents[j].velocity;
                    cohesion += _agents[j].position;
                    alignmentCohesionCount++;
                }
            }

            if (separationCount > 0) data.acceleration += Steer(separation / separationCount, ref data) * _separationWeight;
            if (alignmentCohesionCount > 0)
            {
                data.acceleration += Steer((alignment / alignmentCohesionCount), ref data) * _alignmentWeight;
                data.acceleration += Steer((cohesion / alignmentCohesionCount) - data.position, ref data) * _cohesionWeight;
            }
            data.acceleration += SteerToBounds(ref data) * _boundsWeight;
            _agents[i] = data;
        }
    }

    private void UpdateAgentMovementAndAnimation(float deltaTime)
    {
        for (int i = 0; i < _agents.Count; i++)
        {
            var data = _agents[i];
            data.velocity += data.acceleration * deltaTime;
            float speed = data.velocity.magnitude;
            float newSpeed = Mathf.Clamp(speed, _speedRange.x, _speedRange.y);
            if (speed > 0.001f)
            {
                data.velocity = data.velocity * (newSpeed / speed);
            }
            data.position += data.velocity * deltaTime;

            data.agentComponent.transform.SetPositionAndRotation(data.position, Quaternion.LookRotation(data.velocity));
            UpdateAnimationBasedOnVelocity(data);
            _agents[i] = data;
        }
    }

    private Vector3 Steer(Vector3 target, ref BoidAgentData data)
    {
        Vector3 desired = target.normalized * _speedRange.y;
        Vector3 steer = desired - data.velocity;
        return Vector3.ClampMagnitude(steer, _maxSteerForce);
    }

    private Vector3 SteerToBounds(ref BoidAgentData data)
    {
        Vector3 offset = this.transform.position - data.position;
        float radius = _spawnBounds.x * 0.5f;
        if (offset.magnitude > radius)
        {
            return Steer(offset, ref data);
        }
        return Vector3.zero;
    }

    private void UpdateAnimationBasedOnVelocity(BoidAgentData data)
    {
        float speed = data.velocity.magnitude;
        if (speed > _sprintSpeedThreshold) data.agentComponent.CrossFade(_sprintClip, _animationFadeDuration);
        else if (speed > _runSpeedThreshold) data.agentComponent.CrossFade(_runClip, _animationFadeDuration);
        else if (speed > _walkSpeedThreshold) data.agentComponent.CrossFade(_walkClip, _animationFadeDuration);
        else data.agentComponent.CrossFade(_idleClip, _animationFadeDuration);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.4f);
        Gizmos.DrawWireCube(transform.position, _spawnBounds);
    }
}