using System;
using System.Collections.Generic;
using UnityEngine;

public class StatController : MonoBehaviour
{
    // Dùng Dictionary để truy cập chỉ số nhanh chóng bằng StatType
    public Dictionary<StatType, CharacterStat> Stats = new Dictionary<StatType, CharacterStat>();

    // Sự kiện để thông báo cho các hệ thống khác (UI, Combat...) khi có thay đổi
    public event Action<StatType, float> OnStatChanged; // Gửi đi loại stat và giá trị mới
    public event Action OnLevelUp;

    // Các chỉ số mà người chơi có thể phân phối điểm vào
    private List<StatType> distributableStats = new List<StatType>
    {
        StatType.Strength, StatType.Intelligence, StatType.Vitality, StatType.Agility, StatType.Dexterity
    };

    // Ví dụ về dữ liệu nhân vật
    public int Level = 1;
    public int StatPoints = 5;

    void Awake()
    {
        InitializeStats();
        // Gắn các hàm tính toán vào sự kiện thay đổi của các chỉ số cơ bản
        LinkDerivedStats();
    }

    private void InitializeStats()
    {
        // Khởi tạo tất cả các chỉ số cơ bản với giá trị ban đầu (ví dụ: 5)
        foreach (StatType type in distributableStats)
        {
            Stats.Add(type, new CharacterStat(5));
        }

        // Khởi tạo các chỉ số phụ
        Stats.Add(StatType.MaxHealth, new CharacterStat());
        Stats.Add(StatType.MaxMana, new CharacterStat());
        // ... thêm các chỉ số phụ khác

        // Tính toán lại tất cả các chỉ số phụ lần đầu
        RecalculateAllDerivedStats();
    }

    // Đây là phần "Observer Pattern"
    private void LinkDerivedStats()
    {
        // Khi STR hoặc Level thay đổi, tính lại CarryWeight
        OnStatChanged += (type, value) =>
        {
            if (type == StatType.Strength) RecalculateCarryWeight();
        };

        // Khi VIT hoặc Level thay đổi, tính lại MaxHealth
        OnStatChanged += (type, value) =>
        {
            if (type == StatType.Vitality) RecalculateMaxHealth();
        };

        OnLevelUp += RecalculateAllDerivedStats; // Khi lên cấp, tính lại hết cho chắc
    }

    public void RecalculateAllDerivedStats()
    {
        RecalculateMaxHealth();
        RecalculateMaxMana();
        RecalculateCarryWeight();
        // ... gọi các hàm tính toán khác
    }

    #region Calculation Methods (Nơi hiện thực hóa công thức của bạn)

    private void RecalculateMaxHealth()
    {
        float vit = GetStatValue(StatType.Vitality);
        // Công thức: (VIT * 15) + (Level * 10)
        float newMaxHealth = (vit * 15) + (Level * 10);
        SetStatBaseValue(StatType.MaxHealth, newMaxHealth);
    }

    private void RecalculateMaxMana()
    {
        float intel = GetStatValue(StatType.Intelligence);
        // Giả sử Wisdom chưa mở khóa, nên tạm thời = 0
        float wis = Stats.ContainsKey(StatType.Wisdom) ? GetStatValue(StatType.Wisdom) : 0;
        // Công thức: (INT * 10) + (WIS * 5) + (Level * 5)
        float newMaxMana = (intel * 10) + (wis * 5) + (Level * 5);
        SetStatBaseValue(StatType.MaxMana, newMaxMana);
    }

    private void RecalculateCarryWeight()
    {
        float str = GetStatValue(StatType.Strength);
        // Công thức: 50 + (STR * 2.5)
        float newCarryWeight = 50 + (str * 2.5f);
        SetStatBaseValue(StatType.CarryWeight, newCarryWeight);
    }

    #endregion

    #region Public Helper Methods (Các hàm để bên ngoài gọi)

    public CharacterStat GetStat(StatType type)
    {
        return Stats.ContainsKey(type) ? Stats[type] : null;
    }

    public float GetStatValue(StatType type)
    {
        return Stats.ContainsKey(type) ? Stats[type].Value : 0;
    }

    // Dùng để cộng điểm chỉ số
    public bool DistributeStatPoint(StatType type, int amount = 1)
    {
        if (StatPoints >= amount && distributableStats.Contains(type))
        {
            StatPoints -= amount;
            Stats[type].BaseValue += amount;
            // Phát sự kiện
            OnStatChanged?.Invoke(type, Stats[type].Value);
            return true;
        }
        return false;
    }

    // Dùng để trang bị/tháo trang bị
    public void AddStatModifier(StatType type, StatModifier modifier)
    {
        if (Stats.ContainsKey(type))
        {
            Stats[type].AddModifier(modifier);
            OnStatChanged?.Invoke(type, Stats[type].Value);
        }
    }

    public void RemoveStatModifiersFromSource(object source)
    {
        foreach (var stat in Stats.Values)
        {
            if (stat.RemoveAllModifiersFromSource(source))
            {
                // Nếu có sự thay đổi, phát sự kiện tương ứng
                // (Cần có cách map ngược từ CharacterStat về StatType)
                // Cách đơn giản là duyệt lại dictionary và phát sự kiện
            }
        }
        RecalculateAllDerivedStats(); // An toàn nhất là tính lại hết
    }

    // Hàm nội bộ để thay đổi giá trị gốc của chỉ số phụ
    private void SetStatBaseValue(StatType type, float value)
    {
        if (Stats.ContainsKey(type))
        {
            Stats[type].BaseValue = value;
            OnStatChanged?.Invoke(type, Stats[type].Value);
        }
    }

    #endregion
}