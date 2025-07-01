// Dùng để định danh tất cả các loại chỉ số trong game.
public enum StatType
{
    // Chỉ số cơ bản
    Strength,       // Sức mạnh (STR)
    Intelligence,   // Trí tuệ (INT)
    Vitality,       // Sống còn (VIT)
    Agility,        // Nhanh nhẹn (AGI)
    Dexterity,      // Khéo léo (DEX)

    // Chỉ số cao cấp (mở khóa sau)
    Wisdom,         // Thông thái (WIS)
    Luck,           // May mắn (LUK)
    Technique,      // Kỹ thuật (TECH)

    // Chỉ số phụ (tính toán)
    MaxHealth,      // Máu tối đa
    MaxMana,        // Mana tối đa
    MaxStamina,     // Thể lực tối đa
    HealthRegen,    // Hồi máu
    ManaRegen,      // Hồi Mana
    StaminaRegen,   // Hồi Thể lực
    PhysicalDamageBonus, // Thưởng sát thương vật lý
    MagicalDamageBonus,  // Thưởng sát thương phép
    CritChance,     // Tỉ lệ chí mạng
    CritDamage,     // Sát thương chí mạng
    AttackSpeed,    // Tốc độ đánh
    DodgeChance,    // Tỉ lệ né
    CarryWeight,    // Sức chứa

    // Bạn có thể thêm các chỉ số khác ở đây
    // Ví dụ: FireResistance, IceResistance...
}