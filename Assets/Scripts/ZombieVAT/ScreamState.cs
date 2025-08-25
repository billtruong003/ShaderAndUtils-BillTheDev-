using UnityEngine;

namespace ZombieAI.VAT
{
    public class ScreamState : IState
    {
        private readonly VAT_Zombie _context;
        private float _screamTimer;

        public ScreamState(VAT_Zombie context)
        {
            _context = context;
        }

        public void Enter()
        {
            _screamTimer = 0f;
            _context.NavMeshAgent.ResetPath();
            _context.AnimationManager.PlayScream();
        }

        public void Execute()
        {
            if (_context.PlayerTransform != null)
            {
                Vector3 direction = (_context.PlayerTransform.position - _context.transform.position).normalized;
                Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                _context.transform.rotation = Quaternion.Slerp(_context.transform.rotation, lookRotation, Time.deltaTime * 5f);
            }

            _screamTimer += Time.deltaTime;
            if (_screamTimer >= _context.Stats.ScreamDuration)
            {
                _context.ChangeState(new ChaseState(_context));
            }
        }

        public void Exit() { }
    }
}