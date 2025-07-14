#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using static Mighty.MightyCoreData;
using static Mighty.MightyCoreData.SceneData;
using static MightyFPSHeatmap.FPSHeatmapData;
using static MightyFPSHeatmap.FPSHeatmapData.HeatmapTracking;

namespace MightyFPSHeatmap
{
    [InitializeOnLoad]
    public class MightyFPSHeatmapper : MonoBehaviour
    {
        public float sampleInterval = 0.5f;
        public int polyThreshold = 10000;

        public bool trackFPS = true, trackMemory = false;
        public float updateInterval = 0.5f;
        public float fpsThreshold = 10f;
        public long memorySpikeThreshold = 50 * 1024 * 1024;

        [Header("LOD Filtering")]
        public bool useLODFilter = true;
        public int lodMask = -1;
        public bool logNonLODObjects = true;

        public bool useOcclusionCulling = true;
        public LayerMask occlusionLayerMask;

        [Header("Terrain Logging")]
        public bool logTerrainTrees = true;
        public bool trackBillboards = false;

        private static Dictionary<(TerrainData, int), int> treePolycounts = new Dictionary<(TerrainData, int), int>();
        private static bool treePolycountsCached = false;

        private string id;
        private float timer = 0f;
        private float deltaTime = 0f, fps = 0f;
        private Vector3 lastPosition;
        private Quaternion lastRotation;
        private long lastTimestamp;

        // Moving average FPS tracking
        private const float MOVING_AVERAGE_WINDOW = 10f; // 10 seconds
        private List<(float fps, float timestamp)> fpsHistory = new List<(float, float)>();
        private float previousMovingAverage = 0f;
        private float currentMovingAverage = 0f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private long lastMemoryUsage = 0;
        private long currentMemoryUsage = 0;
#endif


        void Start()
        {
            if (string.IsNullOrEmpty(id))
            {
                id = this.name + "." + System.DateTime.Now.Ticks.ToString() + "." + UnityEngine.Random.Range(1, 10001).ToString();
            }

            if (!treePolycountsCached)
            {
                CacheTreePolycounts();
            }
        }


        void OnEnable()
        {
            lastPosition = transform.position;
            lastRotation = transform.rotation;
            lastTimestamp = System.DateTime.Now.Ticks;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (trackMemory)
            {
                lastMemoryUsage = System.GC.GetTotalMemory(false);
            }
#endif
        }

        void Update()
        {
            if (!FPSHeatmapCore.sceneData?.RecordPlaythrough ?? true) return;
            if (FPSHeatmapCore.trackingType == null) return;
            if (!FPSHeatmapCore.trackingType.IsActive) return;

            timer += Time.deltaTime;
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            fps = 1.0f / deltaTime;

            // Update FPS moving average
            UpdateFPSMovingAverage();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (trackMemory)
            {
                currentMemoryUsage = System.GC.GetTotalMemory(false);
            }
#endif

            if (ShouldRecordData())
            {
                var (positions, polyCounts, lodLevels) = CaptureSample(Camera.main);
                RecordData(fps, positions, polyCounts, lodLevels);
            }
            else if (timer >= updateInterval)
            {
                timer = 0f;
                var (positions, polyCounts, lodLevels) = CaptureSample(Camera.main);
                RecordData(fps, positions, polyCounts, lodLevels);
            }
        }

        private void CacheTreePolycounts()
        {
            if (treePolycountsCached) return;
            treePolycounts.Clear();
            Terrain[] terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            foreach (var terrain in terrains)
            {
                TerrainData terrainData = terrain.terrainData;
                TreePrototype[] treePrototypes = terrainData.treePrototypes;
                for (int i = 0; i < treePrototypes.Length; i++)
                {
                    if (!treePolycounts.ContainsKey((terrainData, i)))
                    {
                        GameObject prefab = treePrototypes[i].prefab;
                        if (prefab != null)
                        {
                            MeshFilter meshFilter = prefab.GetComponentInChildren<MeshFilter>();
                            if (meshFilter != null && meshFilter.sharedMesh != null)
                            {
                                if (meshFilter.sharedMesh.isReadable)
                                {
                                    int polyCount = meshFilter.sharedMesh.triangles.Length / 3;
                                    treePolycounts[(terrainData, i)] = polyCount;
                                }
                                else
                                {
                                    Debug.LogWarning($"Mesh for tree prototype {i} in TerrainData {terrainData.name} is not readable. Polycount set to 0.");
                                    treePolycounts[(terrainData, i)] = 0;
                                }
                            }
                            else
                            {
                                treePolycounts[(terrainData, i)] = 0;
                            }
                        }
                    }
                }
            }
            treePolycountsCached = true;
            DevLog($"Cached polycounts for {treePolycounts.Count} tree prototypes across all TerrainData.");
        }

        (List<Vector3>, List<int>, List<int>) CaptureSample(Camera camera)
        {
            List<Vector3> visibleObjectPositions = new List<Vector3>();
            List<int> visibleObjectPolyCounts = new List<int>();
            List<int> visibleObjectLODLevels = new List<int>();
            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);

            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (var renderer in renderers)
            {
                if (GeometryUtility.TestPlanesAABB(frustumPlanes, renderer.bounds))
                {
                    if (IsObjectVisible(camera, renderer.transform))
                    {
                        int currentLOD = -1;
                        LODGroup lodGroup = renderer.GetComponentInParent<LODGroup>();
                        if (lodGroup != null)
                        {
                            currentLOD = GetCurrentLODLevel(lodGroup, camera);
                            if (useLODFilter && (lodMask & (1 << currentLOD)) == 0)
                            {
                                continue;
                            }
                        }
                        else if (useLODFilter && lodMask != 0 && lodMask != -1 && !logNonLODObjects)
                        {
                            continue;
                        }

                        int polyCount = 0;
                        MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                        if (meshFilter != null && meshFilter.sharedMesh != null && meshFilter.sharedMesh.isReadable)
                        {
                            polyCount = meshFilter.sharedMesh.triangles.Length / 3;
                        }

                        visibleObjectPositions.Add(renderer.transform.position);
                        visibleObjectPolyCounts.Add(polyCount);
                        visibleObjectLODLevels.Add(currentLOD);
                    }
                }
            }

            Terrain[] terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            foreach (var terrain in terrains)
            {
                Terrain terrainComponent = terrain.GetComponent<Terrain>();
                if (terrainComponent == null || terrainComponent.terrainData == null)
                {
                    Debug.LogWarning($"[MightyTracker] Terrain {terrain.name} has no Terrain component or TerrainData.");
                    continue;
                }

                Bounds terrainBounds;
                TerrainCollider terrainCollider = terrain.GetComponent<TerrainCollider>();
                if (terrainCollider != null)
                {
                    terrainBounds = terrainCollider.bounds;
                }
                else
                {
                    Vector3 terrainSize = terrainComponent.terrainData.size;
                    terrainBounds = new Bounds(terrain.transform.position + terrainSize / 2, terrainSize);
                }

                bool isInFrustum = GeometryUtility.TestPlanesAABB(frustumPlanes, terrainBounds);
                if (isInFrustum)
                {
                    TerrainData terrainData = terrainComponent.terrainData;
                    Vector3 terrainPos = terrain.transform.position;

                    if (logTerrainTrees && terrainData != null)
                    {
                        TreeInstance[] trees = terrainData.treeInstances;
                        foreach (var tree in trees)
                        {
                            Vector3 treeLocalPos = tree.position;
                            Vector3 treeWorldPos = Vector3.Scale(treeLocalPos, terrainData.size) + terrainPos;
                            if (IsPointInFrustum(treeWorldPos, frustumPlanes))
                            {
                                float treeDistance = Vector3.Distance(camera.transform.position, treeWorldPos);
                                int treeLOD = CalculateTreeLOD(treeDistance, terrain);
                                if (treeLOD == 0 || (treeLOD == 1 && trackBillboards))
                                {
                                    int polyCount = treePolycounts.ContainsKey((terrainData, tree.prototypeIndex))
                                        ? treePolycounts[(terrainData, tree.prototypeIndex)]
                                        : 0;
                                    visibleObjectPositions.Add(treeWorldPos);
                                    visibleObjectPolyCounts.Add(polyCount);
                                    visibleObjectLODLevels.Add(treeLOD);
                                }
                            }
                        }
                    }
                }
            }

            return (visibleObjectPositions, visibleObjectPolyCounts, visibleObjectLODLevels);
        }

        private bool IsObjectVisible(Camera cam, Transform objTransform)
        {
            if (useOcclusionCulling && cam.useOcclusionCulling)
            {
                Renderer renderer = objTransform.GetComponent<Renderer>();
                return renderer != null && renderer.isVisible;
            }
            else
            {
                Vector3 direction = objTransform.position - cam.transform.position;
                float distance = direction.magnitude;
                direction.Normalize();

                RaycastHit[] hits = Physics.RaycastAll(cam.transform.position, direction, distance, occlusionLayerMask);
                foreach (var hit in hits)
                {
                    if (hit.collider.gameObject != objTransform.gameObject)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        private int GetCurrentLODLevel(LODGroup lodGroup, Camera camera)
        {
            LOD[] lods = lodGroup.GetLODs();
            float distance = Vector3.Distance(camera.transform.position, lodGroup.transform.position);
            float screenPercentage = lodGroup.size / distance;

            for (int i = 0; i < lods.Length; i++)
            {
                if (screenPercentage > lods[i].screenRelativeTransitionHeight)
                {
                    return i;
                }
            }
            return lods.Length - 1;
        }

        private bool IsPointInFrustum(Vector3 point, Plane[] frustumPlanes)
        {
            foreach (var plane in frustumPlanes)
            {
                if (plane.GetDistanceToPoint(point) < 0)
                    return false;
            }
            return true;
        }

        private int CalculateTreeLOD(float distance, Terrain terrain)
        {
            if (distance < terrain.treeDistance) return 0; // 3D model
            if (distance < terrain.treeBillboardDistance) return 1; // Billboard
            return -1; // Not rendered
        }

        private void RecordData(float fps, List<Vector3> visibleObjectPositions, List<int> visibleObjectPolyCounts, List<int> visibleObjectLODLevels)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            long currentTime = System.DateTime.Now.Ticks;
            float fieldOfView = Camera.main != null ? Camera.main.fieldOfView : 60f;

            TriggerReason reason = TriggerReason.None;

            if (trackFPS && fpsHistory.Count > 1)
            {
                // Check if moving average has dropped by the threshold amount
                float avgDrop = previousMovingAverage - currentMovingAverage;
                if (avgDrop >= fpsThreshold)
                {
                    reason |= TriggerReason.FPSDrop;
                }
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            long totalAllocatedMemory = 0, totalReservedMemory = 0, totalUnusedReservedMemory = 0, monoHeapSize = 0, monoUsedSize = 0, managedMemoryUsage = 0;
            int drawCalls = 0, visibleObjectsCount = visibleObjectPositions.Count, totalPolygons = visibleObjectPolyCounts.Sum(), totalVertices = 0, totalMaterials = 0;
            int objectsAboveThreshold = visibleObjectPositions.Count;

            if (trackMemory)
            {
                totalAllocatedMemory = Profiler.GetTotalAllocatedMemoryLong();
                totalReservedMemory = Profiler.GetTotalReservedMemoryLong();
                totalUnusedReservedMemory = Profiler.GetTotalUnusedReservedMemoryLong();
                monoHeapSize = Profiler.GetMonoHeapSizeLong();
                monoUsedSize = Profiler.GetMonoUsedSizeLong();
                managedMemoryUsage = System.GC.GetTotalMemory(false);

                drawCalls = UnityStats.drawCalls;
                visibleObjectsCount = CountVisibleObjects(out totalPolygons, out totalVertices, out totalMaterials, out _, FPSHeatmapCore.data.settings.polygonThreshold);
                objectsAboveThreshold = visibleObjectPositions.Count;
            }
#endif

            stopwatch.Stop();
            float recordTimeMs = stopwatch.ElapsedMilliseconds;

            FPSHeatmapCore.data.GetSceneData().transforms.Add(new HeatmapTracker(
                id, currentTime, transform.position, transform.rotation, transform.localScale, fps,
                fieldOfView, reason,
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                totalAllocatedMemory, totalReservedMemory, totalUnusedReservedMemory, monoHeapSize, monoUsedSize, managedMemoryUsage,
                drawCalls, visibleObjectsCount, totalPolygons, totalVertices, totalMaterials,
                recordTimeMs, visibleObjectPositions, visibleObjectPolyCounts, visibleObjectLODLevels, objectsAboveThreshold
#else
                recordTimeMs, visibleObjectPositions, visibleObjectPolyCounts, visibleObjectLODLevels
#endif
            ));

            lastPosition = transform.position;
            lastRotation = transform.rotation;
            lastTimestamp = currentTime;
        }

        private bool ShouldRecordData()
        {
            if (trackFPS && fpsHistory.Count > 1)
            {
                // Check if moving average has dropped by the threshold amount
                float avgDrop = previousMovingAverage - currentMovingAverage;
                if (avgDrop >= fpsThreshold)
                {
                    return true;
                }
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (trackMemory && Mathf.Abs(currentMemoryUsage - lastMemoryUsage) > memorySpikeThreshold)
            {
                lastMemoryUsage = currentMemoryUsage;
                return true;
            }
#endif

            return false;
        }


        private int CountVisibleObjects(out int totalPolygons, out int totalVertices, out int totalMaterials, out List<Vector3> visibleObjectPositions, int polygonThreshold)
        {
            int visibleCount = 0;
            totalPolygons = 0;
            totalVertices = 0;
            totalMaterials = 0;
            visibleObjectPositions = new List<Vector3>();

            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            Camera mainCamera = Camera.main;

            if (mainCamera == null)
            {
                Debug.LogWarning("Main camera not found.");
                return visibleCount;
            }

            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);

            foreach (var renderer in renderers)
            {
                if (GeometryUtility.TestPlanesAABB(frustumPlanes, renderer.bounds))
                {
                    MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                    Mesh mesh = meshFilter ? meshFilter.sharedMesh : null;

                    if (mesh != null && mesh.isReadable)
                    {
                        int polygons = mesh.triangles.Length / 3;
                        int vertices = mesh.vertexCount;
                        totalPolygons += polygons;
                        totalVertices += vertices;
                        totalMaterials += renderer.sharedMaterials.Length;
                        visibleCount++;

                        if (polygons >= polygonThreshold)
                        {
                            visibleObjectPositions.Add(renderer.transform.position);
                        }
                    }
                    else
                    {
                        float size = renderer.bounds.size.magnitude;
                        if (size > 10f)
                        {
                            visibleObjectPositions.Add(renderer.transform.position);
                        }
                    }
                }
            }

            Terrain[] terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            foreach (var terrain in terrains)
            {
                Renderer terrainRenderer = terrain.GetComponent<Renderer>();
                if (terrainRenderer != null && GeometryUtility.TestPlanesAABB(frustumPlanes, terrainRenderer.bounds))
                {
                    visibleObjectPositions.Add(terrain.transform.position);
                }
            }

            return visibleCount;
        }

        private void UpdateFPSMovingAverage()
        {
            float currentTime = Time.time;
            fpsHistory.Add((fps, currentTime));

            // Remove entries older than the window
            fpsHistory.RemoveAll(entry => currentTime - entry.timestamp > MOVING_AVERAGE_WINDOW);

            // Store previous average before calculating new one
            previousMovingAverage = currentMovingAverage;

            // Calculate current moving average
            if (fpsHistory.Count > 0)
            {
                float totalFPS = 0f;
                foreach (var (fpsValue, _) in fpsHistory)
                {
                    totalFPS += fpsValue;
                }
                currentMovingAverage = totalFPS / fpsHistory.Count;
            }
        }

        static public Color StringToColor(string inputString, float brightness = 1.0f)
        {
            int hash = inputString.GetHashCode();

            float r = ((hash >> 24) & 0xFF) / 255f;
            float g = ((hash >> 16) & 0xFF) / 255f;
            float b = ((hash >> 8) & 0xFF) / 255f;

            r = r / Mathf.Max(r, g, b) * 0.6f;
            g = g / Mathf.Max(r, g, b) * 0.6f;
            b = b / Mathf.Max(r, g, b) * 0.6f;

            float maxBrightness = Mathf.Max(r, Mathf.Max(g, b));
            if (maxBrightness > brightness)
            {
                float brightnessScale = brightness / maxBrightness;
                r *= brightnessScale;
                g *= brightnessScale;
                b *= brightnessScale;
            }

            return new Color(r, g, b, 1);
        }
    }
}
#endif