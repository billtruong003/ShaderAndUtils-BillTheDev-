using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "NewSwordPreset", menuName = "Modular Swords/Sword Preset", order = 10)]
public class SwordPresetSO : ScriptableObject
{
    [Title("Part Indices")]
    [InfoBox("Giá trị -1 có nghĩa là 'Không có' bộ phận đó.")]
    public int BladeIndex = -1;
    public int HiltIndex = -1;
    public int GripIndex = -1;

    [Title("Modifiers")]
    [Range(0, 1)] public float BladeOffsetValue = 0f;
    [Range(0, 1)] public float GripOffsetValue = 0f;
    [Range(-180, 180)] public float BladeRotationValue = 0f;
    [Range(-180, 180)] public float HiltRotationValue = 0f;
    [Range(-180, 180)] public float GripRotationValue = 0f;
}