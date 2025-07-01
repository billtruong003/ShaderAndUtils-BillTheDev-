public class PlayerFallState : PlayerState
{
    public PlayerFallState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        animator.PlayTargetAnimation("Fall");
    }

    public override void Tick(float deltaTime)
    {
        // Cho phép dash khi đang rơi
        if (input.DashInput)
        {
            stateMachine.SwitchState(new PlayerDashState(stateMachine));
            return;
        }

        locomotion.HandleAirborneMovement();
        if (locomotion.IsGrounded())
        {
            stateMachine.SwitchState(new PlayerGroundedState(stateMachine));
        }
    }
}