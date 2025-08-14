using UnityEngine;

namespace ZombieAI
{
    public class AggroState : IState
    {
        private readonly Zombie _context;
        private float _stareTimer;

        public AggroState(Zombie context)
        {
            _context = context;
        }

        public void Enter()
        {
            _stareTimer = 0f;
            _context.NavMeshAgent.ResetPath();
            _context.AnimationManager.SetAggro(true);
        }

        public void Execute()
        {
            if (_context.PlayerTransform == null)
            {
                _context.ChangeState(new IdleState(_context));
                return;
            }

            _context.transform.LookAt(_context.PlayerTransform.position);

            _stareTimer += Time.deltaTime;

            if (_stareTimer >= _context.Stats.AggroStareDuration)
            {
                _context.ChangeState(new ChaseState(_context));
            }
        }

        public void Exit()
        {
            _context.AnimationManager.SetAggro(false);
        }
    }
}