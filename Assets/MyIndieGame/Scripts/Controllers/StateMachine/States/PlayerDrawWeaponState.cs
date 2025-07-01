public class PlayerDrawWeaponState : PlayerState
{
    private float drawAnimationDuration = 0.8f; // Lấy từ dữ liệu hoặc đặt cứng

    public PlayerDrawWeaponState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        // Báo cho EquipmentManager và Animator
        equipment.ToggleWeaponStance();

        // Giả sử có 1 animation tên "DrawWeapon" trong Animator
        // Nếu không, bạn có thể bỏ qua dòng này và chỉ dùng timer
        // animator.PlayTargetAnimation("DrawWeapon"); 
    }

    public override void Tick(float deltaTime)
    {
        drawAnimationDuration -= deltaTime;

        if (drawAnimationDuration <= 0)
        {
            // Sau khi rút vũ khí xong, ngay lập tức chuyển sang trạng thái tấn công
            stateMachine.SwitchState(new PlayerAttackState(stateMachine, 0));
        }
    }

    public override void Exit() { }
}