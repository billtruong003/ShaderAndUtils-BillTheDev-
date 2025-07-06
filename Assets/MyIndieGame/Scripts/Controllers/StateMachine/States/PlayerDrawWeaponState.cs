// File: Assets/MyIndieGame/Scripts/Controllers/StateMachine/States/PlayerDrawWeaponState.cs
public class PlayerDrawWeaponState : PlayerState
{
    // Cân nhắc lấy thời gian này từ animation để chính xác hơn trong tương lai
    private float drawAnimationDuration = 0.8f;

    public PlayerDrawWeaponState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        equipment.ToggleWeaponStance();
        // animator.PlayTargetAnimation("DrawWeapon"); 
    }

    public override void Tick(float deltaTime)
    {
        drawAnimationDuration -= deltaTime;

        if (drawAnimationDuration <= 0)
        {
            // SỬA LỖI Ở ĐÂY: Gọi hàm khởi tạo mới không có comboIndex
            stateMachine.SwitchState(new PlayerAttackState(stateMachine));
        }
    }

    public override void Exit() { }
}