using UnityEngine;
using DentedPixel; // Thêm namespace của LeanTween

public sealed class CameraFogController : MonoBehaviour
{
    [System.Serializable]
    public class FogState
    {
        public Color fogColor = Color.grey;
        [Range(0f, 1f)] public float fogDensity = 0f;
        public float fadeStartDistance = 0.1f;
        public float fadeEndDistance = 5.0f;
    }

    [Header("Component References")]
    [SerializeField] private Renderer fogRenderer;

    [Header("State Configurations")]
    [SerializeField] private FogState normalState;
    [SerializeField] private FogState blackoutState;

    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 0.25f;

    private Material fogMaterial;
    private static readonly int ColorProperty = Shader.PropertyToID("_FogColor");
    private static readonly int DensityProperty = Shader.PropertyToID("_FogDensity");
    private static readonly int FadeStartProperty = Shader.PropertyToID("_FadeStartDistance");
    private static readonly int FadeEndProperty = Shader.PropertyToID("_FadeEndDistance");

    private int currentTweenId = -1;

    private void Awake()
    {
        if (fogRenderer == null)
        {
            if (!TryGetComponent(out fogRenderer))
            {
                Debug.LogError("CameraFogController requires a Renderer component.", this);
                this.enabled = false;
                return;
            }
        }
        fogMaterial = fogRenderer.material;
        InitializeToNormalState();
    }

    private void OnDestroy()
    {
        LeanTween.cancel(currentTweenId);
        if (fogMaterial != null)
        {
            Destroy(fogMaterial);
        }
    }

    private void InitializeToNormalState()
    {
        ApplyState(normalState);
    }

    public void TransitionToBlackout(bool isBlackedOut)
    {
        FogState targetState = isBlackedOut ? blackoutState : normalState;

        LeanTween.cancel(currentTweenId);

        // ĐÂY LÀ PHẦN SỬA LỖI: Dùng LeanTween.value để tween màu
        var tweenAction = LeanTween.value(gameObject, fogMaterial.GetColor(ColorProperty), targetState.fogColor, transitionDuration)
            .setOnUpdate((Color val) => { fogMaterial.SetColor(ColorProperty, val); });

        currentTweenId = tweenAction.id;

        LeanTween.value(gameObject, fogMaterial.GetFloat(DensityProperty), targetState.fogDensity, transitionDuration)
            .setOnUpdate((float val) => { fogMaterial.SetFloat(DensityProperty, val); });

        LeanTween.value(gameObject, fogMaterial.GetFloat(FadeStartProperty), targetState.fadeStartDistance, transitionDuration)
            .setOnUpdate((float val) => { fogMaterial.SetFloat(FadeStartProperty, val); });

        LeanTween.value(gameObject, fogMaterial.GetFloat(FadeEndProperty), targetState.fadeEndDistance, transitionDuration)
            .setOnUpdate((float val) => { fogMaterial.SetFloat(FadeEndProperty, val); });
    }

    private void ApplyState(FogState state)
    {
        fogMaterial.SetColor(ColorProperty, state.fogColor);
        fogMaterial.SetFloat(DensityProperty, state.fogDensity);
        fogMaterial.SetFloat(FadeStartProperty, state.fadeStartDistance);
        fogMaterial.SetFloat(FadeEndProperty, state.fadeEndDistance);
    }
}