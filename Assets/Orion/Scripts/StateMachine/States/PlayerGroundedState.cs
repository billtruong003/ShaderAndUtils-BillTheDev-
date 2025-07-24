namespace Orion
{
    public class PlayerGroundedState : State
    {
        public PlayerIdleState IdleState { get; }
        public PlayerWalkState WalkState { get; }
        public PlayerRunState RunState { get; }
        public PlayerSlideState SlideState { get; }

        private readonly StateMachine _subStateMachine;

        public PlayerGroundedState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
        {
            _subStateMachine = new StateMachine();

            IdleState = new PlayerIdleState(player, _subStateMachine);
            WalkState = new PlayerWalkState(player, _subStateMachine);
            RunState = new PlayerRunState(player, _subStateMachine);
            SlideState = new PlayerSlideState(player, _subStateMachine);
        }

        public override void Enter()
        {
            base.Enter();
            player.AnimationController.SetGrounded(true);
            player.CoyoteTimeCounter = player.GetCoyoteTime();
            _subStateMachine.Initialize(IdleState);
        }

        public override void Exit()
        {
            base.Exit();
            player.AnimationController.SetGrounded(false);
            _subStateMachine.CurrentState.Exit();
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();
            _subStateMachine.CurrentState.LogicUpdate();

            if (player.Input.JumpWasPressed)
            {
                player.JumpBufferCounter = player.GetJumpBufferTime();
            }

            if (player.Input.DashWasPressed)
            {
                stateMachine.ChangeState(player.DashState);
                return;
            }

            if (player.JumpBufferCounter > 0f)
            {
                stateMachine.ChangeState(player.JumpState);
                return;
            }

            if (!player.IsGrounded())
            {
                stateMachine.ChangeState(player.FallState);
                return;
            }
        }

        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
            _subStateMachine.CurrentState.PhysicsUpdate();
        }
    }
}