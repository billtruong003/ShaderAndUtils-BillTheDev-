namespace Orion
{
    public abstract class State
    {
        protected readonly PlayerController player;
        protected readonly StateMachine stateMachine;

        protected State(PlayerController player, StateMachine stateMachine)
        {
            this.player = player;
            this.stateMachine = stateMachine;
        }

        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void LogicUpdate() { }
        public virtual void PhysicsUpdate() { }
    }
}