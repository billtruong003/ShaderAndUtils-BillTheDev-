using UnityEngine;

namespace Orion
{
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerIKController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PlayerController _playerController;
        private Animator _animator;

        [Header("IK Blending Settings")]
        [SerializeField] private bool _enableIK = true;
        [SerializeField] private float _ikBlendSpeed = 15f;

        [Header("Foot IK Settings")]
        [SerializeField, Range(0f, 1f)] private float _footIKPositionWeight = 1f;
        [SerializeField, Range(0f, 1f)] private float _footIKRotationWeight = 1f;
        [SerializeField] private float _footRaycastDistance = 1.2f;
        [SerializeField] private float _footRaycastOriginOffset = 0.5f;
        [SerializeField] private float _footYOffset = 0.05f;
        [SerializeField] private float _footForwardOffset = 0.15f;
        [SerializeField] private LayerMask _ikLayerMask;

        [Header("Hand IK Settings")]
        [SerializeField, Range(0f, 1f)] private float _handIKPositionWeight = 1f;
        [SerializeField, Range(0f, 1f)] private float _handIKRotationWeight = 1f;
        [SerializeField] private float _handRaycastDistance = 1.5f;
        [SerializeField] private float _handPlacementOffset = 0.1f;

        // Blending state variables
        private float _currentLeftFootWeight;
        private float _currentRightFootWeight;
        private float _currentLeftHandWeight;
        private float _currentRightHandWeight;
        private float _targetLeftFootWeight;
        private float _targetRightFootWeight;
        private float _targetLeftHandWeight;
        private float _targetRightHandWeight;

        // Velocity references for SmoothDamp
        private float _weightVelocityLF, _weightVelocityRF, _weightVelocityLH, _weightVelocityRH;

        // Debug data
        private readonly IKDebugData _leftFootDebugData = new IKDebugData();
        private readonly IKDebugData _rightFootDebugData = new IKDebugData();
        private readonly IKDebugData _leftHandDebugData = new IKDebugData();
        private readonly IKDebugData _rightHandDebugData = new IKDebugData();

        private class IKDebugData
        {
            public bool IsActive;
            public bool HitFound;
            public Vector3 RaycastOrigin;
            public Vector3 RaycastDirection;
            public float RaycastLength;
            public Vector3 HitPoint;
            public Vector3 HitNormal;
            public Vector3 TargetIKPosition;
            public Vector3 OriginalLimbPosition;
        }

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_playerController == null)
            {
                enabled = false;
                _enableIK = false;
            }
        }

        private void Update()
        {
            if (!_enableIK) return;
            UpdateIKTargetWeights();
            SmoothIKWeights();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (!_enableIK || !_animator) return;

            ResetAllDebugData();

            ProcessFootIK(AvatarIKGoal.LeftFoot, _leftFootDebugData, _currentLeftFootWeight);
            ProcessFootIK(AvatarIKGoal.RightFoot, _rightFootDebugData, _currentRightFootWeight);
            ProcessHandIK(AvatarIKGoal.LeftHand, _leftHandDebugData, _currentLeftHandWeight, -transform.right);
            ProcessHandIK(AvatarIKGoal.RightHand, _rightHandDebugData, _currentRightHandWeight, transform.right);
        }

        private void UpdateIKTargetWeights()
        {
            bool isGrounded = _playerController.IsGrounded;
            bool isWallRunning = _playerController.CurrentState == _playerController.WallRunState;

            _targetLeftFootWeight = (isGrounded || (isWallRunning && !_playerController.IsWallRunningOnRight)) ? 1f : 0f;
            _targetRightFootWeight = (isGrounded || (isWallRunning && _playerController.IsWallRunningOnRight)) ? 1f : 0f;

            _targetLeftHandWeight = (isWallRunning && !_playerController.IsWallRunningOnRight) ? 1f : 0f;
            _targetRightHandWeight = (isWallRunning && _playerController.IsWallRunningOnRight) ? 1f : 0f;
        }

        private void SmoothIKWeights()
        {
            float blendDuration = 1f / _ikBlendSpeed;
            float deltaTime = Time.deltaTime;

            _currentLeftFootWeight = Mathf.SmoothDamp(_currentLeftFootWeight, _targetLeftFootWeight, ref _weightVelocityLF, blendDuration, Mathf.Infinity, deltaTime);
            _currentRightFootWeight = Mathf.SmoothDamp(_currentRightFootWeight, _targetRightFootWeight, ref _weightVelocityRF, blendDuration, Mathf.Infinity, deltaTime);
            _currentLeftHandWeight = Mathf.SmoothDamp(_currentLeftHandWeight, _targetLeftHandWeight, ref _weightVelocityLH, blendDuration, Mathf.Infinity, deltaTime);
            _currentRightHandWeight = Mathf.SmoothDamp(_currentRightHandWeight, _targetRightHandWeight, ref _weightVelocityRH, blendDuration, Mathf.Infinity, deltaTime);
        }

        private void ProcessFootIK(AvatarIKGoal foot, IKDebugData debugData, float weight)
        {
            debugData.OriginalLimbPosition = _animator.GetIKPosition(foot);
            debugData.IsActive = weight > 0.01f;

            _animator.SetIKPositionWeight(foot, weight * _footIKPositionWeight);
            _animator.SetIKRotationWeight(foot, weight * _footIKRotationWeight);

            if (!debugData.IsActive) return;

            Vector3 rayOrigin = debugData.OriginalLimbPosition + Vector3.up * _footRaycastOriginOffset;
            debugData.RaycastOrigin = rayOrigin;
            debugData.RaycastDirection = Vector3.down;
            debugData.RaycastLength = _footRaycastDistance;

            debugData.HitFound = Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, _footRaycastDistance, _ikLayerMask);

            if (debugData.HitFound)
            {
                Vector3 targetPosition = CalculateIntelligentFootPosition(foot, debugData.OriginalLimbPosition, hit);
                Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * _animator.GetIKRotation(foot);

                _animator.SetIKPosition(foot, targetPosition);
                _animator.SetIKRotation(foot, targetRotation);

                UpdateDebugHitData(debugData, hit, targetPosition);
            }
        }

        private void ProcessHandIK(AvatarIKGoal hand, IKDebugData debugData, float weight, Vector3 raycastDirection)
        {
            debugData.OriginalLimbPosition = _animator.GetIKPosition(hand);
            debugData.IsActive = weight > 0.01f;

            _animator.SetIKPositionWeight(hand, weight * _handIKPositionWeight);
            _animator.SetIKRotationWeight(hand, weight * _handIKRotationWeight);

            if (!debugData.IsActive) return;

            Vector3 rayOrigin = debugData.OriginalLimbPosition - raycastDirection * 0.2f + Vector3.up * 0.1f; // Start raycast slightly behind the hand
            debugData.RaycastOrigin = rayOrigin;
            debugData.RaycastDirection = raycastDirection;
            debugData.RaycastLength = _handRaycastDistance;

            debugData.HitFound = Physics.Raycast(rayOrigin, raycastDirection, out RaycastHit hit, _handRaycastDistance, _ikLayerMask);

            if (debugData.HitFound)
            {
                Vector3 targetPosition = hit.point + hit.normal * _handPlacementOffset;
                Quaternion targetRotation = Quaternion.LookRotation(-hit.normal, Vector3.up);

                _animator.SetIKPosition(hand, targetPosition);
                _animator.SetIKRotation(hand, targetRotation);

                UpdateDebugHitData(debugData, hit, targetPosition);
            }
        }

        private Vector3 CalculateIntelligentFootPosition(AvatarIKGoal foot, Vector3 originalFootPosition, RaycastHit hit)
        {
            Vector3 targetPosition = hit.point;

            HumanBodyBones bone = (foot == AvatarIKGoal.LeftFoot) ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot;
            Transform ankleTransform = _animator.GetBoneTransform(bone);

            Vector3 horizontalOffset = Vector3.ProjectOnPlane(originalFootPosition - ankleTransform.position, Vector3.up);

            targetPosition += horizontalOffset;
            targetPosition.y += _footYOffset;

            float slopeAngle = Vector3.Angle(Vector3.up, hit.normal);
            if (slopeAngle > 1f)
            {
                Vector3 forwardDirection = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;
                targetPosition += forwardDirection * _footForwardOffset * Mathf.Sin(slopeAngle * Mathf.Deg2Rad);
            }

            return targetPosition;
        }

        private void ResetAllDebugData()
        {
            _leftFootDebugData.IsActive = false;
            _rightFootDebugData.IsActive = false;
            _leftHandDebugData.IsActive = false;
            _rightHandDebugData.IsActive = false;
        }

        private void UpdateDebugHitData(IKDebugData data, RaycastHit hit, Vector3 targetPos)
        {
            data.HitPoint = hit.point;
            data.HitNormal = hit.normal;
            data.TargetIKPosition = targetPos;
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || _animator == null) return;

            DrawIKGizmosForLimb(_leftFootDebugData, Color.green);
            DrawIKGizmosForLimb(_rightFootDebugData, Color.cyan);
            DrawIKGizmosForLimb(_leftHandDebugData, Color.magenta);
            DrawIKGizmosForLimb(_rightHandDebugData, Color.yellow);
        }

        private void DrawIKGizmosForLimb(IKDebugData debugData, Color gizmoColor)
        {
            if (!debugData.IsActive) return;

            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(debugData.RaycastOrigin, 0.05f);

            if (debugData.HitFound)
            {
                Gizmos.DrawLine(debugData.RaycastOrigin, debugData.HitPoint);
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(debugData.HitPoint, 0.03f);
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(debugData.HitPoint, debugData.HitNormal * 0.3f);
                Gizmos.color = gizmoColor;
                Gizmos.DrawCube(debugData.TargetIKPosition, Vector3.one * 0.1f);
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(debugData.RaycastOrigin, debugData.RaycastDirection * debugData.RaycastLength);
            }
        }
    }
}