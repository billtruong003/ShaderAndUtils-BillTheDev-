using UnityEngine;

public class PortalCamera : MonoBehaviour
{
    public Transform mainCamera;
    public Transform portal;
    public Transform otherPortal;
    public Camera portalCamera;

    void Update()
    {
        // Calculate relative position
        Vector3 relativePos = portal.InverseTransformPoint(mainCamera.position);
        Vector3 newPos = otherPortal.TransformPoint(relativePos);
        portalCamera.transform.position = newPos;

        // Calculate relative rotation
        Vector3 relativeRot = portal.InverseTransformDirection(mainCamera.forward);
        Vector3 newRot = otherPortal.TransformDirection(relativeRot);
        portalCamera.transform.rotation = Quaternion.LookRotation(newRot);
    }
}