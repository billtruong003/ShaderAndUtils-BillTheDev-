using System.Collections;
using UnityEngine;

namespace ZombieAI
{
    public class BitingState : IState
    {
        private readonly Zombie _context;
        private readonly Transform _corpseTransform;
        private Transform _biteTarget;
        private float _bitingTimer;
        private bool _hasReachedCorpse = false;
        private Coroutine _soundCoroutine;

        public BitingState(Zombie context, Transform corpseTransform)
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

            // Tìm điểm cắn cụ thể, nếu không có thì dùng vị trí của xác
            var bitePoint = _corpseTransform.Find("BitePoint");
            _biteTarget = bitePoint ?? _corpseTransform;

            _context.NavMeshAgent.SetDestination(_biteTarget.position);
            _context.AnimationManager.SetMovement(0.5f, 0f);
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
                    _context.transform.LookAt(_biteTarget.position);
                    _context.AnimationManager.SetMovement(0f, 0f);
                    _context.AnimationManager.PlayBiting();
                    _context.IKTarget = _biteTarget; // Gán mục tiêu IK
                    _bitingTimer = 0f;

                    _soundCoroutine = _context.StartCoroutine(BitingSoundRoutine());

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
            _context.IKTarget = null; // Rất quan trọng: Xóa mục tiêu IK khi thoát
            _context.AnimationManager.SetMovement(0f, 0f);

            if (_soundCoroutine != null)
            {
                _context.StopCoroutine(_soundCoroutine);
            }
        }

        private IEnumerator BitingSoundRoutine()
        {
            while (true)
            {
                _context.PlayRandomSound(_context.Stats.BitingSounds);
                yield return new WaitForSeconds(Random.Range(6, 8)); // Tiếng ăn gặm thường xuyên
            }
        }
    }
}