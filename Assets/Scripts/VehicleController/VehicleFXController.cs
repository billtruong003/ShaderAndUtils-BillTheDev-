using UnityEngine;

[RequireComponent(typeof(UltimateVehicleController))]
public class VehicleFXController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private UltimateVehicleController vehicleController;

    [Header("Effects")]
    [SerializeField] private ParticleSystem[] tireSmokeParticles;
    [SerializeField] private TrailRenderer[] skidMarkTrails;

    [Header("Settings")]
    [Tooltip("Ngưỡng trượt (vận tốc ngang) để bắt đầu hiển thị hiệu ứng.")]
    [SerializeField] private float slipThreshold = 5f;
    [Tooltip("Tỷ lệ phát hạt tối thiểu của khói.")]
    [SerializeField] private float minSmokeEmission = 10f;
    [Tooltip("Tỷ lệ phát hạt tối đa của khói.")]
    [SerializeField] private float maxSmokeEmission = 200f;

    private void Awake()
    {
        if (vehicleController == null)
        {
            vehicleController = GetComponent<UltimateVehicleController>();
        }
    }

    private void Update()
    {
        UpdateDriftEffects();
    }

    private void UpdateDriftEffects()
    {
        float lateralVelocity = Mathf.Abs(transform.InverseTransformDirection(vehicleController.GetComponent<Rigidbody>().linearVelocity).x);
        bool areEffectsActive = vehicleController.IsDrifting || (vehicleController.IsGrounded && lateralVelocity > slipThreshold);

        SetTireSmoke(areEffectsActive, lateralVelocity);
        SetSkidMarks(areEffectsActive);
    }

    private void SetTireSmoke(bool active, float lateralVelocity)
    {
        foreach (var smoke in tireSmokeParticles)
        {
            var emissionModule = smoke.emission;
            if (active)
            {
                if (!smoke.isEmitting)
                {
                    smoke.Play();
                }
                float emissionRate = Mathf.Lerp(minSmokeEmission, maxSmokeEmission, lateralVelocity / (slipThreshold * 5));
                emissionModule.rateOverTime = emissionRate;
            }
            else
            {
                if (smoke.isEmitting)
                {
                    smoke.Stop();
                }
            }
        }
    }

    private void SetSkidMarks(bool active)
    {
        foreach (var trail in skidMarkTrails)
        {
            trail.emitting = active;
        }
    }
}