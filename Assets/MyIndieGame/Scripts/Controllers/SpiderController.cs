using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class SpiderController : MonoBehaviour
{
    [Header("Core Dependencies")]
    [SerializeField] private SpiderIKSettings settings; // Tham chiếu đến file cấu hình
    [SerializeField] private Transform body;
    [SerializeField] private Transform legContainer;

    private Rigidbody physicsBody;
    private float bodyVerticalOffset;
    private List<LegController> allLegs;
    private List<List<LegController>> legGroups;
    private int currentLegGroupIndex;

    // Các thuộc tính này giờ đây sẽ đọc trực tiếp từ file settings
    // Đảm bảo LegController luôn nhận được giá trị mới nhất
    public float StepDistanceThreshold => settings.StepDistanceThreshold;
    public float StepHeight => settings.StepHeight;
    public float StepDuration => settings.StepDuration;
    public LayerMask GroundLayer => settings.GroundLayer;
    public float StanceRadiusOffset => settings.StanceRadiusOffset;
    public float RaycastHeight => settings.RaycastHeight;
    public float RaycastDistance => settings.RaycastDistance;
    public float StepPredictionMultiplier => settings.StepPredictionMultiplier;

    private void Awake()
    {
        if (settings == null)
        {
            Debug.LogError("SpiderIKSettings is not assigned on " + gameObject.name);
            enabled = false;
            return;
        }

        physicsBody = GetComponent<Rigidbody>();
        physicsBody.useGravity = false;
        physicsBody.isKinematic = true;

        InitializeLegs();
        bodyVerticalOffset = body.localPosition.y;
    }

    private void Start()
    {
        StartCoroutine(UpdateLegGaitCoroutine());
    }

    private void FixedUpdate()
    {
        HandleMovementInput();
    }

    private void LateUpdate()
    {
        UpdateBodyOrientation();
    }

    private void InitializeLegs()
    {
        allLegs = legContainer.GetComponentsInChildren<LegController>().ToList();
        legGroups = new List<List<LegController>> { new List<LegController>(), new List<LegController>() };

        for (int i = 0; i < allLegs.Count; i++)
        {
            LegController leg = allLegs[i];
            leg.Initialize(this, i);
            legGroups[i % 2].Add(leg);
        }
    }

    private void HandleMovementInput()
    {
        float verticalInput = Input.GetAxis("Vertical");
        float horizontalInput = Input.GetAxis("Horizontal");

        Vector3 moveDirection = transform.forward * verticalInput * settings.MoveSpeed * Time.fixedDeltaTime;
        physicsBody.MovePosition(physicsBody.position + moveDirection);

        Quaternion turnRotation = Quaternion.Euler(0f, horizontalInput * settings.TurnSpeed * Time.fixedDeltaTime, 0f);
        physicsBody.MoveRotation(physicsBody.rotation * turnRotation);
    }

    private IEnumerator UpdateLegGaitCoroutine()
    {
        while (true)
        {
            bool isAnyLegStepping = allLegs.Any(leg => leg.IsMoving);
            if (isAnyLegStepping)
            {
                yield return null;
                continue;
            }

            List<LegController> currentGroup = legGroups[currentLegGroupIndex];
            LegController legToMove = FindLegFurthestFromHome(currentGroup);

            if (legToMove != null && legToMove.NeedsToStep())
            {
                StartCoroutine(legToMove.TakeStep());
            }

            currentLegGroupIndex = (currentLegGroupIndex + 1) % legGroups.Count;
            yield return new WaitForSeconds(0.05f);
        }
    }

    private LegController FindLegFurthestFromHome(List<LegController> group)
    {
        float maxDistance = 0f;
        LegController furthestLeg = null;

        foreach (var leg in group)
        {
            float distance = leg.GetDistanceFromHome();
            if (distance > maxDistance)
            {
                maxDistance = distance;
                furthestLeg = leg;
            }
        }
        return furthestLeg;
    }

    private void UpdateBodyOrientation()
    {
        Vector3 legCenterPoint = GetLegsCenterPoint();
        Vector3 averageNormal = GetAverageLegNormal();

        Vector3 targetBodyPosition = legCenterPoint + averageNormal * bodyVerticalOffset;
        body.position = Vector3.Lerp(body.position, targetBodyPosition, Time.deltaTime * settings.BodyOrientationSmoothing);

        Vector3 forwardDirection = Vector3.ProjectOnPlane(transform.forward, averageNormal).normalized;
        Quaternion targetBodyRotation = Quaternion.LookRotation(forwardDirection, averageNormal);
        body.rotation = Quaternion.Slerp(body.rotation, targetBodyRotation, Time.deltaTime * settings.BodyOrientationSmoothing);
    }

    private Vector3 GetLegsCenterPoint()
    {
        if (allLegs == null || allLegs.Count == 0) return transform.position;
        Vector3 center = Vector3.zero;
        foreach (var leg in allLegs) center += leg.CurrentPosition;
        return center / allLegs.Count;
    }

    private Vector3 GetAverageLegNormal()
    {
        if (allLegs == null || allLegs.Count == 0) return Vector3.up;
        Vector3 averageNormal = Vector3.zero;
        foreach (var leg in allLegs) averageNormal += leg.CurrentNormal;
        return (averageNormal / allLegs.Count).normalized;
    }

    public Vector3 GetVelocity()
    {
        return physicsBody.linearVelocity;
    }
}