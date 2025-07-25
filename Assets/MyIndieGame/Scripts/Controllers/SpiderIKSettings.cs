using UnityEngine;

[CreateAssetMenu(fileName = "NewSpiderIKSettings", menuName = "IK/Spider IK Settings")]
public sealed class SpiderIKSettings : ScriptableObject
{
    [Header("Movement Parameters")]
    public float MoveSpeed = 5f;
    public float TurnSpeed = 200f;

    [Header("Body Orientation")]
    public float BodyOrientationSmoothing = 8f;

    [Header("Leg Stepping Logic")]
    [Tooltip("The distance a leg's target can be from its ideal home position before it must take a step.")]
    public float StepDistanceThreshold = 0.8f;
    [Tooltip("The height of the leg's step, forming an arc.")]
    public float StepHeight = 0.5f;
    [Tooltip("The duration of a single leg step.")]
    public float StepDuration = 0.2f;
    [Tooltip("How far into the future to project a step, based on velocity.")]
    public float StepPredictionMultiplier = 1.5f;

    [Header("Stance Parameters")]
    [Tooltip("Pushes the resting position of each leg outwards from the body's center. Can be negative.")]
    public float StanceRadiusOffset = 0.0f;

    [Header("Grounding")]
    public LayerMask GroundLayer;
    public float RaycastHeight = 2f;
    public float RaycastDistance = 5f;
}