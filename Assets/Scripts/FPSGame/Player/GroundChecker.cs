using UnityEngine;
using Sirenix.OdinInspector;

namespace FPS
{
    public class GroundChecker : MonoBehaviour
    {
        [SerializeField, Required] private Transform checkPoint;
        [SerializeField] private float checkRadius = 0.4f;
        [SerializeField] private LayerMask groundLayer;

        [field: ShowInInspector, ReadOnly]
        public bool IsGrounded { get; private set; }

        private void Update()
        {
            IsGrounded = Physics.CheckSphere(checkPoint.position, checkRadius, groundLayer);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            if (checkPoint != null)
            {
                Gizmos.DrawWireSphere(checkPoint.position, checkRadius);
            }
        }
    }
}