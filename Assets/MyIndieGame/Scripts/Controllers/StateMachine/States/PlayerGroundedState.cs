// File: Assets/MyIndieGame/Scripts/Controllers/StateMachine/States/PlayerGroundedState.cs
public class PlayerGroundedState : PlayerState
{
    public PlayerGroundedState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        animator.TweenLockOnParameter(false);
    }

    public override void Tick(float deltaTime)
    {
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

        if (input.AttackInput)
        {
            bool hasRealWeaponEquipped = equipment.CurrentWeapon != equipment.UnarmedWeaponData;
            if (!hasRealWeaponEquipped || equipment.IsWeaponDrawn)
            {
                // SỬA LỖI Ở ĐÂY: Gọi hàm khởi tạo mới không có comboIndex
                stateMachine.SwitchState(new PlayerAttackState(stateMachine));
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

        locomotion.HandleGroundedMovement(input.MoveInput, input.IsRunning, null);
    }

    public override void Exit() { }
}