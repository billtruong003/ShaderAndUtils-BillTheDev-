#if UNITY_EDITOR
using UnityEngine;

namespace MightyFPSHeatmap
{
    public class VisibilityData : ScriptableObject
    {
        public float cellSize;
        public int width, height, depth;
        public Vector3 origin;
        public Vector3[] directions;
        public VisibilityCell[,,] cells;
    }

    [System.Serializable]
    public class VisibilityCell
    {
        public float[] depths;
    }
}
#endif