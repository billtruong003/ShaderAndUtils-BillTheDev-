using UnityEngine;
using Sirenix.OdinInspector;

namespace BillTheDev.QuickOutline
{

    [CreateAssetMenu(fileName = "New Outline Configuration", menuName = "BillTheDev/Outline Configuration")]
    public class OutlineConfiguration : ScriptableObject
    {

        [Title("Outline Settings")]
        public Outline.Mode outlineMode = Outline.Mode.OutlineAll;

        [ColorPalette]
        public Color outlineColor = Color.green;

        [Range(0f, 10f)]
        public float outlineWidth = 4f;
    }
}