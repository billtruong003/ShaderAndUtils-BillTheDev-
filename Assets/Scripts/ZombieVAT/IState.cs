namespace ZombieAI.VAT
{
    public interface IState
    {
        void Enter();
        void Execute();
        void Exit();
    }
}