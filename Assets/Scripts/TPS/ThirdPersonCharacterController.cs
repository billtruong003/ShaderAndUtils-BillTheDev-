using UnityEngine;

public class ThirdPersonCharacterController : CharacterStats
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public Transform cameraTransform;
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Transform skillPoint;
    public float attackRange = 2f; // Phạm vi SphereCast cho attack
    public float skillRange = 3f; // Phạm vi SphereCast cho skill
    public LayerMask enemyLayer; // Layer của enemy

    private bool isAttacking = false;
    private bool isUsingSkill = false;

    protected override void Start()
    {
        base.Start();
        if (animator == null) animator = GetComponent<Animator>();
        if (controller == null) controller = GetComponent<CharacterController>();
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        if (currentHP <= 0) return;

        HandleMovement();
        HandleActions();
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 moveDirection = new Vector3(horizontal, 0f, vertical).normalized;

        if (!isAttacking && !isUsingSkill && moveDirection.magnitude > 0.1f)
        {
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 desiredDirection = (forward * vertical + right * horizontal).normalized;
            Vector3 move = desiredDirection * moveSpeed * Time.deltaTime;
            controller.Move(move);

            if (desiredDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(desiredDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            currentRage = Mathf.Min(currentRage + rageGainPerMove * Time.deltaTime, maxRage);
            animator.SetBool("IsMoving", true);
        }
        else
        {
            animator.SetBool("IsMoving", false);
        }
    }

    void HandleActions()
    {
        if (Input.GetKeyDown(KeyCode.J) && CanAttack())
        {
            isAttacking = true;
            animator.SetTrigger("Attack");
            lastAttackTime = Time.time;
            PerformAttack();
        }

        if (Input.GetKeyDown(KeyCode.H) && CanSkill())
        {
            isUsingSkill = true;
            animator.SetTrigger("Skill");
            lastSkillTime = Time.time;
            currentRage -= rageCostForSkill;
            PerformSkill();
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            TakeDamage(10f);
            Debug.Log("HP remaining: " + currentHP);
        }
    }

    void PerformAttack()
    {
        RaycastHit[] hits = Physics.SphereCastAll(attackPoint.position, attackRange, transform.forward, 0f, enemyLayer);
        foreach (var hit in hits)
        {
            if (hit.collider.TryGetComponent<Enemy>(out Enemy enemy))
            {
                float damage = attackDamage;
                if (Random.value <= criticalChance)
                {
                    damage *= criticalMultiplier;
                    ShowFloatingText(hit.point, damage.ToString() + " CRIT!");
                }
                else
                {
                    ShowFloatingText(hit.point, damage.ToString());
                }
                enemy.TakeDamage(damage);
                currentRage = Mathf.Min(currentRage + damage * 0.1f, maxRage); // Tích nộ 10% damage
            }
        }
    }

    void PerformSkill()
    {
        RaycastHit[] hits = Physics.SphereCastAll(skillPoint.position, skillRange, transform.forward, 0f, enemyLayer);
        foreach (var hit in hits)
        {
            if (hit.collider.TryGetComponent<Enemy>(out Enemy enemy))
            {
                float damage = skillDamage;
                if (Random.value <= criticalChance)
                {
                    damage *= criticalMultiplier;
                    ShowFloatingText(hit.point, damage.ToString() + " CRIT!");
                }
                else
                {
                    ShowFloatingText(hit.point, damage.ToString());
                }
                enemy.TakeDamage(damage);
                currentRage = Mathf.Min(currentRage + damage * 0.1f, maxRage); // Tích nộ 10% damage
            }
        }
    }

    void ShowFloatingText(Vector3 position, string text)
    {
        // Giả lập floating text (cần UI Text prefab trong Unity)
        Debug.Log("Floating Text at " + position + ": " + text);
        // Trong Unity, tạo một prefab Text và dùng Object Pool để spawn tại position
    }

    public void OnAttackEnd()
    {
        isAttacking = false;
    }

    public void OnSkillEnd()
    {
        isUsingSkill = false;
    }

    protected override void Die()
    {
        animator.SetTrigger("IsDead");
        this.enabled = false;
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
        if (skillPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(skillPoint.position, skillRange);
        }
    }
}