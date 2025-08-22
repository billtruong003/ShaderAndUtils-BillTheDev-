#if UNITY_EDITOR
using UnityEditor;

namespace PerfHeatMap
{
    public static class PerfHeatMapGlobalSettings
    {
        private const string CaptureInPlayModeKey = "PerfHeatMap.CaptureInPlayMode";
        private const string LockSceneViewKey = "PerfHeatMap.LockSceneView";
        private const string Use360CameraKey = "PerfHeatMap.Use360Camera";
        private const string CameraResolutionXKey = "PerfHeatMap.CameraResolutionX";
        private const string CameraResolutionYKey = "PerfHeatMap.CameraResolutionY";
        private const string HorizontalFOVKey = "PerfHeatMap.HorizontalFOV";
        private const string AspectRatioKey = "PerfHeatMap.AspectRatio";

        public static bool CaptureInPlayMode
        {
            get => EditorPrefs.GetBool(CaptureInPlayModeKey, true);
            set => EditorPrefs.SetBool(CaptureInPlayModeKey, value);
        }

        public static bool LockSceneView
        {
            get => EditorPrefs.GetBool(LockSceneViewKey, true);
            set => EditorPrefs.SetBool(LockSceneViewKey, value);
        }

        public static bool Use360Camera
        {
            get => EditorPrefs.GetBool(Use360CameraKey, true);
            set => EditorPrefs.SetBool(Use360CameraKey, value);
        }

        public static int CameraResolutionX
        {
            get => EditorPrefs.GetInt(CameraResolutionXKey, 256);
            set => EditorPrefs.SetInt(CameraResolutionXKey, value);
        }

        public static int CameraResolutionY
        {
            get => EditorPrefs.GetInt(CameraResolutionYKey, 256);
            set => EditorPrefs.SetInt(CameraResolutionYKey, value);
        }

        public static float HorizontalFOV
        {
            get => EditorPrefs.GetFloat(HorizontalFOVKey, 90f);
            set => EditorPrefs.SetFloat(HorizontalFOVKey, value);
        }

        public static float AspectRatio
        {
            get => EditorPrefs.GetFloat(AspectRatioKey, 1.0f);
            set => EditorPrefs.SetFloat(AspectRatioKey, value);
        }
    }
}
#endif