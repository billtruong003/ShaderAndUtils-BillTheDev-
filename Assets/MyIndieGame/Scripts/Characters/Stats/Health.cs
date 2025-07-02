// File: Assets/MyIndieGame/Scripts/Characters/Health.cs (Phiên bản cuối)

using UnityEngine;
using System;

public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    public float CurrentHealth { get; private set; }

    // --- CÁC SỰ KIỆN ĐƯỢC NÂNG CẤP ---
    // Gửi đi lượng sát thương và VỊ TRÍ va chạm
    public event Action<float, Vector3> OnTakeDamage;
    // Gửi đi máu hiện tại và máu tối đa
    public event Action<float, float> OnHealthChanged;
    // Sự kiện chết
    public event Action OnDie;

    public bool IsDead { get; private set; }

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    // Gọi hàm này ở Start để đảm bảo UI được cập nhật giá trị ban đầu
    private void Start()
    {
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    // --- SIGNATURE HÀM ĐÃ THAY ĐỔI ---
    public void TakeDamage(float damageAmount, Vector3 hitPoint)
    {
        if (IsDead) return;

        CurrentHealth -= damageAmount;
        if (CurrentHealth < 0) CurrentHealth = 0;

        // Kích hoạt sự kiện và gửi đi các thông tin cần thiết
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        OnTakeDamage?.Invoke(damageAmount, hitPoint);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;
        OnDie?.Invoke();
    }
}