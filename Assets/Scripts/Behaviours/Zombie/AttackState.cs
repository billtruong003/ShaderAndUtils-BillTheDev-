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
            _context.NavMeshAgent.enabled = false; // Vô hiệu hóa agent để Root Motion hoạt động
            TryToPerformAttack();
        }

        public void Execute()
        {
            // Nếu animation đang chạy, không làm gì cả, chờ event
            if (_isAttackAnimationPlaying) return;

            // Sau khi animation kết thúc, kiểm tra lại tình hình
            // Nếu người chơi vẫn trong tầm, tấn công tiếp (sau khi hết cooldown)
            // Nếu không, quay lại trạng thái rượt đuổi
            if (!IsPlayerStillInAttackRange())
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
            // Kích hoạt lại NavMeshAgent trước khi chuyển state
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

            var availableAttacks = _context.Stats.Attacks.FindAll(a =>
                Vector3.Distance(_context.transform.position, _context.PlayerTransform.position) <= a.Range);

            if (availableAttacks.Count == 0) return;

            var attackToPerform = availableAttacks[Random.Range(0, availableAttacks.Count)];

            _context.CurrentAttack = attackToPerform;
            _context.AnimationManager.PlayAttack(attackToPerform.AnimationTriggerName);
            _attackCooldownTimer = attackToPerform.Cooldown;
            _isAttackAnimationPlaying = true;
        }

        // Được gọi từ Zombie.cs thông qua Animation Event
        public void OnAttackAnimationFinished()
        {
            _isAttackAnimationPlaying = false;
        }

        private bool IsPlayerStillInAttackRange()
        {
            if (_context.PlayerTransform == null) return false;

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