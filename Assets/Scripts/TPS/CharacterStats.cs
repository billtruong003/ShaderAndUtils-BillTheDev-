using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    [Header("Health")]
    public float maxHP = 100f;
    protected float currentHP;

    [Header("Damage")]
    public float attackDamage = 10f;
    public float skillDamage = 20f;
    [Range(0f, 1f)] public float criticalChance = 0.1f; // Xác suất chí mạng (10%)
    public float criticalMultiplier = 2f; // Nhân đôi sát thương khi chí mạng

    [Header("Cooldown & Interval")]
    public float attackCooldown = 1f;
    public float skillCooldown = 3f;
    public float attackInterval = 0.5f;
    public float skillInterval = 1f;

    [Header("Rage System")]
    public float maxRage = 100f;
    protected float currentRage;
    public float rageGainPerMove = 5f;
    public float rageCostForSkill = 30f;

    protected float lastAttackTime;
    protected float lastSkillTime;

    protected virtual void Start()
    {
        currentHP = maxHP;
        currentRage = 0f;
        lastAttackTime = -attackCooldown;
        lastSkillTime = -skillCooldown;
    }

    public bool CanAttack()
    {
        return Time.time >= lastAttackTime + attackCooldown && Time.time >= lastAttackTime + attackInterval;
    }

    public bool CanSkill()
    {
        return Time.time >= lastSkillTime + skillCooldown && Time.time >= lastSkillTime + skillInterval && currentRage >= rageCostForSkill;
    }

    public void TakeDamage(float damage)
    {
        currentHP -= damage;
        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }
    }

    protected virtual void Die()
    {
        // Override in derived class
    }

    // Getter cho UI
    public float GetCurrentHP() => currentHP;
    public float GetMaxHP() => maxHP;
    public string GetCharacterName() => gameObject.name; // Tên nhân vật mặc định là tên GameObject
    public float GetCurrentRage() => currentRage;
    public float GetMaxRage() => maxRage;
    public float GetSkillCooldownRemaining() => Mathf.Max(0f, lastSkillTime + skillCooldown - Time.time);
    public float GetAttackCooldownRemaining() => Mathf.Max(0f, lastAttackTime + attackCooldown - Time.time);
}