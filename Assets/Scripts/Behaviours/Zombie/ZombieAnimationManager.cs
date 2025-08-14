using UnityEngine;

namespace ZombieAI
{
    public class ZombieAnimationManager : MonoBehaviour
    {
        [SerializeField] private Animator _animator;

        public Animator Animator => _animator;

        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int Turn = Animator.StringToHash("Turn");
        private static readonly int IsAggro = Animator.StringToHash("IsAggro");
        private static readonly int IsWorried = Animator.StringToHash("IsWorried");

        private static readonly int TriggerScream = Animator.StringToHash("Scream");
        private static readonly int TriggerTakeDamage = Animator.StringToHash("TakeDamage");
        private static readonly int TriggerDeath = Animator.StringToHash("Death");
        private static readonly int TriggerBiting = Animator.StringToHash("Biting");

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponent<Animator>();
        }

        public void SetMovement(float speed, float turn)
        {
            _animator.SetFloat(Speed, speed);
            _animator.SetFloat(Turn, turn);
        }

        public void SetAggro(bool isAggro)
        {
            _animator.SetBool(IsAggro, isAggro);
        }

        public void SetWorried(bool isWorried)
        {
            _animator.SetBool(IsWorried, isWorried);
        }

        public void PlayBiting()
        {
            _animator.SetTrigger(TriggerBiting);
        }

        public void PlayScream()
        {
            _animator.SetTrigger(TriggerScream);
        }

        public void PlayTakeDamage()
        {
            _animator.SetTrigger(TriggerTakeDamage);
        }

        public void PlayDeath()
        {
            _animator.SetTrigger(TriggerDeath);
        }

        public void PlayAttack(string triggerName)
        {
            _animator.SetTrigger(triggerName);
        }
    }
}