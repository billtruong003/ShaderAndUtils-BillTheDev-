using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
    [field: SerializeField] public InputHandler InputHandler { get; private set; }
    [field: SerializeField] public PlayerLocomotion Locomotion { get; private set; }
    [field: SerializeField] public PlayerAnimator Animator { get; private set; }
    [field: SerializeField] public StatController Stats { get; private set; }
    [field: SerializeField] public EquipmentManager Equipment { get; private set; }

    [Header("State Parameters")]
    public float jumpHeight = 1.5f;

    private PlayerState currentState;

    void Start()
    {
        SwitchState(new PlayerGroundedState(this));
    }

    void Update()
    {
        currentState?.Tick(Time.deltaTime);
    }

    public void SwitchState(PlayerState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }
}