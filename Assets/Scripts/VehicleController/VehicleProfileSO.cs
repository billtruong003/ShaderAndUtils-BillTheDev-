// VehicleProfileSO.cs
// Đặt file này ở bất kỳ đâu trong thư mục Project của bạn.

using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "NewVehicleProfile", menuName = "Vehicle System/Vehicle Profile")]
public class VehicleProfileSO : ScriptableObject
{
    [Title("VEHICLE IDENTITY", "Thông tin nhận dạng và phân loại xe.", TitleAlignments.Centered)]
    public string vehicleName = "Default Car";
    public Sprite vehicleIcon;

    [TabGroup("Tuning Tabs", "Engine")]
    [Title("Engine & Drivetrain")]
    [SuffixLabel("N")]
    public float maxEnginePower = 45000f;
    public AnimationCurve accelerationCurve = AnimationCurve.Linear(0, 1, 1, 0.8f);
    [SuffixLabel("m/s")]
    public float maxSpeed = 55f;

    [TabGroup("Tuning Tabs", "Handling")]
    [Title("Steering & Grip")]
    [SuffixLabel("Nm")]
    public float turnTorque = 30000f;
    [Tooltip("Độ bám đường ngang cơ bản của lốp xe.")]
    public float baseLateralGrip = 12f;
    [Tooltip("Hệ số nhân với vận tốc ngang để tạo ra lực bám. Đây là thông số quan trọng để kiểm soát độ 'trượt' của xe.")]
    public float gripFactor = 100f;
    public AnimationCurve steeringCurve = AnimationCurve.Linear(0, 1, 1, 0.3f);

    [TabGroup("Tuning Tabs", "Drift & Turbo")]
    [Title("Drifting Physics")]
    [Range(0, 1)]
    [Tooltip("Độ bám đường bị giảm đi bao nhiêu khi drift. Giá trị càng thấp, drift càng trơn trượt.")]
    public float driftGripMultiplier = 0.4f;
    [SuffixLabel("Nm")]
    [Tooltip("Lực xoay tức thời được thêm vào để 'bẻ gãy' độ bám và bắt đầu drift.")]
    public float driftInitiationBoost = 40000f;
    [Range(0, 1)]
    [Tooltip("Độ mạnh của hệ thống tự động phản lái để ổn định xe khi drift. Đặt về 0 để tắt.")]
    public float counterSteerFactor = 0.15f;
    [SuffixLabel("N")]
    public float handbrakeDragForce = 15000f;

    [TabGroup("Tuning Tabs", "Drift & Turbo")]
    [Title("Turbo System")]
    [SuffixLabel("N")]
    public float turboForce = 35000f;
    public float maxTurboFuel = 100f;
    public float turboDrainRate = 25f;
    public float turboRegenRate = 10f;

    [TabGroup("Tuning Tabs", "Aerodynamics")]
    [Title("Aerodynamics")]
    public float dragCoefficient = 0.45f;
    public float downforceCoefficient = 50f;
}