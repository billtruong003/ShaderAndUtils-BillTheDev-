using UnityEngine;
using UnityEngine.InputSystem;

namespace Orion
{
    public class InputHandler : MonoBehaviour
    {
        public Vector2 MoveInput { get; private set; }
        public bool JumpWasPressed { get; private set; }
        public bool JumpIsHeld { get; private set; }
        public bool TetherButtonWasPressed { get; private set; }
        public bool SprintIsHeld { get; private set; }
        public bool DashWasPressed { get; private set; }

        public void OnMove(InputAction.CallbackContext context)
        {
            MoveInput = context.ReadValue<Vector2>();
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                JumpWasPressed = true;
            }
            JumpIsHeld = context.performed;
        }

        public void OnTether(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                TetherButtonWasPressed = true;
            }
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            SprintIsHeld = context.performed;
        }

        public void OnDash(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                DashWasPressed = true;
            }
        }

        public void UseJumpInput() => JumpWasPressed = false;
        public void UseTetherInput() => TetherButtonWasPressed = false;
        public void UseDashInput() => DashWasPressed = false;
    }
}