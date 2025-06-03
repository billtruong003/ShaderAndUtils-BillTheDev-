using UnityEngine;

[ExecuteAlways]
public class ThirdPersonCameraFollow : MonoBehaviour
{
    public Transform target; // Nhân vật để camera theo dõi
    public float distance = 5f; // Khoảng cách từ camera đến nhân vật
    public float height = 2f; // Độ cao của camera so với nhân vật
    public float rotationDamping = 5f; // Tốc độ mượt mà của camera
    public Vector3 offset = new Vector3(1f, 0f, 0f); // Offset để nhìn từ một góc
    public float defaultAngleY = 20f; // Góc nhìn dọc mặc định (nhìn từ trên xuống một chút)

    private float currentX; // Góc xoay ngang (tự động)

    void Start()
    {
        // Khởi tạo góc ban đầu dựa trên hướng của nhân vật
        if (target != null)
        {
            currentX = target.eulerAngles.y;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Cập nhật góc xoay ngang dựa trên hướng của nhân vật
        float targetAngle = target.eulerAngles.y;
        currentX = Mathf.LerpAngle(currentX, targetAngle, rotationDamping * Time.deltaTime);

        // Tính toán góc quay và vị trí của camera
        Quaternion rotation = Quaternion.Euler(defaultAngleY, currentX, 0f);
        Vector3 negDistance = new Vector3(0f, 0f, -distance);
        Vector3 desiredPosition = target.position + rotation * negDistance + offset;

        // Làm mượt chuyển động của camera
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * rotationDamping);

        // Luôn nhìn vào target với độ cao và offset
        transform.LookAt(target.position + Vector3.up * height + offset);
    }
}