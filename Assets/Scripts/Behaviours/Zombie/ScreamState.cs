using UnityEngine;

namespace ZombieAI
{
    public class ScreamState : IState
    {
        private readonly Zombie _context;
        private float _screamTimer;

        public ScreamState(Zombie context)
        {
            _context = context;
        }

        public void Enter()
        {
            _screamTimer = 0f;
            _context.NavMeshAgent.ResetPath();
            _context.AnimationManager.PlayScream();
            _context.PlaySound(_context.Stats.ScreamSound);
        }

        public void Execute()
        {
            _screamTimer += Time.deltaTime;
            if (_screamTimer >= _context.Stats.ScreamDuration)
            {
                _context.ChangeState(new ChaseState(_context));
            }
        }

        public void Exit() { }
    }
}