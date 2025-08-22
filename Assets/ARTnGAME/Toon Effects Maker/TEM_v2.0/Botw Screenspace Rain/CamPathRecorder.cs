#if UNITY_EDITOR
using UnityEngine;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using System.IO;

[RequireComponent(typeof(Camera))]
public class CameraPathRecorderLeanTween : MonoBehaviour
{
    public Transform[] pathWaypoints;
    public float totalPathDuration = 10.0f;
    public LeanTweenType easeType = LeanTweenType.easeInOutSine;
    public string outputFileName = "CameraMovement_LeanTween.mp4";

    private RecorderController recorderController;
    private bool isRecordingAndMoving = false;

    [ContextMenu("Execute Path Movement and Recording with LeanTween")]
    public void StartPathMovementAndRecording()
    {
        if (isRecordingAndMoving)
        {
            Debug.LogWarning("Camera is already moving and recording.");
            return;
        }

        if (pathWaypoints == null || pathWaypoints.Length < 2)
        {
            Debug.LogError("Path requires at least two waypoints.");
            return;
        }

        isRecordingAndMoving = true;
        InitializeRecorder();
        BeginSequence();
    }

    private void InitializeRecorder()
    {
        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        recorderController = new RecorderController(controllerSettings);

        var movieRecorderSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movieRecorderSettings.name = "Game View Recorder (LeanTween)";
        movieRecorderSettings.Enabled = true;
        movieRecorderSettings.VideoBitRateMode = UnityEditor.VideoBitrateMode.High;

        string fullPath = System.IO.Path.Combine(Application.dataPath, "..", outputFileName);
        movieRecorderSettings.OutputFile = System.IO.Path.GetFullPath(fullPath);

        movieRecorderSettings.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = 3840,
            OutputHeight = 2160
        };

        movieRecorderSettings.AudioInputSettings.PreserveAudio = true;

        controllerSettings.AddRecorderSettings(movieRecorderSettings);
        controllerSettings.SetRecordModeToManual();
        controllerSettings.FrameRate = 60.0f;

        RecorderOptions.VerboseMode = false;
    }

    private void BeginSequence()
    {
        recorderController.PrepareRecording();
        recorderController.StartRecording();

        transform.position = pathWaypoints[0].position;
        transform.rotation = pathWaypoints[0].rotation;

        MoveToNextWaypoint(1);
    }

    private void MoveToNextWaypoint(int waypointIndex)
    {
        if (waypointIndex >= pathWaypoints.Length)
        {
            FinalizeRecording();
            return;
        }

        Transform targetWaypoint = pathWaypoints[waypointIndex];
        float segmentDuration = totalPathDuration / (pathWaypoints.Length - 1);

        LeanTween.move(gameObject, targetWaypoint.position, segmentDuration)
            .setEase(easeType);

        LeanTween.rotate(gameObject, targetWaypoint.rotation.eulerAngles, segmentDuration)
            .setEase(easeType)
            .setOnComplete(() => MoveToNextWaypoint(waypointIndex + 1));
    }

    private void FinalizeRecording()
    {
        recorderController.StopRecording();
        isRecordingAndMoving = false;
        Debug.Log($"Recording finished. Video saved to: {Path.GetFullPath(Path.Combine(Application.dataPath, "..", outputFileName))}");
    }
}
#endif