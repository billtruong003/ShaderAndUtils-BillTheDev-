// File: PlayerGroundedLockState.cs (Tạo mới)
// State này xử lý di chuyển khi người chơi đang khóa mục tiêu.

public class PlayerGroundedLockState : PlayerState
{
    public PlayerGroundedLockState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        // Chuyển Animator sang Blend Tree của Lock-on mode
        // Giả sử bạn có một trigger/bool trong Animator để làm việc này
        animator.TweenLockOnParameter(true);
    }

    public override void Tick(float deltaTime)
    {
        // 1. Kiểm tra điều kiện thoát khỏi State
        // Nếu không còn mục tiêu, quay về trạng thái di chuyển tự do
        if (stateMachine.Targeting.CurrentTarget == null)
        {
            stateMachine.SwitchState(new PlayerGroundedState(stateMachine));
            return;
        }

        // Nếu không còn trên mặt đất
        if (!locomotion.IsGrounded())
        {
            stateMachine.SwitchState(new PlayerFallState(stateMachine));
            return;
        }

        // 2. Xử lý Input hành động
        if (input.DashInput)
        {
            stateMachine.SwitchState(new PlayerDashState(stateMachine));
            return;
        }
        
        // **KHÔNG CÓ NHẢY (JUMP)** trong trạng thái này

        if (input.AttackInput)
        {
            // Logic tấn công không đổi
            bool hasRealWeaponEquipped = equipment.CurrentWeapon != equipment.UnarmedWeaponData;
            if (!hasRealWeaponEquipped || equipment.IsWeaponDrawn)
            {
                stateMachine.SwitchState(new PlayerAttackState(stateMachine, 0));
            }
            else
            {
                stateMachine.SwitchState(new PlayerDrawWeaponState(stateMachine));
            }
            return;
        }

        // Logic rút/cất vũ khí không đổi
        if (input.StanceChangeInput)
        {
            input.ConsumeStanceChangeInput();
            if (equipment.CurrentWeapon != equipment.UnarmedWeaponData)
            {
                equipment.ToggleWeaponStance();
            }
        }
        
        // 3. Xử lý di chuyển
        // Luôn truyền CurrentTarget vào đây
        locomotion.HandleGroundedMovement(input.MoveInput, input.IsRunning, stateMachine.Targeting.CurrentTarget);
    }

    public override void Exit()
    {
        // Tắt trạng thái Lock-on trong Animator khi thoát
        animator.TweenLockOnParameter(false);
    }
}