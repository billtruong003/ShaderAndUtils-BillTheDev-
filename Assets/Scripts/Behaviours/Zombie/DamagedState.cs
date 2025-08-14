using UnityEngine;

namespace ZombieAI
{
    public class DamagedState : IState
    {
        private readonly Zombie _context;
        private float _recoveryTimer;

        public DamagedState(Zombie context)
        {
            _context = context;
        }

        public void Enter()
        {
            _recoveryTimer = 0f;
            _context.NavMeshAgent.ResetPath();
            _context.AnimationManager.PlayTakeDamage();
        }

        public void Execute()
        {
            _recoveryTimer += Time.deltaTime;
            if (_recoveryTimer >= _context.Stats.DamagedRecoveryTime)
            {
                if (_context.IsPlayerInSight())
                {
                    _context.ChangeState(new ChaseState(_context));
                }
                else
                {
                    _context.ChangeState(new IdleState(_context));
                }
            }
        }

        public void Exit() { }
    }
}