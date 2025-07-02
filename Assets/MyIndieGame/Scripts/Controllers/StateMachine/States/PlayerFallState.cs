// File: Assets/MyIndieGame/Scripts/Controllers/StateMachine/States/PlayerFallState.cs

public class PlayerFallState : PlayerState
{
    public PlayerFallState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        animator.PlayTargetAnimation("Fall");
        animator.UpdateMoveSpeed(0);
    }

    public override void Tick(float deltaTime)
    {
        if (input.DashInput)
        {
            stateMachine.SwitchState(new PlayerDashState(stateMachine));
            return;
        }

        locomotion.HandleAirborneMovement(input.MoveInput, stateMachine.Targeting?.CurrentTarget);
        if (locomotion.IsGrounded())
        {
            stateMachine.SwitchState(new PlayerGroundedState(stateMachine));
        }
    }

    public override void Exit() { }
}