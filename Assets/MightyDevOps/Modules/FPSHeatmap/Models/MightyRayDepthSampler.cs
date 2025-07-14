#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using MightyFPSHeatmap;
using System.Linq;

namespace MightyFPSHeatmap
{
    public static class MightyRayDepthSampler
    {
        public static float cellSize = 1f;

        public static void GenerateVisibilityData()
        {
            System.Diagnostics.Stopwatch totalStopwatch = System.Diagnostics.Stopwatch.StartNew();

            FPSHeatmapData trackingData = FPSHeatmapData.Load();
            if (trackingData == null)
            {
                Debug.LogError("TrackingData not found.");
                return;
            }

            string sceneName = SceneManager.GetActiveScene().name;
            FPSHeatmapData.SceneData sceneData = trackingData.scenes.FirstOrDefault(s => s.name == sceneName);
            if (sceneData == null)
            {
                sceneData = new FPSHeatmapData.SceneData { name = sceneName };
                trackingData.scenes.Add(sceneData);
            }

            Debug.Log("<color=green><b>Starting visibility data generation...</b></color>");

            Bounds sceneBounds = ComputeSceneBounds();
            Debug.Log($"<color=blue>Computed scene bounds: min={sceneBounds.min}, max={sceneBounds.max}</color>");

            Vector3 min = sceneBounds.min;
            Vector3 max = sceneBounds.max;
            int width = Mathf.CeilToInt((max.x - min.x) / cellSize);
            int height = Mathf.CeilToInt((max.y - min.y) / cellSize);
            int depth = Mathf.CeilToInt((max.z - min.z) / cellSize);
            Debug.Log($"<color=blue>Grid dimensions: {width}x{height}x{depth} cells</color>");

            Vector3 origin = min;

            Vector3[] directions = GetDirections();
            Debug.Log($"<color=blue>Using {directions.Length} raycast directions per cell.</color>");

            FPSHeatmapData.VisibilityCell[] cells = new FPSHeatmapData.VisibilityCell[width * height * depth];

            float sceneDiagonal = (max - min).magnitude;

            int totalCells = width * height * depth;
            int cellsProcessed = 0;
            float progressThreshold = 0.1f;
            float nextProgress = progressThreshold;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        Vector3 cellCenter = origin + new Vector3((x + 0.5f) * cellSize, (y + 0.5f) * cellSize, (z + 0.5f) * cellSize);
                        FPSHeatmapData.VisibilityCell cellData = new FPSHeatmapData.VisibilityCell { depths = new float[directions.Length] };
                        for (int d = 0; d < directions.Length; d++)
                        {
                            Vector3 dir = directions[d];
                            if (Physics.Raycast(cellCenter, dir, out RaycastHit hit, sceneDiagonal))
                            {
                                cellData.depths[d] = hit.distance;
                            }
                            else
                            {
                                cellData.depths[d] = sceneDiagonal;
                            }
                        }
                        int index = x + y * width + z * width * height;
                        cells[index] = cellData;

                        cellsProcessed++;
                        float progress = (float)cellsProcessed / totalCells;
                        if (progress >= nextProgress)
                        {
                            Debug.Log($"<color=yellow>Progress: {Mathf.RoundToInt(progress * 100)}% complete</color>");
                            nextProgress += progressThreshold;
                        }
                    }
                }
            }

            Debug.Log("<color=green>Visibility data generation complete.</color>");

            FPSHeatmapData.VisibilityData visData = new FPSHeatmapData.VisibilityData
            {
                cellSize = cellSize,
                width = width,
                height = height,
                depth = depth,
                origin = origin,
                directions = directions,
                cells = cells
            };

            sceneData.visibilityData = visData;

            EditorUtility.SetDirty(trackingData);

            totalStopwatch.Stop();
            Debug.Log($"<color=green><b>Operation completed in {totalStopwatch.Elapsed.TotalSeconds:F2} seconds.</b></color>");
        }

        private static Bounds ComputeSceneBounds()
        {
            Bounds bounds = new Bounds();
            bool first = true;
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (first)
                {
                    bounds = r.bounds;
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }
            return bounds;
        }

        private static Vector3[] GetDirections()
        {
            List<Vector3> dirs = new List<Vector3>();
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        if (x == 0 && y == 0 && z == 0) continue;
                        Vector3 dir = new Vector3(x, y, z).normalized;
                        dirs.Add(dir);
                    }
                }
            }
            return dirs.ToArray();
        }
    }
}
#endif