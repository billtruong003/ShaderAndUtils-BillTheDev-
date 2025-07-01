using UnityEngine;

public class PlayerJumpState : PlayerState
{
    // Thời gian tối thiểu ở trạng thái nhảy trước khi có thể chuyển sang rơi
    private readonly float jumpDuration = 0.2f;
    private float timer;

    public PlayerJumpState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        timer = jumpDuration;
        input.ConsumeJumpInput();
        locomotion.HandleJump(stateMachine.jumpHeight);
        animator.PlayTargetAnimation("Jump");
    }

    public override void Tick(float deltaTime)
    {
        timer -= deltaTime;

        // Cho phép dash khi đang nhảy
        if (input.DashInput)
        {
            stateMachine.SwitchState(new PlayerDashState(stateMachine));
            return;
        }

        locomotion.HandleAirborneMovement();

        // Sau một khoảng thời gian ngắn, kiểm tra vận tốc để chuyển sang trạng thái rơi
        // Điều này ngăn việc chuyển sang FallState ngay lập tức
        if (timer <= 0 && locomotion.PlayerVelocity.y <= 0)
        {
            stateMachine.SwitchState(new PlayerFallState(stateMachine));
        }
    }

    public override void Exit() { }
}