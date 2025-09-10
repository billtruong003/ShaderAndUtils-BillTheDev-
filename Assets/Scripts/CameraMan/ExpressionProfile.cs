using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EP_NewExpression", menuName = "Character/Expression Profile")]
[InlineEditor]
public class ExpressionProfile : ScriptableObject
{
    /// <summary>
    /// Định nghĩa trọng số cho một Blend Shape.
    /// Được định nghĩa bên trong class cha để tăng tính đóng gói.
    /// </summary>
    [System.Serializable]
    public struct BlendShapeSetting
    {
        [HorizontalGroup("Setting", 150)]
        [ValueDropdown("@IntelligentExpressionController.GetAvailableBlendShapeNames()")]
        [HideLabel]
        public string BlendShapeName;

        [HorizontalGroup("Setting")]
        [Range(0f, 100f)]
        [HideLabel]
        public float Weight;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Event được kích hoạt mỗi khi giá trị trong ScriptableObject này thay đổi từ Inspector.
    /// Chỉ được biên dịch trong Editor.
    /// </summary>
    public static event System.Action<ExpressionProfile> OnProfileChanged;
#endif

    [InfoBox("Định nghĩa một biểu cảm bằng cách kết hợp nhiều Blend Shape với trọng số khác nhau.")]
    [ListDrawerSettings(ShowFoldout = true, AddCopiesLastElement = true)]
    [OnValueChanged("NotifyChange")]
    public List<BlendShapeSetting> ShapeSettings = new List<BlendShapeSetting>();

#if UNITY_EDITOR
    private void NotifyChange()
    {
        OnProfileChanged?.Invoke(this);
    }
#endif
}