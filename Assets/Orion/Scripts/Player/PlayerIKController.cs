using UnityEngine;
using Sirenix.OdinInspector;

namespace Orion
{
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerIKController : MonoBehaviour
    {
        [Title("CORE DEPENDENCIES")]
        [Required, SceneObjectsOnly]
        [SerializeField] private PlayerController _playerController;

        [Title("GENERAL SETTINGS")]
        [SerializeField] private LayerMask _ikLayerMask;

        [TabGroup("Tabs")]
        [TabGroup("Tabs", "General")]
        [BoxGroup("Tabs/General/IK Activation")]
        [Tooltip("Bật/tắt toàn bộ hệ thống IK.")]
        public bool EnableIK = true;

        [BoxGroup("Tabs/General/IK Blending")]
        [Tooltip("Tốc độ khi trọng số IK tăng lên (blend-in).")]
        public float BlendInSpeed = 20f;
        [Tooltip("Tốc độ khi trọng số IK giảm đi (blend-out).")]
        public float BlendOutSpeed = 15f;

        [TabGroup("Tabs", "Feet IK")]
        [BoxGroup("Tabs/Feet IK/State-Driven Settings")]
        [HideLabel]
        [SerializeField] private StateDrivenFootIKSettings _footSettings = new StateDrivenFootIKSettings();

        [BoxGroup("Tabs/Feet IK/Dynamic Weighting")]
        [Tooltip("Bật/tắt tính năng tự động điều chỉnh trọng số IK theo tốc độ.")]
        public bool EnableDynamicWeighting = true;

        [BoxGroup("Tabs/Feet IK/Dynamic Weighting")]
        [ShowIf("EnableDynamicWeighting")]
        [MinMaxSlider(0f, 15f, true)]
        public Vector2 SpeedRange = new Vector2(1f, 7f);

        [BoxGroup("Tabs/Feet IK/Dynamic Weighting")]
        [ShowIf("EnableDynamicWeighting")]
        [MinMaxSlider(0f, 1f, true)]
        public Vector2 WeightRange = new Vector2(1f, 0.2f);

        [TabGroup("Tabs", "Hands IK")]
        [BoxGroup("Tabs/Hands IK/WallRun Settings")]
        [InlineProperty, HideLabel]
        [SerializeField] private HandIKSettings _handSettings = new HandIKSettings();

        [TabGroup("Tabs", "Knee Hint")]
        [BoxGroup("Tabs/Knee Hint/Knee Hinting")]
        public bool EnableKneeHinting = true;

        [BoxGroup("Tabs/Knee Hint/Knee Hinting")]
        [ShowIf("EnableKneeHinting")]
        [Range(0f, 1f)] public float KneeHintWeight = 0.8f;

        [BoxGroup("Tabs/Knee Hint/Knee Hinting")]
        [ShowIf("EnableKneeHinting")]
        [Tooltip("Offset vị trí hint của đầu gối, tính từ trung điểm giữa đùi và bàn chân.")]
        public Vector3 KneeHintOffset = new Vector3(0, 0, 0.5f);

        [TabGroup("Tabs", "Debug")]
        [BoxGroup("Tabs/Debug/Gizmos")]
        public bool EnableGizmos = true;

        [BoxGroup("Tabs/Debug/Gizmos")]
        [ShowIf("EnableGizmos")]
        public bool DrawLeftFoot = true, DrawRightFoot = true, DrawLeftHand = true, DrawRightHand = true;

        [BoxGroup("Tabs/Debug/Gizmos")]
        [ShowIf("EnableGizmos")]
        [InlineEditor(InlineEditorModes.SmallPreview)]
        public GizmoColors GizmoColoring = new GizmoColors();

        [System.Serializable]
        public class GizmoColors
        {
            public Color LeftFoot = Color.green;
            public Color RightFoot = Color.cyan;
            public Color LeftHand = Color.magenta;
            public Color RightHand = Color.yellow;
        }

        private Animator _animator;
        private LimbIKProcessor _leftFootIK, _rightFootIK, _leftHandIK, _rightHandIK;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_playerController == null)
            {
                EnableIK = false;
                enabled = false;
                Debug.LogError("PlayerController is not assigned.", this);
                return;
            }
            InitializeIKProcessors();
        }

        private void Update()
        {
            if (!EnableIK) return;
            UpdateTargetWeights();
            UpdateAllLimbProcessors(Time.deltaTime);
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (!EnableIK || _animator == null) return;

            FootIKSettings activeFootSettings = GetActiveFootSettings();

            _leftFootIK.ProcessIK(_animator, activeFootSettings);
            _rightFootIK.ProcessIK(_animator, activeFootSettings);
            _leftHandIK.ProcessIK(_animator, _handSettings);
            _rightHandIK.ProcessIK(_animator, _handSettings);

            if (EnableKneeHinting)
            {
                ProcessKneeHint(AvatarIKHint.LeftKnee, _leftFootIK);
                ProcessKneeHint(AvatarIKHint.RightKnee, _rightFootIK);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!EnableGizmos || !EnableIK) return;
            if (_animator == null) _animator = GetComponent<Animator>(); // For editor-time gizmos
            if (_leftFootIK == null) InitializeIKProcessors(); // Ensure processors exist for drawing

            FootIKSettings activeFootSettings = GetActiveFootSettings();

            if (DrawLeftFoot) _leftFootIK.DrawGizmos(_animator, activeFootSettings, GizmoColoring.LeftFoot);
            if (DrawRightFoot) _rightFootIK.DrawGizmos(_animator, activeFootSettings, GizmoColoring.RightFoot);
            if (DrawLeftHand) _leftHandIK.DrawGizmos(_animator, _handSettings, GizmoColoring.LeftHand);
            if (DrawRightHand) _rightHandIK.DrawGizmos(_animator, _handSettings, GizmoColoring.RightHand);
        }

        private void InitializeIKProcessors()
        {
            _leftFootIK = new LimbIKProcessor(AvatarIKGoal.LeftFoot, _ikLayerMask, transform);
            _rightFootIK = new LimbIKProcessor(AvatarIKGoal.RightFoot, _ikLayerMask, transform);
            _leftHandIK = new LimbIKProcessor(AvatarIKGoal.LeftHand, _ikLayerMask, transform);
            _rightHandIK = new LimbIKProcessor(AvatarIKGoal.RightHand, _ikLayerMask, transform);
        }

        private FootIKSettings GetActiveFootSettings()
        {
            return _playerController.IsWallRunning ? _footSettings.WallRun : _footSettings.Grounded;
        }

        private void UpdateTargetWeights()
        {
            bool isGrounded = _playerController.IsGrounded;
            bool isWallRunning = _playerController.IsWallRunning;
            bool isWallOnRight = _playerController.IsWallRunningOnRight;

            float dynamicWeight = CalculateDynamicWeightFactor();

            float leftFootTarget = (isGrounded || (isWallRunning && !isWallOnRight)) ? 1f : 0f;
            float rightFootTarget = (isGrounded || (isWallRunning && isWallOnRight)) ? 1f : 0f;

            _leftFootIK.SetTargetWeight(leftFootTarget * dynamicWeight);
            _rightFootIK.SetTargetWeight(rightFootTarget * dynamicWeight);

            _leftHandIK.SetTargetWeight((isWallRunning && !isWallOnRight) ? 1f : 0f);
            _rightHandIK.SetTargetWeight((isWallRunning && isWallOnRight) ? 1f : 0f);
        }

        private void UpdateAllLimbProcessors(float deltaTime)
        {
            _leftFootIK.UpdateCurrentWeight(BlendInSpeed, BlendOutSpeed, deltaTime);
            _rightFootIK.UpdateCurrentWeight(BlendInSpeed, BlendOutSpeed, deltaTime);
            _leftHandIK.UpdateCurrentWeight(BlendInSpeed, BlendOutSpeed, deltaTime);
            _rightHandIK.UpdateCurrentWeight(BlendInSpeed, BlendOutSpeed, deltaTime);
        }

        private float CalculateDynamicWeightFactor()
        {
            if (!EnableDynamicWeighting) return 1.0f;

            float horizontalSpeed = new Vector3(_playerController.CurrentVelocity.x, 0, _playerController.CurrentVelocity.z).magnitude;
            float speedFactor = Mathf.InverseLerp(SpeedRange.x, SpeedRange.y, horizontalSpeed);
            return Mathf.Lerp(WeightRange.x, WeightRange.y, speedFactor);
        }

        private void ProcessKneeHint(AvatarIKHint hint, LimbIKProcessor associatedFoot)
        {
            if (!associatedFoot.IsActive || !associatedFoot.DidHit)
            {
                _animator.SetIKHintPositionWeight(hint, 0);
                return;
            }

            HumanBodyBones thighBone = (hint == AvatarIKHint.LeftKnee) ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg;
            Transform thighTransform = _animator.GetBoneTransform(thighBone);

            Vector3 footTargetPosition = associatedFoot.TargetIKPosition;
            Vector3 thighPosition = thighTransform.position;

            Vector3 hintMidPoint = Vector3.Lerp(thighPosition, footTargetPosition, 0.5f);
            Vector3 hintOffset = transform.rotation * KneeHintOffset;

            _animator.SetIKHintPosition(hint, hintMidPoint + hintOffset);
            _animator.SetIKHintPositionWeight(hint, KneeHintWeight * associatedFoot.CurrentWeight);
        }
    }
}