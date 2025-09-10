using UnityEngine;
using Sirenix.OdinInspector;

[ExecuteAlways]
[AddComponentMenu("Cinematic/Advanced Cinematic Camera")]
public class AdvancedCinematicCamera : MonoBehaviour
{
    // --- TARGET ---
    [Title("Camera Target", "Đối tượng chính mà camera sẽ theo dõi.")]
    [Required("Bạn phải gán một đối tượng Target cho camera.")]
    [SerializeField] private Transform target;

    // --- POSITION SETTINGS ---
    [TabGroup("Settings", "Position")]
    [BoxGroup("Settings/Position/Movement Settings")]
    [Tooltip("Thời gian (giây) để camera bắt kịp vị trí của mục tiêu. Giá trị càng nhỏ, camera di chuyển càng nhanh.")]
    [SuffixLabel("seconds", true)]
    [Range(0.01f, 2f)]
    [SerializeField] private float positionDampTime = 0.2f;

    [BoxGroup("Settings/Position/Movement Settings")]
    [Tooltip("Khoảng cách và hướng của camera so với mục tiêu (tọa độ cục bộ).")]
    [SerializeField] private Vector3 positionOffset = new Vector3(0f, 2f, -5f);

    // --- ROTATION SETTINGS ---
    [TabGroup("Settings", "Rotation")]
    [BoxGroup("Settings/Rotation/Rotation Settings")]
    [Tooltip("Thời gian (giây) để camera xoay theo hướng của mục tiêu. Giá trị càng nhỏ, camera xoay càng nhanh.")]
    [SuffixLabel("seconds", true)]
    [Range(0.01f, 2f)]
    [SerializeField] private float rotationDampTime = 0.1f;

    [BoxGroup("Settings/Rotation/Rotation Settings")]
    [Tooltip("Điểm lệch so với tâm của mục tiêu mà camera sẽ nhìn vào (tọa độ cục bộ).")]
    [SerializeField] private Vector3 lookAtOffset = Vector3.zero;

    // --- ADVANCED SETTINGS ---
    [TabGroup("Settings", "Advanced")]
    [BoxGroup("Settings/Advanced/Look Ahead")]
    [Tooltip("Bật/tắt tính năng camera nhìn về phía trước theo hướng di chuyển của mục tiêu.")]
    [SerializeField] private bool enableLookAhead = true;

    [BoxGroup("Settings/Advanced/Look Ahead")]
    [ShowIf("enableLookAhead")]
    [Tooltip("Hệ số quyết định camera sẽ nhìn về phía trước bao xa.")]
    [Range(0f, 10f)]
    [SerializeField] private float lookAheadFactor = 2f;

    // --- PRIVATE STATE ---
    private Vector3 positionTrackingVelocity = Vector3.zero;
    private Vector3 lastTargetPosition;

    // --- UNITY LIFECYCLE METHODS ---
    private void Start()
    {
        InitializeState();
    }

    private void LateUpdate()
    {
        if (!IsTargetValid()) return;

        TrackTarget();
        UpdateLastTargetPosition();
    }

    // --- CORE LOGIC ---
    private void TrackTarget()
    {
        UpdatePosition();
        UpdateRotation();
    }

    private void UpdatePosition()
    {
        Vector3 desiredPosition = CalculateDesiredPosition();
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionTrackingVelocity, positionDampTime);
    }

    private void UpdateRotation()
    {
        Vector3 finalLookAtPoint = CalculateLookAtPoint();
        Quaternion desiredRotation = Quaternion.LookRotation(finalLookAtPoint - transform.position);

        float angularSpeed = 1f / Mathf.Max(0.001f, rotationDampTime);
        transform.rotation = Damp(transform.rotation, desiredRotation, angularSpeed, Time.deltaTime);
    }

    // --- HELPER METHODS ---
    private void InitializeState()
    {
        if (IsTargetValid())
        {
            lastTargetPosition = target.position;
        }
    }

    private void UpdateLastTargetPosition()
    {
        if (IsTargetValid())
        {
            lastTargetPosition = target.position;
        }
    }

    private bool IsTargetValid()
    {
        return target != null;
    }

    private Vector3 CalculateDesiredPosition()
    {
        return target.position + target.TransformDirection(positionOffset);
    }

    private Vector3 CalculateLookAtPoint()
    {
        Vector3 baseLookAtPoint = GetLookAheadPosition();
        Vector3 worldOffset = target.TransformDirection(lookAtOffset);
        return baseLookAtPoint + worldOffset;
    }

    private Vector3 GetLookAheadPosition()
    {
        if (!enableLookAhead || Time.deltaTime <= 0f)
        {
            return target.position;
        }

        Vector3 targetVelocity = (target.position - lastTargetPosition) / Time.deltaTime;
        return target.position + targetVelocity * lookAheadFactor;
    }

    // --- UTILITY ---
    private Quaternion Damp(Quaternion current, Quaternion target, float angularSpeed, float deltaTime)
    {
        float t = 1f - Mathf.Exp(-angularSpeed * deltaTime);
        return Quaternion.SlerpUnclamped(current, target, t);
    }
}