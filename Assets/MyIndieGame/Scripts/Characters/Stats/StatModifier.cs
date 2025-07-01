// Cần [System.Serializable] để Unity có thể lưu và hiển thị nó trong Inspector.
[System.Serializable]
public class StatModifier
{
    public float Value; // Giá trị thay đổi
    public ModifierType Type; // Kiểu thay đổi (cộng thẳng, nhân phần trăm)

    // Nguồn của modifier này (ví dụ: "Kiếm Thép +5", "Buff Dũng Cảm")
    // Rất hữu ích cho việc debug và hiển thị chi tiết cho người chơi.
    public readonly object Source;

    public StatModifier(float value, ModifierType type, object source = null)
    {
        Value = value;
        Type = type;
        Source = source;
    }
}

// Định nghĩa các kiểu thay đổi. Thứ tự này QUAN TRỌNG cho việc tính toán.
// Flat sẽ được tính trước, sau đó mới đến Percent.
public enum ModifierType
{
    Flat, // Cộng/trừ thẳng. Ví dụ: +10 Strength
    Percent, // Cộng/trừ theo phần trăm. Ví dụ: +10% Strength
}