using UnityEngine;

namespace ZombieAI.VAT
{
    public class AttackState : IState
    {
        private readonly VAT_Zombie _context;
        private float _attackTimer;
        private bool _hasAttacked;

        public AttackState(VAT_Zombie context)
        {
            _context = context;
        }

        public void Enter()
        {
            _attackTimer = _context.Stats.Attacks[0].Cooldown;
            _hasAttacked = false;
            _context.NavMeshAgent.ResetPath();
            _context.AnimationManager.PlayAttack();
        }

        public void Execute()
        {
            if (_context.PlayerTransform == null)
            {
                _context.ChangeState(new IdleState(_context));
                return;
            }

            Vector3 directionToPlayer = _context.PlayerTransform.position - _context.transform.position;
            directionToPlayer.y = 0;
            _context.transform.rotation = Quaternion.LookRotation(directionToPlayer);

            _attackTimer -= Time.deltaTime;

            if (_attackTimer <= 0)
            {
                if (_context.IsPlayerInSight() && Vector3.Distance(_context.transform.position, _context.PlayerTransform.position) <= _context.Stats.Attacks[0].Range)
                {
                    _context.ChangeState(new AttackState(_context));
                }
                else
                {
                    _context.ChangeState(new ChaseState(_context));
                }
            }
        }

        public void Exit() { }
    }
}