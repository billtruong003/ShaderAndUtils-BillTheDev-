using System;
using System.Collections.Generic;

[System.Serializable]
public class CharacterStat
{
    public float BaseValue;

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

    private bool isDirty = true;
    private float _value;
    private float lastBaseValue;

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
        float percentSum = 0;

        for (int i = 0; i < statModifiers.Count; i++)
        {
            StatModifier mod = statModifiers[i];
            if (mod.Type == ModifierType.Flat)
            {
                finalValue += mod.Value;
            }
            else if (mod.Type == ModifierType.Percent)
            {
                percentSum += mod.Value;
            }
            // FUTURE: Xử lý các ModifierType khác (như PercentMult) tại đây.
        }

        finalValue *= 1 + (percentSum / 100f);

        return (float)Math.Round(finalValue, 4);
    }
}