using UnityEngine;

namespace PerfHeatMap
{
    [AddComponentMenu("PerfHeatMap/Scene Settings")]
    public class PerfHeatMapSceneSettings : MonoBehaviour
    {
        [Header("Capture Volume")]
        public Bounds CaptureBounds = new Bounds(Vector3.zero, Vector3.one * 20f);

        [Header("Capture Settings")]
        [Min(0.1f)] public Vector3 CellSize = Vector3.one * 2f;
        public bool ExcludeCellsTooFarFromGround = true;
        [Min(0.1f)] public float MaxDistanceFromGround = 2.5f;
        public bool ExcludeCellsInsideColliders = true;
        public LayerMask ExclusionLayers;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.3f);
            Gizmos.DrawCube(CaptureBounds.center, CaptureBounds.size);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(CaptureBounds.center, CaptureBounds.size);
        }
    }
}