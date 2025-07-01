using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector; // Giả sử bạn vẫn dùng Odin

public class StatController : SerializedMonoBehaviour
{
    [Title("Character Stats")]
    public Dictionary<StatType, CharacterStat> Stats = new Dictionary<StatType, CharacterStat>();

    [Title("Character Vitals")]
    public int Level = 1;
    // Dòng 'public int StatPoints = 5;' thừa thãi đã được XÓA.
    public float Experience = 0;

    [ShowInInspector, ReadOnly]
    public float ExpToNextLevel => GetExpNeededForLevel(Level);

    #region Events
    public event Action<StatType, float> OnStatChanged;
    public event Action OnLevelUp;
    public event Action<int> OnStatPointsChanged;
    public event Action<float, float> OnExperienceChanged;

    public float CurrentHealth { get; private set; }
    public float CurrentMana { get; private set; }
    public float CurrentStamina { get; private set; }

    public event Action<float, float> OnHealthChanged; // current, max
    public event Action<float, float> OnManaChanged;
    public event Action<float, float> OnStaminaChanged;
    #endregion

    // GIỜ ĐÂY CHỈ CÓ MỘT NGUỒN DUY NHẤT CHO STAT POINTS
    [ShowInInspector] // Hiển thị giá trị hiện tại trong Inspector (nhờ Odin)
    public int StatPoints { get; private set; } = 5; // Khởi tạo giá trị mặc định tại đây

    private readonly List<StatType> distributableStats = new List<StatType>
    {
        StatType.Strength, StatType.Intelligence, StatType.Vitality, StatType.Agility, StatType.Dexterity,
    };

    #region Unity Lifecycle
    void Awake()
    {
        // Mình đã bỏ việc gán `_statPoints` ở đây vì đã gán giá trị mặc định ở trên
        InitializeStats();
        LinkDerivedStats();
    }

    void Start()
    {
        foreach (var pair in Stats)
        {
            OnStatChanged?.Invoke(pair.Key, pair.Value.Value);
        }
        OnStatPointsChanged?.Invoke(StatPoints);
        OnExperienceChanged?.Invoke(Experience, ExpToNextLevel);
    }
    #endregion

    #region Initialization
    private void InitializeStats()
    {
        foreach (StatType type in Enum.GetValues(typeof(StatType)))
        {
            if (Stats.ContainsKey(type) == false)
            {
                Stats.Add(type, new CharacterStat());
            }
        }
        foreach (StatType type in distributableStats)
        {
            Stats[type].BaseValue = 5;
        }
        RecalculateAllDerivedStats();
    }

    private void LinkDerivedStats()
    {
        OnStatChanged += (type, value) =>
        {
            switch (type)
            {
                case StatType.Vitality:
                    RecalculateMaxHealth();
                    RecalculateMaxStamina();
                    RecalculateHealthRegen();
                    break;
                case StatType.Strength:
                    RecalculateMaxHealth();
                    RecalculatePhysicalDamageBonus();
                    RecalculateCarryWeight();
                    break;
                case StatType.Intelligence:
                    RecalculateMaxMana();
                    RecalculateMagicalDamageBonus();
                    break;
                case StatType.Agility:
                    RecalculateMaxStamina();
                    RecalculateStaminaRegen();
                    RecalculateAttackSpeed();
                    RecalculateDodgeChance();
                    break;
                case StatType.Wisdom:
                    RecalculateMaxMana();
                    RecalculateManaRegen();
                    break;
                case StatType.Dexterity:
                    RecalculateCritChance();
                    RecalculateCritDamage();
                    break;
                case StatType.Luck:
                    RecalculateCritChance();
                    break;
            }
        };

        OnLevelUp += () =>
        {
            StatPoints += 5;
            RecalculateAllDerivedStats();
        };
    }
    #endregion

    #region Calculation Methods
    public void RecalculateAllDerivedStats()
    {
        RecalculateMaxHealth();
        RecalculateMaxMana();
        RecalculateMaxStamina();
        RecalculateHealthRegen();
        RecalculateManaRegen();
        RecalculateStaminaRegen();
        RecalculatePhysicalDamageBonus();
        RecalculateMagicalDamageBonus();
        RecalculateCritChance();
        RecalculateCritDamage();
        RecalculateAttackSpeed();
        RecalculateDodgeChance();
        RecalculateCarryWeight();
    }

    // Các hàm tính toán mới dựa trên bảng công thức của bạn
    private void RecalculateMaxHealth()
    {
        float vit = GetStatValue(StatType.Vitality);
        float newValue = (vit * 15) + (Level * 10);
        SetStatBaseValueSilent(StatType.MaxHealth, newValue);
    }
    private void RecalculateMaxMana()
    {
        float intel = GetStatValue(StatType.Intelligence);
        float wis = GetStatValue(StatType.Wisdom);
        float newValue = (intel * 10) + (wis * 5) + (Level * 5);
        SetStatBaseValueSilent(StatType.MaxMana, newValue);
    }
    private void RecalculateMaxStamina()
    {
        float vit = GetStatValue(StatType.Vitality);
        float agi = GetStatValue(StatType.Agility);
        float newValue = 100 + (vit * 2) + (agi * 3);
        SetStatBaseValueSilent(StatType.MaxStamina, newValue);
    }
    private void RecalculateHealthRegen()
    {
        float vit = GetStatValue(StatType.Vitality);
        float newValue = vit * 0.1f;
        SetStatBaseValueSilent(StatType.HealthRegen, newValue);
    }
    private void RecalculateManaRegen()
    {
        float wis = GetStatValue(StatType.Wisdom);
        float newValue = wis * 0.2f;
        SetStatBaseValueSilent(StatType.ManaRegen, newValue);
    }
    private void RecalculateStaminaRegen()
    {
        float agi = GetStatValue(StatType.Agility);
        float newValue = 15 + (agi * 0.1f);
        SetStatBaseValueSilent(StatType.StaminaRegen, newValue);
    }
    private void RecalculatePhysicalDamageBonus()
    {
        float str = GetStatValue(StatType.Strength);
        float newValue = str * 0.5f;
        SetStatBaseValueSilent(StatType.PhysicalDamageBonus, newValue);
    }
    private void RecalculateMagicalDamageBonus()
    {
        float intel = GetStatValue(StatType.Intelligence);
        float newValue = intel * 0.5f;
        SetStatBaseValueSilent(StatType.MagicalDamageBonus, newValue);
    }
    private void RecalculateCritChance()
    {
        float dex = GetStatValue(StatType.Dexterity);
        float luk = GetStatValue(StatType.Luck);
        float newValue = 5 + (dex * 0.08f) + (luk * 0.04f);
        SetStatBaseValueSilent(StatType.CritChance, newValue);
    }
    private void RecalculateCritDamage()
    {
        float dex = GetStatValue(StatType.Dexterity);
        float newValue = 150 + (dex * 0.15f);
        SetStatBaseValueSilent(StatType.CritDamage, newValue);
    }
    private void RecalculateAttackSpeed()
    {
        float agi = GetStatValue(StatType.Agility);
        float newValue = agi * 0.2f;
        SetStatBaseValueSilent(StatType.AttackSpeed, newValue);
    }
    private void RecalculateDodgeChance()
    {
        float agi = GetStatValue(StatType.Agility);
        float newValue = agi * 0.05f;
        SetStatBaseValueSilent(StatType.DodgeChance, newValue);
    }
    private void RecalculateCarryWeight()
    {
        float str = GetStatValue(StatType.Strength);
        float newValue = 50 + (str * 2.5f);
        SetStatBaseValueSilent(StatType.CarryWeight, newValue);
    }

    #endregion

    #region Public API
    public CharacterStat GetStat(StatType type)
    {
        Stats.TryGetValue(type, out CharacterStat stat);
        return stat;
    }

    public float GetStatValue(StatType type)
    {
        return GetStat(type)?.Value ?? 0;
    }

    // --- LOGIC MỚI: TÍNH TOÁN CHI PHÍ VÀ CỘNG ĐIỂM ---
    public int GetCostToIncreaseStat(StatType type)
    {
        if (!distributableStats.Contains(type)) return int.MaxValue; // Không thể cộng điểm

        float currentBaseValue = GetStat(type).BaseValue;
        if (currentBaseValue <= 50) return 1;
        if (currentBaseValue <= 100) return 2;
        if (currentBaseValue <= 150) return 3;
        return 4;
    }

    public bool DistributeStatPoint(StatType type, int amount = 1)
    {
        int cost = GetCostToIncreaseStat(type);
        if (StatPoints >= cost && distributableStats.Contains(type))
        {
            StatPoints -= cost;
            Stats[type].BaseValue += amount;
            OnStatChanged?.Invoke(type, Stats[type].Value);
            return true;
        }
        return false;
    }

    // --- LOGIC MỚI: KINH NGHIỆM VÀ LÊN CẤP ---
    public float GetExpNeededForLevel(int level)
    {
        return Mathf.Round(100 * Mathf.Pow(level, 1.5f));
    }

    public void AddExperience(float amount)
    {
        Experience += amount;
        float expNeeded = ExpToNextLevel;
        while (Experience >= expNeeded)
        {
            Experience -= expNeeded;
            Level++;
            OnLevelUp?.Invoke();
            expNeeded = ExpToNextLevel;
        }
        OnExperienceChanged?.Invoke(Experience, expNeeded);
    }

    // ... (các hàm Add/Remove Modifier giữ nguyên) ...
    public void AddStatModifier(StatType type, StatModifier modifier)
    {
        if (GetStat(type) != null)
        {
            GetStat(type).AddModifier(modifier);
            OnStatChanged?.Invoke(type, GetStat(type).Value);
        }
    }

    public void RemoveStatModifiersFromSource(object source)
    {
        var changedStats = new HashSet<StatType>();
        foreach (var pair in Stats)
        {
            if (pair.Value.RemoveAllModifiersFromSource(source))
            {
                changedStats.Add(pair.Key);
            }
        }
        foreach (var type in changedStats)
        {
            OnStatChanged?.Invoke(type, Stats[type].Value);
        }
    }

    // --- CÁC HÀM HELPER "IM LẶNG" VÀ "ỒN ÀO" ---
    private void SetStatBaseValueSilent(StatType type, float value)
    {
        if (GetStat(type) != null)
        {
            GetStat(type).BaseValue = value;
        }
    }

    private void SetStatBaseValue(StatType type, float value)
    {
        SetStatBaseValueSilent(type, value);
        OnStatChanged?.Invoke(type, GetStat(type).Value);
    }
    #endregion

    public bool ConsumeStamina(float amount)
    {
        if (CurrentStamina >= amount)
        {
            CurrentStamina -= amount;
            OnStaminaChanged?.Invoke(CurrentStamina, GetStatValue(StatType.MaxStamina));
            return true;
        }
        return false;
    }
}