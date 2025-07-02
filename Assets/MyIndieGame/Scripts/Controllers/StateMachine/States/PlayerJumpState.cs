// File: Assets/MyIndieGame/Scripts/Controllers/StateMachine/States/PlayerJumpState.cs

using UnityEngine;

public class PlayerJumpState : PlayerState
{
    private readonly float jumpDuration = 0.2f;
    private float timer;

    public PlayerJumpState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        timer = jumpDuration;
        input.ConsumeJumpInput();
        locomotion.HandleJump(stateMachine.jumpHeight);

        animator.PlayTargetAnimation("Jump");
        animator.UpdateMoveSpeed(0);
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

        if (timer <= 0 && locomotion.PlayerVelocity.y <= 0)
        {
            stateMachine.SwitchState(new PlayerFallState(stateMachine));
        }
    }

    public override void Exit() { }
}