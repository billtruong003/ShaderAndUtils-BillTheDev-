public class PlayerGroundedState : PlayerState
{
    public PlayerGroundedState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        animator.PlayTargetAnimation("GroundedMovement", 0.1f);
    }

    public override void Exit() { }

    public override void Tick(float deltaTime)
    {
        if (input.DashInput)
        {
            stateMachine.SwitchState(new PlayerDashState(stateMachine));
            return;
        }

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

        if (input.AttackInput)
        {
            // Lấy thông tin xem người chơi có đang trang bị vũ khí "xịn" hay không
            bool hasRealWeaponEquipped = equipment.CurrentWeapon != equipment.UnarmedWeaponData;

            // Kịch bản 1: Người chơi đang dùng tay không, hoặc vũ khí đã được rút ra rồi
            if (!hasRealWeaponEquipped || equipment.IsWeaponDrawn)
            {
                stateMachine.SwitchState(new PlayerAttackState(stateMachine, 0));
            }
            // Kịch bản 2: Có vũ khí "xịn" VÀ nó đang được cất đi
            else // (hasRealWeaponEquipped && !equipment.IsWeaponDrawn)
            {
                stateMachine.SwitchState(new PlayerDrawWeaponState(stateMachine));
            }
            return;
        }

        if (input.StanceChangeInput)
        {
            input.ConsumeStanceChangeInput();
            // Chỉ cho phép rút/cất khi có vũ khí thật
            if (equipment.CurrentWeapon != equipment.UnarmedWeaponData)
            {
                equipment.ToggleWeaponStance();
            }
        }

        // Truyền mục tiêu vào hàm di chuyển
        locomotion.HandleGroundedMovement(input.MoveInput, input.IsRunning, stateMachine.Targeting?.CurrentTarget);
        animator.UpdateMoveSpeed(locomotion.GetCurrentSpeed());
    }
}