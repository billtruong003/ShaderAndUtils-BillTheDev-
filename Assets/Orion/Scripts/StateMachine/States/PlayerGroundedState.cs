namespace Orion
{
    public class PlayerGroundedState : State
    {
        // Sub-states are now accessible via clean, read-only properties.
        public PlayerIdleState IdleState { get; }
        public PlayerWalkState WalkState { get; }
        public PlayerSlideState SlideState { get; }

        private readonly StateMachine _subStateMachine;

        public PlayerGroundedState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
        {
            _subStateMachine = new StateMachine();

            // Initialize the sub-states
            IdleState = new PlayerIdleState(player, _subStateMachine);
            WalkState = new PlayerWalkState(player, _subStateMachine);
            SlideState = new PlayerSlideState(player, _subStateMachine);
        }

        public override void Enter()
        {
            base.Enter();
            player.CoyoteTimeCounter = player.GetCoyoteTime();
            _subStateMachine.Initialize(IdleState);
        }

        public override void Exit()
        {
            base.Exit();
            // Ensure any sub-state logic is also exited if necessary
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

            // Transition to AirborneState if jump is buffered or coyote time is available
            if (player.JumpBufferCounter > 0f)
            {
                stateMachine.ChangeState(player.AirborneState);
                return; // Important to return after a state change
            }

            // Transition to AirborneState when falling off a ledge
            if (!player.IsGrounded())
            {
                stateMachine.ChangeState(player.AirborneState);
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