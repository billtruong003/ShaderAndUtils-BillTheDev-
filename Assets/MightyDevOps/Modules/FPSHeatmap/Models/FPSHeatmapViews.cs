#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Mighty;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static Mighty.MightyCore;
using static Mighty.MightyCoreData;
using static Mighty.MightyCoreData.SceneData;
using static MightyFPSHeatmap.FPSHeatmapData;
using static MightyFPSHeatmap.FPSHeatmapData.HeatmapTracking;

namespace MightyFPSHeatmap
{
    [InitializeOnLoad]
    [ExecuteInEditMode]
    public class FPSHeatmapViews : MonoBehaviour
    {
        private static FPSHeatmapViews _instance;
        public static FPSHeatmapViews Instance
        {
            get
            {
                if (_instance == null)
                {
                    DevLog("FPSHeatmapViews: Creating instance");
                    GameObject anchor = GameObject.Find("MightySceneAnchor");
                    if (anchor == null)
                    {
                        DevLog("FPSHeatmapViews: Creating MightySceneAnchor GameObject");
                        anchor = new GameObject("MightySceneAnchor");
                        anchor.tag = "EditorOnly";
                        EditorUtility.SetDirty(anchor);
                    }

                    _instance = anchor.GetComponent<FPSHeatmapViews>();
                    if (_instance == null)
                    {
                        DevLog("FPSHeatmapViews: Adding FPSHeatmapViews component");
                        _instance = anchor.AddComponent<FPSHeatmapViews>();
                    }

                    Init();
                }
                return _instance;
            }
        }

        static public VisualElement root;
        static private SceneView sceneView;

        static public Dictionary<string, List<HeatmapTracking.HeatmapTracker>> heatmapTracker =
            new Dictionary<string, List<HeatmapTracking.HeatmapTracker>>();

        private static Dictionary<int, int> treePolycounts = new Dictionary<int, int>();

        private static readonly List<float> _speedPool = new List<float>();
        private static readonly List<Vector3> _positionPool = new List<Vector3>();

        // Object pools to reduce GC pressure
        private static readonly Stack<List<HeatmapTracking.HeatmapTracker>> _trackerListPool = new Stack<List<HeatmapTracking.HeatmapTracker>>();
        private static readonly Stack<List<RawPointData>> _rawPointDataListPool = new Stack<List<RawPointData>>();
        private static readonly Stack<Dictionary<Vector3, List<RawPointData>>> _positionDataDictPool = new Stack<Dictionary<Vector3, List<RawPointData>>>();
        private static readonly Stack<List<(Vector3 position, float intensity)>> _heatmapDataListPool = new Stack<List<(Vector3 position, float intensity)>>();

        // Cache for frequently calculated values
        private static readonly Dictionary<int, Plane[]> _frustumPlanesCache = new Dictionary<int, Plane[]>();
        private static Vector3 _lastCameraPosition;
        private static Quaternion _lastCameraRotation;
        private static float _lastCameraFOV;
        private static int _cameraStateHash;

        // GPU Compute optimization
        private static ComputeShader _fpsComputeShader;
        private static ComputeBuffer _trackerBuffer;
        private static ComputeBuffer _visibleObjectBuffer;
        private static ComputeBuffer _rawPointBuffer;
        private static ComputeBuffer _consolidatedBuffer;
        private static ComputeBuffer _counterBuffer;
        private static bool _useGPUCompute = true; // Can be toggled based on performance needs

        [System.Serializable]
        public struct TrackerData
        {
            public Vector3 position;
            public Vector3 rotation;
            public float fps;
            public int visibleObjectCount;
            public int visibleObjectStartIndex;
        }

        [System.Serializable]
        public struct VisibleObjectData
        {
            public Vector3 position;
            public int polyCount;
        }

        [System.Serializable]
        public struct ComputeConsolidatedPoint
        {
            public Vector3 position;
            public float finalFps;
            public int polyCount;
        }

        [Serializable]
        public class Settings
        {
            [SerializeField]
            public Color borderColor = new Color(0, 0, 0, 0);
            [SerializeField]
            public float distanceStart = 5, distanceEnd = 100;
            [SerializeField]
            public bool show = true;
            [SerializeField]
            public bool showTrackingTrails = true;
            [SerializeField]
            public bool showFPSGizmos = true;
        }
        private static Dictionary<string, Settings> settings = new Dictionary<string, Settings>();

        public static Dictionary<Transform, WeakReference<FPSHeatmapData.MeshData>> meshDataCache
            = new Dictionary<Transform, WeakReference<FPSHeatmapData.MeshData>>();


        private FPSHeatmapViews() { }

        static FPSHeatmapViews()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void Init()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;

            UpdateSceneView -= UpdateWorldSpaceElements;
            UpdateSceneView += UpdateWorldSpaceElements;

            RunPlaybackMinMaxUpdated -= UpdateSelectedHeatmapTrackers;
            RunPlaybackMinMaxUpdated += UpdateSelectedHeatmapTrackers;

            sceneView = SceneView.lastActiveSceneView;
            root = new VisualElement();
            sceneView.rootVisualElement.Add(root);
            var mightyStylesheet = Resources.Load<StyleSheet>("UI/mightystyles");

            if (!root.styleSheets.Contains(mightyStylesheet))
            {
                root.styleSheets.Add(mightyStylesheet);
            }

            CacheTreePolycounts();
        }

        private static void CacheTreePolycounts()
        {
            treePolycounts.Clear();
            Terrain[] terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            foreach (var terrain in terrains)
            {
                if (terrain.terrainData == null) continue;
                TreePrototype[] treePrototypes = terrain.terrainData.treePrototypes;
                for (int i = 0; i < treePrototypes.Length; i++)
                {
                    GameObject prefab = treePrototypes[i].prefab;
                    if (prefab != null)
                    {
                        MeshFilter meshFilter = prefab.GetComponentInChildren<MeshFilter>();
                        if (meshFilter != null && meshFilter.sharedMesh != null)
                        {
                            int polyCount = meshFilter.sharedMesh.triangles.Length / 3;
                            treePolycounts[i] = polyCount;
                        }
                        else
                        {
                            treePolycounts[i] = 0;
                        }
                    }
                }
            }
            DevLog($"Cached polycounts for {treePolycounts.Count} tree prototypes.");
        }

        private static void OnSceneGUI(SceneView sceneView) { }

        public void OnDrawGizmos()
        {
            if (heatmapTracker == null || FPSHeatmapCore.trackingType == null || !FPSHeatmapCore.trackingType.IsActive) return;
            if (isPlaying) return;

            Vector3 camPos = SceneView.lastActiveSceneView.camera.transform.position;

            if (FPSHeatmapCore.data.settings.showHeatmap && FPSHeatmapCore.data.settings.showFPSGizmos)
                DrawFPSGizmos(camPos);
        }

        private void DrawFPSGizmos(Vector3 camPos)
        {
            consolidatedPoints = BuildConsolidatedFpsPoints();
            if (consolidatedPoints.Count == 0) return;

            float baseFontSize = 20f;
            float minFontSize = 8f;
            float maxDistance = FPSHeatmapCore.data.settings.SvFadeDistance;
            float targetFPS = FPSHeatmapCore.data.settings.targetFPS;

            // Performance limits to prevent gizmo overload
            const int maxGizmos = 2000; // Unity can handle this comfortably
            const float minOpacityThreshold = 0.05f; // Don't draw nearly invisible gizmos
            const float maxCullDistance = 200f; // Hard distance limit regardless of fade settings

            // Step 1: Filter and prioritize gizmos
            var candidateGizmos = new List<GizmoCandidate>();

            for (int i = 0; i < consolidatedPoints.Count; i++)
            {
                var dataPoint = consolidatedPoints[i];
                Vector3 pos = dataPoint.pos;
                float finalFps = dataPoint.finalFps;
                int polyCount = dataPoint.polyCount;

                float distance = Vector3.Distance(camPos, pos);

                // Hard distance culling - don't even consider distant points
                if (distance > Mathf.Min(maxDistance * 1.5f, maxCullDistance))
                    continue;

                float baseOpacity = FPSHeatmapCore.data.settings.SvOpacityCurve.Evaluate(distance / maxDistance);
                float cappedOpacity = Mathf.Clamp(baseOpacity, 0f, 0.5f);

                // Skip nearly invisible gizmos
                if (cappedOpacity < minOpacityThreshold)
                    continue;

                // Calculate priority score for this gizmo
                float priority = CalculateGizmoPriority(finalFps, polyCount, distance, targetFPS, cappedOpacity);

                candidateGizmos.Add(new GizmoCandidate
                {
                    position = pos,
                    fps = finalFps,
                    polyCount = polyCount,
                    distance = distance,
                    opacity = cappedOpacity,
                    priority = priority
                });
            }

            // Step 2: Sort by priority and take the most important ones
            candidateGizmos.Sort((a, b) => b.priority.CompareTo(a.priority)); // Highest priority first
            int gizmosToRender = Mathf.Min(candidateGizmos.Count, maxGizmos);

            // DevLog($"Rendering {gizmosToRender} gizmos out of {consolidatedPoints.Count} total points (filtered from {candidateGizmos.Count} candidates)");

            // Step 3: Render the selected gizmos with LOD
            for (int i = 0; i < gizmosToRender; i++)
            {
                var gizmo = candidateGizmos[i];

                Color color = GetColorForFPS(gizmo.fps);
                color.a = gizmo.opacity;

                float size = Mathf.Max(0.3f, 0.2f + (gizmo.polyCount / 20000f)); // Slightly smaller base size
                Gizmos.color = color;

                // LOD: Use simpler rendering for distant gizmos
                if (gizmo.distance < maxDistance * 0.1f)
                {
                    // Close: Full detail sphere + text
                    Gizmos.DrawSphere(gizmo.position, size);

                    float scaleFactor = 1f - (gizmo.distance / maxDistance);
                    int scaledFontSize = Mathf.Clamp((int)((baseFontSize - 2) * scaleFactor), (int)minFontSize, (int)(baseFontSize - 2)); // Slightly smaller base font

                    // Combined FPS and polygon label
                    GUIStyle combinedLabelStyle = new GUIStyle();
                    combinedLabelStyle.normal.textColor = new Color(1f, 1f, 1f, gizmo.opacity * 0.85f);
                    combinedLabelStyle.alignment = TextAnchor.MiddleCenter;
                    combinedLabelStyle.fontSize = scaledFontSize;

                    // Create combined text with carriage return
                    string polyText = gizmo.polyCount >= 1000 ? $"{gizmo.polyCount / 1000f:F1}k p" : $"{gizmo.polyCount} p";
                    string combinedText = $"{gizmo.fps:F1} fps\n{polyText}";

                    Handles.Label(gizmo.position, combinedText, combinedLabelStyle);
                }
                else if (gizmo.distance < maxDistance * 0.3f)
                {
                    // Medium: Sphere only, no text
                    Gizmos.DrawSphere(gizmo.position, size * 0.8f);
                }
                else
                {
                    // Use Handles for 2D circle instead of Gizmos cube
                    color.a = gizmo.opacity * 0.5f;
                    Handles.color = color;
                    Vector3 normal = (SceneView.lastActiveSceneView?.camera?.transform.forward ?? Vector3.forward).normalized;
                    Handles.DrawSolidDisc(gizmo.position, normal, size * 0.5f);
                }
            }
        }

        private struct GizmoCandidate
        {
            public Vector3 position;
            public float fps;
            public int polyCount;
            public float distance;
            public float opacity;
            public float priority;
        }

        private float CalculateGizmoPriority(float fps, int polyCount, float distance, float targetFPS, float opacity)
        {
            float priority = 0f;

            // 1. Distance priority (closer = more important)
            float distancePriority = 1f / (1f + distance * 0.01f); // Inverse distance
            priority += distancePriority * 2f;

            // 2. Performance deviation priority (extreme values = more important)
            float fpsDeviation = Mathf.Abs(fps - targetFPS) / targetFPS;
            priority += fpsDeviation * 3f;

            // 3. Low FPS priority (performance problems = most important)
            if (fps < targetFPS * 0.8f) // 20% below target
            {
                float lowFpsBonus = (targetFPS - fps) / targetFPS;
                priority += lowFpsBonus * 4f; // Heavy weight for performance issues
            }

            // 4. High polycount priority (bottlenecks = important)
            if (polyCount > 5000)
            {
                float polyBonus = Mathf.Min(polyCount / 50000f, 1f);
                priority += polyBonus * 2f;
            }

            // 5. Opacity priority (more visible = more important)
            priority += opacity * 1f;

            return priority;
        }

        // CPU fallback method (simplified version of the original)
        private static List<ConsolidatedPoint> BuildConsolidatedFpsPoints()
        {
            if (!MightyCoreData.IsDirty())
            {
                return consolidatedPoints;
            }

            if (heatmapTracker == null || !FPSHeatmapCore.trackingType.IsActive || sceneData == null)
            {
                consolidatedPoints = new List<ConsolidatedPoint>();
                return consolidatedPoints;
            }

            var filteredTrackers = GetPooledTrackerList();
            try
            {
                // Use more efficient filtering with early exit conditions
                var minFPS = FPSHeatmapCore.data.settings.fpsMin;
                var maxFPS = FPSHeatmapCore.data.settings.fpsMax;
                var minTime = sceneData.RunPlaybackSelectedMin;
                var maxTime = sceneData.RunPlaybackSelectedMax;

                var allTrackers = GetFilteredHeatmapTrackers(minTime, maxTime);
                for (int i = 0; i < allTrackers.Count; i++)
                {
                    var tracker = allTrackers[i];
                    if (tracker.fps >= minFPS && tracker.fps <= maxFPS)
                        filteredTrackers.Add(tracker);
                }

                var fpsDataByPosition = GetPooledPositionDataDict();
                try
                {
                    Camera sceneCamera = null;
                    Plane[] frustumPlanes = null;
                    bool shouldCullFrustum = FPSHeatmapCore.data.settings.useDirectionalWeighting;

                    if (shouldCullFrustum && SceneView.lastActiveSceneView != null)
                    {
                        sceneCamera = SceneView.lastActiveSceneView.camera;
                        if (sceneCamera != null)
                        {
                            frustumPlanes = GetCachedFrustumPlanes(sceneCamera);
                        }
                    }

                    // Pre-calculate filter values
                    var polyMin = FPSHeatmapCore.data.settings.polyMin;
                    var polyMax = FPSHeatmapCore.data.settings.polyMax;

                    for (int i = 0; i < filteredTrackers.Count; i++)
                    {
                        var tracker = filteredTrackers[i];
                        var subPositions = GetPositionsForTracker(tracker);

                        for (int j = 0; j < subPositions.Count; j++)
                        {
                            var dataItem = subPositions[j];

                            // Early polycount filtering
                            if (dataItem.polyCount < polyMin || dataItem.polyCount > polyMax)
                                continue;

                            // Frustum culling with cached planes
                            if (shouldCullFrustum && frustumPlanes != null)
                            {
                                Bounds pointBounds = new Bounds(dataItem.pos, Vector3.one * 0.1f);
                                if (!GeometryUtility.TestPlanesAABB(frustumPlanes, pointBounds))
                                    continue;
                            }

                            if (!fpsDataByPosition.TryGetValue(dataItem.pos, out var list))
                            {
                                list = GetPooledRawPointDataList();
                                fpsDataByPosition[dataItem.pos] = list;
                            }
                            list.Add(dataItem);
                        }

                        // Return pooled list from GetPositionsForTracker
                        ReturnPooledRawPointDataList(subPositions);
                    }

                    bool directionalWeight = FPSHeatmapCore.data.settings.useDirectionalWeighting;
                    float cameraYaw = 0f;
                    float fov = FPSHeatmapCore.data.settings.fustrumFOV;
                    float maxAngleDiff = FPSHeatmapCore.data.settings.maxDeltaAngle;

                    if (directionalWeight && SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
                    {
                        Vector3 forwardXZ = SceneView.lastActiveSceneView.camera.transform.forward;
                        forwardXZ.y = 0f;
                        if (forwardXZ.sqrMagnitude > 0.001f)
                        {
                            cameraYaw = Mathf.Atan2(forwardXZ.x, forwardXZ.z) * Mathf.Rad2Deg;
                        }
                    }

                    float referenceAngle = directionalWeight ? cameraYaw : 0f;

                    consolidatedPoints = new List<ConsolidatedPoint>(fpsDataByPosition.Count);

                    // Use more efficient iteration
                    if (!directionalWeight)
                    {
                        foreach (var kvp in fpsDataByPosition)
                        {
                            Vector3 position = kvp.Key;
                            List<RawPointData> samples = kvp.Value;

                            float sumFps = 0f;
                            int sumPoly = 0;
                            int count = samples.Count;

                            for (int i = 0; i < count; i++)
                            {
                                sumFps += samples[i].fps;
                                sumPoly += samples[i].polyCount;
                            }

                            if (count > 0)
                            {
                                float finalFps = sumFps / count;
                                int avgPoly = sumPoly / count;
                                consolidatedPoints.Add(new ConsolidatedPoint(position, finalFps, avgPoly));
                            }
                        }
                    }
                    else
                    {
                        float halfFOV = fov * 0.5f;

                        foreach (var kvp in fpsDataByPosition)
                        {
                            Vector3 position = kvp.Key;
                            List<RawPointData> samples = kvp.Value;

                            float sumWeightedFps = 0f;
                            float totalContributing = 0f;
                            int sumPoly = 0;

                            for (int i = 0; i < samples.Count; i++)
                            {
                                var s = samples[i];
                                float delta = Mathf.Abs(Mathf.DeltaAngle(referenceAngle, s.trackerYaw));
                                if (delta > halfFOV)
                                    continue;

                                float angleWeight = Mathf.Clamp01(1f - (delta / maxAngleDiff));
                                if (angleWeight > 0f)
                                {
                                    sumWeightedFps += s.fps * angleWeight;
                                    totalContributing += 1f;
                                    sumPoly += s.polyCount;
                                }
                            }

                            if (totalContributing > 0f)
                            {
                                float finalFps = sumWeightedFps / totalContributing;
                                int avgPoly = Mathf.RoundToInt((float)sumPoly / totalContributing);
                                consolidatedPoints.Add(new ConsolidatedPoint(position, finalFps, avgPoly));
                            }
                        }
                    }
                }
                finally
                {
                    ReturnPooledPositionDataDict(fpsDataByPosition);
                }
            }
            finally
            {
                ReturnPooledTrackerList(filteredTrackers);
            }

            return consolidatedPoints;
        }

        private static List<ConsolidatedPoint> BuildConsolidatedFpsPointsGPU()
        {
            DevLog("BuildConsolidatedFpsPointsGPU");
            if (!_useGPUCompute || SystemInfo.supportsComputeShaders == false)
            {
                return BuildConsolidatedFpsPoints(); // Fallback to CPU
            }

            if (_fpsComputeShader == null)
            {
                _fpsComputeShader = Resources.Load<ComputeShader>("FPSPositionComputeShader");
                if (_fpsComputeShader == null)
                {
                    DevLog("FPS Compute Shader not found, falling back to CPU");
                    _useGPUCompute = false;
                    return BuildConsolidatedFpsPoints();
                }

                // Validate kernels exist
                if (!_fpsComputeShader.HasKernel("ProcessTrackerPositions") ||
                    !_fpsComputeShader.HasKernel("FilterByFrustum") ||
                    !_fpsComputeShader.HasKernel("ConsolidatePositions"))
                {
                    DevLog("FPS Compute Shader missing required kernels, falling back to CPU");
                    _useGPUCompute = false;
                    return BuildConsolidatedFpsPoints();
                }

                DevLog("FPS Compute Shader loaded successfully");
            }

            var filteredTrackers = GetFilteredHeatmapTrackers(
                sceneData.RunPlaybackSelectedMin,
                sceneData.RunPlaybackSelectedMax
            )
            .Where(t => t.fps >= FPSHeatmapCore.data.settings.fpsMin &&
                        t.fps <= FPSHeatmapCore.data.settings.fpsMax)
            .ToList();

            if (filteredTrackers.Count == 0)
            {
                return new List<ConsolidatedPoint>();
            }

            try
            {
                // Prepare tracker data for GPU
                var trackerDataList = new List<TrackerData>();
                var visibleObjectList = new List<VisibleObjectData>();
                int currentVisibleObjectIndex = 0;

                foreach (var tracker in filteredTrackers)
                {
                    var trackerData = new TrackerData
                    {
                        position = tracker.position,
                        rotation = tracker.rotation.eulerAngles,
                        fps = tracker.fps,
                        visibleObjectStartIndex = currentVisibleObjectIndex
                    };

                    if (tracker.visibleObjectPositions != null && tracker.visibleObjectPolyCounts != null)
                    {
                        int count = Mathf.Min(tracker.visibleObjectPositions.Count, tracker.visibleObjectPolyCounts.Count);
                        trackerData.visibleObjectCount = count;

                        for (int i = 0; i < count; i++)
                        {
                            visibleObjectList.Add(new VisibleObjectData
                            {
                                position = tracker.visibleObjectPositions[i],
                                polyCount = tracker.visibleObjectPolyCounts[i]
                            });
                        }
                        currentVisibleObjectIndex += count;
                    }
                    else
                    {
                        trackerData.visibleObjectCount = 0;
                    }

                    trackerDataList.Add(trackerData);
                }

                // Check if dataset is too large for GPU processing
                int estimatedTotalPoints = trackerDataList.Count;
                if (FPSHeatmapCore.data.settings.fpsHeatmapMode == FPSHeatmapData.FPSHeatmapMode.VisibleObjects)
                {
                    estimatedTotalPoints = visibleObjectList.Count;
                }

                // If dataset is extremely large, fall back to CPU to avoid memory issues
                if (estimatedTotalPoints > 2000000) // 2M points limit
                {
                    DevLogWarning($"Dataset too large for GPU processing ({estimatedTotalPoints} estimated points). Falling back to optimized CPU processing.");
                    return BuildConsolidatedFpsPoints();
                }

                // Setup compute buffers - calculate more accurate buffer size
                int estimatedPointsPerTracker = 1;
                int totalVisibleObjects = visibleObjectList.Count;

                // Calculate better estimate based on heatmap mode
                switch (FPSHeatmapCore.data.settings.fpsHeatmapMode)
                {
                    case FPSHeatmapData.FPSHeatmapMode.Origin:
                    case FPSHeatmapData.FPSHeatmapMode.Projection:
                        estimatedPointsPerTracker = 1; // Each tracker = 1 point
                        break;
                    case FPSHeatmapData.FPSHeatmapMode.VisibleObjects:
                        estimatedPointsPerTracker = totalVisibleObjects > 0 ?
                            Mathf.Max(1, totalVisibleObjects / trackerDataList.Count) : 100; // Average objects per tracker
                        break;
                }

                // More conservative estimate with safety buffer
                int maxPoints = Mathf.Max(10000, trackerDataList.Count * estimatedPointsPerTracker * 2); // 2x safety factor
                int maxConsolidatedPoints = Mathf.Min(maxPoints, 1000000); // Cap at 1M points to prevent excessive memory usage

                DevLog($"Buffer sizing: {trackerDataList.Count} trackers, {totalVisibleObjects} visible objects, estimated {estimatedPointsPerTracker} points/tracker, max buffer: {maxPoints} points");

                SetupComputeBuffers(trackerDataList.Count, visibleObjectList.Count, maxPoints, maxConsolidatedPoints);

                // Upload data to GPU
                _trackerBuffer.SetData(trackerDataList.ToArray());
                if (visibleObjectList.Count > 0)
                    _visibleObjectBuffer.SetData(visibleObjectList.ToArray());

                // Reset counters
                _counterBuffer.SetData(new int[] { 0, 0 });

                // Set compute shader parameters
                int kernelProcessPositions = _fpsComputeShader.FindKernel("ProcessTrackerPositions");
                int kernelFilterFrustum = _fpsComputeShader.FindKernel("FilterByFrustum");
                int kernelConsolidate = _fpsComputeShader.FindKernel("ConsolidatePositions");

                _fpsComputeShader.SetBuffer(kernelProcessPositions, "TrackerBuffer", _trackerBuffer);
                _fpsComputeShader.SetBuffer(kernelProcessPositions, "VisibleObjectBuffer", _visibleObjectBuffer);
                _fpsComputeShader.SetBuffer(kernelProcessPositions, "RawPointBuffer", _rawPointBuffer);
                _fpsComputeShader.SetBuffer(kernelProcessPositions, "CounterBuffer", _counterBuffer);

                // Set parameters for all kernels
                _fpsComputeShader.SetInt("heatmapMode", (int)FPSHeatmapCore.data.settings.fpsHeatmapMode);
                _fpsComputeShader.SetFloat("maxProjectionDistance", FPSHeatmapCore.data.settings.maxProjectionDistance);
                _fpsComputeShader.SetInt("polyMin", FPSHeatmapCore.data.settings.polyMin);
                _fpsComputeShader.SetInt("polyMax", FPSHeatmapCore.data.settings.polyMax);
                _fpsComputeShader.SetFloat("fpsMin", FPSHeatmapCore.data.settings.fpsMin);
                _fpsComputeShader.SetFloat("fpsMax", FPSHeatmapCore.data.settings.fpsMax);
                _fpsComputeShader.SetInt("trackerCount", trackerDataList.Count);

                // Frustum culling setup
                bool shouldCullFrustum = FPSHeatmapCore.data.settings.useDirectionalWeighting;
                _fpsComputeShader.SetBool("useFrustumCulling", shouldCullFrustum);

                if (shouldCullFrustum && SceneView.lastActiveSceneView?.camera != null)
                {
                    var camera = SceneView.lastActiveSceneView.camera;
                    var frustumPlanes = GetCachedFrustumPlanes(camera);
                    if (frustumPlanes != null)
                    {
                        var planeData = new Vector4[6];
                        for (int i = 0; i < 6; i++)
                        {
                            var plane = frustumPlanes[i];
                            planeData[i] = new Vector4(plane.normal.x, plane.normal.y, plane.normal.z, plane.distance);
                        }
                        _fpsComputeShader.SetVectorArray("frustumPlanes", planeData);
                    }
                }

                // Directional weighting setup
                bool directionalWeight = FPSHeatmapCore.data.settings.useDirectionalWeighting;
                _fpsComputeShader.SetBool("useDirectionalWeighting", directionalWeight);

                if (directionalWeight && SceneView.lastActiveSceneView?.camera != null)
                {
                    Vector3 forwardXZ = SceneView.lastActiveSceneView.camera.transform.forward;
                    forwardXZ.y = 0f;
                    float cameraYaw = 0f;
                    if (forwardXZ.sqrMagnitude > 0.001f)
                    {
                        cameraYaw = Mathf.Atan2(forwardXZ.x, forwardXZ.z) * Mathf.Rad2Deg;
                    }

                    _fpsComputeShader.SetFloat("cameraYaw", cameraYaw);
                    _fpsComputeShader.SetFloat("fov", FPSHeatmapCore.data.settings.fustrumFOV);
                    _fpsComputeShader.SetFloat("maxAngleDiff", FPSHeatmapCore.data.settings.maxDeltaAngle);
                }

                // Dispatch compute shaders
                int threadGroups = Mathf.CeilToInt(trackerDataList.Count / 64.0f);
                int[] countData = new int[2];

                if (threadGroups > 0)
                {
                    _fpsComputeShader.Dispatch(kernelProcessPositions, threadGroups, 1, 1);

                    // Setup frustum filtering
                    _fpsComputeShader.SetBuffer(kernelFilterFrustum, "RawPointBuffer", _rawPointBuffer);
                    _fpsComputeShader.SetBuffer(kernelFilterFrustum, "CounterBuffer", _counterBuffer);

                    // Get intermediate count for frustum filtering
                    _counterBuffer.GetData(countData);
                    int rawPointCount = countData[0];

                    if (rawPointCount > 0)
                    {
                        threadGroups = Mathf.CeilToInt(rawPointCount / 64.0f);
                        if (threadGroups > 0)
                        {
                            _fpsComputeShader.Dispatch(kernelFilterFrustum, threadGroups, 1, 1);

                            // Setup consolidation
                            _fpsComputeShader.SetBuffer(kernelConsolidate, "RawPointBuffer", _rawPointBuffer);
                            _fpsComputeShader.SetBuffer(kernelConsolidate, "ConsolidatedBuffer", _consolidatedBuffer);
                            _fpsComputeShader.SetBuffer(kernelConsolidate, "CounterBuffer", _counterBuffer);

                            _fpsComputeShader.Dispatch(kernelConsolidate, threadGroups, 1, 1);
                        }
                    }
                }

                // Read back results
                _counterBuffer.GetData(countData);
                int finalCount = countData[1];

                DevLog($"GPU processing complete: Raw points: {countData[0]}, Final consolidated: {finalCount}");

                var results = new List<ConsolidatedPoint>();
                if (finalCount > 0)
                {
                    // Safety check: ensure we don't read beyond buffer bounds
                    int actualBufferSize = _consolidatedBuffer.count;
                    int safeReadCount = Mathf.Min(finalCount, actualBufferSize);

                    if (finalCount > actualBufferSize)
                    {
                        DevLogWarning($"GPU generated more points ({finalCount}) than buffer can hold ({actualBufferSize}). Reading first {safeReadCount} points.");
                    }

                    var computeResults = new ComputeConsolidatedPoint[safeReadCount];
                    _consolidatedBuffer.GetData(computeResults, 0, 0, safeReadCount);

                    for (int i = 0; i < safeReadCount; i++)
                    {
                        var cr = computeResults[i];
                        results.Add(new ConsolidatedPoint(cr.position, cr.finalFps, cr.polyCount));
                    }

                    if (finalCount > actualBufferSize)
                    {
                        DevLogWarning($"Note: {finalCount - safeReadCount} points were truncated due to buffer size limits. Consider using CPU processing for this dataset size or increasing buffer limits.");
                    }
                }

                DevLog($"GPU processed {trackerDataList.Count} trackers -> {finalCount} consolidated points");
                return results;
            }
            catch (System.Exception e)
            {
                DevLog($"GPU compute failed, falling back to CPU: {e.Message}");
                _useGPUCompute = false;
                return BuildConsolidatedFpsPoints();
            }
        }

        private static void SetupComputeBuffers(int trackerCount, int visibleObjectCount, int maxPoints, int maxConsolidatedPoints)
        {
            // Clean up existing buffers
            CleanupComputeBuffers();

            // Create new buffers
            _trackerBuffer = new ComputeBuffer(Mathf.Max(1, trackerCount), System.Runtime.InteropServices.Marshal.SizeOf<TrackerData>());
            _visibleObjectBuffer = new ComputeBuffer(Mathf.Max(1, visibleObjectCount), System.Runtime.InteropServices.Marshal.SizeOf<VisibleObjectData>());
            _rawPointBuffer = new ComputeBuffer(maxPoints, System.Runtime.InteropServices.Marshal.SizeOf<RawPointData>());
            _consolidatedBuffer = new ComputeBuffer(maxConsolidatedPoints, System.Runtime.InteropServices.Marshal.SizeOf<ComputeConsolidatedPoint>());
            _counterBuffer = new ComputeBuffer(2, sizeof(int));
        }

        private static void CleanupComputeBuffers()
        {
            _trackerBuffer?.Release();
            _visibleObjectBuffer?.Release();
            _rawPointBuffer?.Release();
            _consolidatedBuffer?.Release();
            _counterBuffer?.Release();

            _trackerBuffer = null;
            _visibleObjectBuffer = null;
            _rawPointBuffer = null;
            _consolidatedBuffer = null;
            _counterBuffer = null;
        }

        // Update the original method to use GPU when available
        private static List<ConsolidatedPoint> BuildConsolidatedFpsPointsOptimized()
        {
            if (!MightyCoreData.IsDirty())
            {
                return consolidatedPoints;
            }

            if (heatmapTracker == null || !FPSHeatmapCore.trackingType.IsActive || sceneData == null)
            {
                consolidatedPoints = new List<ConsolidatedPoint>();
                return consolidatedPoints;
            }

            // Try GPU first for large datasets
            var filteredCount = GetFilteredHeatmapTrackers(
                sceneData.RunPlaybackSelectedMin,
                sceneData.RunPlaybackSelectedMax
            ).Count;

            if (_useGPUCompute && SystemInfo.supportsComputeShaders) // Always use GPU when available
            {
                consolidatedPoints = BuildConsolidatedFpsPointsGPU();
            }
            else
            {
                consolidatedPoints = BuildConsolidatedFpsPoints(); // Use optimized CPU version
            }

            return consolidatedPoints;
        }

        private struct RawPointData
        {
            public Vector3 pos;
            public float fps;
            public float trackerYaw;
            public int polyCount;

            public RawPointData(Vector3 p, float f, float yaw, int poly)
            {
                pos = p;
                fps = f;
                trackerYaw = yaw;
                polyCount = poly;
            }
        }

        private static List<RawPointData> GetPositionsForTracker(HeatmapTracker tracker)
        {
            List<RawPointData> results = GetPooledRawPointDataList();

            switch (FPSHeatmapCore.data.settings.fpsHeatmapMode)
            {
                case FPSHeatmapData.FPSHeatmapMode.Origin:
                    {
                        // For Origin mode, we'll use a default polycount that passes the filter
                        int defaultPolyCount = Mathf.Max(FPSHeatmapCore.data.settings.polyMin, 1000);
                        results.Add(new RawPointData(
                            tracker.position,
                            tracker.fps,
                            tracker.rotation.eulerAngles.y,
                            defaultPolyCount
                        ));
                        break;
                    }
                case FPSHeatmapData.FPSHeatmapMode.Projection:
                    {
                        Vector3 forwardVector = tracker.rotation * Vector3.forward;
                        Vector3 projectedPos = tracker.position +
                            forwardVector * FPSHeatmapCore.data.settings.maxProjectionDistance;

                        // Reduce the distance decay effect
                        // float distanceDecay = Mathf.Exp(
                        //     -FPSHeatmapCore.data.settings.distanceDecayModifier *
                        //      FPSHeatmapCore.data.settings.maxProjectionDistance * 0.1f
                        // );
                        float intensity = tracker.fps;// * distanceDecay;

                        // For Projection mode, we'll use a default polycount that passes the filter
                        int defaultPolyCount = Mathf.Max(FPSHeatmapCore.data.settings.polyMin, 1000);
                        results.Add(new RawPointData(
                            projectedPos,
                            intensity,
                            tracker.rotation.eulerAngles.y,
                            defaultPolyCount
                        ));
                        break;
                    }
                case FPSHeatmapData.FPSHeatmapMode.VisibleObjects:
                default:
                    {
                        if (tracker.visibleObjectPositions != null && tracker.visibleObjectPolyCounts != null)
                        {
                            int count = Math.Min(tracker.visibleObjectPositions.Count, tracker.visibleObjectPolyCounts.Count);
                            float trackerYaw = tracker.rotation.eulerAngles.y;
                            float trackerFps = tracker.fps;

                            // Pre-allocate capacity if we know the size
                            if (results.Capacity < count)
                                results.Capacity = count;

                            for (int i = 0; i < count; i++)
                            {
                                Vector3 objPos = tracker.visibleObjectPositions[i];
                                int polyCount = tracker.visibleObjectPolyCounts[i];

                                results.Add(new RawPointData(objPos, trackerFps, trackerYaw, polyCount));
                            }
                        }
                        break;
                    }
            }

            return results;
        }



        private Color GetColorForFPS(float fps)
        {
            float target = FPSHeatmapCore.data.settings.targetFPS;
            return (fps >= target)
                ? FPSHeatmapCore.data.settings.highFPSColor
                : FPSHeatmapCore.data.settings.lowFPSColor;
        }

        public static void UpdateWorldSpaceElements() { }

        static public void Rebuild()
        {
            Init();
            SceneView.duringSceneGui -= OnSceneGUI;
            UpdateSceneView -= UpdateWorldSpaceElements;

            root.Clear();
            settings.Clear();

            SceneView.duringSceneGui += OnSceneGUI;
            UpdateSceneView += UpdateWorldSpaceElements;

            sceneView = SceneView.lastActiveSceneView;
            sceneView.rootVisualElement.Add(root);
            var mightyStylesheet = Resources.Load<StyleSheet>("UI/mightystyles");
            if (mightyStylesheet != null && !root.styleSheets.Contains(mightyStylesheet))
            {
                root.styleSheets.Add(mightyStylesheet);
            }
        }

        static public List<HeatmapTracking.HeatmapTracker> GetFilteredHeatmapTrackers(long minTimestamp, long maxTimestamp)
        {
            var sceneData = FPSHeatmapCore.data.GetSceneData();
            if (sceneData == null || sceneData.transforms == null)
            {
                return new List<HeatmapTracking.HeatmapTracker>();
            }

            return sceneData.transforms
                .Where(t => t.timeStamp >= minTimestamp && t.timeStamp <= maxTimestamp)
                .ToList();
        }

        static public Dictionary<string, List<HeatmapTracking.HeatmapTracker>> GetOrganizedTrackers(long minTimestamp, long maxTimestamp)
        {
            return GetFilteredHeatmapTrackers(minTimestamp, maxTimestamp)
                .GroupBy(t => t.name)
                .ToDictionary(g => g.Key, g => g.ToList());
        }


        public static float CalculateMedianFPS()
        {
            DevLog("Starting CalculateMedianFPS calculation...");

            if (!FPSHeatmapCore.trackingType.IsActive || sceneData == null)
            {
                DevLog($"CalculateMedianFPS: Early return - Tracking not active or no scene data. Active: {FPSHeatmapCore.trackingType?.IsActive}, SceneData: {sceneData != null}");
                return 0f;
            }

            DevLog($"CalculateMedianFPS: Time range - Min: {sceneData.RunPlaybackSelectedMin}, Max: {sceneData.RunPlaybackSelectedMax}");

            var filteredTrackers = GetFilteredHeatmapTrackers(sceneData.RunPlaybackSelectedMin, sceneData.RunPlaybackSelectedMax)
                .Where(t => t.fps >= FPSHeatmapCore.data.settings.fpsMin && t.fps <= FPSHeatmapCore.data.settings.fpsMax)
                .ToList();

            DevLog($"CalculateMedianFPS: Found {filteredTrackers.Count} trackers within FPS range ({FPSHeatmapCore.data.settings.fpsMin} - {FPSHeatmapCore.data.settings.fpsMax})");

            if (filteredTrackers.Count == 0)
            {
                DevLog("CalculateMedianFPS: No valid trackers found, returning 0");
                return 0f;
            }

            var sortedFps = filteredTrackers.Select(t => t.fps).OrderBy(x => x).ToList();
            DevLog($"CalculateMedianFPS: FPS values range from {sortedFps.First():F1} to {sortedFps.Last():F1}");

            int midIndex = sortedFps.Count / 2;
            float medianFps;

            if (sortedFps.Count % 2 == 1)
            {
                medianFps = sortedFps[midIndex];
                DevLog($"CalculateMedianFPS: Odd number of samples ({sortedFps.Count}), median FPS: {medianFps:F1}");
            }
            else
            {
                medianFps = (sortedFps[midIndex - 1] + sortedFps[midIndex]) * 0.5f;
                DevLog($"CalculateMedianFPS: Even number of samples ({sortedFps.Count}), median FPS: {medianFps:F1} (average of {sortedFps[midIndex - 1]:F1} and {sortedFps[midIndex]:F1})");
            }

            DevLog($"CalculateMedianFPS: Current target FPS: {FPSHeatmapCore.data.settings.targetFPS:F1}");
            FPSHeatmapCore.data.settings.targetFPS = medianFps;
            DevLog($"CalculateMedianFPS: New target FPS set to: {FPSHeatmapCore.data.settings.targetFPS:F1}");

            return medianFps;
        }

        public static void UpdateSelectedHeatmapTrackers()
        {
            // DevLog($"UpdateSelectedHeatmapTrackers - IsDirty: {MightyCoreData.IsDirty()}");
            heatmapTracker.Clear();
            FPSHeatmapCore.data.heatmaps.Clear();
            heatmaps.Clear();

            if (!FPSHeatmapCore.trackingType.IsActive || sceneData == null)
            {
                return;
            }

            DevLog("Updating selected trackers");

            var filteredTrackers = GetPooledTrackerList();
            try
            {
                // Use more efficient filtering
                var minFPS = FPSHeatmapCore.data.settings.fpsMin;
                var maxFPS = FPSHeatmapCore.data.settings.fpsMax;
                var minTime = sceneData.RunPlaybackSelectedMin;
                var maxTime = sceneData.RunPlaybackSelectedMax;

                var allTrackers = GetFilteredHeatmapTrackers(minTime, maxTime);
                for (int i = 0; i < allTrackers.Count; i++)
                {
                    var tracker = allTrackers[i];
                    if (tracker.fps >= minFPS && tracker.fps <= maxFPS)
                        filteredTrackers.Add(tracker);
                }

                heatmapTracker = filteredTrackers
                    .GroupBy(t => t.name)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // DevLog($"selectedRun: {sceneData.SelectedRun} - sceneData.RunPlaybackSelectedMin: {sceneData.RunPlaybackSelectedMin} - sceneData.RunPlaybackSelectedMax: {sceneData.RunPlaybackSelectedMax} - filteredTrackers.Count: {filteredTrackers.Count} - heatmapTracker.Count: {heatmapTracker.Count} - heatmaps.Count: {heatmaps.Count}");

                if (!FPSHeatmapCore.trackingType.IsActive)
                    return;


                // Use GPU optimization for large datasets, fallback to CPU for smaller ones
                var trackerCount = filteredTrackers.Count;
                if (_useGPUCompute && SystemInfo.supportsComputeShaders) // Always use GPU when available
                {
                    consolidatedPoints = BuildConsolidatedFpsPointsGPU();
                }
                else
                {
                    consolidatedPoints = BuildConsolidatedFpsPoints();
                }

                if (consolidatedPoints.Count == 0) return;

                // DevLog("UpdateSelectedTrackers");
                float targetFPS = FPSHeatmapCore.data.settings.targetFPS;

                var heatmapData = GetPooledHeatmapDataList();
                try
                {
                    // Pre-allocate capacity
                    if (heatmapData.Capacity < consolidatedPoints.Count)
                        heatmapData.Capacity = consolidatedPoints.Count;

                    for (int i = 0; i < consolidatedPoints.Count; i++)
                    {
                        var cPoint = consolidatedPoints[i];
                        heatmapData.Add((cPoint.pos, cPoint.finalFps));
                    }

                    if (heatmapData.Count == 0) return;

                    // More efficient min/max calculation
                    Vector3 minPos = heatmapData[0].position;
                    Vector3 maxPos = heatmapData[0].position;

                    for (int i = 1; i < heatmapData.Count; i++)
                    {
                        var pos = heatmapData[i].position;
                        if (pos.x < minPos.x) minPos.x = pos.x;
                        if (pos.x > maxPos.x) maxPos.x = pos.x;
                        if (pos.z < minPos.z) minPos.z = pos.z;
                        if (pos.z > maxPos.z) maxPos.z = pos.z;
                    }

                    float rangeX = maxPos.x - minPos.x;
                    float rangeZ = maxPos.z - minPos.z;

                    int desiredGridWidth = 1000;
                    int desiredGridHeight = 1000;
                    float minCellSize = Mathf.Max(rangeX / desiredGridWidth, rangeZ / desiredGridHeight);
                    float cellSize = Mathf.Max(FPSHeatmapCore.data.settings.cellsize, minCellSize);

                    MightyHeatmap.Heatmap heatmap = new MightyHeatmap.Heatmap(
                        heatmapData,
                        cellSize,
                        FPSHeatmapCore.data.settings.kernal,
                        false,
                        targetFPS,
                        FPSHeatmapCore.data.settings.lowFPSColor,
                        FPSHeatmapCore.data.settings.highFPSColor,
                        FPSHeatmapCore.data.settings.fpsMin,
                        FPSHeatmapCore.data.settings.fpsMax,
                        FPSHeatmapCore.data.settings.aggregationMethod,
                        FPSHeatmapCore.data.settings.opacityFPSHeatmap
                    );

                    FPSHeatmapCore.data.heatmaps["Consolidated"] = heatmap;
                    if (FPSHeatmapCore.data.settings.showHeatmap && FPSHeatmapCore.trackingType.IsActive)
                    {
                        heatmaps.Add(heatmap);
                    }
                }
                finally
                {
                    ReturnPooledHeatmapDataList(heatmapData);
                }
            }
            finally
            {
                ReturnPooledTrackerList(filteredTrackers);
            }

            // DevLog($"heatmaps.Count: {heatmaps.Count}");
        }

        private static List<(Vector3 position, float intensity)> BlendGaps(
            List<(Vector3 position, float intensity)> data,
            float cellSize
        )
        {
            if (data.Count == 0) return data;

            List<(Vector3 position, float intensity)> blended = new List<(Vector3 position, float intensity)>(data);

            var minItem = data.Aggregate((a, b) =>
                new ValueTuple<Vector3, float>(
                    new Vector3(
                        Mathf.Min(a.position.x, b.position.x),
                        0,
                        Mathf.Min(a.position.z, b.position.z)
                    ),
                    0f
                ));
            Vector3 minPos = minItem.Item1;

            var maxItem = data.Aggregate((a, b) =>
                new ValueTuple<Vector3, float>(
                    new Vector3(
                        Mathf.Max(a.position.x, b.position.x),
                        0,
                        Mathf.Max(a.position.z, b.position.z)
                    ),
                    0f
                ));
            Vector3 maxPos = maxItem.Item1;

            int gridWidth = Mathf.CeilToInt((maxPos.x - minPos.x) / cellSize) + 1;
            int gridHeight = Mathf.CeilToInt((maxPos.z - minPos.z) / cellSize) + 1;
            float[,] grid = new float[gridWidth, gridHeight];

            foreach (var (pos, intensity) in data)
            {
                int x = Mathf.FloorToInt((pos.x - minPos.x) / cellSize);
                int z = Mathf.FloorToInt((pos.z - minPos.z) / cellSize);
                if (x >= 0 && x < gridWidth && z >= 0 && z < gridHeight)
                {
                    grid[x, z] = intensity;
                }
            }

            for (int z = 0; z < gridHeight; z++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    if (Mathf.Approximately(grid[x, z], 0f))
                    {
                        float totalIntensity = 0f;
                        int count = 0;
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int nx = x + dx;
                                int nz = z + dz;
                                if (nx >= 0 && nx < gridWidth && nz >= 0 && nz < gridHeight &&
                                    !Mathf.Approximately(grid[nx, nz], 0f))
                                {
                                    totalIntensity += grid[nx, nz];
                                    count++;
                                }
                            }
                        }
                        if (count > 0)
                        {
                            grid[x, z] = totalIntensity / count;
                            Vector3 pos = new Vector3(minPos.x + x * cellSize, 0, minPos.z + z * cellSize);
                            blended.Add((pos, grid[x, z]));
                        }
                    }
                }
            }

            return blended;
        }

        private void OnDestroy()
        {
            if (root != null && sceneView != null && sceneView.rootVisualElement != null)
            {
                sceneView.rootVisualElement.Remove(root);
                root = null;
            }

            heatmapTracker.Clear();
            treePolycounts.Clear();
            settings.Clear();
            _speedPool.Clear();
            _positionPool.Clear();

            SceneView.duringSceneGui -= OnSceneGUI;
            UpdateSceneView -= UpdateWorldSpaceElements;
            RunPlaybackMinMaxUpdated -= UpdateSelectedHeatmapTrackers;

            CleanupComputeBuffers();
        }

        public static void ListAllHeatmaps()
        {
            if (FPSHeatmapCore.data?.heatmaps == null)
            {
                DevLog("No heatmaps available");
                return;
            }

            DevLog("Available heatmaps:");
            foreach (var heatmapName in FPSHeatmapCore.data.heatmaps.Keys)
            {
                DevLog($"- {heatmapName}");
            }
        }

        // Object pooling helper methods
        private static List<HeatmapTracking.HeatmapTracker> GetPooledTrackerList()
        {
            if (_trackerListPool.Count > 0)
            {
                var list = _trackerListPool.Pop();
                list.Clear();
                return list;
            }
            return new List<HeatmapTracking.HeatmapTracker>();
        }

        private static void ReturnPooledTrackerList(List<HeatmapTracking.HeatmapTracker> list)
        {
            if (list != null && list.Count < 10000) // Prevent memory bloat
                _trackerListPool.Push(list);
        }

        private static List<RawPointData> GetPooledRawPointDataList()
        {
            if (_rawPointDataListPool.Count > 0)
            {
                var list = _rawPointDataListPool.Pop();
                list.Clear();
                return list;
            }
            return new List<RawPointData>();
        }

        private static void ReturnPooledRawPointDataList(List<RawPointData> list)
        {
            if (list != null && list.Count < 10000)
                _rawPointDataListPool.Push(list);
        }

        private static Dictionary<Vector3, List<RawPointData>> GetPooledPositionDataDict()
        {
            if (_positionDataDictPool.Count > 0)
            {
                var dict = _positionDataDictPool.Pop();
                dict.Clear();
                return dict;
            }
            return new Dictionary<Vector3, List<RawPointData>>();
        }

        private static void ReturnPooledPositionDataDict(Dictionary<Vector3, List<RawPointData>> dict)
        {
            if (dict != null && dict.Count < 10000)
            {
                foreach (var kvp in dict)
                    ReturnPooledRawPointDataList(kvp.Value);
                dict.Clear();
                _positionDataDictPool.Push(dict);
            }
        }

        private static List<(Vector3 position, float intensity)> GetPooledHeatmapDataList()
        {
            if (_heatmapDataListPool.Count > 0)
            {
                var list = _heatmapDataListPool.Pop();
                list.Clear();
                return list;
            }
            return new List<(Vector3 position, float intensity)>();
        }

        private static void ReturnPooledHeatmapDataList(List<(Vector3 position, float intensity)> list)
        {
            if (list != null && list.Count < 10000)
                _heatmapDataListPool.Push(list);
        }

        // Camera state caching for frustum culling optimization
        private static bool IsCameraStateChanged(Camera camera)
        {
            if (camera == null) return true;

            Vector3 currentPos = camera.transform.position;
            Quaternion currentRot = camera.transform.rotation;
            float currentFOV = camera.fieldOfView;

            int newHash = HashCode.Combine(currentPos, currentRot, currentFOV);

            if (newHash != _cameraStateHash)
            {
                _lastCameraPosition = currentPos;
                _lastCameraRotation = currentRot;
                _lastCameraFOV = currentFOV;
                _cameraStateHash = newHash;
                return true;
            }
            return false;
        }

        private static Plane[] GetCachedFrustumPlanes(Camera camera)
        {
            if (camera == null) return null;

            if (IsCameraStateChanged(camera) || !_frustumPlanesCache.ContainsKey(_cameraStateHash))
            {
                var planes = GeometryUtility.CalculateFrustumPlanes(camera);
                _frustumPlanesCache[_cameraStateHash] = planes;

                // Limit cache size
                if (_frustumPlanesCache.Count > 5)
                {
                    var oldestKey = _frustumPlanesCache.Keys.First();
                    _frustumPlanesCache.Remove(oldestKey);
                }

                return planes;
            }

            return _frustumPlanesCache[_cameraStateHash];
        }

        // Update the BuildConsolidatedFpsPoints call to use optimized version
        public static void EnableGPUCompute(bool enable)
        {
            _useGPUCompute = enable && SystemInfo.supportsComputeShaders;
            DevLog($"GPU Compute {(_useGPUCompute ? "enabled" : "disabled")}");
        }

    }
}
#endif