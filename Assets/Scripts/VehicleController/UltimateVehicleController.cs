// UltimateVehicleController.cs
// PHIÊN BẢN SỬ DỤNG SCRIPTABLE OBJECT - NÂNG CẤP VÀ TỐI ƯU HÓA V2
// Gắn script này vào đối tượng gốc của xe. Yêu cầu Odin Inspector.

using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Rigidbody))]
public class UltimateVehicleController : MonoBehaviour
{
    //================================================================================
    // PROFILE & CORE COMPONENTS
    //================================================================================
    [Title("VEHICLE ARCHITECTURE", "Kiến trúc cốt lõi của xe.", TitleAlignments.Centered)]
    [InfoBox("Đây là trái tim của hệ thống. Kéo file cấu hình (VehicleProfileSO) của xe vào đây. Mọi thông số vật lý sẽ được đọc từ file này.", InfoMessageType.Info)]
    [Required("CHƯA GÁN VEHICLE PROFILE! Xe sẽ không hoạt động.")]
    [InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Boxed)]
    [SerializeField] private VehicleProfileSO profile;

    [Title("CORE COMPONENTS & FX", "Các thành phần vật lý và hiệu ứng trên đối tượng này.", TitleAlignments.Centered)]
    [Required("Chưa gán Rigidbody.")]
    [SerializeField] private Rigidbody body;
    [Tooltip("Vị trí trọng tâm của xe để tăng độ ổn định.")]
    [SerializeField] private Transform centerOfMass;
    [SerializeField] private AudioSource engineAudioSource;

    [Title("WHEELS & GROUND CHECK", "Cấu hình bánh xe để kiểm tra tiếp đất.", TitleAlignments.Centered)]
    [InfoBox("Thêm các Transform của bánh xe vào đây. Hệ thống sẽ dùng chúng để Raycast xuống đất, xác định xem xe có đang ở trên không hay không.", InfoMessageType.Info)]
    [SerializeField] private List<Transform> wheelTransforms = new List<Transform>();
    [SerializeField] private float groundCheckDistance = 0.5f;
    [SerializeField] private LayerMask groundLayer;

    //================================================================================
    // PUBLIC STATE (Read-Only)
    //================================================================================
    [Title("LIVE DATA", "Trạng thái của xe theo thời gian thực (chỉ đọc).", TitleAlignments.Centered)]
    [ShowInInspector, ReadOnly, PropertyOrder(1)]
    [ProgressBar(0, 100, r: 0.2f, g: 0.8f, b: 1f)]
    public float CurrentTurboFuel { get; private set; }
    [ShowInInspector, ReadOnly, PropertyOrder(2)]
    public bool IsDrifting { get; private set; }
    [ShowInInspector, ReadOnly, PropertyOrder(3)]
    public bool IsBoosting { get; private set; }
    [ShowInInspector, ReadOnly, PropertyOrder(4), SuffixLabel("m/s")]
    public float CurrentSpeed { get; private set; }
    [ShowInInspector, ReadOnly, PropertyOrder(5)]
    public bool IsGrounded { get; private set; }

    //================================================================================
    // INTERNAL STATE
    //================================================================================
    private float moveInput, turnInput;
    private bool isHandbraking, isTurboInput;
    private float currentGrip;
    private Vector3 lastAppliedSidewaysForce, lastAppliedAccelerationForce;

    private const float MIN_ENGINE_PITCH = 0.7f;
    private const float MAX_ENGINE_PITCH = 2.2f;

    //================================================================================
    // MONOBEHAVIOUR & CORE LOGIC
    //================================================================================
    private void Awake() => Initialize();
    private void Start() => ResetTurbo();
    private void Update() => ProcessFrameLogic();
    private void FixedUpdate() => ProcessPhysicsLogic();

    private void Initialize()
    {
        if (body == null) body = GetComponent<Rigidbody>();
        if (centerOfMass != null) body.centerOfMass = centerOfMass.localPosition;

        currentGrip = profile != null ? profile.baseLateralGrip : 10f;

        if (profile == null)
        {
            Debug.LogError($"Xe '{name}' không có VehicleProfileSO được gán và sẽ không hoạt động đúng.", this);
            enabled = false;
        }
    }

    [Button(ButtonSizes.Large), GUIColor(0.2f, 1f, 0.2f)]
    [PropertyOrder(0)]
    public void ResetTurbo()
    {
        if (profile != null) CurrentTurboFuel = profile.maxTurboFuel;
    }

    private void ProcessFrameLogic()
    {
        if (profile == null) return;
        GatherInputs();
        UpdateTurboFuel();
        UpdateEngineSound();
        CurrentSpeed = body.linearVelocity.magnitude;
    }

    private void ProcessPhysicsLogic()
    {
        if (profile == null) return;
        UpdateGroundCheck();
        Vector3 relativeVelocity = transform.InverseTransformDirection(body.linearVelocity);
        float speedRatio = Mathf.Clamp01(CurrentSpeed / profile.maxSpeed);
        IsDrifting = isHandbraking && CurrentSpeed > 10f && IsGrounded;
        if (IsGrounded)
        {
            ApplyGrip(relativeVelocity);
            ApplyAcceleration(speedRatio, relativeVelocity);
            ApplySteering(speedRatio);
            ApplyHandbrakeDrag();
        }
        ApplyAerodynamics();
    }

    //================================================================================
    // INPUT & STATE UPDATES
    //================================================================================
    private void GatherInputs()
    {
        moveInput = Input.GetAxis("Vertical");
        turnInput = Input.GetAxis("Horizontal");
        isHandbraking = Input.GetKey(KeyCode.Space);
        isTurboInput = Input.GetKey(KeyCode.LeftShift);
    }

    private void UpdateGroundCheck()
    {
        if (wheelTransforms.Count == 0)
        {
            IsGrounded = true;
            return;
        }
        IsGrounded = wheelTransforms.Any(wheel => Physics.Raycast(wheel.position, -transform.up, groundCheckDistance, groundLayer));
    }

    private void UpdateTurboFuel()
    {
        IsBoosting = isTurboInput && CurrentTurboFuel > 0 && moveInput > 0.1f;
        if (IsBoosting)
        {
            CurrentTurboFuel -= profile.turboDrainRate * Time.deltaTime;
        }
        else if (CurrentTurboFuel < profile.maxTurboFuel)
        {
            CurrentTurboFuel += profile.turboRegenRate * Time.deltaTime;
        }
        CurrentTurboFuel = Mathf.Clamp(CurrentTurboFuel, 0, profile.maxTurboFuel);
    }

    //================================================================================
    // PHYSICS APPLICATION
    //================================================================================
    private void ApplyGrip(Vector3 relativeVelocity)
    {
        float targetGrip = IsDrifting ? profile.baseLateralGrip * profile.driftGripMultiplier : profile.baseLateralGrip;
        currentGrip = Mathf.Lerp(currentGrip, targetGrip, Time.fixedDeltaTime * 10f);
        float lateralVelocity = relativeVelocity.x;
        lastAppliedSidewaysForce = -transform.right * (lateralVelocity * currentGrip * profile.gripFactor);
        body.AddForce(lastAppliedSidewaysForce, ForceMode.Force);
    }

    private void ApplyAcceleration(float speedRatio, Vector3 relativeVelocity)
    {
        float acceleration = profile.accelerationCurve.Evaluate(speedRatio) * profile.maxEnginePower;
        if (IsBoosting)
        {
            acceleration *= profile.turboForce / profile.maxEnginePower;
        }
        float forwardInput = Mathf.Clamp(moveInput, -1f, 1f);
        Vector3 force = transform.forward * acceleration * forwardInput;
        // Prevent applying forward force if exceeding max speed
        if (forwardInput > 0 && CurrentSpeed >= profile.maxSpeed)
        {
            force = Vector3.zero;
        }
        lastAppliedAccelerationForce = force;
        body.AddForce(force, ForceMode.Force);
    }

    private void ApplySteering(float speedRatio)
    {
        float steeringMultiplier = profile.steeringCurve.Evaluate(speedRatio);
        float currentTurnTorque = profile.turnTorque * steeringMultiplier * turnInput;

        if (IsDrifting)
        {
            if (Mathf.Abs(turnInput) > 0.1f)
            {
                currentTurnTorque += profile.driftInitiationBoost * turnInput;
            }

            float counterSteerTorque = -body.angularVelocity.y * (profile.turnTorque * profile.counterSteerFactor);
            body.AddTorque(transform.up * counterSteerTorque, ForceMode.Acceleration);
        }

        body.AddTorque(transform.up * currentTurnTorque, ForceMode.Force);
    }

    private void ApplyHandbrakeDrag()
    {
        if (!isHandbraking) return;
        body.AddForce(-body.linearVelocity.normalized * profile.handbrakeDragForce, ForceMode.Force);
    }

    private void ApplyAerodynamics()
    {
        float speedSquared = body.linearVelocity.sqrMagnitude;
        Vector3 dragForce = -body.linearVelocity.normalized * speedSquared * profile.dragCoefficient;
        Vector3 downforce = -transform.up * speedSquared * profile.downforceCoefficient;
        body.AddForce(dragForce + downforce, ForceMode.Force);
    }

    //================================================================================
    // VISUAL & AUDIO EFFECTS
    //================================================================================
    private void UpdateEngineSound()
    {
        if (engineAudioSource == null || profile == null) return;
        float speedRatio = CurrentSpeed / profile.maxSpeed;
        float pitchFromSpeed = Mathf.Lerp(MIN_ENGINE_PITCH, MAX_ENGINE_PITCH, speedRatio);
        float targetPitch = IsGrounded ? Mathf.Lerp(pitchFromSpeed, MAX_ENGINE_PITCH, Mathf.Abs(moveInput) * 0.5f) : MIN_ENGINE_PITCH;
        if (IsBoosting) targetPitch *= 1.25f;
        if (IsDrifting) targetPitch *= 1.1f;
        engineAudioSource.pitch = Mathf.MoveTowards(engineAudioSource.pitch, targetPitch, Time.deltaTime * 5f);
    }

    //================================================================================
    // DEBUG GIZMOS
    //================================================================================
    private void OnDrawGizmosSelected()
    {
        if (wheelTransforms.Count > 0)
        {
            Gizmos.color = Color.yellow;
            foreach (var wheel in wheelTransforms)
            {
                Gizmos.DrawLine(wheel.position, wheel.position - transform.up * groundCheckDistance);
            }
        }
        if (!Application.isPlaying || body == null) return;
        Gizmos.matrix = transform.localToWorldMatrix;
        Vector3 center = body.centerOfMass;
        float forceScale = 0.0001f;
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(center, center + transform.InverseTransformDirection(body.linearVelocity));
        Gizmos.color = IsBoosting ? Color.cyan : Color.green;
        Gizmos.DrawLine(center, center + lastAppliedAccelerationForce * forceScale);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(center, center + lastAppliedSidewaysForce * forceScale);
    }
}