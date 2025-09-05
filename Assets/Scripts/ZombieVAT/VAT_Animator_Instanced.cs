using UnityEngine;

namespace ZombieAI.VAT
{
    /// <summary>
    /// Một phiên bản nhẹ của VAT_Animator, được tối ưu hóa cho hệ thống instancing.
    /// Component này không tự cập nhật shader. Nó chỉ tính toán trạng thái animation
    /// và cung cấp dữ liệu cho VAT_ZombieDirector để render hàng loạt.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class VAT_Animator_Instanced : MonoBehaviour
    {
        public Material instancedMaterial;
        public VAT_AnimationData animationData;
        public float playbackSpeed = 1.0f;
        [Tooltip("Animation clip to play on Start.")]
        public int defaultClipIndex = 0;

        private VAT_AnimationData.ClipInfo _currentClip;
        private VAT_AnimationData.ClipInfo _previousClip;

        private float _currentTimeSeconds;
        private float _previousTimeSeconds;
        private float _crossFadeTimer;
        private float _crossFadeDuration;
        private bool _isBlending;

        private void OnEnable()
        {
            Initialize();
        }

        private void OnValidate()
        {
            if (this.isActiveAndEnabled)
            {
                Initialize();
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void Tick(float deltaTime)
        {
            if (_currentClip == null || animationData == null) return;
            UpdateTimers(deltaTime * playbackSpeed);
        }

        private void Initialize()
        {
            if (animationData == null || !animationData.IsValid()) return;

            GetComponent<MeshFilter>().sharedMesh = animationData.bakedMesh;
            GetComponent<MeshRenderer>().sharedMaterial = instancedMaterial;

            if (animationData.animationClips.Count > 0)
            {
                int clipIndex = Mathf.Clamp(defaultClipIndex, 0, animationData.animationClips.Count - 1);
                Play(animationData.animationClips[clipIndex].name);
            }
        }

        public void Play(string clipName)
        {
            if (animationData == null || !animationData.TryGetClipInfo(clipName, out var newClip)) return;

            _currentClip = newClip;
            _currentTimeSeconds = 0;
            _isBlending = false;
            _crossFadeTimer = 0;
            _previousClip = null;
        }

        public void CrossFade(string clipName, float duration)
        {
            if (animationData == null || !animationData.TryGetClipInfo(clipName, out var newClip)) return;
            if (_currentClip != null && _currentClip.name == newClip.name) return;

            _previousClip = _currentClip;
            _previousTimeSeconds = _currentTimeSeconds;
            _currentClip = newClip;
            _currentTimeSeconds = 0;
            _crossFadeDuration = Mathf.Max(0, duration);
            _crossFadeTimer = 0;
            _isBlending = duration > 0.001f && _previousClip != null;
        }

        private void UpdateTimers(float adjustedDeltaTime)
        {
            _currentTimeSeconds += adjustedDeltaTime;
            if (_currentClip.wrapMode == WrapMode.Loop && _currentClip.duration > 0)
            {
                _currentTimeSeconds %= _currentClip.duration;
            }

            if (_isBlending)
            {
                _crossFadeTimer += adjustedDeltaTime;
                if (_crossFadeTimer >= _crossFadeDuration)
                {
                    _isBlending = false;
                    _previousClip = null;
                }

                if (_previousClip != null)
                {
                    _previousTimeSeconds += adjustedDeltaTime;
                    if (_previousClip.wrapMode == WrapMode.Loop && _previousClip.duration > 0)
                    {
                        _previousTimeSeconds %= _previousClip.duration;
                    }
                }
            }
        }

        // HÀM QUAN TRỌNG ĐỂ SỬA LỖI CS1061
        public Vector4 GetAnimationDataForInstancing()
        {
            float currentV = CalculateNormalizedVCoordinate(_currentClip, _currentTimeSeconds);
            float previousV = 0f;
            float blendWeight = 0f;

            if (_isBlending && _previousClip != null)
            {
                previousV = CalculateNormalizedVCoordinate(_previousClip, _previousTimeSeconds);
                blendWeight = _crossFadeDuration > 0 ? Mathf.Clamp01(_crossFadeTimer / _crossFadeDuration) : 1f;
            }

            // x = V_current, y = V_previous, z = blend_weight
            return new Vector4(currentV, previousV, blendWeight, 0);
        }

        private float CalculateNormalizedVCoordinate(VAT_AnimationData.ClipInfo clip, float timeSeconds)
        {
            if (clip == null || animationData.positionTexture.height <= 1) return 0f;

            float progress = 0;
            if (clip.duration > 0)
            {
                switch (clip.wrapMode)
                {
                    case WrapMode.Loop:
                        progress = Mathf.Repeat(timeSeconds, clip.duration) / clip.duration;
                        break;
                    case WrapMode.PingPong:
                        progress = Mathf.PingPong(timeSeconds, clip.duration) / clip.duration;
                        break;
                    default:
                        progress = Mathf.Clamp01(timeSeconds / clip.duration);
                        break;
                }
            }

            float frameIndexInClip = progress * (clip.frameCount - 1);
            float absoluteFrame = clip.startFrame + frameIndexInClip;

            return (absoluteFrame + 0.5f) / animationData.positionTexture.height;
        }
    }
}