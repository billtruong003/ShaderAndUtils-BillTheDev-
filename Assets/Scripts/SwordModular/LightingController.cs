using UnityEngine;

[AddComponentMenu("Environment/Lighting Controller")]
public class LightingController : MonoBehaviour
{
    [Tooltip("Ánh sáng Directional Light chính trong scene. Nếu bỏ trống, script sẽ tự động tìm.")]
    [SerializeField] private Light mainDirectionalLight;

    private float currentYRotation = 0f;

    void Awake()
    {
        // Nếu không gán sẵn, tự động tìm Directional Light đầu tiên
        if (mainDirectionalLight == null)
        {
            foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional)
                {
                    mainDirectionalLight = light;
                    break;
                }
            }
        }

        if (mainDirectionalLight != null)
        {
            // Lưu lại góc xoay ban đầu
            currentYRotation = mainDirectionalLight.transform.eulerAngles.y;
        }
        else
        {
            Debug.LogWarning("Không tìm thấy Directional Light trong scene. Lighting Controller sẽ không hoạt động.", this);
            enabled = false;
        }
    }

    /// <summary>
    /// API công khai để UI có thể gọi và đặt góc xoay.
    /// </summary>
    /// <param name="yAngle">Góc xoay theo trục Y, từ 0 đến 360 độ.</param>
    public void SetLightRotation(float yAngle)
    {
        if (mainDirectionalLight == null) return;

        currentYRotation = yAngle;

        // Lấy góc xoay hiện tại và chỉ thay đổi trục Y
        Vector3 currentAngles = mainDirectionalLight.transform.eulerAngles;
        mainDirectionalLight.transform.rotation = Quaternion.Euler(currentAngles.x, currentYRotation, currentAngles.z);
    }

    /// <summary>
    /// API công khai để UI lấy giá trị góc xoay ban đầu.
    /// </summary>
    public float GetCurrentLightRotation()
    {
        return currentYRotation;
    }
}