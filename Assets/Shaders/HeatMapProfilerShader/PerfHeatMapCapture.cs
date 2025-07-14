using UnityEngine;
using Unity.Profiling;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using UnityEditor;

namespace PerfHeatMap
{
    public class PerfHeatMapCapture
    {
        private readonly PerfHeatMapSceneSettings _sceneSettings;

        private ProfilerRecorder _drawCallsRecorder;
        private ProfilerRecorder _trianglesRecorder;
        private ProfilerRecorder _gpuTimeRecorder;

        public PerfHeatMapCapture(PerfHeatMapSceneSettings sceneSettings)
        {
            _sceneSettings = sceneSettings;
        }

        public async Task<PerfHeatMapData> ExecuteAsync()
        {
            var data = ScriptableObject.CreateInstance<PerfHeatMapData>();
            data.AnalysisBounds = _sceneSettings.CaptureBounds;
            data.CellSize = _sceneSettings.CellSize;

            var samplePoints = GenerateValidSamplePoints();

            if (samplePoints.Count == 0)
            {
                EditorUtility.DisplayDialog("PerfHeatMap", "No valid sample points found in the specified volume. Adjust settings and try again.", "OK");
                return null;
            }

            var originalPlayModeState = EditorApplication.isPlaying;
            if (PerfHeatMapGlobalSettings.CaptureInPlayMode && !originalPlayModeState)
            {
                await EnterPlayMode();
            }

            var captureCameraObject = CreateCaptureCamera();
            InitializeRecorders();

            var stopwatch = new Stopwatch();

            for (int i = 0; i < samplePoints.Count; i++)
            {
                var point = samplePoints[i];
                float progress = (float)i / samplePoints.Count;
                if (EditorUtility.DisplayCancelableProgressBar("PerfHeatMap Capture", $"Processing sample {i + 1}/{samplePoints.Count}", progress))
                {
                    CleanUp(captureCameraObject);
                    if (PerfHeatMapGlobalSettings.CaptureInPlayMode && !originalPlayModeState) await ExitPlayMode();
                    return null;
                }

                captureCameraObject.transform.position = point;

                stopwatch.Restart();
                RenderScene(captureCameraObject.GetComponent<Camera>());
                await Task.Yield();
                stopwatch.Stop();

                data.Samples.Add(CreateSample(point, stopwatch.Elapsed.TotalMilliseconds));
            }

            CleanUp(captureCameraObject);
            if (PerfHeatMapGlobalSettings.CaptureInPlayMode && !originalPlayModeState)
            {
                await ExitPlayMode();
            }

            return data;
        }

        private void InitializeRecorders()
        {
            _drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            _trianglesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            _gpuTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "GPU Frame Time");
        }

        private void CleanUp(GameObject cameraObject)
        {
            _drawCallsRecorder.Dispose();
            _trianglesRecorder.Dispose();
            _gpuTimeRecorder.Dispose();

            if (cameraObject != null) Object.DestroyImmediate(cameraObject);
            EditorUtility.ClearProgressBar();
        }

        private HeatMapSample CreateSample(Vector3 position, double frameTime)
        {
            return new HeatMapSample
            {
                Position = position,
                Stat1_DrawCalls = _drawCallsRecorder.LastValue,
                Stat2_Triangles = _trianglesRecorder.LastValue,
                Stat3_GpuTimeMS = _gpuTimeRecorder.LastValue / 1_000_000f,
                Stat4_FrameTimeMS = (float)frameTime
            };
        }

        private void RenderScene(Camera cam)
        {
            if (PerfHeatMapGlobalSettings.Use360Camera)
            {
                var cubemap = new RenderTexture(PerfHeatMapGlobalSettings.CameraResolutionX, PerfHeatMapGlobalSettings.CameraResolutionX, 16, RenderTextureFormat.ARGB32);
                cubemap.dimension = UnityEngine.Rendering.TextureDimension.Cube;
                cam.RenderToCubemap(cubemap);
                Object.DestroyImmediate(cubemap);
            }
            else
            {
                var rt = RenderTexture.GetTemporary(PerfHeatMapGlobalSettings.CameraResolutionX, PerfHeatMapGlobalSettings.CameraResolutionY, 16);
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = null;
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        private GameObject CreateCaptureCamera()
        {
            var cameraGo = new GameObject("PerfHeatMap_CaptureCamera") { hideFlags = HideFlags.HideAndDontSave };
            var cam = cameraGo.AddComponent<Camera>();
            cam.enabled = false;
            cam.clearFlags = CameraClearFlags.Skybox;

            if (!PerfHeatMapGlobalSettings.Use360Camera)
            {
                cam.fieldOfView = PerfHeatMapGlobalSettings.HorizontalFOV;
                cam.aspect = PerfHeatMapGlobalSettings.AspectRatio;
            }

            return cameraGo;
        }

        private List<Vector3> GenerateValidSamplePoints()
        {
            var points = new List<Vector3>();
            Bounds bounds = _sceneSettings.CaptureBounds;
            Vector3 cellSize = _sceneSettings.CellSize;
            Vector3 start = bounds.min + cellSize / 2f;

            for (float x = start.x; x < bounds.max.x; x += cellSize.x)
            {
                for (float y = start.y; y < bounds.max.y; y += cellSize.y)
                {
                    for (float z = start.z; z < bounds.max.z; z += cellSize.z)
                    {
                        var point = new Vector3(x, y, z);
                        if (IsPointValid(point))
                        {
                            points.Add(point);
                        }
                    }
                }
            }
            return points;
        }

        private bool IsPointValid(Vector3 point)
        {
            if (_sceneSettings.ExcludeCellsInsideColliders &&
                Physics.CheckSphere(point, _sceneSettings.CellSize.magnitude * 0.1f, _sceneSettings.ExclusionLayers))
            {
                return false;
            }

            if (_sceneSettings.ExcludeCellsTooFarFromGround &&
                !Physics.Raycast(point, Vector3.down, _sceneSettings.MaxDistanceFromGround))
            {
                return false;
            }
            return true;
        }

        private Task EnterPlayMode()
        {
            var tcs = new TaskCompletionSource<bool>();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.EnterPlaymode();

            void OnPlayModeStateChanged(PlayModeStateChange state)
            {
                if (state == PlayModeStateChange.EnteredPlayMode)
                {
                    EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                    tcs.SetResult(true);
                }
            }
            return tcs.Task;
        }

        private Task ExitPlayMode()
        {
            var tcs = new TaskCompletionSource<bool>();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.ExitPlaymode();

            void OnPlayModeStateChanged(PlayModeStateChange state)
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                    tcs.SetResult(true);
                }
            }
            return tcs.Task;
        }
    }
}