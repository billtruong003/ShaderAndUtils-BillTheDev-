using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Rigidbody))]
public class SmartSpiderLocomotion : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3.0f;
    [SerializeField] private float turnSpeed = 5.0f;

    [Header("Leg & Stepping Settings")]
    [SerializeField] private Transform[] legIkTargets;
    [SerializeField] private Transform[] legDefaultPositions;
    [SerializeField] private float stepDistanceThreshold = 1.2f;
    [SerializeField] private float stepHeight = 0.4f;
    [SerializeField] private float stepSpeed = 10f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody _rb;
    private Vector3 _velocity;
    private List<Leg> _legs = new List<Leg>();
    private List<Leg> _legGroupA = new List<Leg>();
    private List<Leg> _legGroupB = new List<Leg>();
    private bool _isGroupAMoving = false;
    private bool _isGroupBMoving = false;

    // Private class to encapsulate all data for a single leg
    private class Leg
    {
        public int Index;
        public Transform IkTarget;
        public Transform DefaultPosition;
        public Vector3 CurrentPosition;
        public Vector3 LastGroundedPosition;
        public bool IsMoving => _moveCoroutine != null;
        public Coroutine _moveCoroutine;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.freezeRotation = true; // We will handle rotation manually
    }

    private void Start()
    {
        InitializeLegs();
    }

    private void FixedUpdate()
    {
        MoveBody();
        HandleLegStepping();
    }

    /// <summary>
    /// Public method to command the spider to move.
    /// Call this from your Player Input or AI script.
    /// </summary>
    public void SetMovementVelocity(Vector3 velocity)
    {
        _velocity = velocity;
    }

    private void InitializeLegs()
    {
        if (legIkTargets.Length != legDefaultPositions.Length)
        {
            Debug.LogError("Leg IK Targets and Default Positions must have the same length.");
            this.enabled = false;
            return;
        }

        for (int i = 0; i < legIkTargets.Length; i++)
        {
            var leg = new Leg
            {
                Index = i,
                IkTarget = legIkTargets[i],
                DefaultPosition = legDefaultPositions[i]
            };
            leg.CurrentPosition = leg.IkTarget.position;
            leg.LastGroundedPosition = leg.IkTarget.position;

            _legs.Add(leg);

            // Alternating Tripod Gait: Even legs go to group A, odd legs go to group B
            if (i % 2 == 0) _legGroupA.Add(leg);
            else _legGroupB.Add(leg);
        }
    }

    private void MoveBody()
    {
        // Move the Rigidbody
        Vector3 worldVelocity = transform.TransformDirection(_velocity);
        _rb.linearVelocity = new Vector3(worldVelocity.x * moveSpeed, _rb.linearVelocity.y, worldVelocity.z * moveSpeed);

        // Rotate the body towards the movement direction
        if (_velocity.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(worldVelocity);
            _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRotation, Time.fixedDeltaTime * turnSpeed);
        }

        // Body stabilization (optional but recommended)
        // Raycast down from the body center
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 3f, groundLayer))
        {
            // Average position of all feet
            Vector3 feetCenter = Vector3.zero;
            foreach (var leg in _legs) feetCenter += leg.CurrentPosition;
            feetCenter /= _legs.Count;

            // Keep the body at a consistent height above the feet
            _rb.position = Vector3.Lerp(_rb.position, new Vector3(feetCenter.x, feetCenter.y + 1.0f, feetCenter.z), Time.fixedDeltaTime * 5f);
        }
    }

    private void HandleLegStepping()
    {
        // Check Group A
        if (!_isGroupAMoving && !_isGroupBMoving && ShouldGroupStep(_legGroupA))
        {
            StartCoroutine(MoveLegGroup(_legGroupA, (isMoving) => _isGroupAMoving = isMoving));
        }

        // Check Group B
        if (!_isGroupAMoving && !_isGroupBMoving && ShouldGroupStep(_legGroupB))
        {
            StartCoroutine(MoveLegGroup(_legGroupB, (isMoving) => _isGroupBMoving = isMoving));
        }
    }

    private bool ShouldGroupStep(List<Leg> group)
    {
        foreach (var leg in group)
        {
            if (Vector3.Distance(leg.CurrentPosition, leg.DefaultPosition.position) > stepDistanceThreshold)
            {
                return true;
            }
        }
        return false;
    }

    private IEnumerator MoveLegGroup(List<Leg> group, System.Action<bool> setMovingFlag)
    {
        setMovingFlag(true);

        // Start moving all legs in the group simultaneously
        List<Coroutine> moveCoroutines = new List<Coroutine>();
        foreach (var leg in group)
        {
            moveCoroutines.Add(StartCoroutine(MoveSingleLeg(leg)));
        }

        // Wait for all legs in the group to finish their step
        foreach (var coroutine in moveCoroutines)
        {
            yield return coroutine;
        }

        setMovingFlag(false);
    }

    private IEnumerator MoveSingleLeg(Leg leg)
    {
        Vector3 startPoint = leg.CurrentPosition;
        Vector3 targetPoint = FindStepTarget(leg);

        float timeElapsed = 0;
        float moveDuration = Vector3.Distance(startPoint, targetPoint) / stepSpeed;
        if (moveDuration <= 0) yield break;

        while (timeElapsed < moveDuration)
        {
            timeElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(timeElapsed / moveDuration);

            Vector3 position = Vector3.Lerp(startPoint, targetPoint, progress);
            position.y += Mathf.Sin(progress * Mathf.PI) * stepHeight;

            leg.CurrentPosition = position;
            leg.IkTarget.position = position;

            yield return null;
        }

        leg.CurrentPosition = targetPoint;
        leg.IkTarget.position = targetPoint;
    }

    private Vector3 FindStepTarget(Leg leg)
    {
        Vector3 rayOrigin = leg.DefaultPosition.position + Vector3.up * 2f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 5f, groundLayer))
        {
            return hit.point;
        }
        return leg.DefaultPosition.position;
    }
}