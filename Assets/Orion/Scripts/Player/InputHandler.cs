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
        public bool CrouchWasPressed { get; private set; }
        public bool CrouchIsHeld { get; private set; }

        // --- NEW COMBAT INPUTS ---
        public bool DrawWeaponWasPressed { get; private set; }
        public bool AttackWasPressed { get; private set; }
        public bool HeavyAttackWasPressed { get; private set; }
        public bool ParryIsHeld { get; private set; }


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

        public void OnCrouch(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                CrouchWasPressed = true;
            }
            CrouchIsHeld = context.performed;
        }

        // --- NEW COMBAT INPUT METHODS ---
        public void OnDrawWeapon(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                DrawWeaponWasPressed = true;
            }
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                AttackWasPressed = true;
            }
        }

        public void OnHeavyAttack(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                HeavyAttackWasPressed = true;
            }
        }

        public void OnParry(InputAction.CallbackContext context)
        {
            ParryIsHeld = context.performed;
        }


        public void UseJumpInput() => JumpWasPressed = false;
        public void UseTetherInput() => TetherButtonWasPressed = false;
        public void UseDashInput() => DashWasPressed = false;
        public void UseCrouchInput() => CrouchWasPressed = false;
        public void UseDrawWeaponInput() => DrawWeaponWasPressed = false;
        public void UseAttackInput() => AttackWasPressed = false;
        public void UseHeavyAttackInput() => HeavyAttackWasPressed = false;
    }
}