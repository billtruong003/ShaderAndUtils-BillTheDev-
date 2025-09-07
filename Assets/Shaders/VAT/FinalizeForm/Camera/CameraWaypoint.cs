#if UNITY_EDITOR
using UnityEngine;
using Sirenix.OdinInspector;

public class CameraWaypoint : MonoBehaviour
{
    [BoxGroup("Segment Settings")]
    [Tooltip("Thời gian (giây) để di chuyển từ điểm này đến điểm tiếp theo.")]
    [Range(0.1f, 60f)]
    public float durationToNext = 5.0f;

    [BoxGroup("Segment Settings")]
    [Tooltip("Hàm nội suy chuyển động cho đoạn đường đến điểm tiếp theo.")]
    public LeanTweenType easeTypeToNext = LeanTweenType.easeInOutSine;

    [BoxGroup("Camera Settings")]
    [Tooltip("Góc nghiêng của camera (trục Z) tại điểm này. Sẽ được nội suy mượt mà.")]
    [Range(-45f, 45f)]
    public float tiltAngle = 0f;

    [BoxGroup("Path Shape")]
    [Tooltip("Điểm điều khiển local cho đường cong Bezier hướng tới điểm TIẾP THEO.")]
    public Vector3 controlPoint = Vector3.forward * 2;

    [BoxGroup("Path Shape")]
    [Tooltip("Điểm điều khiển local cho đường cong Bezier từ điểm TRƯỚC ĐÓ.")]
    public Vector3 inverseControlPoint = Vector3.back * 2;

    public Vector3 Position => transform.position;
    public Quaternion Rotation => transform.rotation;

    public Vector3 GetGlobalControlPoint() => Position + Rotation * controlPoint;
    public Vector3 GetGlobalInverseControlPoint() => Position + Rotation * inverseControlPoint;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.15f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(Position, GetGlobalControlPoint());
        Gizmos.DrawLine(Position, GetGlobalInverseControlPoint());
    }
}
#endif