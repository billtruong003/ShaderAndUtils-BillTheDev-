namespace Orion
{
    public class PlayerGroundedState : State
    {
        public PlayerIdleState IdleState { get; }
        public PlayerWalkState WalkState { get; }
        public PlayerRunState RunState { get; }
        public PlayerCrouchState CrouchState { get; }
        public PlayerSlopeSlideState SlopeSlideState { get; }

        private readonly StateMachine _subStateMachine;

        public PlayerGroundedState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
        {
            _subStateMachine = new StateMachine();

            IdleState = new PlayerIdleState(player, _subStateMachine);
            WalkState = new PlayerWalkState(player, _subStateMachine);
            RunState = new PlayerRunState(player, _subStateMachine);
            CrouchState = new PlayerCrouchState(player, _subStateMachine);
            SlopeSlideState = new PlayerSlopeSlideState(player, _subStateMachine);
        }

        public override void Enter()
        {
            base.Enter();
            player.AnimationController.SetGrounded(true);
            player.CoyoteTimeCounter = player.CoyoteTime;
            _subStateMachine.Initialize(IdleState);
        }

        public override void Exit()
        {
            base.Exit();
            player.AnimationController.SetGrounded(false);
            _subStateMachine.CurrentState?.Exit();
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            if (player.Input.JumpWasPressed)
            {
                player.JumpBufferCounter = player.JumpBufferTime;
            }

            if (player.Input.DashWasPressed)
            {
                stateMachine.ChangeState(player.DashState);
                return;
            }

            if (player.JumpBufferCounter > 0f && player.CoyoteTimeCounter > 0f)
            {
                stateMachine.ChangeState(player.JumpState);
                return;
            }

            if (player.Input.CrouchWasPressed)
            {
                player.Input.UseCrouchInput();
                if (_subStateMachine.CurrentState == RunState && player.Input.MoveInput != UnityEngine.Vector2.zero)
                {
                    stateMachine.ChangeState(player.ActiveSlideState);
                    return;
                }
            }

            if (!player.IsGrounded)
            {
                stateMachine.ChangeState(player.FallState);
                return;
            }

            _subStateMachine.CurrentState.LogicUpdate();
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
            _subStateMachine.CurrentState.PhysicsUpdate();
        }
    }
}