using UnityEngine;
using System.Collections.Generic;

namespace PerfHeatMap
{
    [System.Serializable]
    public struct HeatMapSample
    {
        public Vector3 Position;
        public float Stat1_DrawCalls;
        public float Stat2_Triangles;
        public float Stat3_GpuTimeMS;
        public float Stat4_FrameTimeMS;
    }

    [CreateAssetMenu(fileName = "PerfHeatMap_Data", menuName = "PerfHeatMap/Capture Data")]
    public class PerfHeatMapData : ScriptableObject
    {
        public Bounds AnalysisBounds;
        public Vector3 CellSize;
        public List<HeatMapSample> Samples = new List<HeatMapSample>();

        public Vector3Int GetGridDimensions()
        {
            if (CellSize.x <= 0 || CellSize.y <= 0 || CellSize.z <= 0)
            {
                return Vector3Int.one;
            }

            return new Vector3Int(
                Mathf.CeilToInt(AnalysisBounds.size.x / CellSize.x),
                Mathf.CeilToInt(AnalysisBounds.size.y / CellSize.y),
                Mathf.CeilToInt(AnalysisBounds.size.z / CellSize.z)
            );
        }
    }
}