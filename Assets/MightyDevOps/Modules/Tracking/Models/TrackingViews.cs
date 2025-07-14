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
using static MightyTracking.TrackingData;
using static MightyTracking.TrackingData.Tracking;

namespace MightyTracking
{
    [InitializeOnLoad]
    [ExecuteInEditMode]
    public class TrackingViews : MonoBehaviour
    {
        private static TrackingViews _instance;
        public static TrackingViews Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject anchor = GameObject.Find("MightySceneAnchor");
                    if (anchor != null)
                    {
                        _instance = anchor.GetComponent<TrackingViews>();
                        if (_instance == null)
                        {
                            _instance = anchor.AddComponent<TrackingViews>();
                        }
                    }

                    Init();
                }
                return _instance;
            }
        }

        static public VisualElement root;
        static private SceneView sceneView;

        /// <summary>
        /// Holds the time-based transforms we display (for line trails).
        /// </summary>
        static public Dictionary<string, List<Tracking.TransformTracker>> transformTracker =
            new Dictionary<string, List<Tracking.TransformTracker>>();

        private static Dictionary<int, int> treePolycounts = new Dictionary<int, int>();

        private static readonly List<float> _speedPool = new List<float>();
        private static readonly List<Vector3> _positionPool = new List<Vector3>();

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

        private TrackingViews() { }

        static TrackingViews()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        /// <summary>
        /// Initialize our UI / events
        /// </summary>
        private static void Init()
        {
            DevLog("Init TrackingViews");
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;

            UpdateSceneView -= UpdateWorldSpaceElements;
            UpdateSceneView += UpdateWorldSpaceElements;

            RunPlaybackMinMaxUpdated -= UpdateSelectedTrackers;
            RunPlaybackMinMaxUpdated += UpdateSelectedTrackers;

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

        /// <summary>
        /// Pre-caches tree polycounts so we don't re-check them every time.
        /// </summary>
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

        /// <summary>
        /// Called by Unity in edit-mode; draws debug Gizmos in the SceneView.
        /// </summary>
        public void OnDrawGizmos()
        {
            if (transformTracker == null || TrackingCore.trackingType == null || !TrackingCore.trackingType.IsActive) return;
            if (EditorApplication.isPlaying) return;

            Vector3 camPos = SceneView.lastActiveSceneView.camera.transform.position;

            if (TrackingCore.data.settings.showTrackingTrails)
                DrawTrackingTrails(camPos);
        }

        /// <summary>
        /// Draws the line paths for tracked objects (the "strobe" lines).
        /// </summary>
        private void DrawTrackingTrails(Vector3 camPos)
        {
            _speedPool.Clear();
            _positionPool.Clear();
            float strobeOffset = -0.5f;
            Vector3 sceneViewPosition = camPos;

            foreach (var kvp in transformTracker)
            {
                var playThrough = kvp.Value;

                float maxSpeed = 0;
                List<float> speeds = _speedPool;
                for (int i = 1; i < playThrough.Count; i++)
                {
                    float speed = Vector3.Distance(playThrough[i].position, playThrough[i - 1].position);
                    speeds.Add(speed);
                    maxSpeed = Mathf.Max(maxSpeed, speed);
                }

                for (int i = 0; i < playThrough.Count - 1; i++)
                {
                    Vector3 currentPosition = playThrough[i].position;
                    Vector3 previousPosition = i == 0 ? playThrough[i].position : playThrough[i - 1].position;

                    if (currentPosition == previousPosition) continue;

                    float currentTime = (float)EditorApplication.timeSinceStartup;
                    float distance = Vector3.Distance(currentPosition, sceneViewPosition);
                    float opacity = TrackingCore.data.settings.SvOpacityCurve.Evaluate(distance / TrackingCore.data.settings.SvFadeDistance);
                    Handles.color = playThrough[i].color;

                    float strobeValue = Mathf.Sin(currentTime * TrackingCore.data.settings.SvSpeed + i * strobeOffset);
                    Color strobeColor = playThrough[i].color * (1f + strobeValue * TrackingCore.data.settings.SvIntensity);

                    Color lineColorWithOpacity = strobeColor * new Color(1f, 1f, 1f, opacity);
                    Handles.color = lineColorWithOpacity;
                    Handles.DrawAAPolyLine(
                        TrackingCore.data.settings.SvThickness,
                        new Vector3[] { previousPosition, currentPosition }
                    );
                }
            }
        }

        public static void UpdateWorldSpaceElements() { }

        /// <summary>
        /// Rebuild / re-init the UI elements
        /// </summary>
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

        /// <summary>
        /// Filters trackers by time range
        /// </summary>
        static public List<Tracking.TransformTracker> GetFilteredTrackers(long minTimestamp, long maxTimestamp)
        {
            var sceneData = TrackingCore.data.GetSceneData();
            if (sceneData == null || sceneData.transforms == null)
            {
                return new List<Tracking.TransformTracker>();
            }

            return sceneData.transforms
                .Where(t => t.timeStamp >= minTimestamp && t.timeStamp <= maxTimestamp)
                .ToList();
        }

        /// <summary>
        /// Organizes trackers by name
        /// </summary>
        static public Dictionary<string, List<Tracking.TransformTracker>> GetOrganizedTrackers(long minTimestamp, long maxTimestamp)
        {
            return GetFilteredTrackers(minTimestamp, maxTimestamp)
                .GroupBy(t => t.name)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public static void UpdateSelectedTrackers()
        {
            DevLog("UpdateSelectedTrackers");
            transformTracker.Clear();

            if (!TrackingCore.trackingType.IsActive || sceneData == null)
            {
                return;
            }

            // Debug.Log("Updating selected trackers");

            // Group by name/time, so the line trails can still be drawn
            var filteredTrackers = GetFilteredTrackers(sceneData.RunPlaybackSelectedMin, sceneData.RunPlaybackSelectedMax)
                .ToList();

            transformTracker = filteredTrackers
                .GroupBy(t => t.name)
                .ToDictionary(g => g.Key, g => g.ToList());

            // TrackingCore.BuildSceneView();
        }

        private void OnDestroy()
        {
            // Cleanup UI elements
            if (root != null && sceneView != null && sceneView.rootVisualElement != null)
            {
                sceneView.rootVisualElement.Remove(root);
                root = null;
            }

            // Cleanup static collections
            transformTracker.Clear();
            treePolycounts.Clear();
            settings.Clear();
            _speedPool.Clear();
            _positionPool.Clear();

            // Unsubscribe from events
            SceneView.duringSceneGui -= OnSceneGUI;
            UpdateSceneView -= UpdateWorldSpaceElements;
            RunPlaybackMinMaxUpdated -= UpdateSelectedTrackers;
        }
    }
}
#endif