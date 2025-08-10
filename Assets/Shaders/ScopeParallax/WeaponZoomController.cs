using UnityEngine;
using cowsins; // Giả định namespace này chứa WeaponSpecificEffects và CameraFogController

public sealed class WeaponZoomController : MonoBehaviour
{
    [Header("CORE CONFIGURATION")]
    [SerializeField] private Camera scopeCamera;
    [SerializeField] private bool isSniperScope = true;

    [Header("ZOOM VISUALS")]
    [SerializeField, Min(0.01f)] private float zoomTransitionDuration = 0.15f;
    [SerializeField] private float defaultFov = 60f;
    [SerializeField] private float zoomedFov = 15f;

    [Header("ANIMATION")]
    [SerializeField] private Animator weaponAnimator;
    [SerializeField] private string aimingBlendParameter = "isAiming";
    [SerializeField, Min(0.1f)] private float animationTransitionSpeed = 10f;

    [Header("SWAY CONTROL")]
    [SerializeField] private bool enableSwayControl = true;
    [SerializeField] private WeaponSpecificEffects weaponEffects;
    [SerializeField, Range(0, 1)] private float swayReductionOnZoom = 0.1f;
    [SerializeField, Min(1f)] private float smoothMultiplierOnZoom = 6.0f;

    [Header("SCOPE RENDERER")]
    [SerializeField] private Renderer scopeLensRenderer;
    [SerializeField] private Material realScopeMaterial;
    [SerializeField] private Material fakeScopeMaterial;

    [Header("EXTRA EFFECTS")]
    [SerializeField] private CameraFogController fogController;

    private bool isAiming;
    private float currentFovVelocity;
    private float aimAnimationBlend;

    private void Awake()
    {
        InitializeAndValidateComponents();
    }

    private void OnEnable()
    {
        ResetToDefaultState();
    }

    private void OnDisable()
    {
        if (isAiming)
        {
            ResetToDefaultState();
        }
    }

    private void Update()
    {
        ProcessAimingInput();

        UpdateZoomTransition();
        UpdateAnimationBlend();
    }

    bool isConfigurationValid;
    private void InitializeAndValidateComponents()
    {
        isConfigurationValid = scopeCamera != null && scopeLensRenderer != null &&
                                    realScopeMaterial != null && fakeScopeMaterial != null &&
                                    weaponAnimator != null;

        if (!isConfigurationValid)
        {
            enabled = false;
            return;
        }

        if (isSniperScope && fogController == null)
        {
            fogController = GetComponentInParent<CameraFogController>();
        }

        if (enableSwayControl && weaponEffects == null)
        {
            enableSwayControl = false;
        }
    }

    bool aimInputActive;
    private void ProcessAimingInput()
    {
        aimInputActive = Input.GetMouseButton(1);
        if (isAiming == aimInputActive) return;

        SetAimingState(aimInputActive);
    }

    private void SetAimingState(bool shouldAim)
    {
        isAiming = shouldAim;

        UpdateScopeVisuals(isAiming);
        UpdateSwayControl(isAiming);

        if (isSniperScope)
        {
            UpdateSniperEffects(isAiming);
        }
    }

    private void ResetToDefaultState()
    {
        SetAimingState(false);

        scopeCamera.fieldOfView = defaultFov;
        aimAnimationBlend = 0f;
        weaponAnimator.SetFloat(aimingBlendParameter, aimAnimationBlend);
    }

    private void UpdateScopeVisuals(bool isAimingNow)
    {
        scopeLensRenderer.material = isAimingNow ? realScopeMaterial : fakeScopeMaterial;
        scopeCamera.gameObject.SetActive(isAimingNow);
    }

    private void UpdateSwayControl(bool isAimingNow)
    {
        if (!enableSwayControl || weaponEffects == null) return;

        if (isAimingNow)
        {
            weaponEffects.SetAimingModifiers(swayReductionOnZoom, smoothMultiplierOnZoom);
        }
        else
        {
            weaponEffects.ResetAimingModifiers();
        }
    }

    private void UpdateSniperEffects(bool isAimingNow)
    {
        if (fogController != null)
        {
            fogController.TransitionToBlackout(isAimingNow);
        }
    }

    float targetFov;
    private void UpdateZoomTransition()
    {
        if (!scopeCamera.gameObject.activeInHierarchy) return;

        targetFov = isAiming ? zoomedFov : defaultFov;
        scopeCamera.fieldOfView = Mathf.SmoothDamp(scopeCamera.fieldOfView, targetFov, ref currentFovVelocity, zoomTransitionDuration);
    }

    float targetBlend;
    private void UpdateAnimationBlend()
    {
        targetBlend = isAiming ? 1.0f : 0.0f;
        aimAnimationBlend = Mathf.MoveTowards(aimAnimationBlend, targetBlend, Time.deltaTime * animationTransitionSpeed);
        weaponAnimator.SetFloat(aimingBlendParameter, aimAnimationBlend);
    }
}