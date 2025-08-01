using UnityEngine;

namespace Orion
{
    public sealed class LimbIKProcessor
    {
        private readonly AvatarIKGoal _limbGoal;
        private readonly LayerMask _ikLayerMask;
        private readonly Transform _playerTransform;

        private float _targetWeight;
        private float _currentWeight;
        private RaycastHit _lastHitInfo;

        public bool IsActive => _currentWeight > 0.01f;
        public float CurrentWeight => _currentWeight;
        public Vector3 TargetIKPosition { get; private set; }
        public bool DidHit { get; private set; }

        private Vector3 _raycastOriginDebug;

        public LimbIKProcessor(AvatarIKGoal limbGoal, LayerMask layerMask, Transform playerTransform)
        {
            _limbGoal = limbGoal;
            _ikLayerMask = layerMask;
            _playerTransform = playerTransform;
        }

        public void SetTargetWeight(float weight)
        {
            _targetWeight = Mathf.Clamp01(weight);
        }

        public void UpdateCurrentWeight(float blendInSpeed, float blendOutSpeed, float deltaTime)
        {
            float speed = (_targetWeight > _currentWeight) ? blendInSpeed : blendOutSpeed;
            _currentWeight = Mathf.MoveTowards(_currentWeight, _targetWeight, speed * deltaTime);
        }

        public void ProcessIK(Animator animator, IKSolverSettings settings)
        {
            animator.SetIKPositionWeight(_limbGoal, _currentWeight * settings.PositionWeight);
            animator.SetIKRotationWeight(_limbGoal, _currentWeight * settings.RotationWeight);

            if (!IsActive)
            {
                DidHit = false;
                return;
            }

            Vector3 originalLimbPosition = animator.GetIKPosition(_limbGoal);
            Vector3 raycastOriginOffset = (settings is FootIKSettings footSettings) ? footSettings.RaycastOriginOffset : Vector3.zero;
            _raycastOriginDebug = originalLimbPosition + raycastOriginOffset;

            Vector3 raycastDirection = GetRaycastDirection();
            DidHit = Physics.Raycast(_raycastOriginDebug, raycastDirection, out _lastHitInfo, settings.RaycastDistance, _ikLayerMask);

            if (DidHit)
            {
                ApplyIKTransformations(animator, settings, originalLimbPosition);
            }
        }

        private void ApplyIKTransformations(Animator animator, IKSolverSettings settings, Vector3 originalLimbPosition)
        {
            Quaternion targetRotation;

            if (settings is FootIKSettings footSettings)
            {
                TargetIKPosition = CalculateIntelligentFootPosition(animator, footSettings, originalLimbPosition, _lastHitInfo);
                targetRotation = Quaternion.FromToRotation(Vector3.up, _lastHitInfo.normal) * animator.GetIKRotation(_limbGoal);
            }
            else if (settings is HandIKSettings handSettings)
            {
                TargetIKPosition = _lastHitInfo.point + _lastHitInfo.normal * handSettings.PlacementOffset;
                targetRotation = Quaternion.LookRotation(-_lastHitInfo.normal, _playerTransform.up);
            }
            else return;

            animator.SetIKPosition(_limbGoal, TargetIKPosition);
            animator.SetIKRotation(_limbGoal, targetRotation);
        }

        private Vector3 GetRaycastDirection()
        {
            if (_limbGoal == AvatarIKGoal.LeftFoot || _limbGoal == AvatarIKGoal.RightFoot) return Vector3.down;
            return (_limbGoal == AvatarIKGoal.LeftHand) ? -_playerTransform.right : _playerTransform.right;
        }

        private Vector3 CalculateIntelligentFootPosition(Animator animator, FootIKSettings settings, Vector3 originalFootPosition, RaycastHit hit)
        {
            Vector3 targetPosition = hit.point;
            targetPosition.y += settings.YOffset;

            float slopeAngle = Vector3.Angle(Vector3.up, hit.normal);
            if (slopeAngle > 1f)
            {
                Vector3 forwardDirection = Vector3.ProjectOnPlane(_playerTransform.forward, hit.normal).normalized;
                targetPosition += forwardDirection * settings.ForwardOffsetOnSlopes * Mathf.Sin(slopeAngle * Mathf.Deg2Rad);
            }
            return targetPosition;
        }

        public void DrawGizmos(Animator animator, IKSolverSettings settings, Color gizmoColor)
        {
            if (animator == null || !animator.isInitialized) return;

            Vector3 originalLimbPosition = animator.GetIKPosition(_limbGoal);
            Vector3 raycastOriginOffset = (settings is FootIKSettings footSettings) ? footSettings.RaycastOriginOffset : Vector3.zero;
            Vector3 raycastOrigin = originalLimbPosition + raycastOriginOffset;

            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(raycastOrigin, 0.05f);

            Vector3 raycastDirection = GetRaycastDirection();

            if (Physics.Raycast(raycastOrigin, raycastDirection, out RaycastHit hit, settings.RaycastDistance, _ikLayerMask))
            {
                Gizmos.DrawLine(raycastOrigin, hit.point);
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(hit.point, 0.03f);
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(hit.point, hit.normal * 0.3f);
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(raycastOrigin, raycastDirection * settings.RaycastDistance);
            }
        }
    }
}