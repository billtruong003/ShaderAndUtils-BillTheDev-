namespace StateSystem
{
    public abstract class State
    {
        protected readonly StateMachine StateMachine;

        protected State(StateMachine stateMachine)
        {
            this.StateMachine = stateMachine;
        }

        public virtual void Enter() { }
        public virtual void LogicUpdate() { }
        public virtual void PhysicsUpdate() { }
        public virtual void Exit() { }
    }
}