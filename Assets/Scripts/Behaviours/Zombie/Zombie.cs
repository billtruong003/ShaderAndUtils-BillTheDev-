using UnityEngine;
using UnityEngine.AI;
using Sirenix.OdinInspector;

namespace ZombieAI
{
    [RequireComponent(typeof(NavMeshAgent), typeof(ZombieAnimationManager), typeof(CapsuleCollider))]
    public class Zombie : MonoBehaviour
    {
        [Title("Configuration")]
        [Required("Zombie must have a Stats asset.")]
        [SerializeField] private ZombieStats stats;

        // --- Public Properties for States ---
        public ZombieStats Stats => stats;
        public Transform PlayerTransform { get; private set; }
        public NavMeshAgent NavMeshAgent { get; private set; }
        public ZombieAnimationManager AnimationManager { get; private set; }
        public ZombieDirector Director { get; private set; }
        public Vector3 AnchorPoint { get; private set; }
        public Vector3 LastHeardSoundPosition { get; private set; }
        public AttackDefinition CurrentAttack { get; set; }
        public int CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }
        public Transform IKTarget { get; set; }

        public void AlreadyDead() => IsDead = true;
        private IState _currentState;
        private CapsuleCollider _collider;

        [Title("Debugging")]
        [ShowIf("Application.isPlaying")]
        [Button("Instant Kill", ButtonSizes.Large), GUIColor(1, 0.2f, 0.2f)]
        private void Debug_InstantKill()
        {
            if (IsDead) return;
            ChangeState(new DeadState(this));
        }

        private void OnEnable()
        {
            if (NavMeshAgent != null) NavMeshAgent.enabled = true;
            if (_collider == null) _collider = GetComponent<CapsuleCollider>();
            _collider.enabled = true;
            IKTarget = null;
        }

        private void Awake()
        {
            NavMeshAgent = GetComponent<NavMeshAgent>();
            AnimationManager = GetComponent<ZombieAnimationManager>();
            _collider = GetComponent<CapsuleCollider>();
        }

        public void Initialize(Transform player, ZombieDirector director)
        {
            PlayerTransform = player;
            Director = director;
            CurrentHealth = stats.MaxHealth;
            IsDead = false;
            NavMeshAgent.speed = stats.WanderSpeed;
            NavMeshAgent.angularSpeed = stats.TurnSpeed;
            ChangeState(new IdleState(this));
        }

        private void Update()
        {
            if (IsDead) return;
            _currentState?.Execute();
        }

        public void ChangeState(IState newState)
        {
            _currentState?.Exit();
            _currentState = newState;
            _currentState.Enter();
        }

        public void SetAnchorPoint(Vector3 point)
        {
            AnchorPoint = point;
        }

        public void OnHeardSound(Vector3 soundPosition)
        {
            if (IsDead || !(_currentState is IdleState)) return;
            LastHeardSoundPosition = soundPosition;
            ChangeState(new WorriedState(this));
        }

        public void TakeDamage(int damage)
        {
            if (IsDead) return;
            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
            if (CurrentHealth <= 0)
            {
                ChangeState(new DeadState(this));
            }
            else
            {
                ChangeState(new DamagedState(this));
            }
        }

        public void AnimationEvent_DealDamage()
        {
            if (PlayerTransform == null || CurrentAttack == null || IsDead) return;
            float distanceToPlayer = Vector3.Distance(transform.position, PlayerTransform.position);
            if (distanceToPlayer <= CurrentAttack.Range)
            {
                // PlayerHealth playerHealth = PlayerTransform.GetComponent<PlayerHealth>();
                // playerHealth?.TakeDamage(CurrentAttack.Damage);
            }
        }

        public void AnimationEvent_AttackFinished()
        {
            if (_currentState is AttackState attackState)
            {
                attackState.OnAttackAnimationFinished();
            }
        }

        public bool IsPlayerInSight()
        {
            if (PlayerTransform == null) return false;
            Vector3 directionToPlayer = PlayerTransform.position - transform.position;
            float distanceToPlayer = directionToPlayer.magnitude;
            if (distanceToPlayer > stats.ViewRange) return false;
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer.normalized);
            if (angleToPlayer > stats.ViewAngle / 2f) return false;
            if (Physics.Raycast(transform.position, directionToPlayer.normalized, distanceToPlayer, stats.ObstacleLayer)) return false;
            return true;
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (IsDead || AnimationManager.Animator == null) return;

            if (IKTarget != null)
            {
                AnimationManager.Animator.SetLookAtWeight(1.0f, 0.3f, 1.0f, 0.0f, 0.5f);
                AnimationManager.Animator.SetLookAtPosition(IKTarget.position);
            }
            else
            {
                AnimationManager.Animator.SetLookAtWeight(0f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (stats == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(AnchorPoint, stats.WanderRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, stats.ViewRange);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, stats.HearingRange);
        }
    }
}