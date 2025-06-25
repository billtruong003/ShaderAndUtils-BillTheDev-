using UnityEngine;
using Autohand; // Đảm bảo bạn đã import namespace của Autohand

// Bạn có thể đặt namespace này sao cho phù hợp với cấu trúc dự án của mình.
namespace YourProject.Scripts
{
    /// <summary>
    /// Lấy dữ liệu từ một PhysicsGadgetJoystick của Autohand và sử dụng nó
    /// để gọi phương thức PanWithJoystick trên một MinimapController đã được tham chiếu.
    /// </summary>
    public class JoyStickPanMap : PhysicsGadgetJoystick
    {
        [Header("Minimap Control")]
        [Tooltip("Kéo và thả GameObject có chứa script MinimapController vào đây từ Hierarchy.")]
        public MinimapController minimapController;

        void Update()
        {
            if (minimapController == null)
            {
                return;
            }

            var axis = GetValue();
            if (axis.sqrMagnitude > 0.01f)
            {
                minimapController.PanWithJoystick(axis);
            }
        }
    }
}