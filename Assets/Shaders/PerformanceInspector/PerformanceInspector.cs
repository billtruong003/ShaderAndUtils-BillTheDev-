using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class PerformanceInspector : MonoBehaviour
{
    [Header("Activation & Control")]
    [SerializeField] private bool _isInspectorActiveOnStart = true;
    [SerializeField] private KeyCode _toggleKey = KeyCode.P;
    [SerializeField] private KeyCode _freezeAnalysisKey = KeyCode.F;
    [SerializeField] private KeyCode _gpuAnalysisKey = KeyCode.G;
    [SerializeField] private bool _isMovementEnabled = true; // Boolean to toggle movement
    [SerializeField] private KeyCode _toggleMovementKey = KeyCode.M; // Key to toggle movement

    [Header("Camera Controls")]
    [SerializeField] private float _moveSpeed = 15.0f;
    [SerializeField] private float _mouseSensitivity = 2.5f;

    [Header("Analysis Configuration")]
    [SerializeField, Range(0.1f, 2.0f)] private float _analysisIntervalSeconds = 0.5f;

    [Header("Visualization")]
    [SerializeField] private Gradient _heatmapGradient = new Gradient();
    [SerializeField, Range(0f, 1f)] private float _heatmapAlpha = 0.4f;

    public Camera InspectorCamera { get; private set; }
    public PerformanceDataCollector DataCollector { get; private set; }
    public IEnumerable<InspectedObjectDetailedData> VisibleObjectsData => _visibleObjectsData;
    public float MinPerformanceScore { get; private set; }
    public float MaxPerformanceScore { get; private set; }
    public Gradient HeatmapGradient => _heatmapGradient;
    public float HeatmapAlpha => _heatmapAlpha;
    public bool IsAnalysisFrozen { get; private set; }
    public bool IsInspectorActive { get; private set; }

#if UNITY_EDITOR
    public IPerformanceVisualizer Visualizer { get; private set; }
    public RuntimeGpuAnalyzer GpuAnalyzer { get; private set; }
#endif

    private SceneOctree _sceneOctree;
    private readonly List<Renderer> _visibleRenderersCache = new List<Renderer>(1024);
    private readonly List<InspectedObjectDetailedData> _visibleObjectsData = new List<InspectedObjectDetailedData>(1024);

    private float _timeSinceLastAnalysis;
    private float _rotationX;
    private float _rotationY;
    private const float MaxRaycastDistance = 10000f;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        DataCollector?.Start();
        SetInspectorActive(_isInspectorActiveOnStart);
    }

    private void OnDisable()
    {
        DataCollector?.Stop();
#if UNITY_EDITOR
        GpuAnalyzer?.Dispose();
#endif
        SetCursorState(false);
    }

    private void Update()
    {
        HandleInput();

        if (!IsInspectorActive || !Application.isPlaying) return;

        UpdateCameraMovement();
        DataCollector.Update();

#if UNITY_EDITOR
        GpuAnalyzer.Update();
#endif

        UpdateAnalysis();
    }

    private void Initialize()
    {
        InspectorCamera = GetComponent<Camera>();
        _sceneOctree = new SceneOctree(FindObjectsOfType<Renderer>(), 32, 5);
        DataCollector = new PerformanceDataCollector();

#if UNITY_EDITOR
        Visualizer = new PerformanceVisualizer();
        GpuAnalyzer = new RuntimeGpuAnalyzer(this);
#endif

        IsInspectorActive = _isInspectorActiveOnStart;
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(_toggleKey))
        {
            SetInspectorActive(!IsInspectorActive);
        }

        if (!IsInspectorActive) return;

        if (Input.GetKeyDown(_freezeAnalysisKey))
        {
            IsAnalysisFrozen = !IsAnalysisFrozen;
        }

        if (Input.GetKeyDown(_gpuAnalysisKey))
        {
            TriggerGpuAnalysisForTarget();
        }

        if (Input.GetKeyDown(_toggleMovementKey))
        {
            _isMovementEnabled = !_isMovementEnabled;
        }
    }

    private void UpdateAnalysis()
    {
        if (IsAnalysisFrozen) return;

        _timeSinceLastAnalysis += Time.unscaledDeltaTime;
        if (_timeSinceLastAnalysis >= _analysisIntervalSeconds)
        {
            AnalyzeVisibleObjects();
            _timeSinceLastAnalysis = 0f;
        }
    }

    private void AnalyzeVisibleObjects()
    {
        _visibleObjectsData.Clear();
        MinPerformanceScore = float.MaxValue;
        MaxPerformanceScore = float.MinValue;

        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(InspectorCamera);
        _sceneOctree.GetRenderersInFrustum(frustumPlanes, _visibleRenderersCache);

        foreach (var rend in _visibleRenderersCache)
        {
            if (rend == null || !rend.enabled || !rend.isVisible) continue;

            var detailedData = new InspectedObjectDetailedData(rend);
            _visibleObjectsData.Add(detailedData);
            UpdatePerformanceScoreRange(detailedData.DynamicPerformanceScore);
        }
    }

    private void UpdatePerformanceScoreRange(float score)
    {
        if (score <= 0) return;
        MinPerformanceScore = Mathf.Min(MinPerformanceScore, score);
        MaxPerformanceScore = Mathf.Max(MaxPerformanceScore, score);
    }

    private void TriggerGpuAnalysisForTarget()
    {
#if UNITY_EDITOR
        if (GpuAnalyzer == null) return;
        if (TryGetInspectedDataAtScreenCenter(out _, out var data))
        {
            GpuAnalyzer.TriggerAnalysis(data.Renderer);
        }
#endif
    }

    public void RebuildSceneOctree()
    {
        _sceneOctree.Build(FindObjectsOfType<Renderer>());
        Debug.Log("Performance Inspector: Scene Octree rebuilt.");
    }

    private void SetInspectorActive(bool isActive)
    {
        IsInspectorActive = isActive;
        SetCursorState(IsInspectorActive);
    }

    private void SetCursorState(bool isLocked)
    {
        Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isLocked;
    }

    private void UpdateCameraMovement()
    {
        if (_isMovementEnabled)
        {
            // Rotation always enabled
            _rotationX += Input.GetAxis("Mouse X") * _mouseSensitivity;
            _rotationY -= Input.GetAxis("Mouse Y") * _mouseSensitivity;
            _rotationY = Mathf.Clamp(_rotationY, -90f, 90f);
            transform.localRotation = Quaternion.Euler(_rotationY, _rotationX, 0);

            // Movement only if enabled

            float verticalMove = (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);
            Vector3 moveInput = new Vector3(Input.GetAxis("Horizontal"), verticalMove, Input.GetAxis("Vertical"));
            Vector3 moveDirection = transform.TransformDirection(moveInput.normalized);
            transform.position += moveDirection * _moveSpeed * Time.deltaTime;
        }
    }

    public bool TryGetInspectedDataAtScreenCenter(out RaycastHit hit, out InspectedObjectDetailedData targetData)
    {
        targetData = default;
        Ray ray = new Ray(InspectorCamera.transform.position, InspectorCamera.transform.forward);

        if (Physics.Raycast(ray, out hit, MaxRaycastDistance) && hit.collider.TryGetComponent<Renderer>(out var hitRenderer))
        {
            targetData = new InspectedObjectDetailedData(hitRenderer);
            return true;
        }
        return false;
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (!IsInspectorActive || !Application.isPlaying || Visualizer == null) return;
        Visualizer.DrawScreenOverlays(this);
    }

    private void OnDrawGizmos()
    {
        if (!IsInspectorActive || !Application.isPlaying || Visualizer == null) return;
        Visualizer.DrawSceneVisuals(this);
    }
#endif
}