using UnityEngine;
using UnityEngine.AI;
using Sirenix.OdinInspector;
using System.Linq;
using BillTheDev.Anim;

namespace ZombieAI
{
    [RequireComponent(typeof(NavMeshAgent), typeof(ZombieAnimationManager), typeof(CapsuleCollider))]
    public class Zombie : MonoBehaviour
    {
        [Title("Configuration")]
        [Required("Zombie must have a Stats asset.")]
        [SerializeField] private ZombieStats stats;

        public ZombieStats Stats => stats;
        public Transform PlayerTransform { get; private set; }
        public NavMeshAgent NavMeshAgent { get; private set; }
        public ZombieAnimationManager AnimationManager { get; private set; }
        public DynamicAnimationEventHub EventHub { get; private set; }
        public ZombieDirector Director { get; private set; }
        public Vector3 AnchorPoint { get; private set; }
        public Vector3 LastHeardSoundPosition { get; private set; }
        public AttackDefinition CurrentAttack { get; set; }
        public int CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }
        public Transform IKTarget { get; set; }

        private IState _currentState;
        private CapsuleCollider _collider;

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
            EventHub = GetComponent<DynamicAnimationEventHub>();
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
            gameObject.name = stats.ZombieName;
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

        public void SetAsDead()
        {
            IsDead = true;
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

        [Button("Event: Perform Damage Check", ButtonSizes.Medium), GUIColor(0.8f, 0.4f, 0.4f)]
        public void Event_PerformDamageCheck()
        {
            if (PlayerTransform == null || CurrentAttack == null || IsDead) return;
            float distanceToPlayer = Vector3.Distance(transform.position, PlayerTransform.position);
            if (distanceToPlayer <= CurrentAttack.Range)
            {
                // Thực hiện logic gây sát thương cho người chơi tại đây
                // Ví dụ: PlayerTransform.GetComponent<PlayerHealth>()?.TakeDamage(CurrentAttack.Damage);
                Debug.Log($"Zombie '{name}' dealt {CurrentAttack.Damage} damage to player.");
            }
        }

        [Button("Event: Attack Animation Finished", ButtonSizes.Medium), GUIColor(0.4f, 0.8f, 0.4f)]
        public void Event_AttackAnimationFinished()
        {
            (_currentState as AttackState)?.OnAttackAnimationFinished();
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

        public bool IsPlayerInAttackRange(out AttackDefinition availableAttack)
        {
            availableAttack = null;
            if (PlayerTransform == null) return false;

            var possibleAttacks = Stats.Attacks
                .Where(a => Vector3.Distance(transform.position, PlayerTransform.position) <= a.Range)
                .ToList();

            if (possibleAttacks.Any())
            {
                availableAttack = possibleAttacks[Random.Range(0, possibleAttacks.Count)];
                return true;
            }

            return false;
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (IsDead || AnimationManager.Animator == null || IKTarget == null)
            {
                AnimationManager.Animator.SetLookAtWeight(0f);
                return;
            }

            AnimationManager.Animator.SetLookAtWeight(1.0f, 0.3f, 1.0f, 0.0f, 0.5f);
            AnimationManager.Animator.SetLookAtPosition(IKTarget.position);
        }
    }
}