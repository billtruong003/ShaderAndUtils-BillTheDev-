using UnityEngine;

namespace ZombieAI
{
    public class AttackState : IState
    {
        private readonly Zombie _context;
        private bool _isAttackAnimationPlaying;
        private float _attackCooldownTimer;

        public AttackState(Zombie context)
        {
            _context = context;
        }

        public void Enter()
        {
            _isAttackAnimationPlaying = false;
            _context.NavMeshAgent.enabled = false;
            TryToPerformAttack();
        }

        public void Execute()
        {
            if (_isAttackAnimationPlaying) return;

            if (!_context.IsPlayerInAttackRange(out _))
            {
                _context.ChangeState(new ChaseState(_context));
                return;
            }

            _attackCooldownTimer -= Time.deltaTime;
            if (_attackCooldownTimer <= 0)
            {
                TryToPerformAttack();
            }
        }

        public void Exit()
        {
            if (_context != null && !_context.IsDead)
            {
                _context.NavMeshAgent.enabled = true;
            }
        }

        private void TryToPerformAttack()
        {
            if (_context.PlayerTransform == null)
            {
                _context.ChangeState(new IdleState(_context));
                return;
            }

            Vector3 directionToPlayer = _context.PlayerTransform.position - _context.transform.position;
            directionToPlayer.y = 0;
            _context.transform.rotation = Quaternion.LookRotation(directionToPlayer);

            if (_context.IsPlayerInAttackRange(out AttackDefinition attackToPerform))
            {
                _context.CurrentAttack = attackToPerform;
                _context.AnimationManager.PlayAttack(attackToPerform.AnimationTriggerName);
                _context.PlaySound(_context.Stats.AttackSound);
                _attackCooldownTimer = attackToPerform.Cooldown;
                _isAttackAnimationPlaying = true;
            }
            else
            {
                _context.ChangeState(new ChaseState(_context));
            }
        }

        public void OnAttackAnimationFinished()
        {
            _isAttackAnimationPlaying = false;
        }
    }
}