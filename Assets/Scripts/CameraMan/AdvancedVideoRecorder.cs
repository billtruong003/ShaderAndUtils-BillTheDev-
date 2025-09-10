#if UNITY_EDITOR
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEditor.Media;
using Unity.Collections; // THÊM DÒNG NÀY để sử dụng NativeArray
using System.IO;
using System.Collections.Generic;

[AddComponentMenu("Utilities/Advanced Video Recorder")]
public class AdvancedVideoRecorder : MonoBehaviour
{
    [System.Serializable]
    public struct ResolutionPreset
    {
        public string Name;
        public uint Width;
        public uint Height;

        public override string ToString() => Name;
    }

    public enum VideoQuality { Low, Medium, High }

    private static readonly List<ResolutionPreset> resolutionPresets = new List<ResolutionPreset>
    {
        new ResolutionPreset { Name = "720p (HD)", Width = 1280, Height = 720 },
        new ResolutionPreset { Name = "1080p (Full HD)", Width = 1920, Height = 1080 },
        new ResolutionPreset { Name = "1440p (QHD)", Width = 2560, Height = 1440 },
        new ResolutionPreset { Name = "4K (UHD)", Width = 3840, Height = 2160 }
    };

    [Title("Recorder Controller")]
    [InfoBox("Sử dụng API MediaEncoder mới. Chỉ hoạt động trong Unity Editor.", InfoMessageType.Info)]
    [InfoBox("Trạng thái: Đang dừng.", InfoMessageType.Info, "IsIdle")]
    [InfoBox("ĐANG GHI HÌNH...", InfoMessageType.Error, "isRecording")]

    [HideIf("isRecording")]
    [Button("Start Recording", ButtonSizes.Large), GUIColor(0, 1, 0)]
    private void StartRecordingAction() => StartRecording();

    [ShowIf("isRecording")]
    [Button("Stop Recording", ButtonSizes.Large), GUIColor(1, 0, 0)]
    private void StopRecordingAction() => StopRecording();

    [ShowIf("isRecording")]
    [ProgressBar(0, 600, ColorGetter = "GetRecordingTimeColor", CustomValueStringGetter = "GetRecordingTimeString")]
    [SerializeField] private float recordingTime = 0f;

    [TabGroup("Settings", "General")]
    [Required]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private KeyCode recordKey = KeyCode.R;

    [TabGroup("Settings", "Output")]
    [FolderPath(ParentFolder = "Assets")]
    [SerializeField] private string outputDirectory = "Recordings";
    [SerializeField] private string baseFileName = "MyGameplay";

    [TabGroup("Settings", "Quality")]
    [ValueDropdown("resolutionPresets")]
    [SerializeField] private ResolutionPreset resolution = new ResolutionPreset { Name = "1080p (Full HD)", Width = 1920, Height = 1080 };
    [Range(24, 120)]
    [SuffixLabel("fps")]
    [SerializeField] private int frameRate = 60;
    [EnumToggleButtons]
    [SerializeField] private VideoQuality quality = VideoQuality.High;
    [SerializeField] private bool includeAudio = true;

    private MediaEncoder mediaEncoder;
    private RenderTexture captureTexture;
    private Texture2D frameTexture;
    private float frameTimer;
    private bool isRecording = false;

    // Thay đổi để sử dụng cả mảng managed và native cho audio
    private float[] audioSamplesManaged;
    private NativeArray<float> audioSamplesNative;

    private void Update()
    {
        if (Input.GetKeyDown(recordKey))
        {
            ToggleRecording();
        }

        if (isRecording)
        {
            recordingTime += Time.unscaledDeltaTime;
        }
    }

    private void LateUpdate()
    {
        if (!isRecording || mediaEncoder == null) return;

        frameTimer += Time.unscaledDeltaTime;
        float frameDuration = 1.0f / frameRate;

        if (frameTimer >= frameDuration)
        {
            CaptureFrame();
            frameTimer -= frameDuration;
        }
    }

    private void OnDisable()
    {
        if (isRecording)
        {
            StopRecording();
        }
    }

    private void ToggleRecording()
    {
        if (isRecording)
        {
            StopRecording();
        }
        else
        {
            StartRecording();
        }
    }

    private void StartRecording()
    {
        if (isRecording) return;

        InitializeResources();

        string fullPath = GetSanitizedOutputPath();

        var videoAttrs = new VideoTrackAttributes
        {
            frameRate = new MediaRational(frameRate),
            width = resolution.Width,
            height = resolution.Height,
            includeAlpha = false
        };

        if (includeAudio)
        {
            int audioSampleRate = AudioSettings.outputSampleRate;
            ushort audioChannelCount = (ushort)AudioSettings.speakerMode;
            var audioAttrs = new AudioTrackAttributes
            {
                sampleRate = new MediaRational(audioSampleRate),
                channelCount = audioChannelCount,
                language = "en"
            };
            mediaEncoder = new MediaEncoder(fullPath, videoAttrs, audioAttrs);
        }
        else
        {
            mediaEncoder = new MediaEncoder(fullPath, videoAttrs);
        }

        isRecording = true;
        recordingTime = 0f;
        frameTimer = 0f;
        Debug.Log($"Bắt đầu quay! File sẽ được lưu tại: {fullPath}");
    }

    private void StopRecording()
    {
        if (!isRecording) return;

        isRecording = false;
        CleanupResources();
        Debug.Log("Dừng quay video.");
    }

    private void InitializeResources()
    {
        captureTexture = new RenderTexture((int)resolution.Width, (int)resolution.Height, 24);
        frameTexture = new Texture2D((int)resolution.Width, (int)resolution.Height, TextureFormat.RGBA32, false);

        if (includeAudio)
        {
            int audioSampleRate = AudioSettings.outputSampleRate;
            int audioChannelCount = (int)AudioSettings.speakerMode;
            int samplesPerFrame = (audioSampleRate / frameRate) * audioChannelCount;

            audioSamplesManaged = new float[samplesPerFrame];
            audioSamplesNative = new NativeArray<float>(samplesPerFrame, Allocator.Persistent);
        }
    }

    private void CleanupResources()
    {
        mediaEncoder?.Dispose();
        mediaEncoder = null;

        if (captureTexture != null) DestroyImmediate(captureTexture);
        if (frameTexture != null) DestroyImmediate(frameTexture);

        if (audioSamplesNative.IsCreated)
        {
            audioSamplesNative.Dispose();
        }

        captureTexture = null;
        frameTexture = null;
        audioSamplesManaged = null;
    }

    private void CaptureFrame()
    {
        RenderTexture previousTarget = targetCamera.targetTexture;
        targetCamera.targetTexture = captureTexture;
        targetCamera.Render();
        targetCamera.targetTexture = previousTarget;

        RenderTexture previousActive = RenderTexture.active;
        RenderTexture.active = captureTexture;
        frameTexture.ReadPixels(new Rect(0, 0, resolution.Width, resolution.Height), 0, 0);
        frameTexture.Apply();
        RenderTexture.active = previousActive;

        mediaEncoder.AddFrame(frameTexture);

        if (includeAudio && audioSamplesNative.IsCreated)
        {
            AudioListener.GetOutputData(audioSamplesManaged, 0);
            audioSamplesNative.CopyFrom(audioSamplesManaged);
            mediaEncoder.AddSamples(audioSamplesNative);
        }
    }

    private string GetSanitizedOutputPath()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string directoryPath = Path.Combine(projectRoot, outputDirectory);

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string fileName = $"{baseFileName}_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.mp4";
        return Path.Combine(directoryPath, fileName);
    }

    #region Odin Inspector Utilities
    private bool IsIdle => !isRecording;
    private Color GetRecordingTimeColor() => Color.red;
    private string GetRecordingTimeString() => recordingTime.ToString("F1") + "s";
    #endregion
}
#endif