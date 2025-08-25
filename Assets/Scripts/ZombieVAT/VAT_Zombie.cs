using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using System.Collections.Generic;

namespace ZombieAI.VAT
{
    [RequireComponent(typeof(NavMeshAgent), typeof(VAT_ZombieAnimationManager), typeof(CapsuleCollider))]
    public class VAT_Zombie : MonoBehaviour
    {
        [SerializeField] private ZombieStats stats;

        public ZombieStats Stats => stats;
        public Transform PlayerTransform { get; private set; }
        public NavMeshAgent NavMeshAgent { get; private set; }
        public VAT_ZombieAnimationManager AnimationManager { get; private set; }
        public VAT_Animator VatAnimator { get; private set; }

        public Vector3 AnchorPoint { get; private set; }
        public Vector3 LastHeardSoundPosition { get; private set; }
        public int CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }

        private IState _currentState;
        private CapsuleCollider _collider;

        private void Awake()
        {
            NavMeshAgent = GetComponent<NavMeshAgent>();
            AnimationManager = GetComponent<VAT_ZombieAnimationManager>();
            VatAnimator = GetComponent<VAT_Animator>();
            _collider = GetComponent<CapsuleCollider>();
        }

        private void OnEnable()
        {
            ResetForPooling();
            VAT_ZombieDirector.Instance?.Register(this);
        }

        private void OnDisable()
        {
            VAT_ZombieDirector.Instance?.Unregister(this);
        }

        public void Setup(Transform player)
        {
            PlayerTransform = player;
            CurrentHealth = stats.MaxHealth;
            NavMeshAgent.speed = stats.WanderSpeed;
            NavMeshAgent.angularSpeed = stats.TurnSpeed;
            SetAnchorPoint(transform.position);
            ChangeState(new IdleState(this));
        }

        private void ResetForPooling()
        {
            if (NavMeshAgent != null) NavMeshAgent.enabled = true;
            if (_collider == null) _collider = GetComponent<CapsuleCollider>();

            _collider.enabled = true;
            IsDead = false;
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
            // The director will automatically handle the dead (culled) zombie.
            // We just need to disable its logic and collider.
            StartCoroutine(ReturnToPoolAfterDelay());
        }

        private System.Collections.IEnumerator ReturnToPoolAfterDelay()
        {
            yield return new WaitForSeconds(stats.DespawnTimeAfterDeath);
            // This object should be managed by a pooling system, which would call OnDisable
            gameObject.SetActive(false);
        }

        // --- CÁC HÀM LOGIC GỐC (GIỮ NGUYÊN HOẶC CHỈNH SỬA NHẸ) ---

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

        // Các hàm khác như IsPlayerInAttackRange, OnHeardSound... có thể được giữ nguyên
        // vì chúng là logic AI và không liên quan trực tiếp đến hệ thống render.
        // Lưu ý: Các state (Idle, Chase, etc.) cần được điều chỉnh để tham chiếu đến VAT_Zombie thay vì Zombie.
    }
}