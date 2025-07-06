// File: Assets/MyIndieGame/Scripts/Controllers/StateMachine/States/PlayerAttackState.cs
// PHIÊN BẢN NÂNG CẤP - CHO PHÉP ĐIỀU KHIỂN GIỮA CÁC ĐÒN COMBO
using UnityEngine;

public class PlayerAttackState : PlayerState
{
    public PlayerAttackState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.WeaponController.OnAttackFinished += OnAttackSequenceFinished;
        input.ConsumeAttackInput();
        stateMachine.WeaponController.RequestAttack();

        // Thoát ngay nếu không thể tấn công (ví dụ: hết stamina)
        // OnAttackFinished sẽ không được gọi trong trường hợp này, nên phải chuyển state ở đây.
        if (!stateMachine.WeaponController.IsAttacking)
        {
            stateMachine.SwitchState(new PlayerGroundedState(stateMachine));
        }
    }

    public override void Tick(float deltaTime)
    {
        // Ưu tiên cao nhất: Dash có thể ngắt mọi hành động.
        if (input.DashInput)
        {
            stateMachine.SwitchState(new PlayerDashState(stateMachine));
            return;
        }

        // --- LOGIC MỚI: XỬ LÝ KHI Ở TRONG "KHOẢNG NGHỈ" GIỮA CÁC ĐÒN COMBO ---
        if (!stateMachine.WeaponController.IsAttacking)
        {
            // Lúc này, animation của đòn đánh trước đã kết thúc, nhưng chuỗi combo chưa bị hủy.
            // Đây là thời điểm vàng để trao lại quyền kiểm soát cho người chơi.

            // 1. Cho phép xoay người để nhắm cho đòn tiếp theo
            if (stateMachine.Targeting.CurrentTarget == null)
            {
                // Nếu không khóa mục tiêu, cho phép xoay tự do theo hướng di chuyển
                Vector3 moveDirection = CalculateCameraRelativeMoveDirection(input.MoveInput);
                locomotion.HandleRotation(moveDirection, null);
            }
            // Nếu có mục tiêu, nhân vật sẽ tự xoay về phía mục tiêu trong HandleGroundedMovement.

            // 2. Cho phép di chuyển nhẹ (chỉ tốc độ đi bộ) để điều chỉnh vị trí
            locomotion.HandleGroundedMovement(input.MoveInput, false, stateMachine.Targeting.CurrentTarget);

            // 3. Kiểm tra các hành động khác có thể thực hiện từ "khoảng nghỉ" này
            if (input.JumpInput)
            {
                stateMachine.SwitchState(new PlayerJumpState(stateMachine));
                return;
            }
            if (!locomotion.IsGrounded())
            {
                stateMachine.SwitchState(new PlayerFallState(stateMachine));
                return;
            }
        }
        // --- KẾT THÚC LOGIC MỚI ---

        // Luôn lắng nghe input tấn công để nối combo.
        // Logic này hoạt động cả khi đang trong animation và khi đang trong "khoảng nghỉ".
        if (input.AttackInput)
        {
            input.ConsumeAttackInput();
            stateMachine.WeaponController.RequestAttack();
        }
    }

    private void OnAttackSequenceFinished()
    {
        // Sự kiện này được gọi khi chuỗi combo kết thúc hoàn toàn (do hết combo hoặc người chơi không bấm tiếp).
        stateMachine.SwitchState(new PlayerGroundedState(stateMachine));
    }

    public override void Exit()
    {
        stateMachine.WeaponController.OnAttackFinished -= OnAttackSequenceFinished;
    }

    // Tiện ích nhỏ để lấy hướng di chuyển, vì PlayerState không có sẵn camTransform
    private Vector3 CalculateCameraRelativeMoveDirection(Vector2 moveInput)
    {
        Transform camTransform = Camera.main.transform;
        Vector3 camForward = camTransform.forward;
        Vector3 camRight = camTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        return (camForward.normalized * moveInput.y + camRight.normalized * moveInput.x).normalized;
    }
}