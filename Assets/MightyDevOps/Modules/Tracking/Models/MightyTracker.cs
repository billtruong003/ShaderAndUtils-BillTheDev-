#if UNITY_EDITOR
using System;
using UnityEngine;
using static MightyTracking.TrackingData.Tracking;
using Mighty;
using static Mighty.MightyCoreData;

using UnityEditor;

namespace MightyTracking
{
    [InitializeOnLoad]
    public class MightyTracker : MonoBehaviour
    {
        public float sampleInterval = 0.5f;

        public float updateInterval = 0.5f;

        public Color trackingColor = Color.white;

        public bool captureScreens = false, mainCamera = true;
        public Camera captureCamera = null;

        [Header("Render Texture Settings")]
        public TrackingData.Tracking.CustomRenderTextureFormat renderFormat = TrackingData.Tracking.CustomRenderTextureFormat.ARGB32;
        public int depthBuffer = 24;

        [Header("Custom Resolution Override")]
        public bool forceCustomResolution = false;
        [Range(64, 7680)]
        public int customWidth = 1920;
        [Range(64, 4320)]
        public int customHeight = 1080;

        [Header("Image Compression Settings")]
        public TrackingData.Tracking.ImageCompressionFormat compressionFormat = TrackingData.Tracking.ImageCompressionFormat.PNG;
        [Range(1, 100)]
        public int jpgQuality = 75;
        public bool pngCompression = true;

        [Header("Quality Presets")]
        public QualityPreset qualityPreset = QualityPreset.Medium;

        public enum QualityPreset
        {
            UltraLow,
            Low,
            Medium,
            High,
            HDRP_UltraLow,
            HDRP_Low,
            HDRP_Medium,
            HDRP_High,
            URP_UltraLow,
            URP_Low,
            URP_Medium,
            URP_High,
            Custom
        }

        private string id;
        private float timer = 0f;
        private float deltaTime = 0f;
        private Vector3 lastPosition;
        private Quaternion lastRotation;
        private long lastTimestamp;

        void Start()
        {
            Debug.Log($"[Start] captureScreens: {captureScreens}, captureCamera: {captureCamera}");
            if (string.IsNullOrEmpty(id))
            {
                id = this.name + "." + System.DateTime.Now.Ticks.ToString() + "." + UnityEngine.Random.Range(1, 10001).ToString();
            }
            if (trackingColor == Color.black)
                trackingColor = StringToColor(id);
        }

        void OnEnable()
        {
            Debug.Log($"[OnEnable] captureScreens: {captureScreens}, captureCamera: {captureCamera}");
            lastPosition = transform.position;
            lastRotation = transform.rotation;
            lastTimestamp = System.DateTime.Now.Ticks;
        }


        void Update()
        {
            // Debug logging to identify which condition is causing early return
            var sceneData = MightyCoreData.sceneData;
            var recordPlaythrough = sceneData?.RecordPlaythrough ?? true;
            var trackingType = TrackingCore.trackingType;
            var isActive = trackingType?.IsActive ?? false;

            if (!recordPlaythrough)
            {
                return;
            }
            if (trackingType == null)
            {
                return;
            }
            if (!isActive)
            {
                return;
            }

            timer += Time.deltaTime;
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

            if (timer >= updateInterval)
            {
                timer = 0f;
                RecordData();
            }
        }


        private void RecordData()
        {
            long currentTime = System.DateTime.Now.Ticks;
            float movementSpeed = CalculateMovementSpeed(transform.position, lastPosition, currentTime - lastTimestamp);
            float rotationSpeed = CalculateRotationSpeed(transform.rotation, lastRotation, currentTime - lastTimestamp);

            Debug.Log($"Capture Screens: {captureScreens} and Capture Camera: {captureCamera} and Main Camera: {mainCamera} and Render Format: {renderFormat} and Depth Buffer: {depthBuffer} and Custom Width: {customWidth} and Custom Height: {customHeight}");
            if (captureScreens && captureCamera != null)
            {
                if (forceCustomResolution)
                {
                    TrackingRenders.AddRenderTexture(captureCamera, currentTime, mainCamera, renderFormat, depthBuffer, customWidth, customHeight);
                }
                else
                {
                    TrackingRenders.AddRenderTexture(captureCamera, currentTime, mainCamera, renderFormat, depthBuffer);
                }
            }

            int width = captureCamera != null ? captureCamera.pixelWidth : 0;
            int height = captureCamera != null ? captureCamera.pixelHeight : 0;

            TrackingCore.data.GetSceneData().transforms.Add(new TransformTracker(
                id, currentTime, transform.position, transform.rotation, transform.localScale, trackingColor,
                movementSpeed, rotationSpeed, null, width, height, compressionFormat, jpgQuality, pngCompression, renderFormat, depthBuffer
            ));


            lastPosition = transform.position;
            lastRotation = transform.rotation;
            lastTimestamp = currentTime;
        }

        private float CalculateMovementSpeed(Vector3 currentPosition, Vector3 previousPosition, long deltaTimeTicks)
        {
            return deltaTimeTicks > 0 ? Vector3.Distance(currentPosition, previousPosition) / (deltaTimeTicks / (float)TimeSpan.TicksPerSecond) : 0f;
        }

        private float CalculateRotationSpeed(Quaternion currentRotation, Quaternion previousRotation, long deltaTimeTicks)
        {
            return deltaTimeTicks > 0 ? Quaternion.Angle(currentRotation, previousRotation) / (deltaTimeTicks / (float)TimeSpan.TicksPerSecond) : 0f;
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

        public void AutoDetectRenderTextureSettings()
        {
            // Detect current rendering pipeline
            var currentPipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;

            if (currentPipeline != null)
            {
                string pipelineName = currentPipeline.GetType().Name;

                if (pipelineName.Contains("HDRenderPipelineAsset") || pipelineName.Contains("HDRP"))
                {
                    // HDRP detected
                    renderFormat = TrackingData.Tracking.CustomRenderTextureFormat.DefaultHDR;
                    depthBuffer = 32;
                    compressionFormat = TrackingData.Tracking.ImageCompressionFormat.PNG;
                    qualityPreset = QualityPreset.HDRP_Medium;
                }
                else if (pipelineName.Contains("UniversalRenderPipelineAsset") || pipelineName.Contains("URP"))
                {
                    // URP detected
                    renderFormat = TrackingData.Tracking.CustomRenderTextureFormat.RGBAHalf;
                    depthBuffer = 24;
                    compressionFormat = TrackingData.Tracking.ImageCompressionFormat.JPG;
                    jpgQuality = 85;
                    qualityPreset = QualityPreset.URP_Medium;
                }
                else
                {
                    // Built-in pipeline
                    renderFormat = TrackingData.Tracking.CustomRenderTextureFormat.ARGB32;
                    depthBuffer = 24;
                    compressionFormat = TrackingData.Tracking.ImageCompressionFormat.PNG;
                    qualityPreset = QualityPreset.Medium;
                }
            }
            else
            {
                // Built-in pipeline (no SRP)
                renderFormat = TrackingData.Tracking.CustomRenderTextureFormat.ARGB32;
                depthBuffer = 24;
                compressionFormat = TrackingData.Tracking.ImageCompressionFormat.PNG;
                qualityPreset = QualityPreset.Medium;
            }

            Debug.Log($"Auto-detected settings for pipeline: {(currentPipeline != null ? currentPipeline.GetType().Name : "Built-in")}");
        }

        public void ApplyQualityPreset(QualityPreset preset)
        {
            qualityPreset = preset;

            switch (preset)
            {
                case QualityPreset.UltraLow:
                    renderFormat = TrackingData.Tracking.CustomRenderTextureFormat.RGB24;
                    depthBuffer = 16;
                    compressionFormat = TrackingData.Tracking.ImageCompressionFormat.JPG;
                    jpgQuality = 50;
                    pngCompression = true;
                    // Ultra low quality uses very small resolution
                    forceCustomResolution = true;
                    customWidth = 320;
                    customHeight = 240;
                    break;

                case QualityPreset.Low:
                    renderFormat = TrackingData.Tracking.CustomRenderTextureFormat.RGB24;
                    depthBuffer = 16;
                    compressionFormat = TrackingData.Tracking.ImageCompressionFormat.JPG;
                    jpgQuality = 60;
                    pngCompression = true;
                    // Low quality uses smaller resolution
                    forceCustomResolution = true;
                    customWidth = 640;
                    customHeight = 480;
                    break;

                case QualityPreset.Medium:
                    renderFormat = TrackingData.Tracking.CustomRenderTextureFormat.ARGB32;
                    depthBuffer = 24;
                    compressionFormat = TrackingData.Tracking.ImageCompressionFormat.PNG;
                    jpgQuality = 75;
                    pngCompression = true;
                    // Medium quality uses standard resolution
                    forceCustomResolution = true;
                    customWidth = 1280;
                    customHeight = 720;
                    break;

                case QualityPreset.High:
                    renderFormat = TrackingData.Tracking.CustomRenderTextureFormat.RGBA32;
                    depthBuffer = 32;
                    compressionFormat = TrackingData.Tracking.ImageCompressionFormat.PNG;
                    jpgQuality = 95;
                    pngCompression = false; // Uncompressed PNG
                    // High quality uses full HD
                    forceCustomResolution = true;
                    customWidth = 1920;
                    customHeight = 1080;
                    break;

                case QualityPreset.HDRP_UltraLow:
                    renderFormat = TrackingData.Tracking.CustomRenderTextureFormat.DefaultHDR;
                    depthBuffer = 24;
                    compressionFormat = TrackingData.Tracking.ImageCompressionFormat.JPG;
                    jpgQuality = 50;
                    pngCompression = true;
                    // HDRP Ultra Low: very small resolution for performance
                    forceCustomResolution = true;
                    customWidth = 640;
                    customHeight = 360;
                    break;

                case QualityPreset.HDRP_Low:
                    renderFormat = TrackingData.Tracking.CustomRenderTextureFormat.DefaultHDR;
                    depthBuffer = 32;
                    compressionFormat = TrackingData.Tracking.ImageCompressionFormat.JPG;
                    jpgQuality = 70;
                    pngCompression = true;
                    // HDRP Low: smaller resolution for performance
                    forceCustomResolution = true;
                    customWidth = 1280;
                    customHeight = 720;
                    break;

                case QualityPreset.HDRP_Medium:
                    renderFormat = TrackingData.Tracking.CustomRenderTextureFormat.DefaultHDR;
                    depthBuffer = 32;
                    compressionFormat = TrackingData.Tracking.ImageCompressionFormat.PNG;
                    jpgQuality = 85;
                    pngCompression = true;
                    // HDRP Medium: full HD
                    forceCustomResolution = true;
                    customWidth = 1920;
                    customHeight = 1080;
                    break;

                case QualityPreset.HDRP_High:
                    renderFormat = TrackingData.Tracking.CustomRenderTextureFormat.DefaultHDR;
                    depthBuffer = 32;
                    compressionFormat = TrackingData.Tracking.ImageCompressionFormat.PNG;
                    jpgQuality = 90;
                    pngCompression = false;
                    // HDRP High: 4K resolution
                    forceCustomResolution = true;
                    customWidth = 3840;
                    customHeight = 2160;
                    break;

                case QualityPreset.URP_UltraLow:
                    renderFormat = TrackingData.Tracking.CustomRenderTextureFormat.RGBAHalf;
                    depthBuffer = 16;
                    compressionFormat = TrackingData.Tracking.ImageCompressionFormat.JPG;
                    jpgQuality = 45;
                    pngCompression = true;
                    // URP Ultra Low: very small for mobile
                    forceCustomResolution = true;
                    customWidth = 480;
                    customHeight = 270;
                    break;

                case QualityPreset.URP_Low:
                    renderFormat = TrackingData.Tracking.CustomRenderTextureFormat.RGBAHalf;
                    depthBuffer = 24;
                    compressionFormat = TrackingData.Tracking.ImageCompressionFormat.JPG;
                    jpgQuality = 65;
                    pngCompression = true;
                    // URP Low: optimized for mobile
                    forceCustomResolution = true;
                    customWidth = 960;
                    customHeight = 540;
                    break;

                case QualityPreset.URP_Medium:
                    renderFormat = TrackingData.Tracking.CustomRenderTextureFormat.RGBAHalf;
                    depthBuffer = 24;
                    compressionFormat = TrackingData.Tracking.ImageCompressionFormat.JPG;
                    jpgQuality = 80;
                    pngCompression = true;
                    // URP Medium: HD resolution
                    forceCustomResolution = true;
                    customWidth = 1280;
                    customHeight = 720;
                    break;

                case QualityPreset.URP_High:
                    renderFormat = TrackingData.Tracking.CustomRenderTextureFormat.RGBAHalf;
                    depthBuffer = 24;
                    compressionFormat = TrackingData.Tracking.ImageCompressionFormat.PNG;
                    jpgQuality = 85;
                    pngCompression = true;
                    // URP High: full HD resolution
                    forceCustomResolution = true;
                    customWidth = 1920;
                    customHeight = 1080;
                    break;

                case QualityPreset.Custom:
                    // Don't change anything, let user customize
                    break;
            }

            Debug.Log($"Applied quality preset: {preset}");
        }

        public float CalculateEstimatedStorageCostPerMinute()
        {
            if (!captureScreens || captureCamera == null || updateInterval <= 0)
                return 0f;

            // Get Game View resolution (more accurate than camera pixel dimensions)
            Vector2 gameViewResolution = GetGameViewResolution();
            int width = (int)gameViewResolution.x;
            int height = (int)gameViewResolution.y;

            // Calculate captures per minute using floor to avoid partial captures
            float capturesPerMinute = Mathf.Floor(60f / updateInterval);

            // Calculate base image size in bytes based on format
            float bytesPerPixel = GetBytesPerPixel(renderFormat);
            float baseImageSizeBytes = width * height * bytesPerPixel;

            // Apply compression factor
            float compressionFactor = GetCompressionFactor(compressionFormat, jpgQuality, pngCompression);
            float compressedImageSizeBytes = baseImageSizeBytes * compressionFactor;

            // Calculate total MB per minute
            float totalBytesPerMinute = compressedImageSizeBytes * capturesPerMinute;
            float megabytesPerMinute = totalBytesPerMinute / (1024f * 1024f);

            return megabytesPerMinute;
        }

        public float CalculateEstimatedStorageCostPerCapture()
        {
            if (!captureScreens || captureCamera == null)
                return 0f;

            // Get Game View resolution
            Vector2 gameViewResolution = GetGameViewResolution();
            int width = (int)gameViewResolution.x;
            int height = (int)gameViewResolution.y;

            // Calculate base image size in bytes based on format
            float bytesPerPixel = GetBytesPerPixel(renderFormat);
            float baseImageSizeBytes = width * height * bytesPerPixel;

            // Apply compression factor
            float compressionFactor = GetCompressionFactor(compressionFormat, jpgQuality, pngCompression);
            float compressedImageSizeBytes = baseImageSizeBytes * compressionFactor;

            // Return MB per capture
            return compressedImageSizeBytes / (1024f * 1024f);
        }

        public string GetStorageCostDisplayString()
        {
            if (!captureScreens)
                return "Screen capture disabled";

            float mbPerMinute = CalculateEstimatedStorageCostPerMinute();
            float mbPerCapture = CalculateEstimatedStorageCostPerCapture();

            // Format per-capture cost
            string perCaptureText;
            if (mbPerCapture < 0.001f)
                perCaptureText = $"{(mbPerCapture * 1024f * 1024f):F0} bytes";
            else if (mbPerCapture < 0.1f)
                perCaptureText = $"{(mbPerCapture * 1024f):F1} KB";
            else
                perCaptureText = $"{mbPerCapture:F2} MB";

            // Format per-minute cost
            string perMinuteText;
            if (mbPerMinute < 0.1f)
                perMinuteText = $"{(mbPerMinute * 1024f):F1} KB/min";
            else if (mbPerMinute < 1000f)
                perMinuteText = $"{mbPerMinute:F1} MB/min";
            else
                perMinuteText = $"{(mbPerMinute / 1024f):F2} GB/min";

            return $"~{perMinuteText} (~{perCaptureText} per capture)";
        }

        private float GetBytesPerPixel(TrackingData.Tracking.CustomRenderTextureFormat format)
        {
            switch (format)
            {
                case TrackingData.Tracking.CustomRenderTextureFormat.RGB24:
                    return 3f;
                case TrackingData.Tracking.CustomRenderTextureFormat.ARGB32:
                case TrackingData.Tracking.CustomRenderTextureFormat.RGBA32:
                    return 4f;
                case TrackingData.Tracking.CustomRenderTextureFormat.RGBAFloat:
                    return 16f; // 4 channels * 4 bytes per float
                case TrackingData.Tracking.CustomRenderTextureFormat.RGBAHalf:
                    return 8f; // 4 channels * 2 bytes per half
                case TrackingData.Tracking.CustomRenderTextureFormat.R16G16B16A16_SFloat:
                    return 8f; // 4 channels * 2 bytes per signed float
                case TrackingData.Tracking.CustomRenderTextureFormat.R11G11B10_UFloat:
                    return 4f; // Packed 32-bit format
                case TrackingData.Tracking.CustomRenderTextureFormat.RGB111110Float:
                    return 4f; // Packed 32-bit format
                case TrackingData.Tracking.CustomRenderTextureFormat.ARGB2101010:
                    return 4f; // Packed 32-bit format
                case TrackingData.Tracking.CustomRenderTextureFormat.DefaultHDR:
                    return 8f; // Assume RGBAHalf as default HDR
                default:
                    return 4f; // Default to ARGB32
            }
        }

        private float GetCompressionFactor(TrackingData.Tracking.ImageCompressionFormat format, int jpgQuality, bool pngCompression)
        {
            switch (format)
            {
                case TrackingData.Tracking.ImageCompressionFormat.JPG:
                    // JPG compression factor based on real-world performance data
                    // 4K RGB24 at 50% quality: 354.8KB from ~24MB uncompressed = 0.015 ratio
                    if (jpgQuality >= 95) return 0.20f;      // Very high quality
                    else if (jpgQuality >= 85) return 0.12f; // High quality 
                    else if (jpgQuality >= 75) return 0.08f; // Good quality
                    else if (jpgQuality >= 65) return 0.05f; // Medium quality
                    else if (jpgQuality >= 50) return 0.025f; // Low quality - based on real data
                    else return 0.02f;                       // Very low quality

                case TrackingData.Tracking.ImageCompressionFormat.PNG:
                    if (pngCompression)
                        return 0.4f; // PNG with compression (lossless but still decent compression)
                    else
                        return 1.0f; // Uncompressed PNG

                default:
                    return 1.0f;
            }
        }

        public Vector2 GetGameViewResolution()
        {
            // Check for custom resolution override first
            if (forceCustomResolution)
            {
                return new Vector2(customWidth, customHeight);
            }

#if UNITY_EDITOR
            // For main camera or when using screenshot mode, use Game View resolution
            if (mainCamera)
            {
                try
                {
                    // Use reflection to access Game View resolution
                    var gameViewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
                    var gameViews = UnityEngine.Resources.FindObjectsOfTypeAll(gameViewType);

                    if (gameViews.Length > 0)
                    {
                        var gameView = gameViews[0];

                        // Get the current game view size
                        var currentGameViewSizeProperty = gameViewType.GetProperty("currentGameViewSize",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                        if (currentGameViewSizeProperty != null)
                        {
                            var gameViewSize = currentGameViewSizeProperty.GetValue(gameView);

                            if (gameViewSize != null)
                            {
                                var gameViewSizeType = gameViewSize.GetType();

                                // Check if this is Free Aspect mode
                                var sizeTypeProperty = gameViewSizeType.GetProperty("sizeType");
                                if (sizeTypeProperty != null)
                                {
                                    var sizeType = sizeTypeProperty.GetValue(gameViewSize);
                                    // If it's Free Aspect (sizeType == 0), use actual Game View window size
                                    if (sizeType.ToString() == "AspectRatio" || sizeType.ToString() == "0")
                                    {
                                        // For Free Aspect, get the actual Game View window size
                                        var targetSizeProperty = gameViewType.GetProperty("targetSize",
                                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                                        if (targetSizeProperty != null)
                                        {
                                            var targetSize = (Vector2)targetSizeProperty.GetValue(gameView);
                                            if (targetSize.x > 0 && targetSize.y > 0)
                                                return targetSize;
                                        }
                                    }
                                }

                                // Try to get fixed resolution width and height for preset resolutions
                                var widthProperty = gameViewSizeType.GetProperty("width");
                                var heightProperty = gameViewSizeType.GetProperty("height");

                                if (widthProperty != null && heightProperty != null)
                                {
                                    int width = (int)widthProperty.GetValue(gameViewSize);
                                    int height = (int)heightProperty.GetValue(gameViewSize);
                                    if (width > 0 && height > 0)
                                        return new Vector2(width, height);
                                }
                            }
                        }

                        // Fallback: try to get the target size from the game view itself
                        var fallbackTargetSizeProperty = gameViewType.GetProperty("targetSize",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                        if (fallbackTargetSizeProperty != null)
                        {
                            var fallbackTargetSize = (Vector2)fallbackTargetSizeProperty.GetValue(gameView);
                            if (fallbackTargetSize.x > 0 && fallbackTargetSize.y > 0)
                                return fallbackTargetSize;
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Could not access Game View resolution via reflection: {e.Message}");
                }
            }
#endif

            // For non-main cameras or fallback, use camera's actual resolution
            // This matches the logic in TrackingRenders.RenderTextureStack constructor
            if (captureCamera != null)
            {
                int width = captureCamera.pixelWidth > 0 ? captureCamera.pixelWidth : 512;
                int height = captureCamera.pixelHeight > 0 ? captureCamera.pixelHeight : 512;
                return new Vector2(width, height);
            }

            return new Vector2(1920, 1080); // Default fallback
        }

        public string GetGameViewResolutionInfo()
        {
            // Check for custom resolution override first
            if (forceCustomResolution)
            {
                return $"Custom Override ({customWidth}x{customHeight})";
            }

#if UNITY_EDITOR
            if (mainCamera)
            {
                try
                {
                    var gameViewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
                    var gameViews = UnityEngine.Resources.FindObjectsOfTypeAll(gameViewType);

                    if (gameViews.Length > 0)
                    {
                        var gameView = gameViews[0];

                        // Get the current game view size
                        var currentGameViewSizeProperty = gameViewType.GetProperty("currentGameViewSize",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                        if (currentGameViewSizeProperty != null)
                        {
                            var gameViewSize = currentGameViewSizeProperty.GetValue(gameView);

                            if (gameViewSize != null)
                            {
                                var gameViewSizeType = gameViewSize.GetType();

                                // Check if this is Free Aspect mode
                                var sizeTypeProperty = gameViewSizeType.GetProperty("sizeType");
                                if (sizeTypeProperty != null)
                                {
                                    var sizeType = sizeTypeProperty.GetValue(gameViewSize);
                                    if (sizeType.ToString() == "AspectRatio" || sizeType.ToString() == "0")
                                    {
                                        Vector2 actualSize = GetGameViewResolution();
                                        return $"Free Aspect ({actualSize.x}x{actualSize.y})";
                                    }
                                }

                                // Get display name for fixed resolutions
                                var displayTextProperty = gameViewSizeType.GetProperty("displayText");
                                if (displayTextProperty != null)
                                {
                                    string displayText = (string)displayTextProperty.GetValue(gameViewSize);
                                    if (!string.IsNullOrEmpty(displayText))
                                        return displayText;
                                }

                                // Fallback to baseText
                                var baseTextProperty = gameViewSizeType.GetProperty("baseText");
                                if (baseTextProperty != null)
                                {
                                    string baseText = (string)baseTextProperty.GetValue(gameViewSize);
                                    if (!string.IsNullOrEmpty(baseText))
                                        return baseText;
                                }
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Could not access Game View resolution info via reflection: {e.Message}");
                }
            }
            else
            {
                // For non-main cameras, explain the behavior
                if (captureCamera != null)
                {
                    Vector2 resolution = GetGameViewResolution();
                    return $"Camera POV ({resolution.x}x{resolution.y}) - Uses camera.pixelWidth/Height";
                }
            }
#endif

            Vector2 finalResolution = GetGameViewResolution();
            return $"{finalResolution.x}x{finalResolution.y}";
        }

        public string GetResolutionExplanation()
        {
            if (!captureScreens || captureCamera == null)
                return "Screen capture disabled";

            if (forceCustomResolution)
            {
                return $"Custom Resolution Override: Forces {customWidth}x{customHeight} regardless of Game View or camera settings. " +
                       "Useful for thumbnails, performance testing, or specific output requirements.";
            }

            if (mainCamera)
            {
                return "Main Camera: Uses Game View resolution setting. " +
                       "Free Aspect = actual Game View window size, " +
                       "Fixed resolutions = preset dimensions (1920x1080, 4K, etc.)";
            }
            else
            {
                Vector2 resolution = GetGameViewResolution();
                return $"Non-Main Camera: Uses camera.pixelWidth/Height ({resolution.x}x{resolution.y}). " +
                       "Resolution determined by camera's render target or defaults to 512x512 if not set. " +
                       "Independent of Game View settings.";
            }
        }
    }
}
#endif