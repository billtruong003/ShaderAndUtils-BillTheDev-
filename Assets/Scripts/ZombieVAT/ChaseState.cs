using UnityEngine;

namespace ZombieAI.VAT
{
    public class ChaseState : IState
    {
        private readonly VAT_Zombie _context;
        private float _timeSinceLostSight = 0f;

        public ChaseState(VAT_Zombie context)
        {
            _context = context;
        }

        public void Enter()
        {
            _context.NavMeshAgent.speed = _context.Stats.ChaseSpeed;
            _context.AnimationManager.PlayChase();
        }

        public void Execute()
        {
            if (_context.PlayerTransform == null)
            {
                _context.ChangeState(new IdleState(_context));
                return;
            }

            if (_context.IsPlayerInSight())
            {
                _timeSinceLostSight = 0f;
                _context.NavMeshAgent.SetDestination(_context.PlayerTransform.position);

                if (_context.NavMeshAgent.remainingDistance <= _context.Stats.Attacks[0].Range)
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
            if (_context.NavMeshAgent.isOnNavMesh)
            {
                _context.NavMeshAgent.ResetPath();
            }
            _context.AnimationManager.PlayIdle();
        }
    }
}