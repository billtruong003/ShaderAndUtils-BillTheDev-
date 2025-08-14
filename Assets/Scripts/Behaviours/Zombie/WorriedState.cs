using UnityEngine;

namespace ZombieAI
{
    public class WorriedState : IState
    {
        private readonly Zombie _context;
        private float _searchTimer;

        public WorriedState(Zombie context)
        {
            _context = context;
        }

        public void Enter()
        {
            _searchTimer = 0f;
            _context.NavMeshAgent.ResetPath();
            _context.AnimationManager.SetWorried(true);
        }

        public void Execute()
        {
            _searchTimer += Time.deltaTime;

            Vector3 direction = (_context.LastHeardSoundPosition - _context.transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            _context.transform.rotation = Quaternion.Slerp(_context.transform.rotation, lookRotation, Time.deltaTime * 2f);

            if (_context.IsPlayerInSight())
            {
                _context.ChangeState(new AggroState(_context));
                return;
            }

            if (_searchTimer >= _context.Stats.WorriedDuration)
            {
                _context.ChangeState(new IdleState(_context));
            }
        }

        public void Exit()
        {
            _context.AnimationManager.SetWorried(false);
        }
    }
}