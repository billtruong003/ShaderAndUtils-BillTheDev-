[System.Serializable]
public class StatModifier
{
    public float Value;
    public ModifierType Type;
    public readonly object Source;

    public StatModifier(float value, ModifierType type, object source = null)
    {
        Value = value;
        Type = type;
        Source = source;
    }

    // NOTE: 'object Source' sẽ cần một cơ chế tùy chỉnh nếu bạn muốn lưu/tải game.
}

public enum ModifierType
{
    Flat,
    Percent,

    // FUTURE: Có thể mở rộng với kiểu nhân dồn (Multiplicative) nếu cần.
    // PercentMult,
}