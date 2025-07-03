// File: PlayerGroundedState.cs (Đã cập nhật để chuyển sang LockState)
// State này chỉ xử lý di chuyển Tự Do.

public class PlayerGroundedState : PlayerState
{
    public PlayerGroundedState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        // Đảm bảo Animator ở trạng thái Tự do
        animator.SetBool("IsLockedOn", false);
    }

    public override void Tick(float deltaTime)
    {
        // 1. Kiểm tra điều kiện chuyển State
        // Nếu người chơi khóa mục tiêu, chuyển sang Lock State
        if (stateMachine.Targeting.CurrentTarget != null)
        {
            stateMachine.SwitchState(new PlayerGroundedLockState(stateMachine));
            return;
        }

        if (!locomotion.IsGrounded())
        {
            stateMachine.SwitchState(new PlayerFallState(stateMachine));
            return;
        }

        // 2. Xử lý Input hành động (Tự do)
        if (input.DashInput)
        {
            stateMachine.SwitchState(new PlayerDashState(stateMachine));
            return;
        }

        // **CÓ NHẢY (JUMP)** trong trạng thái này
        if (input.JumpInput)
        {
            stateMachine.SwitchState(new PlayerJumpState(stateMachine));
            return;
        }

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

        if (input.StanceChangeInput)
        {
            input.ConsumeStanceChangeInput();
            if (equipment.CurrentWeapon != equipment.UnarmedWeaponData)
            {
                equipment.ToggleWeaponStance();
            }
        }

        // 3. Xử lý di chuyển
        // Luôn truyền null vào target vì đây là chế độ Tự do
        locomotion.HandleGroundedMovement(input.MoveInput, input.IsRunning, null);
    }

    public override void Exit() { }
}