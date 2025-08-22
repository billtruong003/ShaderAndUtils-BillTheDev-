using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace OptimizeVariousVAT
{
    [DefaultExecutionOrder(-100)]
    public class VAT_InstanceManager : MonoBehaviour
    {
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
            public readonly MaterialPropertyBlock propertyBlock;
            public readonly List<ManagedAgent> agents = new List<ManagedAgent>(50000);
            public readonly List<Matrix4x4> matrices = new List<Matrix4x4>(50000);
            public Vector4[] animationDataBuffer;

            public RenderBatch(Material sourceMaterial, VAT_AnimationData animData, Texture2D albedo)
            {
                animationData = animData;
                instanceMaterial = new Material(sourceMaterial) { enableInstancing = true };
                instanceMaterial.SetTexture("_PositionTexture", animData.positionTexture);
                instanceMaterial.SetVector("_PositionMin", animData.positionMinBounds);
                instanceMaterial.SetVector("_PositionMax", animData.positionMaxBounds);
                instanceMaterial.SetTexture("_MainTex", albedo);

                propertyBlock = new MaterialPropertyBlock();
                animationDataBuffer = new Vector4[5000];
            }

            public void EnsureBufferCapacity()
            {
                int requiredCapacity = agents.Count;
                if (animationDataBuffer.Length < requiredCapacity)
                {
                    int newCapacity = Mathf.NextPowerOfTwo(requiredCapacity);
                    System.Array.Resize(ref animationDataBuffer, newCapacity);
                    matrices.Capacity = newCapacity;
                }
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

        [Title("Configuration")]
        [SerializeField, Required("Phải gán một Material nguồn sử dụng shader Instanced.")]
        private Material _baseInstancedMaterial;

        [SerializeField, ListDrawerSettings(Expanded = true)]
        private List<AgentTypeDefinition> _agentTypes;

        private readonly List<RenderBatch> _renderBatches = new List<RenderBatch>();
        private static readonly int AnimationDataID = Shader.PropertyToID("_AnimationData");

        public bool IsInitialized { get; private set; } = false;

        public int GetAgentTypeCount() => _agentTypes.Count;
        public VAT_BoidsAgent GetAgentPrefab(int typeIndex) => (typeIndex >= 0 && typeIndex < _agentTypes.Count) ? _agentTypes[typeIndex].agentPrefab : null;

        private void Awake()
        {
            Initialize();
        }

        private void LateUpdate()
        {
            if (!IsInitialized) return;

            float deltaTime = Time.deltaTime;
            foreach (var batch in _renderBatches)
            {
                if (batch.agents.Count > 0)
                {
                    UpdateAndRenderBatch(batch, deltaTime);
                }
            }
        }

        public void Register(VAT_BoidsAgent agent, int agentTypeIndex)
        {
            if (!IsInitialized)
            {
                Debug.LogError("Cố gắng đăng ký Agent khi InstanceManager chưa được khởi tạo thành công.", agent);
                return;
            }
            if (agent.StateIndex != -1) return; // Agent đã được đăng ký
            if (agentTypeIndex < 0 || agentTypeIndex >= _renderBatches.Count)
            {
                Debug.LogError($"Không thể đăng ký Agent. AgentTypeIndex không hợp lệ: {agentTypeIndex}. Có thể do cấu hình sai trong danh sách AgentTypes.", agent);
                return;
            }

            var batch = _renderBatches[agentTypeIndex];
            agent.StateIndex = batch.agents.Count;
            var newManagedAgent = new ManagedAgent { transform = agent.transform, agentComponent = agent };
            PlayDefaultClip(ref newManagedAgent, batch.animationData);
            batch.agents.Add(newManagedAgent);
        }

        public void Unregister(VAT_BoidsAgent agent, int agentTypeIndex)
        {
            if (!IsInitialized || agent.StateIndex == -1) return;
            if (agentTypeIndex < 0 || agentTypeIndex >= _renderBatches.Count)
            {
                Debug.LogError($"Không thể hủy đăng ký Agent. AgentTypeIndex không hợp lệ: {agentTypeIndex}.", agent);
                return;
            }

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

        // Assets/Shaders/VAT/FinalizeForm/VAT_InstanceManager.cs

        private void Initialize()
        {
            // THÊM LOG NGAY ĐẦU
            Debug.Log("[VAT_MANAGER_DEBUG] Bắt đầu Initialize().");

            IsInitialized = false;
            if (_baseInstancedMaterial == null)
            {
                Debug.LogError("[VAT_MANAGER_DEBUG] Lỗi: _baseInstancedMaterial bị null.", this);
                return;
            }

            // THÊM LOG KIỂM TRA LIST
            if (_agentTypes == null)
            {
                Debug.LogError("[VAT_MANAGER_DEBUG] Lỗi: List _agentTypes bị null!");
                return;
            }
            Debug.Log($"[VAT_MANAGER_DEBUG] Tìm thấy {_agentTypes.Count} agent types trong danh sách.");


            _renderBatches.Clear();
            for (int i = 0; i < _agentTypes.Count; i++)
            {
                var agentType = _agentTypes[i];
                // THÊM LOG KIỂM TRA TỪNG THÀNH PHẦN
                Debug.Log($"[VAT_MANAGER_DEBUG] Đang kiểm tra AgentType #{i}: " +
                          $"Prefab is null? {(agentType.agentPrefab == null)}. " +
                          $"AnimData is null? {(agentType.animationData == null)}. " +
                          $"Albedo is null? {(agentType.albedoTexture == null)}.");

                if (agentType.agentPrefab == null || agentType.animationData == null || !agentType.animationData.IsValid() || agentType.albedoTexture == null)
                {
                    Debug.LogError($"[VAT_MANAGER_DEBUG] AgentTypeDefinition #{i} KHÔNG HỢP LỆ. Bỏ qua.", this);
                    continue;
                }

                agentType.agentPrefab.gameObject.SetActive(false);
                _renderBatches.Add(new RenderBatch(_baseInstancedMaterial, agentType.animationData, agentType.albedoTexture));
                Debug.Log($"[VAT_MANAGER_DEBUG] Đã thêm thành công RenderBatch cho AgentType #{i}.");
            }

            if (_renderBatches.Count == 0)
            {
                Debug.LogError("[VAT_MANAGER_DEBUG] Lỗi nghiêm trọng: Không có RenderBatch nào được tạo. Tất cả các AgentTypeDefinition đều không hợp lệ.", this);
                return;
            }

            Debug.Log($"[VAT_MANAGER_DEBUG] Initialize() thành công! Tổng số RenderBatches được tạo: {_renderBatches.Count}");
            IsInitialized = true;
        }

        private void UpdateAndRenderBatch(RenderBatch batch, float deltaTime)
        {
            batch.EnsureBufferCapacity();
            batch.matrices.Clear();

            for (int i = 0; i < batch.agents.Count; i++)
            {
                var agent = batch.agents[i];
                if (agent.transform == null) continue;

                UpdateTimers(ref agent, deltaTime);
                batch.matrices.Add(agent.transform.localToWorldMatrix);

                float currentV = CalculateNormalizedVCoordinate(batch.animationData, agent.currentClip, agent.currentTimeSeconds);
                float previousV = 0f;
                float blendWeight = 0f;

                if (agent.isBlending && agent.previousClip != null)
                {
                    previousV = CalculateNormalizedVCoordinate(batch.animationData, agent.previousClip, agent.previousTimeSeconds);
                    blendWeight = agent.crossFadeDuration > 0 ? Mathf.Clamp01(agent.crossFadeTimer / agent.crossFadeDuration) : 1f;
                }

                batch.animationDataBuffer[i] = new Vector4(currentV, previousV, blendWeight, 0);
                batch.agents[i] = agent;
            }

            if (batch.matrices.Count == 0) return;

            batch.propertyBlock.SetVectorArray(AnimationDataID, batch.animationDataBuffer);
            Graphics.DrawMeshInstanced(
                batch.animationData.bakedMesh, 0, batch.instanceMaterial, batch.matrices, batch.propertyBlock
            );
        }

        private void UpdateTimers(ref ManagedAgent agent, float deltaTime)
        {
            agent.currentTimeSeconds += deltaTime;
            if (agent.currentClip.wrapMode == WrapMode.Loop && agent.currentClip.duration > 0)
            {
                agent.currentTimeSeconds %= agent.currentClip.duration;
            }

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
                    {
                        agent.previousTimeSeconds %= agent.previousClip.duration;
                    }
                }
            }
        }

        private float CalculateNormalizedVCoordinate(VAT_AnimationData data, VAT_AnimationData.ClipInfo clip, float timeSeconds)
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