// File: Assets/MyIndieGame/Scripts/Weapons/WeaponController.cs
using UnityEngine;
using System;
using System.Collections;

public class WeaponController : MonoBehaviour
{
    // --- Tham chiếu ---
    [SerializeField] private PlayerAnimator playerAnimator;
    [SerializeField] private StatController statController;
    [SerializeField] private PlayerLocomotion playerLocomotion;
    [SerializeField] private TargetingController targetingController;
    [SerializeField] private EquipmentManager equipmentManager;
    [SerializeField] private CharacterHitboxController characterHitboxController;
    [SerializeField] private PlayerParticlesController particlesController;

    // --- Trạng thái ---
    public bool IsAttacking { get; private set; }
    public WeaponData CurrentWeaponData { get; private set; }
    public WeaponInstance CurrentWeaponInstance { get; private set; }
    public AttackData CurrentAttackData { get; private set; }
    public event Action OnAttackFinished;

    private IWeaponAttackStrategy currentStrategy = new NullAttackStrategy();
    private bool isAttackBuffered = false;
    private int currentComboIndex = 0;
    private Coroutine attackChainCoroutine;

    // --- Properties ---
    public PlayerAnimator Animator => playerAnimator;
    public StatController Stats => statController;
    public PlayerLocomotion Locomotion => playerLocomotion;
    public TargetingController Targeting => targetingController;
    public CharacterHitboxController CharacterHitboxes => characterHitboxController;
    public PlayerParticlesController ParticlesController => particlesController;

    // --- SỬA ĐỔI: Thêm hàm để Strategy chạy Coroutine ---
    public Coroutine StartStrategyCoroutine(IEnumerator routine) => StartCoroutine(routine);

    void Awake()
    {
        if (equipmentManager != null && equipmentManager.UnarmedWeaponData != null)
        {
            EquipWeapon(equipmentManager.UnarmedWeaponData, null);
        }
    }

    void Update()
    {
        if (IsAttacking)
        {
            currentStrategy.Tick(Time.deltaTime);
        }
    }

    public void EquipWeapon(WeaponData weaponData, WeaponInstance weaponInstance)
    {
        this.CurrentWeaponData = weaponData;
        this.CurrentWeaponInstance = weaponInstance;

        if (this.CurrentWeaponData == null)
        {
            currentStrategy = new NullAttackStrategy();
            return;
        }

        switch (weaponData.AttackStrategyType)
        {
            case AttackStrategyType.MeleeSphereCast: currentStrategy = new MeleeSphereCastAttack(); break;
            case AttackStrategyType.Projectile: currentStrategy = new ProjectileAttack(); break;
            default: currentStrategy = new NullAttackStrategy(); break;
        }
        currentStrategy.OnEquip(this);
    }

    public void RequestAttack()
    {
        if (CurrentWeaponData == null) return;

        if (IsAttacking)
        {
            isAttackBuffered = true;
        }
        else
        {
            if (attackChainCoroutine != null)
            {
                StopCoroutine(attackChainCoroutine);
            }
            currentComboIndex = 0;
            StartAttack();
        }
    }

    private void StartAttack()
    {
        CurrentAttackData = CurrentWeaponData.GetAttackData(currentComboIndex);

        if (CurrentAttackData == null) return;
        if (statController.CurrentStamina < CurrentAttackData.staminaCost)
        {
            ResetAttackState();
            return;
        }

        statController.ConsumeStamina(CurrentAttackData.staminaCost);
        IsAttacking = true;
        isAttackBuffered = false;
        currentStrategy.StartAttack(currentComboIndex);
    }

    private bool CanPerformNextCombo()
    {
        return CurrentWeaponData.GetAttackData(currentComboIndex + 1) != null;
    }

    public void AnimationFinished()
    {
        if (isAttackBuffered && CanPerformNextCombo())
        {
            if (attackChainCoroutine != null) StopCoroutine(attackChainCoroutine);
            attackChainCoroutine = StartCoroutine(StartNextAttackAfterFrame());
        }
        else
        {
            ResetAttackState();
        }
    }

    private IEnumerator StartNextAttackAfterFrame()
    {
        IsAttacking = false;
        isAttackBuffered = false;

        yield return new WaitForEndOfFrame();

        currentComboIndex++;
        StartAttack();
    }

    private void ResetAttackState()
    {
        IsAttacking = false;
        currentComboIndex = 0;
        isAttackBuffered = false;
        OnAttackFinished?.Invoke();
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && IsAttacking)
        {
            currentStrategy?.OnDrawGizmos();
        }
    }
}

// Lớp chiến lược rỗng vẫn giữ nguyên
public class NullAttackStrategy : IWeaponAttackStrategy
{
    public void OnEquip(WeaponController controller) { }
    public void StartAttack(int comboIndex) { }
    public void Tick(float deltaTime) { }
    public void StopAttack() { }
    public void OnDrawGizmos() { }
}