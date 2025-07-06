// File: Assets/MyIndieGame/Scripts/Controllers/StateMachine/States/PlayerJumpState.cs
using UnityEngine;

public class PlayerJumpState : PlayerState
{
    private float jumpStateTime = 0.2f;
    private float timer;

    public PlayerJumpState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        timer = jumpStateTime;
        input.ConsumeJumpInput();
        animator.UpdateMoveSpeed(0f, 0f);

        bool wasDoubleJump;
        bool jumpSucceeded = locomotion.PerformJump(
            stateMachine.jumpHeight,
            stateMachine.doubleJumpHeight,
            input.MoveInput,
            out wasDoubleJump
        );

        if (jumpSucceeded)
        {
            if (wasDoubleJump)
            {
                animator.PlayTargetAnimation("DoubleJump");
                particles?.PlayParticle(PlayerParticlesController.PlayerParticleType.Jump, locomotion.transform.position);
            }
            else
            {
                animator.PlayTargetAnimation("Jump");
                particles?.PlayParticle(PlayerParticlesController.PlayerParticleType.Jump, locomotion.transform.position);
            }
        }
        else
        {
            stateMachine.SwitchState(new PlayerFallState(stateMachine));
        }
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