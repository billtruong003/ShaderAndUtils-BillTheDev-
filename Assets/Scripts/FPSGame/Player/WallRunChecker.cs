using UnityEngine;
using Sirenix.OdinInspector;

namespace FPS
{
    public class WallRunChecker : MonoBehaviour
    {
        [SerializeField, Required] private Transform orientation;
        [SerializeField] private float wallCheckDistance = 0.7f;
        [SerializeField] private LayerMask wallLayer;

        [field: ShowInInspector, ReadOnly]
        public bool IsOnLeftWall { get; private set; }
        [field: ShowInInspector, ReadOnly]
        public bool IsOnRightWall { get; private set; }
        public bool CanWallRun => IsOnLeftWall || IsOnRightWall;

        public RaycastHit LeftWallHit { get; private set; }
        public RaycastHit RightWallHit { get; private set; }

        private void Update()
        {
            IsOnLeftWall = Physics.Raycast(transform.position, -orientation.right, out RaycastHit leftHit, wallCheckDistance, wallLayer);
            LeftWallHit = leftHit;

            IsOnRightWall = Physics.Raycast(transform.position, orientation.right, out RaycastHit rightHit, wallCheckDistance, wallLayer);
            RightWallHit = rightHit;
        }
    }
}