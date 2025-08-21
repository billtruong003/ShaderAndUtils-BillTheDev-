using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public class VAT_InstanceManager : MonoBehaviour
{
    [Title("Configuration")]
    [Required("Phải gán Animation Data Asset.")]
    [SerializeField]
    private VAT_AnimationData _animationDataAsset;

    [Required("Phải gán một Material nguồn sử dụng shader Instanced.")]
    [SerializeField]
    private Material _sourceMaterial;

    [Title("Live Debugging State")]
    [ShowInInspector, ReadOnly, ListDrawerSettings(IsReadOnly = true, Expanded = true)]
    private List<ManagedAgent> _agents = new List<ManagedAgent>(25000);

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

    private List<Matrix4x4> _matrices = new List<Matrix4x4>(25000);
    private Vector4[] _animationDataBuffer;
    private Material _instanceMaterial;
    private MaterialPropertyBlock _propertyBlock;

    private static readonly int AnimationDataID = Shader.PropertyToID("_AnimationData");

    private void Start()
    {
        Initialize();
    }

    private void LateUpdate()
    {
        if (_agents.Count == 0 || _instanceMaterial == null) return;
        UpdateAndRenderAllAgents(Time.deltaTime);
    }

    public void Register(VAT_BoidsAgent agent)
    {
        if (agent.StateIndex != -1 || agent.animationData != _animationDataAsset) return;

        agent.StateIndex = _agents.Count;
        var newManagedAgent = new ManagedAgent { transform = agent.transform, agentComponent = agent };
        PlayDefaultClip(ref newManagedAgent);
        _agents.Add(newManagedAgent);
    }

    public void Unregister(VAT_BoidsAgent agent)
    {
        int indexToRemove = agent.StateIndex;
        if (indexToRemove < 0 || indexToRemove >= _agents.Count || _agents[indexToRemove].agentComponent != agent) return;

        int lastIndex = _agents.Count - 1;

        if (indexToRemove == lastIndex)
        {
            _agents.RemoveAt(lastIndex);
        }
        else
        {
            ManagedAgent lastAgent = _agents[lastIndex];
            lastAgent.agentComponent.StateIndex = indexToRemove;
            _agents[indexToRemove] = lastAgent;
            _agents.RemoveAt(lastIndex);
        }
        agent.StateIndex = -1;
    }

    public void SetAnimationState(int stateIndex, string clipName, float duration)
    {
        if (stateIndex < 0 || stateIndex >= _agents.Count) return;
        if (!_animationDataAsset.TryGetClipInfo(clipName, out var newClip)) return;

        var currentState = _agents[stateIndex];
        if (currentState.currentClip != null && currentState.currentClip.name == newClip.name) return;

        currentState.previousClip = currentState.currentClip;
        currentState.previousTimeSeconds = currentState.currentTimeSeconds;
        currentState.currentClip = newClip;
        currentState.currentTimeSeconds = 0;
        currentState.crossFadeDuration = Mathf.Max(0, duration);
        currentState.crossFadeTimer = 0;
        currentState.isBlending = duration > 0.001f && currentState.previousClip != null;
        _agents[stateIndex] = currentState;
    }

    private void Initialize()
    {
        if (_animationDataAsset == null || !_animationDataAsset.IsValid() || _sourceMaterial == null)
        {
            this.enabled = false;
            return;
        }

        _instanceMaterial = new Material(_sourceMaterial)
        {
            enableInstancing = true
        };
        _instanceMaterial.SetTexture("_PositionTexture", _animationDataAsset.positionTexture);
        _instanceMaterial.SetVector("_PositionMin", _animationDataAsset.positionMinBounds);
        _instanceMaterial.SetVector("_PositionMax", _animationDataAsset.positionMaxBounds);
        _propertyBlock = new MaterialPropertyBlock();

        int capacity = 25000;
        _animationDataBuffer = new Vector4[capacity];
    }

    private void UpdateAndRenderAllAgents(float deltaTime)
    {
        EnsureBufferCapacity();
        _matrices.Clear();

        for (int i = 0; i < _agents.Count; i++)
        {
            var agent = _agents[i];
            if (agent.transform == null || !agent.transform.gameObject.activeInHierarchy) continue;

            UpdateTimers(ref agent, deltaTime);
            _matrices.Add(agent.transform.localToWorldMatrix);

            float currentV = CalculateNormalizedVCoordinate(agent.currentClip, agent.currentTimeSeconds);
            float previousV = 0f;
            float blendWeight = 0f;

            if (agent.isBlending && agent.previousClip != null)
            {
                previousV = CalculateNormalizedVCoordinate(agent.previousClip, agent.previousTimeSeconds);
                blendWeight = agent.crossFadeDuration > 0 ? Mathf.Clamp01(agent.crossFadeTimer / agent.crossFadeDuration) : 1f;
            }

            _animationDataBuffer[i] = new Vector4(currentV, previousV, blendWeight, 0);
            _agents[i] = agent;
        }

        if (_matrices.Count == 0) return;

        _propertyBlock.SetVectorArray(AnimationDataID, _animationDataBuffer);
        Graphics.DrawMeshInstanced(
            _animationDataAsset.bakedMesh, 0, _instanceMaterial, _matrices, _propertyBlock
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

    private float CalculateNormalizedVCoordinate(VAT_AnimationData.ClipInfo clip, float timeSeconds)
    {
        if (clip == null || _animationDataAsset.positionTexture.height <= 1) return 0f;
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
        return (absoluteFrame + 0.5f) / _animationDataAsset.positionTexture.height;
    }

    private void PlayDefaultClip(ref ManagedAgent agent)
    {
        if (_animationDataAsset.animationClips.Count > 0)
        {
            agent.currentClip = _animationDataAsset.animationClips[0];
            agent.agentComponent.UpdateCurrentAnimationName(agent.currentClip.name);
        }
    }

    private void EnsureBufferCapacity()
    {
        int requiredCapacity = _agents.Count;
        if (_animationDataBuffer.Length < requiredCapacity)
        {
            int newCapacity = Mathf.NextPowerOfTwo(requiredCapacity);
            System.Array.Resize(ref _animationDataBuffer, newCapacity);
            _matrices.Capacity = newCapacity;
        }
    }
}