using UnityEngine;

// Enum để phân loại vật phẩm
public enum ItemType { Equipment, Consumable, Material, Quest }

// Dùng [System.Serializable] để nó có thể hiển thị trong Inspector của ItemDatabase
[System.Serializable]
public class ItemDefinition : ScriptableObject
{
    [Header("General Info")]
    public string ItemID; // Mã định danh duy nhất, ví dụ: "SWD_001", "POT_002"
    public string Name;
    [TextArea] public string Description;
    public Sprite Icon;
    public ItemType Type;

    [Header("Stacking")]
    public int MaxStack = 1;

    [Header("Stats & Effects (Chỉ dùng cho Equipment/Consumable)")]
    public EquipmentModifier[] Modifiers;
}

[System.Serializable]
public struct EquipmentModifier
{
    public StatType TargetStat;
    public float Value;
    public ModifierType Type;
}