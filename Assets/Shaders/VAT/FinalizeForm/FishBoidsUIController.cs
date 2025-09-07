// File: Assets/UI/FishBoidsUIController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Globalization;
using Unity.Profiling;

namespace OptimizeVariousVAT
{
    public class FishBoidsUIController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private FishBoidsController _boidsController;

        [Header("UI Panel Containers")]
        [SerializeField] private GameObject _simulationGroup;
        [SerializeField] private GameObject _boidsBehaviorGroup;
        [SerializeField] private GameObject _statsGroup;

        [Header("UI Panel Buttons")]
        [SerializeField] private Button _showSimulationButton;
        [SerializeField] private Button _showBoidsBehaviorButton;
        [SerializeField] private Button _showStatsButton;

        [Header("Stats Display")]
        [SerializeField, Range(60, 240)] private int _targetFrameRate = 120;
        [SerializeField] private TextMeshProUGUI _fpsText;
        [SerializeField] private TextMeshProUGUI _drawCallsText;
        [SerializeField] private TextMeshProUGUI _trisText;
        [SerializeField] private TextMeshProUGUI _vertsText;
        [SerializeField, Range(0.1f, 2f)] private float _statsUpdateInterval = 0.5f;

        [Header("Simulation Controls")]
        [SerializeField] private Slider _agentCountSlider;
        [SerializeField] private TextMeshProUGUI _agentCountText;
        [SerializeField] private TMP_InputField _spawnBoundsXInput;
        [SerializeField] private TMP_InputField _spawnBoundsYInput;
        [SerializeField] private TMP_InputField _spawnBoundsZInput;
        [SerializeField] private Button _respawnButton;

        [Header("Boids Behavior Controls")]
        [SerializeField] private Slider _neighborRadiusSlider;
        [SerializeField] private TextMeshProUGUI _neighborRadiusText;
        [SerializeField] private Slider _maxSpeedSlider;
        [SerializeField] private TextMeshProUGUI _maxSpeedText;
        [SerializeField] private Slider _separationSlider;
        [SerializeField] private TextMeshProUGUI _separationText;
        [SerializeField] private Slider _alignmentSlider;
        [SerializeField] private TextMeshProUGUI _alignmentText;
        [SerializeField] private Slider _cohesionSlider;
        [SerializeField] private TextMeshProUGUI _cohesionText;
        [SerializeField] private Slider _boundsSlider;
        [SerializeField] private TextMeshProUGUI _boundsText;

        private List<GameObject> _allPanels;
        private float _fpsAccumulator;
        private int _fpsFrameCount;
        private ProfilerRecorder _drawCallsRecorder, _trisRecorder, _vertsRecorder;

        private void OnEnable()
        {
            _drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            _trisRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            _vertsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
        }

        private void OnDisable()
        {
            _drawCallsRecorder.Dispose();
            _trisRecorder.Dispose();
            _vertsRecorder.Dispose();
        }

        private void Awake()
        {
            _allPanels = new List<GameObject> { _simulationGroup, _boidsBehaviorGroup, _statsGroup };
            Application.targetFrameRate = _targetFrameRate;
        }

        private void Start()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }
            SetupInitialValues();
            AddListeners();
            SetActivePanel(_simulationGroup);
        }

        private void Update()
        {
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

        #region Setup and Listeners

        private void SetupInitialValues()
        {
            // Simulation
            _agentCountSlider.minValue = 1;
            _agentCountSlider.maxValue = _boidsController.MaxAgentCount;
            _agentCountSlider.value = _boidsController.AgentCount;
            UpdateText(_agentCountText, "Agents", _boidsController.AgentCount, "F0");

            Vector3 bounds = _boidsController.SpawnBounds;
            _spawnBoundsXInput.text = bounds.x.ToString(CultureInfo.InvariantCulture);
            _spawnBoundsYInput.text = bounds.y.ToString(CultureInfo.InvariantCulture);
            _spawnBoundsZInput.text = bounds.z.ToString(CultureInfo.InvariantCulture);

            // Boids Behavior
            SetupSlider(_neighborRadiusSlider, _neighborRadiusText, "Neighbor Radius", 1f, 20f, _boidsController.NeighborRadius);
            SetupSlider(_maxSpeedSlider, _maxSpeedText, "Max Speed", 1f, 30f, _boidsController.MaxSpeed);
            SetupSlider(_separationSlider, _separationText, "Separation", 0f, 10f, _boidsController.SeparationWeight);
            SetupSlider(_alignmentSlider, _alignmentText, "Alignment", 0f, 10f, _boidsController.AlignmentWeight);
            SetupSlider(_cohesionSlider, _cohesionText, "Cohesion", 0f, 10f, _boidsController.CohesionWeight);
            SetupSlider(_boundsSlider, _boundsText, "Bounds Force", 0f, 20f, _boidsController.BoundsWeight);
        }

        private void AddListeners()
        {
            // Panel Buttons
            _showSimulationButton.onClick.AddListener(() => SetActivePanel(_simulationGroup));
            _showBoidsBehaviorButton.onClick.AddListener(() => SetActivePanel(_boidsBehaviorGroup));
            _showStatsButton.onClick.AddListener(() => SetActivePanel(_statsGroup));

            // Simulation
            _agentCountSlider.onValueChanged.AddListener(value => UpdateText(_agentCountText, "Agents", value, "F0"));
            _respawnButton.onClick.AddListener(OnRespawnButtonClicked);
            _spawnBoundsXInput.onEndEdit.AddListener(OnSpawnBoundsChanged);
            _spawnBoundsYInput.onEndEdit.AddListener(OnSpawnBoundsChanged);
            _spawnBoundsZInput.onEndEdit.AddListener(OnSpawnBoundsChanged);

            // Boids Behavior
            _neighborRadiusSlider.onValueChanged.AddListener(OnNeighborRadiusChanged);
            _maxSpeedSlider.onValueChanged.AddListener(OnMaxSpeedChanged);
            _separationSlider.onValueChanged.AddListener(OnSeparationChanged);
            _alignmentSlider.onValueChanged.AddListener(OnAlignmentChanged);
            _cohesionSlider.onValueChanged.AddListener(OnCohesionChanged);
            _boundsSlider.onValueChanged.AddListener(OnBoundsChanged);
        }

        private void RemoveListeners()
        {
            // Remove all to prevent potential leaks
            _showSimulationButton.onClick.RemoveAllListeners();
            _showBoidsBehaviorButton.onClick.RemoveAllListeners();
            _showStatsButton.onClick.RemoveAllListeners();
            _agentCountSlider.onValueChanged.RemoveAllListeners();
            _respawnButton.onClick.RemoveAllListeners();
            _spawnBoundsXInput.onEndEdit.RemoveAllListeners();
            _spawnBoundsYInput.onEndEdit.RemoveAllListeners();
            _spawnBoundsZInput.onEndEdit.RemoveAllListeners();
            _neighborRadiusSlider.onValueChanged.RemoveAllListeners();
            _maxSpeedSlider.onValueChanged.RemoveAllListeners();
            _separationSlider.onValueChanged.RemoveAllListeners();
            _alignmentSlider.onValueChanged.RemoveAllListeners();
            _cohesionSlider.onValueChanged.RemoveAllListeners();
            _boundsSlider.onValueChanged.RemoveAllListeners();
        }

        #endregion

        #region Callbacks

        private void OnRespawnButtonClicked()
        {
            // Read the final slider value and tell the controller to respawn
            typeof(FishBoidsController).GetField("_agentCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_boidsController, (int)_agentCountSlider.value);
            _boidsController.SendMessage("RespawnAgents");
        }

        private void OnSpawnBoundsChanged(string _)
        {
            float.TryParse(_spawnBoundsXInput.text, NumberStyles.Any, CultureInfo.InvariantCulture, out float x);
            float.TryParse(_spawnBoundsYInput.text, NumberStyles.Any, CultureInfo.InvariantCulture, out float y);
            float.TryParse(_spawnBoundsZInput.text, NumberStyles.Any, CultureInfo.InvariantCulture, out float z);
            _boidsController.SpawnBounds = new Vector3(x, y, z);
        }

        private void OnNeighborRadiusChanged(float value)
        {
            _boidsController.NeighborRadius = value;
            UpdateText(_neighborRadiusText, "Neighbor Radius", value);
        }

        private void OnMaxSpeedChanged(float value)
        {
            _boidsController.MaxSpeed = value;
            UpdateText(_maxSpeedText, "Max Speed", value);
        }

        private void OnSeparationChanged(float value)
        {
            _boidsController.SeparationWeight = value;
            UpdateText(_separationText, "Separation", value);
        }

        private void OnAlignmentChanged(float value)
        {
            _boidsController.AlignmentWeight = value;
            UpdateText(_alignmentText, "Alignment", value);
        }

        private void OnCohesionChanged(float value)
        {
            _boidsController.CohesionWeight = value;
            UpdateText(_cohesionText, "Cohesion", value);
        }

        private void OnBoundsChanged(float value)
        {
            _boidsController.BoundsWeight = value;
            UpdateText(_boundsText, "Bounds Force", value);
        }

        #endregion

        #region Helper Methods

        public void SetActivePanel(GameObject panelToShow)
        {
            foreach (var panel in _allPanels)
            {
                if (panel != null) panel.SetActive(panel == panelToShow);
            }
        }

        private void UpdateStatsDisplay()
        {
            float fps = _fpsFrameCount / _fpsAccumulator;
            _fpsText.text = $"FPS: {fps:F1}";
            _drawCallsText.text = $"Draw Calls: {_drawCallsRecorder.LastValue}";
            _trisText.text = $"Tris: {FormatNumber(_trisRecorder.LastValue)}";
            _vertsText.text = $"Verts: {FormatNumber(_vertsRecorder.LastValue)}";
        }

        private string FormatNumber(long num)
        {
            if (num >= 1000000) return (num / 1000000.0f).ToString("F1", CultureInfo.InvariantCulture) + "M";
            if (num >= 1000) return (num / 1000.0f).ToString("F1", CultureInfo.InvariantCulture) + "k";
            return num.ToString();
        }

        private void UpdateText(TextMeshProUGUI textElement, string prefix, float value, string format = "F2")
        {
            textElement.text = $"{prefix}: {value.ToString(format, CultureInfo.InvariantCulture)}";
        }

        private void SetupSlider(Slider slider, TextMeshProUGUI text, string prefix, float min, float max, float currentValue)
        {
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = currentValue;
            UpdateText(text, prefix, currentValue);
        }

        private bool ValidateReferences()
        {
            if (_boidsController == null) { Debug.LogError("FishBoidsController is not assigned.", this); return false; }
            // Add more validation for all UI elements if needed
            return true;
        }

        #endregion
    }
}