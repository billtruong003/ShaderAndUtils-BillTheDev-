// File: Assets/MyIndieGame/Scripts/Weapon/MeleeSphereCastAttack.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MeleeSphereCastAttack : IWeaponAttackStrategy
{
    private WeaponController controller;
    private float elapsedTime;
    private HashSet<Collider> hitTargets;
    private Dictionary<Transform, Vector3> lastFramePositions;
    private List<Transform> activeCastPoints;
    private bool isLungeActive; // Thay thế isLungeInProgress để chỉ cho biết lunge có được kích hoạt hay không

    public void OnEquip(WeaponController controller)
    {
        this.controller = controller;
        this.hitTargets = new HashSet<Collider>();
        this.lastFramePositions = new Dictionary<Transform, Vector3>();
    }

    // --- TÁI CẤU TRÚC HOÀN TOÀN HÀM NÀY ---
    public void StartAttack(int comboIndex)
    {
        // --- BƯỚC 1: Khởi tạo các thông số cơ bản ---
        elapsedTime = 0f;
        hitTargets.Clear();
        lastFramePositions.Clear();
        activeCastPoints = GetSourceCastPoints();
        isLungeActive = false; // Reset cờ

        // Xoay người về mục tiêu ngay từ đầu (nếu có)
        if (controller.Targeting?.CurrentTarget != null)
        {
            controller.Locomotion.ForceLookAtTarget(controller.Targeting.CurrentTarget);
        }

        // --- BƯỚC 2: BẮT ĐẦU ANIMATION NGAY LẬP TỨC ---
        // Đây là thay đổi quan trọng nhất để đảm bảo phản hồi tức thì và mượt mà.
        controller.Animator.PlayAction(controller.CurrentAttackData.AttackID);

        // --- BƯỚC 3: Kiểm tra và kích hoạt Lunge (nếu cần) ---
        AttackData data = controller.CurrentAttackData;
        Transform target = controller.Targeting?.CurrentTarget;

        if (data.canLunge && target != null)
        {
            float distanceToTarget = Vector3.Distance(controller.transform.position, target.position);
            if (distanceToTarget > data.idealAttackRange && distanceToTarget <= data.maxLungeDistance)
            {
                isLungeActive = true;
                // Bắt đầu lướt song song với animation, không cần chờ đợi.
                controller.Locomotion.PerformLunge(target, data.idealAttackRange, data.lungeDuration);
            }
        }

        // --- BƯỚC 4: Thiết lập các điểm cast ban đầu ---
        if (activeCastPoints != null)
        {
            foreach (var point in activeCastPoints)
            {
                if (point != null) lastFramePositions[point] = point.position;
            }
        }
    }

    public void Tick(float deltaTime)
    {
        elapsedTime += deltaTime;
        HandleMovement(); // Xử lý di chuyển
        HandleSphereCast(); // Xử lý hitbox

        if (elapsedTime >= controller.CurrentAttackData.Duration)
        {
            controller.AnimationFinished();
        }
    }

    // --- SỬA ĐỔI: Chỉ áp dụng "root motion giả" nếu Lunge không được kích hoạt ---
    private void HandleMovement()
    {
        if (isLungeActive) return; // Nếu đang lướt, không áp dụng di chuyển này

        AttackData data = controller.CurrentAttackData;
        if (elapsedTime <= data.moveDuration)
        {
            controller.Locomotion.HandleAttackMovement(data.moveForwardSpeed);
        }
    }

    // Các hàm còn lại (HandleSphereCast, ProcessHit, GetSourceCastPoints, StopAttack, OnDrawGizmos) giữ nguyên.
    // ...
    private void HandleSphereCast()
    {
        if (activeCastPoints == null) return;

        AttackData data = controller.CurrentAttackData;
        if (elapsedTime < data.hitboxStartTime || elapsedTime > data.hitboxEndTime) return;

        foreach (var point in activeCastPoints)
        {
            if (point == null) continue;

            Vector3 startPosition = lastFramePositions[point];
            Vector3 endPosition = point.position;
            Vector3 direction = endPosition - startPosition;
            float distance = direction.magnitude;

            if (distance > 0.001f)
            {
                if (Physics.SphereCast(startPosition, controller.CurrentWeaponData.castRadius, direction.normalized, out RaycastHit hit, distance, controller.Targeting.EnemyLayer))
                {
                    if (!hitTargets.Contains(hit.collider) && hit.transform.root != controller.transform.root)
                    {
                        ProcessHit(hit.transform.gameObject, hit.point);
                    }
                }
            }

            Collider[] overlappingColliders = Physics.OverlapSphere(endPosition, controller.CurrentWeaponData.castRadius, controller.Targeting.EnemyLayer);
            foreach (var col in overlappingColliders)
            {
                if (!hitTargets.Contains(col) && col.transform.root != controller.transform.root)
                {
                    ProcessHit(col.gameObject, col.ClosestPoint(endPosition));
                }
            }
        }

        foreach (var point in activeCastPoints)
        {
            if (point != null) lastFramePositions[point] = point.position;
        }
    }
    private void ProcessHit(GameObject hitObject, Vector3 hitPoint)
    {
        if (hitTargets.Contains(hitObject.GetComponent<Collider>())) return;
        hitTargets.Add(hitObject.GetComponent<Collider>());

        float weaponDamage = controller.CurrentWeaponData.baseDamage;
        float statBonus = controller.Stats.GetStatValue(StatType.PhysicalDamageBonus);
        float totalBaseDamage = weaponDamage * (1 + statBonus / 100f);
        float finalDamage = totalBaseDamage * controller.CurrentAttackData.damageMultiplier;

        if (hitObject.TryGetComponent<Health>(out var health))
        {
            health.TakeDamage(finalDamage, hitPoint);
        }
        if (hitObject.TryGetComponent<PoiseController>(out var poise))
        {
            poise.TakePoiseDamage(controller.CurrentAttackData.poiseDamage);
        }
        controller.ParticlesController?.PlayParticle(
            PlayerParticlesController.PlayerParticleType.Impact,
            hitPoint
        );
    }
    private List<Transform> GetSourceCastPoints()
    {
        BodyPart attackingPart = controller.CurrentAttackData.attackingPart;
        if (controller.CurrentWeaponInstance != null && (attackingPart == BodyPart.Weapon_Primary || attackingPart == BodyPart.Weapon_Secondary))
        {
            return controller.CurrentWeaponInstance.MeleeCastPoints;
        }
        if (controller.CharacterHitboxes != null)
        {
            return controller.CharacterHitboxes.GetHitboxes(attackingPart);
        }
        return new List<Transform>();
    }
    public void StopAttack() { }
    public void OnDrawGizmos()
    {
        if (controller?.CurrentAttackData == null || activeCastPoints == null) return;
        AttackData data = controller.CurrentAttackData;
        bool isActive = elapsedTime >= data.hitboxStartTime && elapsedTime <= data.hitboxEndTime;
        Gizmos.color = isActive ? new Color(1, 0, 0, 0.5f) : new Color(0, 1, 1, 0.5f);
        foreach (var point in activeCastPoints)
        {
            if (point != null)
            {
                Gizmos.DrawSphere(point.position, controller.CurrentWeaponData.castRadius);
            }
        }
    }
}