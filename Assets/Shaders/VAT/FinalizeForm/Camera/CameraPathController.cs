#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

[RequireComponent(typeof(Camera))]
public class CameraPathController : MonoBehaviour
{
    [TitleGroup("Path Definition")]
    [InfoBox("Tập trung hoàn toàn vào việc điều khiển chuyển động của camera. Hãy sử dụng cửa sổ Recorder của Unity để ghi hình thủ công.")]
    [ListDrawerSettings(ShowIndexLabels = true, OnBeginListElementGUI = "BeginWaypointListElement", OnEndListElementGUI = "EndWaypointListElement")]
    public List<CameraWaypoint> waypoints = new List<CameraWaypoint>();
    [TitleGroup("Path Definition")]
    public bool playOnStart = false;

    [TitleGroup("Movement State")]
    [ShowInInspector, ReadOnly, ProgressBar(0, 1)]
    private float movementProgress = 0;

    [TitleGroup("Movement State")]
    [ShowInInspector, ReadOnly]
    public string FormattedTotalDuration => System.TimeSpan.FromSeconds(TotalCalculatedDuration).ToString(@"mm\:ss\:ff");

    private float TotalCalculatedDuration => waypoints.Count > 1 && waypoints.All(w => w != null)
        ? waypoints.Take(waypoints.Count - 1).Sum(w => w.durationToNext)
        : 0;

    private bool isMoving = false;
    private LTSeq activeSequence;

    [PropertyOrder(-1)]
    [ButtonGroup("Playback Controls")]
    [Button(ButtonSizes.Large), GUIColor(0.2f, 0.8f, 0.4f)]
    [EnableIf("@!isMoving && waypoints.Count >= 2")]
    public void PlayPath()
    {
        if (!AreWaypointsValid()) return;

        isMoving = true;
        BeginPathSequence();
    }

    [PropertyOrder(-1)]
    [ButtonGroup("Playback Controls")]
    [Button(ButtonSizes.Large), GUIColor(1f, 0.5f, 0.5f)]
    [EnableIf("isMoving")]
    public void StopPath()
    {
        if (activeSequence != null)
        {
            LeanTween.cancel(activeSequence.id);
        }
        ResetToStart();
        movementProgress = 0;
        isMoving = false;
    }

    [PropertyOrder(-1)]
    [ButtonGroup("Playback Controls")]
    [Button(ButtonSizes.Large)]
    [EnableIf("@!isMoving && waypoints.Count >= 1")]
    public void GoToStart()
    {
        if (waypoints == null || waypoints.Count == 0 || waypoints[0] == null) return;
        ResetToStart();
    }

    private void ResetToStart()
    {
        transform.position = waypoints[0].Position;
        transform.rotation = waypoints[0].Rotation;
    }

    private void BeginPathSequence()
    {
        ResetToStart();

        activeSequence = LeanTween.sequence();
        float cumulativeTime = 0f;
        float totalDuration = TotalCalculatedDuration;

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            CameraWaypoint from = waypoints[i];
            CameraWaypoint to = waypoints[i + 1];
            float segmentDuration = from.durationToNext;
            float startTime = cumulativeTime;

            var tween = LeanTween.value(0f, 1f, segmentDuration)
                .setEase(from.easeTypeToNext)
                .setOnUpdate((float t) =>
                {
                    UpdateCameraTransform(t, from, to);
                    movementProgress = (startTime + (t * segmentDuration)) / totalDuration;
                });

            activeSequence.append(tween);
            cumulativeTime += segmentDuration;
        }

        activeSequence.append(() =>
        {
            isMoving = false;
            movementProgress = 1;
        });
    }

    private void UpdateCameraTransform(float t, CameraWaypoint from, CameraWaypoint to)
    {
        transform.position = GetPointOnCubicBezier(
            from.Position,
            from.GetGlobalControlPoint(),
            to.GetGlobalInverseControlPoint(),
            to.Position,
            t
        );

        Quaternion targetRotation = Quaternion.Slerp(from.Rotation, to.Rotation, t);
        float tilt = Mathf.Lerp(from.tiltAngle, to.tiltAngle, t);

        transform.rotation = targetRotation * Quaternion.Euler(0, 0, tilt);
    }

    private bool AreWaypointsValid()
    {
        if (waypoints == null || waypoints.Count < 2 || waypoints.Any(w => w == null))
        {
            EditorUtility.DisplayDialog("Lỗi đường đi", "Cần ít nhất 2 waypoints hợp lệ để bắt đầu di chuyển.", "OK");
            return false;
        }
        if (TotalCalculatedDuration <= 0)
        {
            EditorUtility.DisplayDialog("Lỗi thời gian", "Tổng thời gian di chuyển của đường đi phải lớn hơn 0.", "OK");
            return false;
        }
        return true;
    }

    private static Vector3 GetPointOnCubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        float oneMinusTSquared = oneMinusT * oneMinusT;
        float tSquared = t * t;

        return (oneMinusTSquared * oneMinusT * p0) +
               (3f * oneMinusTSquared * t * p1) +
               (3f * oneMinusT * tSquared * p2) +
               (tSquared * t * p3);
    }

    private void Awake()
    {
        if (playOnStart)
            PlayPath();
    }


    #region Odin Inspector Helpers
    private void BeginWaypointListElement(int index) => GUILayout.BeginHorizontal();
    private void EndWaypointListElement(int index)
    {
        if (index < waypoints.Count - 1 && waypoints[index] != null)
        {
            var waypoint = waypoints[index];
            string durationLabel = $"{waypoint.durationToNext:F1}s";
            string easeLabel = $"{waypoint.easeTypeToNext}";
            EditorGUILayout.LabelField(new GUIContent("→", "Duration and Ease to the next waypoint"), GUILayout.Width(20));
            EditorGUILayout.LabelField(durationLabel, GUILayout.Width(40));
            EditorGUILayout.LabelField(easeLabel, GUILayout.MinWidth(100));
        }
        GUILayout.EndHorizontal();
    }
    #endregion
}
#endif