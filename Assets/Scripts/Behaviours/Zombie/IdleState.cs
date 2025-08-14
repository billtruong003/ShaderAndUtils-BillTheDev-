using UnityEngine;
using UnityEngine.AI;

namespace ZombieAI
{
    public class IdleState : IState
    {
        private readonly Zombie _context;
        private float _searchTimer;

        public IdleState(Zombie context)
        {
            _context = context;
        }

        public void Enter()
        {
            _context.NavMeshAgent.speed = _context.Stats.WanderSpeed;
            _context.AnimationManager.SetMovement(0.5f, 0f);
            _searchTimer = _context.Stats.SearchForCorpseInterval; // Bắt đầu quét ngay
            SetNewWanderDestination();
        }

        public void Execute()
        {
            if (_context.IsPlayerInSight())
            {
                _context.ChangeState(new ScreamState(_context));
                return;
            }

            if (!_context.NavMeshAgent.pathPending && _context.NavMeshAgent.remainingDistance < 1.5f)
            {
                SetNewWanderDestination();
            }

            _searchTimer += Time.deltaTime;
            if (_searchTimer >= _context.Stats.SearchForCorpseInterval)
            {
                _searchTimer = 0f;
                LookForCorpses();
            }
        }

        public void Exit()
        {
            _context.AnimationManager.SetMovement(0f, 0f);
        }

        private void SetNewWanderDestination()
        {
            Vector3 randomDirection = Random.insideUnitSphere * _context.Stats.WanderRadius;
            randomDirection += _context.AnchorPoint;
            NavMesh.SamplePosition(randomDirection, out NavMeshHit navHit, _context.Stats.WanderRadius, NavMesh.AllAreas);
            _context.NavMeshAgent.SetDestination(navHit.position);
        }

        private void LookForCorpses()
        {
            var colliders = Physics.OverlapSphere(_context.transform.position, _context.Stats.WanderRadius, _context.Stats.CorpseLayer);
            if (colliders.Length > 0)
            {
                Transform targetCorpse = colliders[0].transform;
                _context.ChangeState(new BitingState(_context, targetCorpse));
            }
        }
    }
}