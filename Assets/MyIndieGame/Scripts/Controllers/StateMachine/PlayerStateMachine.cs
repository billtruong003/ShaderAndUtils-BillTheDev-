// File: PlayerStateMachine.cs (Đã sửa lỗi Gizmos)
using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
    [field: SerializeField] public InputHandler InputHandler { get; private set; }
    [field: SerializeField] public PlayerLocomotion Locomotion { get; private set; }
    [field: SerializeField] public PlayerAnimator Animator { get; private set; }
    [field: SerializeField] public StatController Stats { get; private set; }
    [field: SerializeField] public EquipmentManager Equipment { get; private set; }
    [field: SerializeField] public TargetingController Targeting { get; private set; }

    [Header("State Parameters")]
    public float jumpHeight = 1.5f;

    // --- THAY ĐỔI: Biến này giờ là public để các state con có thể đọc, nhưng chỉ StateMachine có thể ghi ---
    public PlayerState CurrentState { get; private set; }

    void Start()
    {
        SwitchState(new PlayerGroundedState(this));
    }

    void Update()
    {
        if (InputHandler.TargetInput && Targeting != null)
        {
            InputHandler.ConsumeTargetInput();
            Targeting.HandleTargeting();
        }

        CurrentState?.Tick(Time.deltaTime);
        Targeting?.ValidateTarget();
        Animator.SetGrounded(Locomotion.IsGrounded());
    }

    public void SwitchState(PlayerState newState)
    {
        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState?.Enter();
    }
    
    // --- ON DRAW GIZMOS ĐÃ ĐƯỢC CẬP NHẬT HOÀN CHỈNH ---
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Vẽ Gizmos cho Targeting
        if (Targeting != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + transform.forward * 2f);

            if (Targeting.CurrentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position + Vector3.up, Targeting.CurrentTarget.position);
                Gizmos.DrawWireSphere(Targeting.CurrentTarget.position, 1f);
            }
        }
        
        // Vẽ Gizmos cho SphereCast của đòn tấn công hiện tại
        if (CurrentState is PlayerAttackState attackState)
        {
            if (Equipment.CurrentWeapon == null) return;

            // Lấy dữ liệu từ AttackState
            bool isActive = attackState.IsAttackWindowActive();
            var activePoints = attackState.GetActiveCastPoints();

            // Chọn màu: Đỏ khi active, Cyan khi không
            Gizmos.color = isActive ? new Color(1, 0, 0, 0.5f) : new Color(0, 1, 1, 0.5f);
            
            float radius = Equipment.CurrentWeapon.castRadius;

            if (activePoints != null)
            {
                foreach (var point in activePoints)
                {
                    if (point != null)
                    {
                        Gizmos.DrawSphere(point.position, radius);
                    }
                }
            }
        }
    }
}