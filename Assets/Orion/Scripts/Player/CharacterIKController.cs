using UnityEngine;

namespace Orion
{
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerIKController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PlayerController _playerController;

        [Header("Foot IK Settings")]
        [SerializeField] private bool _enableFootIK = true;
        [SerializeField, Range(0f, 1f)] private float _footIKPositionWeight = 1f;
        [SerializeField, Range(0f, 1f)] private float _footIKRotationWeight = 1f;
        [SerializeField] private float _footRaycastDistance = 1.2f;
        [SerializeField] private float _footRaycastOriginOffset = 0.5f;
        [SerializeField] private float _footYOffset = 0.05f;
        [SerializeField] private LayerMask _ikLayerMask;

        [Header("Hand IK Settings")]
        [SerializeField] private bool _enableHandIK = true;
        [SerializeField, Range(0f, 1f)] private float _handIKPositionWeight = 1f;
        [SerializeField, Range(0f, 1f)] private float _handIKRotationWeight = 1f;
        [SerializeField] private float _handRaycastDistance = 1.5f;
        [SerializeField] private Vector3 _handRaycastOriginOffset = new Vector3(0, 1.5f, 0);

        private Animator _animator;

        private readonly IKDebugData _leftFootDebugData = new IKDebugData();
        private readonly IKDebugData _rightFootDebugData = new IKDebugData();
        private readonly IKDebugData _leftHandDebugData = new IKDebugData();
        private readonly IKDebugData _rightHandDebugData = new IKDebugData();

        [System.Serializable]
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

            public void Reset()
            {
                IsActive = false;
                HitFound = false;
            }
        }

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_playerController == null)
            {
                enabled = false;
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (!_playerController || !_animator) return;

            _leftFootDebugData.Reset();
            _rightFootDebugData.Reset();
            _leftHandDebugData.Reset();
            _rightHandDebugData.Reset();

            if (_enableFootIK)
            {
                HandleFootIK();
            }

            if (_enableHandIK)
            {
                HandleHandIK();
            }
        }

        private void HandleFootIK()
        {
            bool shouldApplyIK = _playerController.IsGrounded || _playerController.CurrentState == _playerController.WallRunState;
            if (!shouldApplyIK)
            {
                ResetIKWeight(AvatarIKGoal.LeftFoot);
                ResetIKWeight(AvatarIKGoal.RightFoot);
                return;
            }

            ApplyIKForFoot(AvatarIKGoal.LeftFoot, _leftFootDebugData);
            ApplyIKForFoot(AvatarIKGoal.RightFoot, _rightFootDebugData);
        }

        private void ApplyIKForFoot(AvatarIKGoal foot, IKDebugData debugData)
        {
            debugData.IsActive = true;
            debugData.OriginalLimbPosition = _animator.GetIKPosition(foot);

            Vector3 rayOrigin = debugData.OriginalLimbPosition + Vector3.up * _footRaycastOriginOffset;

            debugData.RaycastOrigin = rayOrigin;
            debugData.RaycastDirection = Vector3.down;
            debugData.RaycastLength = _footRaycastDistance;

            bool hitFound = Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, _footRaycastDistance, _ikLayerMask);
            debugData.HitFound = hitFound;

            if (hitFound)
            {
                _animator.SetIKPositionWeight(foot, _footIKPositionWeight);
                _animator.SetIKRotationWeight(foot, _footIKRotationWeight);

                Vector3 targetPosition = hit.point + new Vector3(0, _footYOffset, 0);
                _animator.SetIKPosition(foot, targetPosition);

                Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * _animator.GetIKRotation(foot);
                _animator.SetIKRotation(foot, targetRotation);

                debugData.HitPoint = hit.point;
                debugData.HitNormal = hit.normal;
                debugData.TargetIKPosition = targetPosition;
            }
            else
            {
                ResetIKWeight(foot);
            }
        }

        // >>> LOGIC ĐÃ ĐƯỢC CẤU TRÚC LẠI HOÀN TOÀN <<<
        private void HandleHandIK()
        {
            bool isWallRunning = _playerController.CurrentState == _playerController.WallRunState;

            // Luôn cập nhật vị trí gốc của tay cho Gizmos
            _leftHandDebugData.OriginalLimbPosition = _animator.GetIKPosition(AvatarIKGoal.LeftHand);
            _rightHandDebugData.OriginalLimbPosition = _animator.GetIKPosition(AvatarIKGoal.RightHand);

            if (!isWallRunning)
            {
                ResetIKWeight(AvatarIKGoal.LeftHand);
                ResetIKWeight(AvatarIKGoal.RightHand);
                return;
            }

            bool isWallOnRight = _playerController.IsWallRunningOnRight;
            if (isWallOnRight)
            {
                ApplyIKForHand(AvatarIKGoal.RightHand, _rightHandDebugData);
                ResetIKWeight(AvatarIKGoal.LeftHand);
            }
            else
            {
                ApplyIKForHand(AvatarIKGoal.LeftHand, _leftHandDebugData);
                ResetIKWeight(AvatarIKGoal.RightHand);
            }
        }

        private void ApplyIKForHand(AvatarIKGoal hand, IKDebugData debugData)
        {
            debugData.IsActive = true;

            bool isRightHand = (hand == AvatarIKGoal.RightHand);
            Vector3 raycastDirection = isRightHand ? transform.right : -transform.right;
            Vector3 raycastOrigin = transform.position + _handRaycastOriginOffset;

            debugData.RaycastOrigin = raycastOrigin;
            debugData.RaycastDirection = raycastDirection;
            debugData.RaycastLength = _handRaycastDistance;

            bool hitFound = Physics.Raycast(raycastOrigin, raycastDirection, out RaycastHit hit, _handRaycastDistance, _ikLayerMask);
            debugData.HitFound = hitFound;

            if (hitFound)
            {
                _animator.SetIKPositionWeight(hand, _handIKPositionWeight);
                _animator.SetIKRotationWeight(hand, _handIKRotationWeight);

                Vector3 targetPosition = hit.point;
                _animator.SetIKPosition(hand, targetPosition);

                // Sử dụng pháp tuyến thực tế của tường để xoay bàn tay
                Quaternion targetRotation = Quaternion.LookRotation(-hit.normal, Vector3.up);
                _animator.SetIKRotation(hand, targetRotation);

                debugData.HitPoint = hit.point;
                debugData.HitNormal = hit.normal;
                debugData.TargetIKPosition = targetPosition;
            }
            else
            {
                ResetIKWeight(hand);
            }
        }
        // >>> KẾT THÚC THAY ĐỔI <<<

        private void ResetIKWeight(AvatarIKGoal goal)
        {
            _animator.SetIKPositionWeight(goal, 0);
            _animator.SetIKRotationWeight(goal, 0);
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || _animator == null) return;

            DrawIKGizmosForLimb(_leftFootDebugData, Color.green, "Left Foot");
            DrawIKGizmosForLimb(_rightFootDebugData, Color.cyan, "Right Foot");
            DrawIKGizmosForLimb(_leftHandDebugData, Color.magenta, "Left Hand");
            DrawIKGizmosForLimb(_rightHandDebugData, Color.yellow, "Right Hand");
        }

        private void DrawIKGizmosForLimb(IKDebugData debugData, Color gizmoColor, string limbName)
        {
            // Vẽ vị trí gốc của chi (nếu không hoạt động)
            if (!debugData.IsActive)
            {
                if (limbName.Contains("Hand")) // Chỉ vẽ cho tay để tránh rối
                {
                    Gizmos.color = Color.gray;
                    Gizmos.DrawWireSphere(debugData.OriginalLimbPosition, 0.05f);
                }
                return;
            }

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