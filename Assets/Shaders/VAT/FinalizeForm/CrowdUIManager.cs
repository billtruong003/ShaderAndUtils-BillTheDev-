using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using Unity.Profiling; // Thêm namespace để sử dụng ProfilerRecorder

namespace OptimizeVariousVAT
{
    public class CrowdUIManager : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private CrowdController _crowdController;

        [Header("UI Panel Containers")]
        [SerializeField] private GameObject _spawningGroup;
        [SerializeField] private GameObject _movementGroup;
        [SerializeField] private GameObject _performanceGroup;
        [SerializeField] private GameObject _statsGroup; // Panel mới

        [Header("UI Panel Buttons")]
        [SerializeField] private Button _showSpawningButton;
        [SerializeField] private Button _showMovementButton;
        [SerializeField] private Button _showPerformanceButton;
        [SerializeField] private Button _showStatsButton; // Nút mới

        [Header("Stats Display")]
        [SerializeField] private TextMeshProUGUI _fpsText;
        [SerializeField] private TextMeshProUGUI _drawCallsText;
        [SerializeField] private TextMeshProUGUI _trisText;
        [SerializeField] private TextMeshProUGUI _vertsText;
        [SerializeField, Range(0.1f, 2f)] private float _statsUpdateInterval = 0.5f;

        // --- Các trường UI khác giữ nguyên ---
        [Header("Spawning Controls")]
        [SerializeField] private Slider _agentCountSlider;
        [SerializeField] private TextMeshProUGUI _agentCountText;
        [SerializeField] private TMP_InputField _spawnBoundsXInput;
        [SerializeField] private TMP_InputField _spawnBoundsZInput;
        [SerializeField] private Button _respawnButton;

        [Header("Movement Controls")]
        [SerializeField] private Slider _walkSpeedSlider;
        [SerializeField] private TextMeshProUGUI _walkSpeedText;
        [SerializeField] private Slider _runSpeedSlider;
        [SerializeField] private TextMeshProUGUI _runSpeedText;
        [SerializeField] private Slider _destinationThresholdSlider;
        [SerializeField] private TextMeshProUGUI _destinationThresholdText;

        [Header("Performance Controls")]
        [SerializeField] private TMP_Dropdown _updateModeDropdown;
        [SerializeField] private TMP_Dropdown _simulationModeDropdown;
        [SerializeField] private GameObject _coroutineIntervalContainer;
        [SerializeField] private TMP_InputField _coroutineIntervalInput;

        private List<GameObject> _allPanels;

        // FPS Counter
        private float _fpsAccumulator;
        private int _fpsFrameCount;

        // Profiler Recorders for stats
        private ProfilerRecorder _drawCallsRecorder;
        private ProfilerRecorder _trisRecorder;
        private ProfilerRecorder _vertsRecorder;

        private void OnEnable()
        {
            // Khởi tạo các recorder để bắt đầu thu thập dữ liệu
            _drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            _trisRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            _vertsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
        }

        private void OnDisable()
        {
            // Giải phóng các recorder khi không cần thiết để tránh memory leak
            _drawCallsRecorder.Dispose();
            _trisRecorder.Dispose();
            _vertsRecorder.Dispose();
        }

        private void Awake()
        {
            _allPanels = new List<GameObject> { _spawningGroup, _movementGroup, _performanceGroup, _statsGroup };
        }

        private void Start()
        {
            if (!ValidateReferences())
            {
                this.enabled = false;
                return;
            }

            SetupInitialValues();
            AddListeners();
            SetActivePanel(_spawningGroup);
        }

        private void Update()
        {
            // Cập nhật thông số FPS và rendering
            _fpsFrameCount++;
            _fpsAccumulator += Time.unscaledDeltaTime;

            if (_fpsAccumulator >= _statsUpdateInterval)
            {
                UpdateStatsDisplay();
                _fpsAccumulator = 0f;
                _fpsFrameCount = 0;
            }
        }

        private void OnDestroy()
        {
            RemoveListeners();
        }

        private void UpdateStatsDisplay()
        {
            float fps = _fpsFrameCount / _fpsAccumulator;
            _fpsText.text = $"FPS: {fps:F1}";

            _drawCallsText.text = $"Draw Calls: {_drawCallsRecorder.LastValue}";
            _trisText.text = $"Tris: {FormatNumber(_trisRecorder.LastValue)}";
            _vertsText.text = $"Verts: {FormatNumber(_vertsRecorder.LastValue)}";
        }

        public void SetActivePanel(GameObject panelToShow)
        {
            foreach (var panel in _allPanels)
            {
                if (panel != null)
                {
                    panel.SetActive(panel == panelToShow);
                }
            }
        }

        private void AddListeners()
        {
            // Panel Buttons
            _showSpawningButton.onClick.AddListener(() => SetActivePanel(_spawningGroup));
            _showMovementButton.onClick.AddListener(() => SetActivePanel(_movementGroup));
            _showPerformanceButton.onClick.AddListener(() => SetActivePanel(_performanceGroup));
            _showStatsButton.onClick.AddListener(() => SetActivePanel(_statsGroup));

            // Spawning
            _agentCountSlider.onValueChanged.AddListener(OnAgentCountSliderChanged);
            _spawnBoundsXInput.onEndEdit.AddListener(OnSpawnBoundsChanged);
            _spawnBoundsZInput.onEndEdit.AddListener(OnSpawnBoundsChanged);
            _respawnButton.onClick.AddListener(OnRespawnButtonClicked);

            // Movement
            _walkSpeedSlider.onValueChanged.AddListener(OnWalkSpeedChanged);
            _runSpeedSlider.onValueChanged.AddListener(OnRunSpeedChanged);
            _destinationThresholdSlider.onValueChanged.AddListener(OnDestinationThresholdChanged);

            // Performance
            _updateModeDropdown.onValueChanged.AddListener(OnUpdateModeChanged);
            _simulationModeDropdown.onValueChanged.AddListener(OnSimulationModeChanged);
            _coroutineIntervalInput.onEndEdit.AddListener(OnCoroutineIntervalChanged);
        }

        private void RemoveListeners()
        {
            // Panel Buttons
            _showSpawningButton.onClick.RemoveAllListeners();
            _showMovementButton.onClick.RemoveAllListeners();
            _showPerformanceButton.onClick.RemoveAllListeners();
            _showStatsButton.onClick.RemoveAllListeners();

            // Other controls
            _agentCountSlider.onValueChanged.RemoveAllListeners();
            _spawnBoundsXInput.onEndEdit.RemoveAllListeners();
            _spawnBoundsZInput.onEndEdit.RemoveAllListeners();
            _respawnButton.onClick.RemoveAllListeners();
            _walkSpeedSlider.onValueChanged.RemoveAllListeners();
            _runSpeedSlider.onValueChanged.RemoveAllListeners();
            _destinationThresholdSlider.onValueChanged.RemoveAllListeners();
            _updateModeDropdown.onValueChanged.RemoveAllListeners();
            _simulationModeDropdown.onValueChanged.RemoveAllListeners();
            _coroutineIntervalInput.onEndEdit.RemoveAllListeners();
        }

        #region Callback Methods

        private void OnAgentCountSliderChanged(float value) => UpdateText(_agentCountText, "Agents", value);
        private void OnRespawnButtonClicked()
        {
            int newCount = Mathf.RoundToInt(_agentCountSlider.value);
            _crowdController.ApplyAgentCount(newCount, true);
        }
        private void OnSpawnBoundsChanged(string value)
        {
            float.TryParse(_spawnBoundsXInput.text, NumberStyles.Any, CultureInfo.InvariantCulture, out float x);
            float.TryParse(_spawnBoundsZInput.text, NumberStyles.Any, CultureInfo.InvariantCulture, out float z);
            _crowdController.SpawnBounds = new Vector3(x, _crowdController.SpawnBounds.y, z);
        }
        private void OnWalkSpeedChanged(float value)
        {
            _crowdController.WalkSpeed = value;
            UpdateText(_walkSpeedText, "Walk Speed", value);
        }
        private void OnRunSpeedChanged(float value)
        {
            _crowdController.RunSpeed = value;
            UpdateText(_runSpeedText, "Run Speed", value);
        }
        private void OnDestinationThresholdChanged(float value)
        {
            _crowdController.DestinationReachedThreshold = value;
            UpdateText(_destinationThresholdText, "Threshold", value);
        }
        private void OnUpdateModeChanged(int index)
        {
            _crowdController.CurrentUpdateMode = (CrowdController.UpdateMode)index;
            UpdateCoroutineIntervalVisibility();
        }
        private void OnSimulationModeChanged(int index)
        {
            _crowdController.CurrentSimulationMode = (CrowdController.SimulationMode)index;
        }
        private void OnCoroutineIntervalChanged(string value)
        {
            if (float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out float interval))
            {
                _crowdController.CoroutineUpdateInterval = Mathf.Max(0.01f, interval);
            }
        }

        #endregion

        #region Helper Methods

        private string FormatNumber(long num)
        {
            if (num >= 1000000)
                return (num / 1000000.0f).ToString("F1", CultureInfo.InvariantCulture) + "M";
            if (num >= 1000)
                return (num / 1000.0f).ToString("F1", CultureInfo.InvariantCulture) + "k";
            return num.ToString();
        }

        private void SetupInitialValues()
        {
            // Spawning
            _agentCountSlider.minValue = 0;
            _agentCountSlider.maxValue = _crowdController.MaxAgentCount;
            _agentCountSlider.value = _crowdController.CurrentAgentCount;
            UpdateText(_agentCountText, "Agents", _crowdController.CurrentAgentCount);
            _spawnBoundsXInput.text = _crowdController.SpawnBounds.x.ToString(CultureInfo.InvariantCulture);
            _spawnBoundsZInput.text = _crowdController.SpawnBounds.z.ToString(CultureInfo.InvariantCulture);

            // Movement
            _walkSpeedSlider.minValue = 1f;
            _walkSpeedSlider.maxValue = 10f;
            _walkSpeedSlider.value = _crowdController.WalkSpeed;
            UpdateText(_walkSpeedText, "Walk Speed", _crowdController.WalkSpeed);
            _runSpeedSlider.minValue = 5f;
            _runSpeedSlider.maxValue = 20f;
            _runSpeedSlider.value = _crowdController.RunSpeed;
            UpdateText(_runSpeedText, "Run Speed", _crowdController.RunSpeed);
            _destinationThresholdSlider.minValue = 0.1f;
            _destinationThresholdSlider.maxValue = 10f;
            _destinationThresholdSlider.value = _crowdController.DestinationReachedThreshold;
            UpdateText(_destinationThresholdText, "Threshold", _crowdController.DestinationReachedThreshold);

            // Performance
            PopulateDropdown(_updateModeDropdown, typeof(CrowdController.UpdateMode));
            _updateModeDropdown.value = (int)_crowdController.CurrentUpdateMode;
            PopulateDropdown(_simulationModeDropdown, typeof(CrowdController.SimulationMode));
            _simulationModeDropdown.value = (int)_crowdController.CurrentSimulationMode;
            _coroutineIntervalInput.text = _crowdController.CoroutineUpdateInterval.ToString(CultureInfo.InvariantCulture);
            UpdateCoroutineIntervalVisibility();
        }

        private void UpdateCoroutineIntervalVisibility()
        {
            bool isCoroutineMode = (CrowdController.UpdateMode)_updateModeDropdown.value == CrowdController.UpdateMode.ViaCoroutine;
            _coroutineIntervalContainer.SetActive(isCoroutineMode);
        }

        private void UpdateText(TextMeshProUGUI textElement, string prefix, float value, string format = "F2")
        {
            textElement.text = $"{prefix}: {Mathf.RoundToInt(value)}";
        }

        private void PopulateDropdown(TMP_Dropdown dropdown, System.Type enumType)
        {
            var names = System.Enum.GetNames(enumType).ToList();
            dropdown.ClearOptions();
            dropdown.AddOptions(names);
        }

        private bool ValidateReferences()
        {
            if (_crowdController == null) { Debug.LogError("CrowdController is not assigned.", this); return false; }
            // Panel Containers and Buttons
            if (_spawningGroup == null || _movementGroup == null || _performanceGroup == null || _statsGroup == null) { Debug.LogError("One or more UI Panel Containers are not assigned.", this); return false; }
            if (_showSpawningButton == null || _showMovementButton == null || _showPerformanceButton == null || _showStatsButton == null) { Debug.LogError("One or more UI Panel Buttons are not assigned.", this); return false; }
            // Stats
            if (_fpsText == null || _drawCallsText == null || _trisText == null || _vertsText == null) { Debug.LogError("A Stats display control is not assigned.", this); return false; }
            // Spawning Controls
            if (_agentCountSlider == null || _agentCountText == null || _spawnBoundsXInput == null || _spawnBoundsZInput == null || _respawnButton == null) { Debug.LogError("A Spawning control is not assigned.", this); return false; }
            // Movement Controls
            if (_walkSpeedSlider == null || _walkSpeedText == null || _runSpeedSlider == null || _runSpeedText == null || _destinationThresholdSlider == null || _destinationThresholdText == null) { Debug.LogError("A Movement control is not assigned.", this); return false; }
            // Performance Controls
            if (_updateModeDropdown == null || _simulationModeDropdown == null || _coroutineIntervalContainer == null || _coroutineIntervalInput == null) { Debug.LogError("A Performance control is not assigned.", this); return false; }
            return true;
        }

        #endregion
    }
}