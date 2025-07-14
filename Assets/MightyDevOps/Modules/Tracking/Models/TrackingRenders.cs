#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace MightyTracking
{
    public class TrackingRenders
    {
        // Helper method to convert custom enum to Unity RenderTextureFormat
        public static RenderTextureFormat ConvertToUnityRenderFormat(TrackingData.Tracking.CustomRenderTextureFormat customFormat)
        {
            return customFormat switch
            {
                // Standard formats
                TrackingData.Tracking.CustomRenderTextureFormat.ARGB32 => RenderTextureFormat.ARGB32,
                TrackingData.Tracking.CustomRenderTextureFormat.RGB24 => RenderTextureFormat.RGB565,
                TrackingData.Tracking.CustomRenderTextureFormat.RGBA32 => RenderTextureFormat.ARGB32,

                // High precision formats
                TrackingData.Tracking.CustomRenderTextureFormat.RGBAFloat => RenderTextureFormat.ARGBFloat,
                TrackingData.Tracking.CustomRenderTextureFormat.RGBAHalf => RenderTextureFormat.ARGBHalf,

                // HDR formats for modern pipelines
                TrackingData.Tracking.CustomRenderTextureFormat.R16G16B16A16_SFloat => RenderTextureFormat.ARGBHalf,
                TrackingData.Tracking.CustomRenderTextureFormat.R11G11B10_UFloat => RenderTextureFormat.RGB111110Float,
                TrackingData.Tracking.CustomRenderTextureFormat.RGB111110Float => RenderTextureFormat.RGB111110Float,
                TrackingData.Tracking.CustomRenderTextureFormat.ARGB2101010 => RenderTextureFormat.ARGB2101010,
                TrackingData.Tracking.CustomRenderTextureFormat.DefaultHDR => GetDefaultHDRFormat(),

                _ => RenderTextureFormat.ARGB32
            };
        }

        // Helper method to get the best HDR format for the current system
        private static RenderTextureFormat GetDefaultHDRFormat()
        {
            // Check if the system supports high-end HDR formats
            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
                return RenderTextureFormat.ARGBHalf;
            else if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float))
                return RenderTextureFormat.RGB111110Float;
            else
                return RenderTextureFormat.ARGB32; // Fallback to standard format
        }

        // Helper method to convert custom enum to Unity TextureFormat
        public static TextureFormat ConvertToUnityTextureFormat(TrackingData.Tracking.CustomRenderTextureFormat customFormat)
        {
            return customFormat switch
            {
                // Standard formats
                TrackingData.Tracking.CustomRenderTextureFormat.ARGB32 => TextureFormat.ARGB32,
                TrackingData.Tracking.CustomRenderTextureFormat.RGB24 => TextureFormat.RGB24,
                TrackingData.Tracking.CustomRenderTextureFormat.RGBA32 => TextureFormat.RGBA32,

                // High precision formats
                TrackingData.Tracking.CustomRenderTextureFormat.RGBAFloat => TextureFormat.RGBAFloat,
                TrackingData.Tracking.CustomRenderTextureFormat.RGBAHalf => TextureFormat.RGBAHalf,

                // HDR formats for modern pipelines
                TrackingData.Tracking.CustomRenderTextureFormat.R16G16B16A16_SFloat => TextureFormat.RGBAHalf,
                TrackingData.Tracking.CustomRenderTextureFormat.R11G11B10_UFloat => TextureFormat.RGB9e5Float,
                TrackingData.Tracking.CustomRenderTextureFormat.RGB111110Float => TextureFormat.RGB9e5Float,
                TrackingData.Tracking.CustomRenderTextureFormat.ARGB2101010 => TextureFormat.ARGB32, // Fallback for texture
                TrackingData.Tracking.CustomRenderTextureFormat.DefaultHDR => GetDefaultHDRTextureFormat(),

                _ => TextureFormat.RGBA32
            };
        }

        // Helper method to get the best HDR texture format for the current system
        private static TextureFormat GetDefaultHDRTextureFormat()
        {
            // Check if the system supports high-end HDR texture formats
            if (SystemInfo.SupportsTextureFormat(TextureFormat.RGBAHalf))
                return TextureFormat.RGBAHalf;
            else if (SystemInfo.SupportsTextureFormat(TextureFormat.RGB9e5Float))
                return TextureFormat.RGB9e5Float;
            else
                return TextureFormat.RGBA32; // Fallback to standard format
        }

        class RenderTextureStack
        {
            public RenderTexture render;
            public long timeStamp;

            public RenderTextureStack(Camera camera, long timeStamp, bool useScreenShot = false, TrackingData.Tracking.CustomRenderTextureFormat renderFormat = TrackingData.Tracking.CustomRenderTextureFormat.ARGB32, int depthBuffer = 24, int customWidth = -1, int customHeight = -1)
            {
                RenderTextureFormat unityRenderFormat = ConvertToUnityRenderFormat(renderFormat);

                if (useScreenShot)
                {
                    // Use original screenshot method to capture full UI, etc.
                    int width = customWidth > 0 ? customWidth : Screen.width;
                    int height = customHeight > 0 ? customHeight : Screen.height;

                    this.render = new RenderTexture(width, height, depthBuffer, unityRenderFormat);
                    this.render.Create();

                    // Capture the full screen
                    var temp = ScreenCapture.CaptureScreenshotAsTexture();

                    // Copy to our render texture
                    var prev = RenderTexture.active;
                    RenderTexture.active = this.render;
                    Graphics.Blit(temp, this.render);
                    RenderTexture.active = prev;

                    UnityEngine.Object.DestroyImmediate(temp);
                }
                else
                {
                    // Use camera rendering method for specific camera POV
                    int width = customWidth > 0 ? customWidth : (camera.pixelWidth > 0 ? camera.pixelWidth : 512);
                    int height = customHeight > 0 ? customHeight : (camera.pixelHeight > 0 ? camera.pixelHeight : 512);

                    this.render = new RenderTexture(width, height, depthBuffer, unityRenderFormat);
                    this.render.Create();

                    try
                    {
                        // Store the camera's original target texture
                        var originalTargetTexture = camera.targetTexture;

                        // Set the camera to render to our render texture
                        camera.targetTexture = this.render;

                        // Render the camera's view to our render texture
                        camera.Render();

                        // Restore the camera's original target texture
                        camera.targetTexture = originalTargetTexture;
                    }
                    catch (System.Exception e)
                    {
                        // Clean up if something goes wrong
                        if (this.render != null)
                        {
                            this.render.Release();
                            UnityEngine.Object.DestroyImmediate(this.render);
                            this.render = null;
                        }
                        throw e;
                    }
                }

                this.timeStamp = timeStamp;
            }

            public void Cleanup()
            {
                if (render != null)
                {
                    render.Release();
                    UnityEngine.Object.DestroyImmediate(render);
                    render = null;
                }
            }

            public byte[] GetBytes(TrackingData.Tracking.ImageCompressionFormat compressionFormat = TrackingData.Tracking.ImageCompressionFormat.PNG, int jpgQuality = 75, TrackingData.Tracking.CustomRenderTextureFormat textureFormat = TrackingData.Tracking.CustomRenderTextureFormat.RGBA32)
            {
                if (render == null)
                {
                    // Debug.LogWarning("Attempting to get bytes from null render texture");
                    return null;
                }

                // Convert RenderTexture to Texture2D
                TextureFormat unityTextureFormat = ConvertToUnityTextureFormat(textureFormat);
                var temp = new Texture2D(render.width, render.height, unityTextureFormat, false);
                var prev = RenderTexture.active;
                RenderTexture.active = render;
                temp.ReadPixels(new Rect(0, 0, render.width, render.height), 0, 0);
                temp.Apply();
                RenderTexture.active = prev;

                // Encode based on compression format
                byte[] bytes;
                if (compressionFormat == TrackingData.Tracking.ImageCompressionFormat.JPG)
                {
                    bytes = temp.EncodeToJPG(jpgQuality);
                }
                else
                {
                    bytes = temp.EncodeToPNG();
                }

                UnityEngine.Object.DestroyImmediate(temp);
                return bytes;
            }
        }

        static List<RenderTextureStack> renderTextureStack = new List<RenderTextureStack>();

        public static void AddRenderTexture(Camera camera, long timeStamp, bool useScreenShot, TrackingData.Tracking.CustomRenderTextureFormat renderFormat = TrackingData.Tracking.CustomRenderTextureFormat.ARGB32, int depthBuffer = 24, int customWidth = -1, int customHeight = -1)
        {
            if (renderTextureStack == null)
                renderTextureStack = new List<RenderTextureStack>();
            renderTextureStack.Add(new RenderTextureStack(camera, timeStamp, useScreenShot, renderFormat, depthBuffer, customWidth, customHeight));

            string captureMethod = useScreenShot ? "screenshot" : "camera render";
            string resolutionInfo = customWidth > 0 && customHeight > 0 ? $" at custom resolution {customWidth}x{customHeight}" : "";
            // Debug.Log($"Added render texture for camera: {camera.name} at time: {timeStamp} using {captureMethod} method with format: {renderFormat}, depth: {depthBuffer}{resolutionInfo} | Total render textures: {renderTextureStack.Count}");
        }

        public static byte[] GetRenderTextureBytes(long timeStamp, TrackingData.Tracking.ImageCompressionFormat compressionFormat = TrackingData.Tracking.ImageCompressionFormat.PNG, int jpgQuality = 75, TrackingData.Tracking.CustomRenderTextureFormat textureFormat = TrackingData.Tracking.CustomRenderTextureFormat.RGBA32)
        {
            RenderTextureStack render;
            render = renderTextureStack.FirstOrDefault(x => x.timeStamp == timeStamp);
            if (render == null)
            {
                // Debug.Log($"No render texture found for time stamp: {timeStamp}");
                return null;
            }
            return render.GetBytes(compressionFormat, jpgQuality, textureFormat);
        }

        public static Texture2D BytesToTexture2D(byte[] bytes, int width, int height)
        {
            var temp = new Texture2D(width, height, TextureFormat.RGBA32, false);
            temp.LoadImage(bytes);
            return temp;
        }

        public static RenderTexture BytesToRenderTexture(byte[] bytes, int width, int height)
        {
            var renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            renderTexture.Create();

            var temp = new Texture2D(width, height, TextureFormat.RGBA32, false);
            temp.LoadImage(bytes);

            var prev = RenderTexture.active;
            RenderTexture.active = renderTexture;
            Graphics.Blit(temp, renderTexture);
            RenderTexture.active = prev;

            Object.DestroyImmediate(temp);
            return renderTexture;
        }

        public static void CleanupRenderTextures()
        {
            if (renderTextureStack != null)
            {
                foreach (var rt in renderTextureStack)
                {
                    if (rt != null)
                    {
                        rt.Cleanup();
                    }
                }
                renderTextureStack.Clear();
            }
        }
    }
}
#endif