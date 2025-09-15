// File: Assets/Shaders/VAT/FinalizeForm/CrowdController_GPU_VAT.cs
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using Random = UnityEngine.Random;

namespace OptimizeVariousVAT
{
    [DefaultExecutionOrder(-50)]
    public class CrowdController_GPU_VAT : MonoBehaviour
    {
        #region Struct Definitions
        private struct Agent
        {
            public float3 position;
            public float3 velocity;
            public float3 forward;
            public float3 targetPosition;
            public int currentState;
            public float stateTimer;
            public uint randomSeed;
            public int typeIndex;
            public int currentClipIndex;
            public int previousClipIndex;
            public float currentTimeSeconds;
            public float previousTimeSeconds; // THÊM DÒNG NÀY
            public float crossFadeTimer;
        }

        private enum AgentBehaviorState { Idle = 0, Walking = 1, Running = 2, Flocking = 3 }

        [StructLayout(LayoutKind.Sequential)]
        private struct ClipInfoGPU { public int startFrame; public int frameCount; public float duration; public int wrapMode; }

        [StructLayout(LayoutKind.Sequential)]
        private struct AgentTypeInfo { public ClipInfoGPU clip0; public ClipInfoGPU clip1; public ClipInfoGPU clip2; public int textureHeight; }

        [System.Serializable]
        public class AgentTypeDefinition
        {
            public VAT_AnimationData animationData; public Texture2D albedoTexture;
            public string idleClipName = "idle";
            public string walkClipName = "walk";
            public string runClipName = "run";
        }
        #endregion

        #region Inspector Fields
        [TitleGroup("Performance & Spawning")]
        [SerializeField, Range(1, 200000)] private int _agentCount = 50000;
        [SerializeField, Required] private ComputeShader _crowdComputeShader;
        [SerializeField] private Vector3 _spawnBounds = new Vector3(200, 0, 200);

        [TitleGroup("Agent Types & Rendering")]
        [SerializeField, Required] private Material _baseInstancedMaterial;
        [SerializeField, ListDrawerSettings(ShowFoldout = true)] private List<AgentTypeDefinition> _agentTypes;
        [SerializeField] private float _agentBoundsRadius = 1.0f;

        [TitleGroup("Movement Settings")]
        [SerializeField] private float _walkSpeed = 3.5f;
        [SerializeField] private float _runSpeed = 8.0f;
        [SerializeField, Range(1f, 100f)] private float _maxSteerForce = 30f;
        [SerializeField, Range(0.1f, 5f)] private float _destinationReachedThreshold = 1f;

        [TitleGroup("Behavior Settings")]
        [SerializeField, Range(0f, 20f)] private float _separationRadius = 5f;
        [SerializeField, Range(0f, 10f)] private float _separationWeight = 3.0f;
        [SerializeField, Range(0f, 10f)] private float _cohesionWeight = 2.0f;
        [SerializeField, Range(0f, 10f)] private float _alignmentWeight = 2.5f;
        [SerializeField, Range(0f, 10f)] private float _goalSeekingWeight = 1.0f;

        [TitleGroup("State & Animation")]
        [SerializeField, MinMaxSlider(1f, 20f, true)] private Vector2 _idleDurationRange = new Vector2(2f, 8f);
        [SerializeField, MinMaxSlider(5f, 60f, true)] private Vector2 _flockingDurationRange = new Vector2(15f, 30f);
        [SerializeField, Range(0f, 1f)] private float _chanceToRunInsteadOfWalk = 0.15f;
        [SerializeField, Range(0f, 1f)] private float _chanceToFlock = 0.5f;
        [SerializeField, Range(0.1f, 1f)] private float _animationFadeDuration = 0.3f;

        [TitleGroup("Debug", "Bật tính năng này sẽ làm giảm hiệu năng đáng kể trong Editor.")]
        [SerializeField] private bool _enableDebugGizmos;
        [SerializeField, ShowIf("_enableDebugGizmos")] private bool _drawGridCells = true;
        [SerializeField, ShowIf("_enableDebugGizmos")] private bool _focusOnSingleAgent = true;
        [SerializeField, ShowIf("_enableDebugGizmos"), Range(0, 200000)] private int _debugAgentIndex = 0;
        #endregion

        private const int THREAD_GROUP_SIZE = 256;

        private ComputeBuffer _agentsBuffer, _agentTypeInfoBuffer, _gridBuffer, _gridIndicesBuffer;
        private readonly List<RenderBatch> _renderBatches = new List<RenderBatch>();
        private int _threadGroups;
        private Camera _mainCamera;
        private int _kernelClearGrid, _kernelUpdateGrid, _kernelSimulation, _kernelIntegration, _kernelPrepareDraw;
        private readonly Plane[] _cameraFrustumPlanes = new Plane[6];
        private readonly Vector4[] _frustumPlanesV4 = new Vector4[6];

        private static class ShaderIDs
        {
            public static readonly int AgentsRO = Shader.PropertyToID("_AgentsRO");
            public static readonly int AgentsRW = Shader.PropertyToID("_AgentsRW");
            public static readonly int AgentTypeInfo = Shader.PropertyToID("_AgentTypeInfo");
            public static readonly int Grid = Shader.PropertyToID("_Grid");
            public static readonly int GridIndices = Shader.PropertyToID("_GridIndices");
            public static readonly int DeltaTime = Shader.PropertyToID("_DeltaTime");
            public static readonly int Time = Shader.PropertyToID("_Time");
            public static readonly int FrustumPlanes = Shader.PropertyToID("_FrustumPlanes");
            public static readonly int GridSide = Shader.PropertyToID("_GridSide");
            public static readonly int InverseCellSize = Shader.PropertyToID("_InverseCellSize");
            public static readonly int MaxAgents = Shader.PropertyToID("_MaxAgents");
            public static readonly int SpawnBounds = Shader.PropertyToID("_SpawnBounds");
            public static readonly int WalkSpeed = Shader.PropertyToID("_WalkSpeed");
            public static readonly int RunSpeed = Shader.PropertyToID("_RunSpeed");
            public static readonly int MaxSteerForce = Shader.PropertyToID("_MaxSteerForce");
            public static readonly int DestinationReachedThreshold = Shader.PropertyToID("_DestinationReachedThreshold");
            public static readonly int SeparationRadiusSq = Shader.PropertyToID("_SeparationRadiusSq");
            public static readonly int SeparationWeight = Shader.PropertyToID("_SeparationWeight");
            public static readonly int CohesionWeight = Shader.PropertyToID("_CohesionWeight");
            public static readonly int AlignmentWeight = Shader.PropertyToID("_AlignmentWeight");
            public static readonly int GoalSeekingWeight = Shader.PropertyToID("_GoalSeekingWeight");
            public static readonly int IdleDurationRange = Shader.PropertyToID("_IdleDurationRange");
            public static readonly int FlockingDurationRange = Shader.PropertyToID("_FlockingDurationRange");
            public static readonly int ChanceToRun = Shader.PropertyToID("_ChanceToRun");
            public static readonly int ChanceToFlock = Shader.PropertyToID("_ChanceToFlock");
            public static readonly int AnimationFadeDuration = Shader.PropertyToID("_AnimationFadeDuration");
            public static readonly int AgentBoundsRadius = Shader.PropertyToID("_AgentBoundsRadius");
            public static readonly int AgentDataBuffer = Shader.PropertyToID("_AgentDataBuffer");
        }

        private class RenderBatch
        {
            public readonly Material material; public readonly Mesh mesh;
            public readonly ComputeBuffer indirectArgsBuffer; public readonly ComputeBuffer visibleAgentDataOutputBuffer;
            private readonly uint[] _indirectArgs = { 0, 0, 0, 0, 0 };
            private readonly Bounds _renderBounds = new Bounds(Vector3.zero, Vector3.one * 10000f);

            public RenderBatch(Material baseMaterial, VAT_AnimationData animData, Texture2D albedo, int capacity)
            {
                mesh = animData.bakedMesh;
                material = new Material(baseMaterial) { enableInstancing = true };
                material.SetTexture("_MainTex", albedo);
                material.SetTexture("_PositionTexture", animData.positionTexture);
                material.SetVector("_PositionMin", animData.positionMinBounds);
                material.SetVector("_PositionMax", animData.positionMaxBounds);
                if (mesh != null) { _indirectArgs[0] = mesh.GetIndexCount(0); _indirectArgs[2] = mesh.GetBaseVertex(0); }
                indirectArgsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
                visibleAgentDataOutputBuffer = new ComputeBuffer(capacity, (16 + 4) * sizeof(float), ComputeBufferType.Append);
                material.SetBuffer(ShaderIDs.AgentDataBuffer, visibleAgentDataOutputBuffer);
            }
            public void PrepareForDispatch() { _indirectArgs[1] = 0; indirectArgsBuffer.SetData(_indirectArgs); visibleAgentDataOutputBuffer.SetCounterValue(0); }
            public void Draw() { Graphics.DrawMeshInstancedIndirect(mesh, 0, material, _renderBounds, indirectArgsBuffer, 0, null); }
            public void ReleaseBuffers() { indirectArgsBuffer?.Release(); visibleAgentDataOutputBuffer?.Release(); }
        }

        private void Start()
        {
            _mainCamera = Camera.main;
            InitializeAgentDataAndBuffers();
            InitializeRenderBatches();
            InitializeComputeShader();
        }

        private void Update()
        {
            if (_mainCamera == null || _agentsBuffer == null) return;
            UpdateShaderUniforms();
            foreach (var batch in _renderBatches) { batch.PrepareForDispatch(); }
            _crowdComputeShader.Dispatch(_kernelClearGrid, Mathf.CeilToInt((float)_gridBuffer.count / THREAD_GROUP_SIZE), 1, 1);
            _crowdComputeShader.Dispatch(_kernelUpdateGrid, _threadGroups, 1, 1);
            _crowdComputeShader.Dispatch(_kernelSimulation, _threadGroups, 1, 1);
            _crowdComputeShader.Dispatch(_kernelIntegration, _threadGroups, 1, 1);
            _crowdComputeShader.Dispatch(_kernelPrepareDraw, _threadGroups, 1, 1);
            foreach (var batch in _renderBatches) { batch.Draw(); }
        }

        private void OnDestroy()
        {
            _agentsBuffer?.Release(); _agentTypeInfoBuffer?.Release();
            _gridBuffer?.Release(); _gridIndicesBuffer?.Release();
            foreach (var batch in _renderBatches) batch.ReleaseBuffers();
        }

        private void InitializeAgentDataAndBuffers()
        {
            var agents = new Agent[_agentCount];
            for (int i = 0; i < _agentCount; i++)
            {
                int typeIndex = Random.Range(0, _agentTypes.Count);
                agents[i] = new Agent
                {
                    position = GetRandomPositionInBounds(),
                    forward = math.normalize(new float3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f))),
                    currentState = (int)AgentBehaviorState.Idle,
                    stateTimer = Random.Range(_idleDurationRange.x, _idleDurationRange.y),
                    randomSeed = (uint)Random.Range(1, 100000),
                    typeIndex = typeIndex,
                    currentClipIndex = 0,
                    previousClipIndex = -1,
                    currentTimeSeconds = 0,
                    previousTimeSeconds = 0, // KHỞI TẠO GIÁ TRỊ
                    crossFadeTimer = _animationFadeDuration + 1
                };
            }
            _agentsBuffer = new ComputeBuffer(_agentCount, Marshal.SizeOf(typeof(Agent)));
            _agentsBuffer.SetData(agents);

            var typeInfos = new AgentTypeInfo[_agentTypes.Count];
            for (int i = 0; i < _agentTypes.Count; i++)
            {
                var definition = _agentTypes[i];
                definition.animationData.TryGetClipInfo(definition.idleClipName, out var idleClip);
                definition.animationData.TryGetClipInfo(definition.walkClipName, out var walkClip);
                definition.animationData.TryGetClipInfo(definition.runClipName, out var runClip);
                typeInfos[i] = new AgentTypeInfo
                {
                    clip0 = ToGPU(idleClip),
                    clip1 = ToGPU(walkClip),
                    clip2 = ToGPU(runClip),
                    textureHeight = definition.animationData.positionTexture.height
                };
            }
            _agentTypeInfoBuffer = new ComputeBuffer(_agentTypes.Count, Marshal.SizeOf(typeof(AgentTypeInfo)));
            _agentTypeInfoBuffer.SetData(typeInfos);

            float gridCellSize = math.max(_separationRadius, 2f);
            int gridSide = Mathf.CeilToInt(math.max(_spawnBounds.x, _spawnBounds.z) / gridCellSize) + 1;
            _gridBuffer = new ComputeBuffer(gridSide * gridSide, sizeof(uint));
            _gridIndicesBuffer = new ComputeBuffer(_agentCount, sizeof(uint));
        }

        private void InitializeRenderBatches()
        {
            for (int i = 0; i < _agentTypes.Count; i++)
            {
                var type = _agentTypes[i];
                var batch = new RenderBatch(_baseInstancedMaterial, type.animationData, type.albedoTexture, _agentCount);
                _renderBatches.Add(batch);
            }
        }

        private void InitializeComputeShader()
        {
            _kernelClearGrid = _crowdComputeShader.FindKernel("ClearGrid");
            _kernelUpdateGrid = _crowdComputeShader.FindKernel("UpdateGrid");
            _kernelSimulation = _crowdComputeShader.FindKernel("Simulation");
            _kernelIntegration = _crowdComputeShader.FindKernel("Integration");
            _kernelPrepareDraw = _crowdComputeShader.FindKernel("PrepareDraw");

            float gridCellSize = math.max(_separationRadius, 2f);
            int gridSide = Mathf.CeilToInt(math.max(_spawnBounds.x, _spawnBounds.z) / gridCellSize) + 1;

            _crowdComputeShader.SetInt(ShaderIDs.GridSide, gridSide);
            _crowdComputeShader.SetFloat(ShaderIDs.InverseCellSize, 1.0f / gridCellSize);
            _crowdComputeShader.SetInt(ShaderIDs.MaxAgents, _agentCount);
            _crowdComputeShader.SetVector(ShaderIDs.SpawnBounds, _spawnBounds);
            _crowdComputeShader.SetFloat(ShaderIDs.WalkSpeed, _walkSpeed);
            _crowdComputeShader.SetFloat(ShaderIDs.RunSpeed, _runSpeed);
            _crowdComputeShader.SetFloat(ShaderIDs.MaxSteerForce, _maxSteerForce);
            _crowdComputeShader.SetFloat(ShaderIDs.DestinationReachedThreshold, _destinationReachedThreshold);
            _crowdComputeShader.SetFloat(ShaderIDs.SeparationRadiusSq, _separationRadius * _separationRadius);
            _crowdComputeShader.SetFloat(ShaderIDs.SeparationWeight, _separationWeight);
            _crowdComputeShader.SetFloat(ShaderIDs.CohesionWeight, _cohesionWeight);
            _crowdComputeShader.SetFloat(ShaderIDs.AlignmentWeight, _alignmentWeight);
            _crowdComputeShader.SetFloat(ShaderIDs.GoalSeekingWeight, _goalSeekingWeight);
            _crowdComputeShader.SetVector(ShaderIDs.IdleDurationRange, _idleDurationRange);
            _crowdComputeShader.SetVector(ShaderIDs.FlockingDurationRange, _flockingDurationRange);
            _crowdComputeShader.SetFloat(ShaderIDs.ChanceToRun, _chanceToRunInsteadOfWalk);
            _crowdComputeShader.SetFloat(ShaderIDs.ChanceToFlock, _chanceToFlock);
            _crowdComputeShader.SetFloat(ShaderIDs.AnimationFadeDuration, _animationFadeDuration);
            _crowdComputeShader.SetFloat(ShaderIDs.AgentBoundsRadius, _agentBoundsRadius);

            _threadGroups = Mathf.CeilToInt((float)_agentCount / THREAD_GROUP_SIZE);

            BindAllComputeBuffers();
        }

        private void BindAllComputeBuffers()
        {
            _crowdComputeShader.SetBuffer(_kernelClearGrid, ShaderIDs.Grid, _gridBuffer);
            _crowdComputeShader.SetBuffer(_kernelUpdateGrid, ShaderIDs.AgentsRO, _agentsBuffer);
            _crowdComputeShader.SetBuffer(_kernelUpdateGrid, ShaderIDs.Grid, _gridBuffer);
            _crowdComputeShader.SetBuffer(_kernelUpdateGrid, ShaderIDs.GridIndices, _gridIndicesBuffer);
            _crowdComputeShader.SetBuffer(_kernelSimulation, ShaderIDs.AgentsRO, _agentsBuffer);
            _crowdComputeShader.SetBuffer(_kernelSimulation, ShaderIDs.AgentsRW, _agentsBuffer);
            _crowdComputeShader.SetBuffer(_kernelSimulation, ShaderIDs.Grid, _gridBuffer);
            _crowdComputeShader.SetBuffer(_kernelSimulation, ShaderIDs.GridIndices, _gridIndicesBuffer);
            _crowdComputeShader.SetBuffer(_kernelIntegration, ShaderIDs.AgentsRW, _agentsBuffer);
            _crowdComputeShader.SetBuffer(_kernelIntegration, ShaderIDs.AgentTypeInfo, _agentTypeInfoBuffer);
            _crowdComputeShader.SetBuffer(_kernelPrepareDraw, ShaderIDs.AgentsRO, _agentsBuffer);
            _crowdComputeShader.SetBuffer(_kernelPrepareDraw, ShaderIDs.AgentTypeInfo, _agentTypeInfoBuffer);
            for (int i = 0; i < _renderBatches.Count; i++)
            {
                _crowdComputeShader.SetBuffer(_kernelPrepareDraw, $"_VisibleAgentDataOutput{i}", _renderBatches[i].visibleAgentDataOutputBuffer);
                _crowdComputeShader.SetBuffer(_kernelPrepareDraw, $"_IndirectArgsBuffer{i}", _renderBatches[i].indirectArgsBuffer);
            }
        }

        private void UpdateShaderUniforms()
        {
            _crowdComputeShader.SetFloat(ShaderIDs.DeltaTime, Time.deltaTime);
            _crowdComputeShader.SetFloat(ShaderIDs.Time, Time.time);

            GeometryUtility.CalculateFrustumPlanes(_mainCamera, _cameraFrustumPlanes);
            for (int i = 0; i < 6; i++)
            {
                _frustumPlanesV4[i] = new Vector4(_cameraFrustumPlanes[i].normal.x, _cameraFrustumPlanes[i].normal.y, _cameraFrustumPlanes[i].normal.z, _cameraFrustumPlanes[i].distance);
            }
            _crowdComputeShader.SetVectorArray(ShaderIDs.FrustumPlanes, _frustumPlanesV4);
        }

        private Vector3 GetRandomPositionInBounds() => transform.position + new Vector3(Random.Range(-_spawnBounds.x / 2f, _spawnBounds.x / 2f), 0, Random.Range(-_spawnBounds.z / 2f, _spawnBounds.z / 2f));
        private ClipInfoGPU ToGPU(VAT_AnimationData.ClipInfo clip) => new ClipInfoGPU { startFrame = clip?.startFrame ?? 0, frameCount = clip?.frameCount ?? 1, duration = clip?.duration ?? 1f, wrapMode = clip != null ? (int)clip.wrapMode : 0 };
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.5f, 0.4f);
            Gizmos.DrawWireCube(transform.position, new Vector3(_spawnBounds.x, 0.1f, _spawnBounds.z));

            if (!_enableDebugGizmos || !Application.isPlaying) return;
            if (_agentsBuffer == null || _gridBuffer == null || _gridIndicesBuffer == null) return;

            float gridCellSize = math.max(_separationRadius, 2f);
            int gridSide = Mathf.CeilToInt(math.max(_spawnBounds.x, _spawnBounds.z) / gridCellSize) + 1;

            if (_drawGridCells)
            {
                Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.2f);
                Vector3 gridCenterOffset = new Vector3(gridSide * gridCellSize, 0, gridSide * gridCellSize) * 0.5f - new Vector3(gridCellSize, 0, gridCellSize) * 0.5f;
                for (int z = 0; z < gridSide; z++)
                    for (int x = 0; x < gridSide; x++)
                    {
                        Vector3 cellCenter = transform.position + new Vector3(x * gridCellSize, 0, z * gridCellSize) - gridCenterOffset;
                        Gizmos.DrawWireCube(cellCenter, new Vector3(gridCellSize, 0.1f, gridCellSize));
                    }
            }

            if (_focusOnSingleAgent)
            {
                _debugAgentIndex = Mathf.Clamp(_debugAgentIndex, 0, _agentCount - 1);

                var agents = new Agent[_agentCount];
                _agentsBuffer.GetData(agents);

                Agent debugAgent = agents[_debugAgentIndex];

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(debugAgent.position, _separationRadius);
                Gizmos.DrawRay(debugAgent.position, debugAgent.forward * 5f);

                Gizmos.color = Color.cyan;
                float inverseCellSize = 1.0f / gridCellSize;
                int gridCenterX = (int)(debugAgent.position.x * inverseCellSize + (gridSide / 2.0f));
                int gridCenterZ = (int)(debugAgent.position.z * inverseCellSize + (gridSide / 2.0f));

                for (int z = -1; z <= 1; z++)
                    for (int x = -1; x <= 1; x++)
                    {
                        int cellX = gridCenterX + x;
                        int cellZ = gridCenterZ + z;
                        uint hash = (uint)Mathf.Max(0, Mathf.Min(gridSide * gridSide - 1, cellX + cellZ * gridSide));

                        Vector3 gridCenterOffset = new Vector3(gridSide * gridCellSize, 0, gridSide * gridCellSize) * 0.5f - new Vector3(gridCellSize, 0, gridCellSize) * 0.5f;
                        Vector3 cellCenter = transform.position + new Vector3((cellX - 0.5f) * gridCellSize, 0, (cellZ - 0.5f) * gridCellSize) - gridCenterOffset;
                        Gizmos.DrawCube(cellCenter, new Vector3(gridCellSize, 0.1f, gridCellSize) * 0.95f);
                    }
            }
        }
    }
}