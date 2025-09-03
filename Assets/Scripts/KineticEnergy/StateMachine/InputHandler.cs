using UnityEngine;

namespace Kaelia
{
    public class InputHandler : MonoBehaviour
    {
        // Movement
        public float Horizontal { get; private set; }
        public float Vertical { get; private set; }
        public bool JumpDown { get; private set; }
        public bool RunHeld { get; private set; }

        // Skills
        public bool DashDown { get; private set; }
        public bool SlideDown { get; private set; }
        public bool SlideUp { get; private set; }

        // Combat
        public bool LightAttackDown { get; private set; }
        public bool DrawWeaponDown { get; private set; }

        private KeybindingSO keybindings;
        private bool areInputsDisabled;

        public void Initialize(KeybindingSO bindings)
        {
            keybindings = bindings;
        }

        public void DisableInputsForDuration(float duration)
        {
            areInputsDisabled = true;
            Invoke(nameof(EnableInputs), duration);
        }

        private void EnableInputs()
        {
            areInputsDisabled = false;
        }

        void Update()
        {
            if (keybindings == null || areInputsDisabled)
            {
                ClearAllInputs();
                return;
            }

            // Movement
            Horizontal = Input.GetAxisRaw("Horizontal");
            Vertical = Input.GetAxisRaw("Vertical");
            JumpDown = Input.GetKeyDown(keybindings.jumpKey);
            RunHeld = Input.GetKey(keybindings.runKey);

            // Skills
            DashDown = Input.GetKeyDown(keybindings.dashKey);
            SlideDown = Input.GetKeyDown(keybindings.slideKey);
            SlideUp = Input.GetKeyUp(keybindings.slideKey);

            // Combat
            LightAttackDown = Input.GetKeyDown(keybindings.lightAttackKey);
            DrawWeaponDown = Input.GetKeyDown(keybindings.drawWeaponKey);
        }

        private void ClearAllInputs()
        {
            Horizontal = 0f;
            Vertical = 0f;
            JumpDown = false;
            RunHeld = false;
            DashDown = false;
            SlideDown = false;
            SlideUp = false;
            LightAttackDown = false;
            DrawWeaponDown = false;
        }
    }
}