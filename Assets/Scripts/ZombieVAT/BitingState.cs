using UnityEngine;

namespace ZombieAI.VAT
{
    public class BitingState : IState
    {
        private readonly VAT_Zombie _context;
        private readonly Transform _corpseTransform;
        private float _bitingTimer;
        private bool _hasReachedCorpse;

        public BitingState(VAT_Zombie context, Transform corpseTransform)
        {
            _context = context;
            _corpseTransform = corpseTransform;
        }

        public void Enter()
        {
            if (_corpseTransform == null)
            {
                _context.ChangeState(new IdleState(_context));
                return;
            }

            _hasReachedCorpse = false;
            _context.NavMeshAgent.SetDestination(_corpseTransform.position);
            _context.AnimationManager.PlayWalk();
        }

        public void Execute()
        {
            if (_corpseTransform == null)
            {
                _context.ChangeState(new IdleState(_context));
                return;
            }

            if (!_hasReachedCorpse)
            {
                if (!_context.NavMeshAgent.pathPending && _context.NavMeshAgent.remainingDistance < 1.5f)
                {
                    _hasReachedCorpse = true;
                    _context.NavMeshAgent.ResetPath();
                    _context.transform.LookAt(_corpseTransform.position);
                    _context.AnimationManager.PlayAttack(); // Giả sử đây là animation "Biting"
                    _bitingTimer = 0f;
                }
            }
            else
            {
                _bitingTimer += Time.deltaTime;
                if (_bitingTimer >= _context.Stats.BitingDuration)
                {
                    _context.ChangeState(new IdleState(_context));
                }
            }
        }

        public void Exit()
        {
            _context.AnimationManager.PlayIdle();
        }
    }
}