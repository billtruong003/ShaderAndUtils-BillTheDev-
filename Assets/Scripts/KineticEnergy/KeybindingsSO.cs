using UnityEngine;

namespace Kaelia
{
    [CreateAssetMenu(fileName = "NewKeybindings", menuName = "Kaelia/Keybindings")]
    public class KeybindingSO : ScriptableObject
    {
        [Header("Movement")]
        public KeyCode jumpKey = KeyCode.Space;
        public KeyCode runKey = KeyCode.LeftShift;

        [Header("Skills")]
        public KeyCode dashKey = KeyCode.F;
        public KeyCode slideKey = KeyCode.LeftControl;
        public KeyCode kineticPulseKey = KeyCode.Mouse0;
    }
}