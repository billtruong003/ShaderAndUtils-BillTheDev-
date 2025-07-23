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

        private PlayerInput _playerInput;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
        }

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

        public void UseJumpInput() => JumpWasPressed = false;
        public void UseTetherInput() => TetherButtonWasPressed = false;
    }
}