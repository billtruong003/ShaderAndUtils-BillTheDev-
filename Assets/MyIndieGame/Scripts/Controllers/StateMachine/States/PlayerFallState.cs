// File: Assets/MyIndieGame/Scripts/Controllers/StateMachine/States/PlayerFallState.cs
// (Đã sửa lỗi)

public class PlayerFallState : PlayerState
{
    public PlayerFallState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        animator.PlayTargetAnimation("Fall"); // Hoặc tên animation rơi của bạn
        
        // --- SỬA LỖI Ở ĐÂY ---
        // Cung cấp cả hai tham số. Khi rơi, tốc độ animation là 0.
        animator.UpdateMoveSpeed(0f, 0f);
    }

    public override void Tick(float deltaTime)
    {
        if (input.DashInput)
        {
            stateMachine.SwitchState(new PlayerDashState(stateMachine));
            return;
        }

        locomotion.HandleAirborneMovement(input.MoveInput, stateMachine.Targeting?.CurrentTarget);

        // Khi chạm đất, quay về trạng thái di chuyển tự do
        // GroundedState sẽ tự quyết định có chuyển sang LockState hay không
        if (locomotion.IsGrounded())
        {
            stateMachine.SwitchState(new PlayerGroundedState(stateMachine));
        }
    }

    public override void Exit() { }
}