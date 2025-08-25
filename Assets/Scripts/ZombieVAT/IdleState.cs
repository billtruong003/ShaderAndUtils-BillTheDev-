using UnityEngine;
using UnityEngine.AI;

namespace ZombieAI.VAT
{
    public class IdleState : IState
    {
        private readonly VAT_Zombie _context;
        private float _wanderTimer;
        private const float WANDER_INTERVAL = 5f;

        public IdleState(VAT_Zombie context)
        {
            _context = context;
        }

        public void Enter()
        {
            _context.NavMeshAgent.speed = _context.Stats.WanderSpeed;
            _context.AnimationManager.PlayWalk();
            _wanderTimer = WANDER_INTERVAL;
        }

        public void Execute()
        {
            if (_context.IsPlayerInSight())
            {
                _context.ChangeState(new ScreamState(_context));
                return;
            }

            _wanderTimer += Time.deltaTime;

            if (!_context.NavMeshAgent.pathPending &&
                (_context.NavMeshAgent.remainingDistance < _context.NavMeshAgent.stoppingDistance || _wanderTimer >= WANDER_INTERVAL))
            {
                SetNewWanderDestination();
                _wanderTimer = 0f;
            }
        }

        public void Exit()
        {
            _context.AnimationManager.PlayIdle();
        }

        private void SetNewWanderDestination()
        {
            Vector3 randomDirection = Random.insideUnitSphere * _context.Stats.WanderRadius;
            randomDirection += _context.AnchorPoint;
            NavMesh.SamplePosition(randomDirection, out NavMeshHit navHit, _context.Stats.WanderRadius, NavMesh.AllAreas);
            _context.NavMeshAgent.SetDestination(navHit.position);
        }
    }
}