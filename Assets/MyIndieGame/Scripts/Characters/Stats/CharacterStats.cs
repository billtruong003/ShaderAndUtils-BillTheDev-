using System;
using System.Collections.Generic;

[System.Serializable]
public class CharacterStat
{
    // Giá trị gốc của chỉ số (từ tăng cấp, điểm phân phối)
    public float BaseValue;

    // Thuộc tính (property) chỉ đọc để lấy giá trị cuối cùng sau khi đã tính toán
    public float Value
    {
        get
        {
            if (isDirty || BaseValue != lastBaseValue)
            {
                lastBaseValue = BaseValue;
                _value = CalculateFinalValue();
                isDirty = false;
            }
            return _value;
        }
    }

    private bool isDirty = true; // Cờ để biết khi nào cần tính toán lại
    private float _value;
    private float lastBaseValue;

    // Danh sách các modifier đang ảnh hưởng đến chỉ số này
    private readonly List<StatModifier> statModifiers;

    public CharacterStat(float baseValue = 0)
    {
        BaseValue = baseValue;
        statModifiers = new List<StatModifier>();
    }

    public void AddModifier(StatModifier mod)
    {
        isDirty = true;
        statModifiers.Add(mod);
    }

    public void RemoveModifier(StatModifier mod)
    {
        isDirty = true;
        statModifiers.Remove(mod);
    }

    // Rất quan trọng: Xóa tất cả modifier từ một nguồn cụ thể
    // Ví dụ: khi người chơi cởi một món đồ, ta xóa tất cả modifier từ món đồ đó
    public bool RemoveAllModifiersFromSource(object source)
    {
        int numRemovals = statModifiers.RemoveAll(mod => mod.Source == source);

        if (numRemovals > 0)
        {
            isDirty = true;
            return true;
        }
        return false;
    }

    private float CalculateFinalValue()
    {
        float finalValue = BaseValue;

        // Tính các modifier dạng Flat trước
        for (int i = 0; i < statModifiers.Count; i++)
        {
            if (statModifiers[i].Type == ModifierType.Flat)
            {
                finalValue += statModifiers[i].Value;
            }
        }

        // Tính các modifier dạng Percent sau
        float percentSum = 0;
        for (int i = 0; i < statModifiers.Count; i++)
        {
            if (statModifiers[i].Type == ModifierType.Percent)
            {
                percentSum += statModifiers[i].Value;
            }
        }

        finalValue *= 1 + (percentSum / 100f);

        return (float)Math.Round(finalValue, 4);
    }
}