// File: Assets/MyIndieGame/Scripts/Controllers/StateMachine/PlayerStateMachine.cs
using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
    [field: SerializeField] public InputHandler InputHandler { get; private set; }
    [field: SerializeField] public PlayerLocomotion Locomotion { get; private set; }
    [field: SerializeField] public PlayerAnimator Animator { get; private set; }
    [field: SerializeField] public StatController Stats { get; private set; }
    [field: SerializeField] public EquipmentManager Equipment { get; private set; }
    [field: SerializeField] public TargetingController Targeting { get; private set; }
    [field: SerializeField] public WeaponController WeaponController { get; private set; }
    [field: SerializeField] public PlayerParticlesController ParticlesController { get; private set; }
    [field: SerializeField] public AfterImageController AfterImageController { get; private set; }

    [Header("State Parameters")]
    public float jumpHeight = 1.5f;
    public float doubleJumpHeight = 1.3f; // <-- ĐÃ THÊM

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
}