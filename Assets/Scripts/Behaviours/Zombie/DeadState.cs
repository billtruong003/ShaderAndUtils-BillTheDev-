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
            _context.SetAsDead();
            _context.NavMeshAgent.enabled = false;
            _context.GetComponent<Collider>().enabled = false;
            _context.AnimationManager.PlayDeath();
            _context.Director.OnZombieDied();

            _context.StartCoroutine(ReturnToPoolAfterDelay());
        }

        private IEnumerator ReturnToPoolAfterDelay()
        {
            yield return new WaitForSeconds(_context.Stats.DespawnTimeAfterDeath);
            ZombiePoolManager.Instance.ReturnToPool(_context.gameObject);
        }

        public void Execute() { }
        public void Exit() { }
    }
}