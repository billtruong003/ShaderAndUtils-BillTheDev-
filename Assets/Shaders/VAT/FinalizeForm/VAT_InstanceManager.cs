using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Mathematics;

namespace OptimizeVariousVAT
{
    [DefaultExecutionOrder(-100)]
    public class VAT_InstanceManager : MonoBehaviour
    {
        #region Inner Classes
        [System.Serializable]
        public class AgentTypeDefinition
        {
            public VAT_BoidsAgent agentPrefab;
            public VAT_AnimationData animationData;
            public Texture2D albedoTexture;
        }

        private class RenderBatch
        {
            public readonly VAT_AnimationData animationData;
            public readonly Material instanceMaterial;
            public readonly Material depthNormalMaterial;
            public readonly ComputeShader cullingShader;
            public readonly List<ManagedAgent> agents = new List<ManagedAgent>(50000);
            public readonly Mesh bakedMesh;

            private readonly uint[] _indirectArgs = { 0, 0, 0, 0, 0 };
            private readonly int _cullingKernel;
            private static readonly Vector4[] FrustumPlanesV4 = new Vector4[6];

            public ComputeBuffer allAgentDataSourceBuffer;
            public ComputeBuffer allAgentRotationSourceBuffer;
            public ComputeBuffer allAgentAnimationSourceBuffer;
            public ComputeBuffer visibleAgentDataOutputBuffer;
            public ComputeBuffer indirectArgsBuffer;

            private AgentSourceData[] _sourceDataArray;
            private float4[] _rotationDataArray;
            private Vector4[] _animationDataArray;

            public RenderBatch(Material sourceMaterial, Shader depthNormalShader, ComputeShader computeShader, VAT_AnimationData animData, Texture2D albedo)
            {
                animationData = animData;
                bakedMesh = animData.bakedMesh;
                cullingShader = Instantiate(computeShader);
                instanceMaterial = new Material(sourceMaterial) { enableInstancing = true };
                depthNormalMaterial = new Material(depthNormalShader) { enableInstancing = true };
                instanceMaterial.SetTexture("_MainTex", albedo);

                SetCommonMaterialProperties(instanceMaterial, animData);
                SetCommonMaterialProperties(depthNormalMaterial, animData);

                if (bakedMesh != null)
                {
                    _indirectArgs[0] = bakedMesh.GetIndexCount(0);
                    _indirectArgs[2] = bakedMesh.GetBaseVertex(0);
                }
                _cullingKernel = cullingShader.FindKernel("CSMain");
            }

            private void SetCommonMaterialProperties(Material mat, VAT_AnimationData animData)
            {
                mat.SetTexture("_PositionTexture", animData.positionTexture);
                mat.SetVector("_PositionMin", animData.positionMinBounds);
                mat.SetVector("_PositionMax", animData.positionMaxBounds);
            }

            public void EnsureBufferCapacity()
            {
                int requiredCapacity = agents.Count > 0 ? agents.Count : 1;
                if ((allAgentDataSourceBuffer?.count ?? 0) >= requiredCapacity) return;

                ReleaseBuffers();
                int capacity = Mathf.NextPowerOfTwo(requiredCapacity);

                allAgentDataSourceBuffer = new ComputeBuffer(capacity, sizeof(float) * 3);
                allAgentRotationSourceBuffer = new ComputeBuffer(capacity, sizeof(float) * 4);
                allAgentAnimationSourceBuffer = new ComputeBuffer(capacity, sizeof(float) * 4);
                visibleAgentDataOutputBuffer = new ComputeBuffer(capacity, sizeof(float) * (16 + 4), ComputeBufferType.Append);
                indirectArgsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);

                _sourceDataArray = new AgentSourceData[capacity];
                _rotationDataArray = new float4[capacity];
                _animationDataArray = new Vector4[capacity];

                instanceMaterial.SetBuffer("_AgentDataBuffer", visibleAgentDataOutputBuffer);
                depthNormalMaterial.SetBuffer("_AgentDataBuffer", visibleAgentDataOutputBuffer);
            }

            public void UpdateGpuData()
            {
                for (int i = 0; i < agents.Count; i++)
                {
                    var agent = agents[i];
                    _sourceDataArray[i].position = agent.transform.position;
                    _rotationDataArray[i] = ((quaternion)agent.transform.rotation).value;
                    _animationDataArray[i] = CalculateAnimationData(agent);
                }

                allAgentDataSourceBuffer.SetData(_sourceDataArray, 0, 0, agents.Count);
                allAgentRotationSourceBuffer.SetData(_rotationDataArray, 0, 0, agents.Count);
                allAgentAnimationSourceBuffer.SetData(_animationDataArray, 0, 0, agents.Count);
            }

            private Vector4 CalculateAnimationData(ManagedAgent agent)
            {
                float currentV = CalculateNormalizedV(animationData, agent.currentClip, agent.currentTimeSeconds);
                float previousV = 0f;
                float blendWeight = 0f;

                if (agent.isBlending && agent.previousClip != null)
                {
                    previousV = CalculateNormalizedV(animationData, agent.previousClip, agent.previousTimeSeconds);
                    blendWeight = agent.crossFadeDuration > 0 ? Mathf.Clamp01(agent.crossFadeTimer / agent.crossFadeDuration) : 1f;
                }
                return new Vector4(currentV, previousV, blendWeight, 0);
            }

            public void DispatchCulling(Plane[] planes, float agentBoundsRadius)
            {
                _indirectArgs[1] = 0;
                indirectArgsBuffer.SetData(_indirectArgs);
                visibleAgentDataOutputBuffer.SetCounterValue(0);

                cullingShader.SetBuffer(_cullingKernel, "_AllAgentDataSource", allAgentDataSourceBuffer);
                cullingShader.SetBuffer(_cullingKernel, "_AllAgentRotationSource", allAgentRotationSourceBuffer);
                cullingShader.SetBuffer(_cullingKernel, "_AllAgentAnimationSource", allAgentAnimationSourceBuffer);
                cullingShader.SetBuffer(_cullingKernel, "_VisibleAgentDataOutput", visibleAgentDataOutputBuffer);
                cullingShader.SetBuffer(_cullingKernel, "_IndirectArgsBuffer", indirectArgsBuffer);

                for (int i = 0; i < 6; i++)
                {
                    FrustumPlanesV4[i] = new Vector4(planes[i].normal.x, planes[i].normal.y, planes[i].normal.z, planes[i].distance);
                }
                cullingShader.SetVectorArray("_FrustumPlanes", FrustumPlanesV4);
                cullingShader.SetFloat("_AgentBoundsRadius", agentBoundsRadius);
                cullingShader.SetInt("_MaxAgents", agents.Count);

                int threadGroups = Mathf.CeilToInt((float)agents.Count / 64.0f);
                cullingShader.Dispatch(_cullingKernel, threadGroups, 1, 1);
            }

            public void ReleaseBuffers()
            {
                allAgentDataSourceBuffer?.Release();
                allAgentRotationSourceBuffer?.Release();
                allAgentAnimationSourceBuffer?.Release();
                visibleAgentDataOutputBuffer?.Release();
                indirectArgsBuffer?.Release();
            }
        }

        private struct ManagedAgent
        {
            public Transform transform;
            public VAT_BoidsAgent agentComponent;
            public VAT_AnimationData.ClipInfo currentClip;
            public VAT_AnimationData.ClipInfo previousClip;
            public float currentTimeSeconds;
            public float previousTimeSeconds;
            public float crossFadeTimer;
            public float crossFadeDuration;
            public bool isBlending;
        }

        private struct AgentSourceData { public float3 position; }
        #endregion

        [Title("Configuration")]
        [SerializeField, Required] private Material _baseInstancedMaterial;
        [SerializeField, Required] private Shader _depthNormalShader;
        [SerializeField, Required] private ComputeShader _cullingComputeShader;
        [SerializeField, ListDrawerSettings] private List<AgentTypeDefinition> _agentTypes;
        [SerializeField] private float _agentBoundsRadius = 1.0f;

        [Title("Outline Configuration")]
        [SerializeField, Range(-0.1f, 1f)] private float _outlineDepthOffset = 0.0f;

        private readonly List<RenderBatch> _renderBatches = new List<RenderBatch>();
        private Camera _mainCamera;
        private readonly Plane[] _frustumPlanes = new Plane[6];
        private readonly Bounds _renderBounds = new Bounds(Vector3.zero, Vector3.one * 10000f);

        public bool IsInitialized { get; private set; }

        public int GetAgentTypeCount() => _agentTypes.Count;
        public VAT_BoidsAgent GetAgentPrefab(int typeIndex) => (typeIndex >= 0 && typeIndex < _agentTypes.Count) ? _agentTypes[typeIndex].agentPrefab : null;

        private void Awake()
        {
            _mainCamera = Camera.main;
            Initialize();
        }

        private void OnDestroy()
        {
            foreach (var batch in _renderBatches)
            {
                batch.ReleaseBuffers();
            }
        }

        private void LateUpdate()
        {
            if (!IsInitialized || _mainCamera == null) return;

            GeometryUtility.CalculateFrustumPlanes(_mainCamera, _frustumPlanes);

            foreach (var batch in _renderBatches)
            {
                if (batch.agents.Count > 0)
                {
                    UpdateAndRenderBatchGPU(batch);
                }
            }
        }

        private void UpdateAndRenderBatchGPU(RenderBatch batch)
        {
            float deltaTime = Time.deltaTime;
            for (int i = 0; i < batch.agents.Count; i++)
            {
                var agent = batch.agents[i];
                UpdateTimers(ref agent, deltaTime);
                batch.agents[i] = agent;
            }

            batch.UpdateGpuData();
            batch.DispatchCulling(_frustumPlanes, _agentBoundsRadius);
            batch.depthNormalMaterial.SetFloat("_OutlineDepthOffset", _outlineDepthOffset);

            Graphics.DrawMeshInstancedIndirect(batch.bakedMesh, 0, batch.depthNormalMaterial, _renderBounds, batch.indirectArgsBuffer);
            Graphics.DrawMeshInstancedIndirect(batch.bakedMesh, 0, batch.instanceMaterial, _renderBounds, batch.indirectArgsBuffer);
        }

        public void Register(VAT_BoidsAgent agent, int agentTypeIndex)
        {
            if (!IsInitialized || agent.StateIndex != -1 || agentTypeIndex < 0 || agentTypeIndex >= _renderBatches.Count) return;
            var batch = _renderBatches[agentTypeIndex];
            agent.StateIndex = batch.agents.Count;
            var newManagedAgent = new ManagedAgent { transform = agent.transform, agentComponent = agent };
            PlayDefaultClip(ref newManagedAgent, batch.animationData);
            batch.agents.Add(newManagedAgent);
            batch.EnsureBufferCapacity();
        }

        public void Unregister(VAT_BoidsAgent agent, int agentTypeIndex)
        {
            if (!IsInitialized || agent.StateIndex == -1 || agentTypeIndex < 0 || agentTypeIndex >= _renderBatches.Count) return;

            var batch = _renderBatches[agentTypeIndex];
            int indexToRemove = agent.StateIndex;
            if (indexToRemove < 0 || indexToRemove >= batch.agents.Count || batch.agents[indexToRemove].agentComponent != agent) return;

            int lastIndex = batch.agents.Count - 1;
            if (indexToRemove == lastIndex)
            {
                batch.agents.RemoveAt(lastIndex);
            }
            else
            {
                ManagedAgent lastAgent = batch.agents[lastIndex];
                lastAgent.agentComponent.StateIndex = indexToRemove;
                batch.agents[indexToRemove] = lastAgent;
                batch.agents.RemoveAt(lastIndex);
            }
            agent.StateIndex = -1;
        }

        public void SetAnimationState(int stateIndex, int agentTypeIndex, string clipName, float duration)
        {
            if (!IsInitialized || agentTypeIndex < 0 || agentTypeIndex >= _renderBatches.Count) return;
            var batch = _renderBatches[agentTypeIndex];
            if (stateIndex < 0 || stateIndex >= batch.agents.Count) return;
            if (!batch.animationData.TryGetClipInfo(clipName, out var newClip)) return;

            var currentState = batch.agents[stateIndex];
            if (currentState.currentClip != null && currentState.currentClip.name == newClip.name) return;

            currentState.previousClip = currentState.currentClip;
            currentState.previousTimeSeconds = currentState.currentTimeSeconds;
            currentState.currentClip = newClip;
            currentState.currentTimeSeconds = 0;
            currentState.crossFadeDuration = Mathf.Max(0, duration);
            currentState.crossFadeTimer = 0;
            currentState.isBlending = duration > 0.001f && currentState.previousClip != null;
            batch.agents[stateIndex] = currentState;
        }

        private void Initialize()
        {
            if (_baseInstancedMaterial == null || _depthNormalShader == null || _cullingComputeShader == null || _agentTypes == null)
            {
                IsInitialized = false;
                return;
            }

            _renderBatches.Clear();
            foreach (var agentType in _agentTypes)
            {
                if (agentType.agentPrefab == null || agentType.animationData == null || !agentType.animationData.IsValid() || agentType.albedoTexture == null) continue;
                agentType.agentPrefab.gameObject.SetActive(false);
                var newBatch = new RenderBatch(_baseInstancedMaterial, _depthNormalShader, _cullingComputeShader, agentType.animationData, agentType.albedoTexture);
                newBatch.EnsureBufferCapacity();
                _renderBatches.Add(newBatch);
            }
            IsInitialized = _renderBatches.Count > 0;
        }

        private void UpdateTimers(ref ManagedAgent agent, float deltaTime)
        {
            agent.currentTimeSeconds += deltaTime;
            if (agent.currentClip.wrapMode == WrapMode.Loop && agent.currentClip.duration > 0)
                agent.currentTimeSeconds %= agent.currentClip.duration;

            if (agent.isBlending)
            {
                agent.crossFadeTimer += deltaTime;
                if (agent.crossFadeTimer >= agent.crossFadeDuration)
                {
                    agent.isBlending = false;
                    agent.previousClip = null;
                }

                if (agent.previousClip != null)
                {
                    agent.previousTimeSeconds += deltaTime;
                    if (agent.previousClip.wrapMode == WrapMode.Loop && agent.previousClip.duration > 0)
                        agent.previousTimeSeconds %= agent.previousClip.duration;
                }
            }
        }

        private static float CalculateNormalizedV(VAT_AnimationData data, VAT_AnimationData.ClipInfo clip, float timeSeconds)
        {
            if (clip == null || data.positionTexture.height <= 1) return 0f;
            float progress = 0;
            if (clip.duration > 0)
            {
                switch (clip.wrapMode)
                {
                    case WrapMode.Loop: progress = Mathf.Repeat(timeSeconds, clip.duration) / clip.duration; break;
                    case WrapMode.PingPong: progress = Mathf.PingPong(timeSeconds, clip.duration) / clip.duration; break;
                    default: progress = Mathf.Clamp01(timeSeconds / clip.duration); break;
                }
            }
            float frameIndex = progress * (clip.frameCount - 1);
            float absoluteFrame = clip.startFrame + frameIndex;
            return (absoluteFrame + 0.5f) / data.positionTexture.height;
        }

        private void PlayDefaultClip(ref ManagedAgent agent, VAT_AnimationData data)
        {
            if (data.animationClips.Count > 0)
            {
                agent.currentClip = data.animationClips[0];
                agent.agentComponent.UpdateCurrentAnimationName(agent.currentClip.name);
            }
        }
    }
}