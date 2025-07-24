using UnityEngine;

namespace Orion
{
    public static class MovementUtilities
    {
        public static Vector3 GetCameraRelativeMoveDirection(Transform cameraTransform, Vector2 moveInput)
        {
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;

            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            return (forward * moveInput.y + right * moveInput.x).normalized;
        }
    }
}