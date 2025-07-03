// File: EquipmentItemData.cs (Tạo mới)
using UnityEngine;

[CreateAssetMenu(fileName = "New Equipment Item", menuName = "Luminaria/Items/Equipment Item")]
public class EquipmentItemData : ItemDefinition
{
    [Header("Equipment Specific Data")]
    // Tham chiếu đến dữ liệu chiến đấu và model
    public WeaponData weaponData;
}