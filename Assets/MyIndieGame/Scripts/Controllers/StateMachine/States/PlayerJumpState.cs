// File: Assets/MyIndieGame/Scripts/Controllers/StateMachine/States/PlayerJumpState.cs
// (Đã sửa lỗi)

using UnityEngine;

public class PlayerJumpState : PlayerState
{
    private readonly float jumpDuration = 0.2f; // Thời gian tối thiểu ở trạng thái nhảy
    private float timer;

    public PlayerJumpState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        timer = jumpDuration;
        input.ConsumeJumpInput();
        locomotion.HandleJump(stateMachine.jumpHeight);

        animator.PlayTargetAnimation("Jump"); // Hoặc tên animation nhảy của bạn
        
        // --- SỬA LỖI Ở ĐÂY ---
        // Cung cấp cả hai tham số. Khi nhảy, tốc độ animation là 0.
        animator.UpdateMoveSpeed(0f, 0f);
    }

    public override void Tick(float deltaTime)
    {
        timer -= deltaTime;

        if (input.DashInput)
        {
            stateMachine.SwitchState(new PlayerDashState(stateMachine));
            return;
        }

        locomotion.HandleAirborneMovement(input.MoveInput, stateMachine.Targeting?.CurrentTarget);

        // Chuyển sang FallState khi vận tốc y bắt đầu đi xuống
        if (timer <= 0 && locomotion.PlayerVelocity.y <= 0)
        {
            stateMachine.SwitchState(new PlayerFallState(stateMachine));
        }
    }

    public override void Exit() { }
}