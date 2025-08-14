using UnityEngine;

namespace ZombieAI
{
    public class ChaseState : IState
    {
        private readonly Zombie _context;
        private float _timeSinceLostSight = 0f;

        public ChaseState(Zombie context)
        {
            _context = context;
        }

        public void Enter()
        {
            _context.NavMeshAgent.speed = _context.Stats.ChaseSpeed;
            _context.AnimationManager.SetMovement(1f, 0f);
        }

        public void Execute()
        {
            if (_context.IsPlayerInSight())
            {
                _timeSinceLostSight = 0f;
                _context.NavMeshAgent.SetDestination(_context.PlayerTransform.position);

                if (IsCloseEnoughToAttack())
                {
                    _context.ChangeState(new AttackState(_context));
                }
            }
            else
            {
                _timeSinceLostSight += Time.deltaTime;
                if (_timeSinceLostSight > _context.Stats.TimeToForgetPlayer)
                {
                    _context.ChangeState(new IdleState(_context));
                }
            }
        }

        public void Exit()
        {
            _context.NavMeshAgent.ResetPath();
            _context.AnimationManager.SetMovement(0f, 0f);
        }

        private bool IsCloseEnoughToAttack()
        {
            foreach (var attack in _context.Stats.Attacks)
            {
                if (Vector3.Distance(_context.transform.position, _context.PlayerTransform.position) <= attack.Range)
                {
                    return true;
                }
            }
            return false;
        }
    }
}