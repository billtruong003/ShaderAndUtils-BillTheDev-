// File: Assets/MyIndieGame/Scripts/Controllers/StateMachine/States/PlayerAttackState.cs
// Phiên bản đã được dọn dẹp, sửa lỗi và tối ưu hóa logic combo.

using UnityEngine;
using System.Collections.Generic;

public class PlayerAttackState : PlayerState
{
    // Các biến chỉ đọc, được thiết lập một lần khi tạo state
    private readonly int comboIndex;

    // Các biến trạng thái của đòn đánh
    private float elapsedTime;
    private AttackData attackData;
    private bool nextAttackQueued;

    // Các biến dùng cho SphereCast
    private HashSet<Collider> hitTargets;
    private Dictionary<Transform, Vector3> lastFramePositions;
    private LayerMask enemyLayer;
    private List<Transform> activeCastPoints;

    public PlayerAttackState(PlayerStateMachine stateMachine, int comboIndex) : base(stateMachine)
    {
        this.comboIndex = comboIndex;

        // Khởi tạo các collection để tránh lỗi null
        this.hitTargets = new HashSet<Collider>();
        this.lastFramePositions = new Dictionary<Transform, Vector3>();
        this.activeCastPoints = new List<Transform>();

        // Lấy LayerMask một cách an toàn
        if (stateMachine.Targeting != null)
        {
            this.enemyLayer = stateMachine.Targeting.EnemyLayer;
        }
        else
        {
            Debug.LogError("TargetingController is not assigned. Falling back to 'Default' layer.", stateMachine.gameObject);
            this.enemyLayer = LayerMask.GetMask("Default");
        }
    }

    public override void Enter()
    {
        // 1. Reset trạng thái cho đòn đánh mới
        elapsedTime = 0f;
        nextAttackQueued = false;

        // 2. Tiêu thụ input đã đưa chúng ta vào state này
        input.ConsumeAttackInput();

        // 3. Lấy dữ liệu cho đòn đánh hiện tại
        attackData = equipment.GetCurrentAttackData(comboIndex);

        // 4. Kiểm tra xem có thể thực hiện đòn đánh không (đủ stamina, có data...)
        if (attackData == null || stats.CurrentStamina < attackData.staminaCost)
        {
            stateMachine.SwitchState(new PlayerGroundedState(stateMachine));
            return;
        }

        // 5. Chuẩn bị cho SphereCast
        activeCastPoints.Clear();
        if (equipment.AllCastPoints.TryGetValue(attackData.attackingPart, out var points) && points.Count > 0)
        {
            activeCastPoints.AddRange(points);
        }
        else if (attackData.attackingPart != BodyPart.None)
        {
            Debug.LogWarning($"No cast points for BodyPart '{attackData.attackingPart}' in Attack '{attackData.AttackID}'.", stateMachine.gameObject);
        }

        // 6. Thực hiện các hành động
        stats.ConsumeStamina(attackData.staminaCost);
        if (stateMachine.Targeting?.CurrentTarget != null)
        {
            // Ưu tiên 1: Nếu có mục tiêu đã khóa, xoay về phía nó
            locomotion.ForceLookAtTarget(stateMachine.Targeting.CurrentTarget);
            Debug.Log("Forcing look at TARGET.");
        }
        else if (input.MoveInput != Vector2.zero)
        {
            // Ưu tiên 2: Nếu không có mục tiêu nhưng người chơi đang di chuyển, xoay về hướng đó
            // Điều này cho phép đổi hướng tấn công giữa các đòn combo mà không cần khóa mục tiêu
            Vector3 moveDirection = new Vector3(input.MoveInput.x, 0, input.MoveInput.y);

            // Chuyển hướng di chuyển từ không gian của người chơi sang không gian của thế giới dựa trên camera
            Vector3 cameraRelativeDirection = Camera.main.transform.TransformDirection(moveDirection);
            cameraRelativeDirection.y = 0; // Đảm bảo không xoay lên/xuống

            locomotion.ForceLookAtDirection(cameraRelativeDirection.normalized);
            Debug.Log("Forcing look at MOVE direction.");
        }

        animator.PlayAction(attackData.AttackID);

        // 7. Reset các biến cho việc phát hiện va chạm
        hitTargets.Clear();
        lastFramePositions.Clear();
        foreach (var point in activeCastPoints)
        {
            lastFramePositions[point] = point.position;
        }
    }

    public override void Tick(float deltaTime)
    {
        elapsedTime += deltaTime;

        // Cho phép ngắt đòn bằng Dash
        if (input.DashInput)
        {
            stateMachine.SwitchState(new PlayerDashState(stateMachine));
            return;
        }

        // Thực hiện các logic con
        HandleMovement(deltaTime);
        HandleSphereCastAttack();
        HandleComboQueuing(); // Đổi tên cho rõ ràng

        // Khi animation kết thúc, kiểm tra xem có đòn combo đã đặt hàng không
        if (elapsedTime >= attackData.Duration)
        {
            PerformQueuedAction();
        }
    }

    public override void Exit() { }

    private void HandleMovement(float deltaTime)
    {
        if (elapsedTime <= attackData.moveDuration)
        {
            locomotion.HandleAttackMovement(attackData.moveForwardSpeed);
        }
    }

    private void HandleSphereCastAttack()
    {
        // Chỉ cast trong "active frames"
        if (elapsedTime < attackData.hitboxStartTime || elapsedTime > attackData.hitboxEndTime)
        {
            return;
        }

        foreach (var point in activeCastPoints)
        {
            Vector3 startPosition = lastFramePositions[point];
            Vector3 endPosition = point.position;
            Vector3 direction = endPosition - startPosition;
            float distance = direction.magnitude;

            if (distance > 0.001f && Physics.SphereCast(startPosition, equipment.CurrentWeapon.castRadius, direction.normalized, out RaycastHit hit, distance, enemyLayer))
            {
                if (hitTargets.Contains(hit.collider) || hit.transform.root == stateMachine.transform.root) continue;

                hitTargets.Add(hit.collider);
                ProcessHit(hit.transform.gameObject, hit.point);
            }
        }

        // Cập nhật vị trí cho frame sau
        foreach (var point in activeCastPoints)
        {
            lastFramePositions[point] = point.position;
        }
    }

    private void ProcessHit(GameObject hitObject, Vector3 hitPoint)
    {
        Health health = hitObject.GetComponent<Health>() ?? hitObject.GetComponentInParent<Health>();
        if (health == null) return;

        if (hitObject.TryGetComponent<PoiseController>(out var poise))
        {
            poise.TakePoiseDamage(attackData.poiseDamage);
        }

        float weaponDamage = equipment.CurrentWeapon.baseDamage;
        float statBonusDamage = stats.GetStatValue(StatType.PhysicalDamageBonus);
        float totalBaseDamage = weaponDamage + statBonusDamage;
        float finalDamage = totalBaseDamage * attackData.damageMultiplier;

        health.TakeDamage(finalDamage, hitPoint);
        GameFeelManager.Instance?.DoHitStop(0.08f);
    }

    // --- LOGIC COMBO "CHUẨN" ---
    private void HandleComboQueuing()
    {
        // Nếu người chơi bấm tấn công và chưa có đòn nào được đặt hàng
        if (input.AttackInput && !nextAttackQueued)
        {
            // Tiêu thụ input ngay lập tức để tránh xử lý lại
            input.ConsumeAttackInput();

            // Kiểm tra xem đòn tấn công tiếp theo trong chuỗi có tồn tại không
            if (equipment.GetCurrentAttackData(comboIndex + 1) != null)
            {
                // Đặt cờ để báo rằng có một đòn đã được "đặt hàng"
                nextAttackQueued = true;
                Debug.Log("<color=green>COMBO QUEUED SUCCESSFULLY!</color>");
            }
        }
    }

    private void PerformQueuedAction()
    {
        // Nếu có một đòn tấn công đã được đặt hàng
        if (nextAttackQueued)
        {
            // Chuyển sang state của đòn tấn công tiếp theo
            stateMachine.SwitchState(new PlayerAttackState(stateMachine, comboIndex + 1));
        }
        else
        {
            // Nếu không, quay về trạng thái bình thường
            stateMachine.SwitchState(new PlayerGroundedState(stateMachine));
        }
    }

    // --- Các hàm cung cấp dữ liệu cho Gizmos ---
    public bool IsAttackWindowActive()
    {
        return attackData != null && elapsedTime >= attackData.hitboxStartTime && elapsedTime <= attackData.hitboxEndTime;
    }

    public IReadOnlyList<Transform> GetActiveCastPoints()
    {
        return activeCastPoints;
    }
}