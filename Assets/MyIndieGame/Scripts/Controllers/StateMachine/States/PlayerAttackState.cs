// File: Assets/MyIndieGame/Scripts/Controllers/StateMachine/States/PlayerAttackState.cs (ĐÃ SỬA LỖI)
using UnityEngine;
using System.Collections.Generic;

public class PlayerAttackState : PlayerState
{
    private int comboIndex;
    private float timer;
    private AttackData attackData;
    private bool isNextAttackQueued;
    private float moveTimer;
    private HashSet<Collider> hitColliders;

    public AttackData CurrentAttackData => attackData;
    public float TimeSinceEntered => attackData != null ? attackData.Duration - timer : 0f;

    public PlayerAttackState(PlayerStateMachine stateMachine, int comboIndex) : base(stateMachine)
    {
        this.comboIndex = comboIndex;
        this.hitColliders = new HashSet<Collider>();
    }

    public override void Enter()
    {
        input.ConsumeAttackInput();
        isNextAttackQueued = false;
        attackData = equipment.GetCurrentAttackData(comboIndex);

        if (attackData == null)
        {
            stateMachine.SwitchState(new PlayerGroundedState(stateMachine));
            return;
        }

        if (stateMachine.Stats.CurrentStamina < attackData.staminaCost)
        {
            stateMachine.SwitchState(new PlayerGroundedState(stateMachine));
            return;
        }
        stateMachine.Stats.ConsumeStamina(attackData.staminaCost);

        // --- THAY ĐỔI QUAN TRỌNG ---
        // Xoay người về phía mục tiêu NGAY LẬP TỨC khi bắt đầu tấn công
        locomotion.ForceLookAtTarget(stateMachine.Targeting?.CurrentTarget);
        // -------------------------

        timer = attackData.Duration;
        animator.PlayAction(attackData.AttackID);
        moveTimer = attackData.moveDuration;
        hitColliders.Clear();
    }

    public override void Tick(float deltaTime)
    {
        if (input.DashInput)
        {
            stateMachine.SwitchState(new PlayerDashState(stateMachine));
            return;
        }

        timer -= deltaTime;

        // --- THAY ĐỔI QUAN TRỌNG ---
        // XÓA DÒNG GỌI HandleRotation() Ở ĐÂY ĐỂ KHÓA HƯỚNG KHI ĐANG TẤN CÔNG
        // locomotion.HandleRotation(input.MoveInput, stateMachine.Targeting?.CurrentTarget); // <-- DÒNG NÀY ĐÃ BỊ XÓA/VÔ HIỆU HÓA

        HandleMovement(deltaTime);

        bool isHitboxActive = TimeSinceEntered >= attackData.hitboxStartTime && TimeSinceEntered <= attackData.hitboxEndTime;
        if (isHitboxActive)
        {
            HandleHitDetection();
        }

        HandleQueuing();

        if (timer <= 0f)
        {
            PerformQueuedAttack();
        }
    }

    private void HandleMovement(float deltaTime)
    {
        if (moveTimer > 0)
        {
            moveTimer -= deltaTime;
            locomotion.HandleAttackMovement(attackData.moveForwardSpeed);
        }
    }

    private void HandleHitDetection()
    {
        Vector3 hitboxCenter = stateMachine.transform.position + stateMachine.transform.forward * 1.0f;
        Collider[] colliders = Physics.OverlapSphere(hitboxCenter, attackData.hitboxRadius);

        foreach (var collider in colliders)
        {
            if (collider.transform == stateMachine.transform || hitColliders.Contains(collider)) continue;

            if (collider.TryGetComponent<Health>(out Health targetHealth))
            {
                hitColliders.Add(collider);
                float baseDamage = stateMachine.Stats.GetStatValue(StatType.PhysicalDamageBonus);
                float finalDamage = baseDamage * attackData.damageMultiplier;
                Vector3 hitPoint = collider.ClosestPoint(stateMachine.transform.position);
                targetHealth.TakeDamage(finalDamage, hitPoint);
            }
        }
    }

    private void HandleQueuing()
    {
        if (input.AttackInput && !isNextAttackQueued)
        {
            input.ConsumeAttackInput();
            if (equipment.GetCurrentAttackData(comboIndex + 1) != null)
            {
                isNextAttackQueued = true;
            }
        }
    }

    private void PerformQueuedAttack()
    {
        if (isNextAttackQueued)
        {
            stateMachine.SwitchState(new PlayerAttackState(stateMachine, comboIndex + 1));
        }
        else
        {
            stateMachine.SwitchState(new PlayerGroundedState(stateMachine));
        }
    }

    public override void Exit() { }
}