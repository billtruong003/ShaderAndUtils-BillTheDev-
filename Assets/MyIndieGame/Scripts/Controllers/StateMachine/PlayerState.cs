public abstract class PlayerState
{
    protected readonly PlayerStateMachine stateMachine;
    // Cung cấp truy cập nhanh tới các component cốt lõi
    protected readonly InputHandler input;
    protected readonly PlayerLocomotion locomotion;
    protected readonly PlayerAnimator animator;
    protected readonly StatController stats;
    protected readonly EquipmentManager equipment;
    protected readonly PlayerParticlesController particles;
    protected readonly AfterImageController afterImageController;

    public PlayerState(PlayerStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        this.input = stateMachine.InputHandler;
        this.locomotion = stateMachine.Locomotion;
        this.animator = stateMachine.Animator;
        this.stats = stateMachine.Stats;
        this.equipment = stateMachine.Equipment;
        this.particles = stateMachine.ParticlesController;
        this.afterImageController = stateMachine.AfterImageController;
    }

    public virtual void Enter() { }
    public virtual void Tick(float deltaTime) { }
    public virtual void Exit() { }
}