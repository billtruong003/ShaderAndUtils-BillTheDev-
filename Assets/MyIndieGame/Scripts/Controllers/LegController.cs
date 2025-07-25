using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LegController : MonoBehaviour
{
    [SerializeField] private Transform ikTarget;

    private SpiderController controller;
    private Vector3 originalLocalHomePosition; // Lưu vị trí gốc, không thay đổi

    public Vector3 CurrentPosition { get; private set; }
    public Vector3 CurrentNormal { get; private set; }
    public bool IsMoving { get; private set; }

    /// <summary>
    /// Thuộc tính động: Tính toán vị trí nghỉ lý tưởng của chân trong không gian thế giới.
    /// Giá trị này sẽ luôn được cập nhật khi StanceRadiusOffset thay đổi.
    /// </summary>
    public Vector3 IdealHomeWorldPosition
    {
        get
        {
            if (controller == null) return transform.position;

            // Tính toán hướng đẩy ra từ tâm dựa trên vị trí gốc
            Vector3 directionFromCenter = new Vector3(originalLocalHomePosition.x, 0, originalLocalHomePosition.z).normalized;

            // Áp dụng offset vào vị trí gốc để có vị trí local mới
            Vector3 dynamicLocalHome = originalLocalHomePosition + directionFromCenter * controller.StanceRadiusOffset;

            // Chuyển đổi vị trí local lý tưởng đó sang không gian thế giới
            return controller.transform.TransformPoint(dynamicLocalHome);
        }
    }

    public void Initialize(SpiderController owner, int index)
    {
        controller = owner;
        originalLocalHomePosition = transform.localPosition; // Chỉ lưu vị trí gốc một lần

        FindGroundTarget(transform.position, out Vector3 initialTarget, out Vector3 initialNormal);
        CurrentPosition = initialTarget;
        CurrentNormal = initialNormal;
        ikTarget.position = CurrentPosition;
        ikTarget.up = CurrentNormal;
    }

    public float GetDistanceFromHome()
    {
        return Vector3.Distance(CurrentPosition, IdealHomeWorldPosition);
    }

    public bool NeedsToStep()
    {
        return GetDistanceFromHome() > controller.StepDistanceThreshold;
    }

    public IEnumerator TakeStep()
    {
        IsMoving = true;

        Vector3 startPoint = CurrentPosition;
        Vector3 startNormal = CurrentNormal;

        FindStepTarget(out Vector3 targetPoint, out Vector3 targetNormal);

        float timer = 0f;
        while (timer < controller.StepDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / controller.StepDuration);
            float easedProgress = 1 - Mathf.Pow(1 - progress, 3); // EaseOutCubic

            Vector3 stepPosition = Vector3.Lerp(startPoint, targetPoint, easedProgress);
            stepPosition.y += Mathf.Sin(progress * Mathf.PI) * controller.StepHeight;

            ikTarget.position = stepPosition;
            ikTarget.up = Vector3.Slerp(startNormal, targetNormal, easedProgress);

            yield return null;
        }

        CurrentPosition = targetPoint;
        CurrentNormal = targetNormal;
        ikTarget.position = CurrentPosition;
        ikTarget.up = CurrentNormal;

        IsMoving = false;
    }

    private void FindStepTarget(out Vector3 targetPoint, out Vector3 targetNormal)
    {
        Vector3 idealPosition = IdealHomeWorldPosition; // Sử dụng thuộc tính động
        Vector3 stepPrediction = controller.GetVelocity() * controller.StepDuration * controller.StepPredictionMultiplier;

        Vector3 searchOrigin = idealPosition + stepPrediction;
        FindGroundTarget(searchOrigin, out targetPoint, out targetNormal);
    }

    private void FindGroundTarget(Vector3 origin, out Vector3 targetPoint, out Vector3 targetNormal)
    {
        Ray ray = new Ray(origin + Vector3.up * controller.RaycastHeight, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, controller.RaycastDistance, controller.GroundLayer))
        {
            targetPoint = hit.point;
            targetNormal = hit.normal;
        }
        else
        {
            targetPoint = origin;
            targetNormal = Vector3.up;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || controller == null) return;

        Vector3 idealPosition = IdealHomeWorldPosition; // Luôn lấy vị trí mới nhất để vẽ Gizmos

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(idealPosition, 0.1f);
        Gizmos.DrawLine(CurrentPosition, idealPosition);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(idealPosition, controller.StepDistanceThreshold);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(CurrentPosition, 0.15f);
        Gizmos.DrawRay(CurrentPosition, CurrentNormal * 0.5f);
    }
}