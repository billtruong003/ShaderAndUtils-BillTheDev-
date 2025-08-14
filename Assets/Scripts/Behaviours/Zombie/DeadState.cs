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
            _context.AlreadyDead();
            _context.NavMeshAgent.enabled = false;
            _context.GetComponent<Collider>().enabled = false;
            _context.AnimationManager.PlayDeath();
            _context.Director.OnZombieDied();

            // Bắt đầu coroutine để trả về pool sau một thời gian
            _context.StartCoroutine(ReturnToPoolAfterDelay());
        }

        private IEnumerator ReturnToPoolAfterDelay()
        {
            // Chờ theo thời gian được định nghĩa trong ZombieStats
            yield return new WaitForSeconds(_context.Stats.DespawnTimeAfterDeath);

            // THAY ĐỔI: Trả về pool thay vì hủy
            ZombiePoolManager.Instance.ReturnToPool(_context.gameObject.name, _context.gameObject);
        }

        public void Execute() { }
        public void Exit() { }
    }
}