public class PlayerGroundedState : PlayerState
{
    public PlayerGroundedState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Tick(float deltaTime)
    {
        // Ưu tiên các hành động có tính "ngắt quãng" (interrupting)
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

        // --- LOGIC TẤN CÔNG ĐÃ ĐƯỢC SỬA ---
        if (input.AttackInput)
        {
            if (equipment.IsWeaponDrawn)
            {
                // Nếu đã rút vũ khí, tấn công ngay
                stateMachine.SwitchState(new PlayerAttackState(stateMachine, 0));
            }
            else
            {
                // Nếu chưa, chuyển sang trạng thái rút vũ khí
                stateMachine.SwitchState(new PlayerDrawWeaponState(stateMachine));
            }
            return; // Luôn return sau khi chuyển state
        }

        // --- LOGIC ĐỔI STANCE VẪN GIỮ NGUYÊN ---
        if (input.StanceChangeInput)
        {
            input.ConsumeStanceChangeInput();
            equipment.ToggleWeaponStance();
            // Nếu cất vũ khí, logic sẽ tự động xử lý
        }

        // Nếu không có hành động, xử lý di chuyển
        locomotion.HandleGroundedMovement(input.MoveInput, input.IsRunning);
        animator.UpdateMoveSpeed(locomotion.GetCurrentSpeed());
    }
}