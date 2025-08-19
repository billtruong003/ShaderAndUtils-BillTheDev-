using UnityEngine;
using System.Collections;

namespace ZombieAI
{
    public class DeadState : IState
    {
        private readonly Zombie _context;

        public DeadState(Zombie context)
        {
            _context = context;
        }

        public void Enter()
        {
            _context.SetAsDead(); // This will notify the Director
            _context.NavMeshAgent.enabled = false;
            _context.GetComponent<Collider>().enabled = false;
            _context.AnimationManager.PlayDeath();
            _context.PlaySound(_context.Stats.DeathSound);

            _context.StartCoroutine(ReturnToPoolAfterDelay());
        }

        private IEnumerator ReturnToPoolAfterDelay()
        {
            yield return new WaitForSeconds(_context.Stats.DespawnTimeAfterDeath);
            // Crucially, pass the original prefab for correct pooling
            ZombiePoolManager.Instance.ReturnToPool(_context.gameObject, _context.OriginalPrefab);
        }

        public void Execute() { }
        public void Exit() { }
    }
}