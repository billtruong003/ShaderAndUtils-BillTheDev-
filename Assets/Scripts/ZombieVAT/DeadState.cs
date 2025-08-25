using UnityEngine;

namespace ZombieAI.VAT
{
    public class DeadState : IState
    {
        private readonly VAT_Zombie _context;

        public DeadState(VAT_Zombie context)
        {
            _context = context;
        }

        public void Enter()
        {
            _context.AnimationManager.PlayDeath();
            _context.NavMeshAgent.enabled = false;
            _context.GetComponent<Collider>().enabled = false;
            _context.SetAsDead();
        }

        public void Execute() { }
        public void Exit() { }
    }
}