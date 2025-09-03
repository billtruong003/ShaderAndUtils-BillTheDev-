using UnityEngine;
using Sirenix.OdinInspector;

namespace Kaelia
{
    [CreateAssetMenu(fileName = "NewKeybindings", menuName = "Kaelia/Keybindings")]
    public class KeybindingSO : ScriptableObject
    {
        [TabGroup("Tabs", "Movement")]
        [BoxGroup("Tabs/Movement/Core")] public KeyCode jumpKey = KeyCode.Space;
        [BoxGroup("Tabs/Movement/Core")] public KeyCode runKey = KeyCode.LeftShift;

        [TabGroup("Tabs", "Skills")]
        [BoxGroup("Tabs/Skills/Abilities")] public KeyCode dashKey = KeyCode.E; // Changed for better layout
        [BoxGroup("Tabs/Skills/Abilities")] public KeyCode slideKey = KeyCode.LeftControl;

        [TabGroup("Tabs", "Combat")]
        [BoxGroup("Tabs/Combat/Actions")] public KeyCode lightAttackKey = KeyCode.Mouse0;
        [BoxGroup("Tabs/Combat/Actions")] public KeyCode heavyAttackKey = KeyCode.Mouse1; // For future expansion
        [BoxGroup("Tabs/Combat/Actions")] public KeyCode drawWeaponKey = KeyCode.Q;
    }
}