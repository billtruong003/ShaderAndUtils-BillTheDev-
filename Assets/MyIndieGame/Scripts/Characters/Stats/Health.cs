// File: Assets/MyIndieGame/Scripts/Characters/Health.cs
using UnityEngine;
using System;
using System.Collections;
using Sirenix.OdinInspector; // Sử dụng Odin để làm Inspector gọn gàng hơn

public class Health : MonoBehaviour
{
    [Title("Core Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    public float CurrentHealth { get; private set; }

    // --- CÁC SỰ KIỆN GỐC (GIỮ NGUYÊN) ---
    public event Action<float, Vector3> OnTakeDamage;
    public event Action<float, float> OnHealthChanged;
    public event Action OnDie;

    public bool IsDead { get; private set; }

    // ===================================================================
    // === CÁC TÙY CHỌN MỚI CHO TRAINING DUMMY ===
    // ===================================================================
    [TitleGroup("Training Dummy Options")]
    [Tooltip("Nếu false, máu sẽ không bao giờ xuống dưới 1 và nhân vật sẽ không chết.")]
    [SerializeField] private bool canDie = true;

    [TitleGroup("Training Dummy Options")]
    [Tooltip("Bật tính năng tự động hồi máu sau một khoảng thời gian không nhận sát thương.")]
    [SerializeField] private bool enableAutoRecovery = false;

    [TitleGroup("Training Dummy Options")]
    [ShowIf("enableAutoRecovery")] // Chỉ hiển thị nếu auto recovery được bật
    [Tooltip("Thời gian chờ (giây) sau khi nhận sát thương cuối cùng trước khi bắt đầu hồi máu.")]
    [SerializeField] private float recoveryDelay = 5f;

    [TitleGroup("Training Dummy Options")]
    [ShowIf("enableAutoRecovery")]
    [Tooltip("Lượng máu hồi mỗi giây.")]
    [SerializeField] private float recoveryRate = 10f;

    // Biến nội bộ để theo dõi thời gian hồi máu
    private float timeSinceLastHit = 0f;
    private bool isRecovering = false;
    // ===================================================================

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private void Update()
    {
        // Chỉ thực thi logic hồi máu nếu được bật và nhân vật chưa chết
        if (!enableAutoRecovery || IsDead) return;

        timeSinceLastHit += Time.deltaTime;

        if (timeSinceLastHit >= recoveryDelay && CurrentHealth < maxHealth)
        {
            isRecovering = true;
            CurrentHealth += recoveryRate * Time.deltaTime;
            CurrentHealth = Mathf.Min(CurrentHealth, maxHealth); // Đảm bảo không vượt quá máu tối đa
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }
        else
        {
            isRecovering = false;
        }
    }

    public void TakeDamage(float damageAmount, Vector3 hitPoint)
    {
        if (IsDead) return;
        // Nếu không thể chết và máu chỉ còn 1, không nhận thêm sát thương
        if (!canDie && CurrentHealth <= 1f) return;

        // Reset bộ đếm thời gian hồi máu mỗi khi nhận sát thương
        if (enableAutoRecovery)
        {
            timeSinceLastHit = 0f;
            isRecovering = false;
        }

        CurrentHealth -= damageAmount;

        // Logic bất tử: Nếu không thể chết, kẹp máu ở mức 1
        if (!canDie && CurrentHealth < 1f)
        {
            CurrentHealth = 1f;
        }
        // Logic thông thường: Kẹp máu ở mức 0
        else if (CurrentHealth < 0)
        {
            CurrentHealth = 0;
        }

        // Kích hoạt các sự kiện như cũ
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        OnTakeDamage?.Invoke(damageAmount, hitPoint);

        // Chỉ gọi Die() nếu máu bằng 0 VÀ có thể chết
        if (CurrentHealth <= 0 && canDie)
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