using UnityEngine;

public class PlayerDashState : PlayerState
{
    // Các giá trị này có thể được lấy từ StatController trong tương lai
    private readonly float dashDuration = 0.4f;
    private readonly float dashSpeed = 15f;
    private float timer;

    public PlayerDashState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        timer = dashDuration;
        input.ConsumeDashInput();

        // Giả sử có 1 animation tên "Dash" trong Animator
        animator.PlayTargetAnimation("Dash");
    }

    public override void Tick(float deltaTime)
    {
        timer -= deltaTime;

        // Thực hiện di chuyển vật lý của cú dash
        locomotion.HandleDash(dashSpeed);

        // Khi hết thời gian, quay về trạng thái nền
        if (timer <= 0)
        {
            // Kiểm tra xem đang ở trên không hay mặt đất để quay về đúng trạng thái
            if (locomotion.IsGrounded())
            {
                stateMachine.SwitchState(new PlayerGroundedState(stateMachine));
            }
            else
            {
                stateMachine.SwitchState(new PlayerFallState(stateMachine)); // Quan trọng: có thể dash trên không và rơi xuống
            }
        }
    }

    public override void Exit()
    {
        // Có thể thêm logic để dừng hoàn toàn vận tốc dash nếu cần
    }
}