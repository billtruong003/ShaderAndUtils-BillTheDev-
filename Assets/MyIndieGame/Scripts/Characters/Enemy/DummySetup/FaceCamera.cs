

using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    private Transform cameraTransform;

    void Start()
    {
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogWarning("FaceCamera: Không tìm thấy camera chính (Main Camera) trong scene.", this);
        }
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;
        transform.rotation = cameraTransform.rotation;
    }
}