using UnityEngine;
using System.Collections.Generic;

public class DriftFXController : MonoBehaviour
{
    [SerializeField] private UltimateVehicleController vehicleController;
    [SerializeField] private List<WheelFX> wheelsToMonitor = new List<WheelFX>();

    void Update()
    {
        HandleDriftFX();
    }

    private void HandleDriftFX()
    {
        bool isDrifting = vehicleController != null && vehicleController.IsDrifting;

        foreach (var wheel in wheelsToMonitor)
        {
            // Chỉ bật hiệu ứng nếu xe đang drift VÀ bánh xe chạm đất
            bool useEffects = isDrifting && wheel.IsGrounded();

            wheel.SetSmokeEmission(useEffects);
            wheel.SetSkidMarkEmission(useEffects);
        }
    }
}

[System.Serializable]
public class WheelFX
{
    public Transform wheelTransform;
    public ParticleSystem smokeParticles;
    public TrailRenderer skidMarkTrail;
    public float groundCheckDistance = 0.2f;

    public void SetSmokeEmission(bool state)
    {
        if (smokeParticles == null) return;
        var emission = smokeParticles.emission;
        emission.enabled = state;
    }

    public void SetSkidMarkEmission(bool state)
    {
        if (skidMarkTrail == null) return;
        skidMarkTrail.emitting = state;
    }

    public bool IsGrounded()
    {
        // Raycast xuống dưới để kiểm tra xem bánh xe có chạm đất không
        return Physics.Raycast(wheelTransform.position, -wheelTransform.up, groundCheckDistance);
    }
}