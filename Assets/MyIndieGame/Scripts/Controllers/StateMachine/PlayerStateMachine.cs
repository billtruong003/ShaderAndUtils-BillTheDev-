using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
    [field: SerializeField] public InputHandler InputHandler { get; private set; }
    [field: SerializeField] public PlayerLocomotion Locomotion { get; private set; }
    [field: SerializeField] public PlayerAnimator Animator { get; private set; }
    [field: SerializeField] public StatController Stats { get; private set; }
    [field: SerializeField] public EquipmentManager Equipment { get; private set; }

    // --- THUỘC TÍNH TARGETING ĐƯỢC THÊM VÀO ĐÂY ---
    // Nhớ tạo và kéo component TargetingController vào ô này trong Inspector
    [field: SerializeField] public TargetingController Targeting { get; private set; }

    [Header("State Parameters")]
    public float jumpHeight = 1.5f;

    private PlayerState currentState;

    void Start()
    {
        SwitchState(new PlayerGroundedState(this));
    }

    void Update()
    {
        // Xử lý Target Input trước khi Tick State
        if (InputHandler.TargetInput && Targeting != null)
        {
            InputHandler.ConsumeTargetInput();
            Targeting.HandleTargeting();
        }

        currentState?.Tick(Time.deltaTime);

        // Gọi validate mỗi frame để tự động hủy target nếu mục tiêu chết hoặc ngoài tầm
        Targeting?.ValidateTarget();

        Animator.SetGrounded(Locomotion.IsGrounded());
    }

    public void SwitchState(PlayerState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying == false) return;

        // --- GIZMOS CHO ATTACK STATE ---
        if (currentState is PlayerAttackState attackState)
        {
            AttackData attackData = attackState.CurrentAttackData;
            if (attackData != null)
            {
                Vector3 hitboxCenter = transform.position + transform.forward * 1.0f;
                bool isHitboxActive = attackState.TimeSinceEntered >= attackData.hitboxStartTime &&
                                      attackState.TimeSinceEntered <= attackData.hitboxEndTime;

                Gizmos.color = isHitboxActive ? new Color(1f, 0f, 0f, 0.5f) : new Color(1f, 1f, 0f, 0.2f);
                Gizmos.DrawSphere(hitboxCenter, attackData.hitboxRadius);
            }
        }

        // --- GIZMOS CHO TARGETING (LUÔN HIỂN THỊ) ---
        // Vẽ hướng nhân vật đang nhìn (để so sánh)
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);

        // Nếu có mục tiêu, vẽ một đường thẳng tới nó
        if (Targeting != null && Targeting.CurrentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, Targeting.CurrentTarget.position);
            Gizmos.DrawWireSphere(Targeting.CurrentTarget.position, 1f);
        }
    }
}