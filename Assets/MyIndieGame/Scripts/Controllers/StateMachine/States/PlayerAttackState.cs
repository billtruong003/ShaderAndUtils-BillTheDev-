public class PlayerAttackState : PlayerState
{
    private int comboIndex;
    private float timer;
    private AttackData attackData;
    private bool canCombo;
    private bool nextAttackExists;

    public PlayerAttackState(PlayerStateMachine stateMachine, int comboIndex) : base(stateMachine)
    {
        this.comboIndex = comboIndex;
    }

    public override void Enter()
    {
        input.ConsumeAttackInput();
        attackData = equipment.GetCurrentAttackData(comboIndex);

        if (attackData == null)
        {
            stateMachine.SwitchState(new PlayerGroundedState(stateMachine));
            return;
        }

        // Kiểm tra trước xem có đòn đánh tiếp theo không
        nextAttackExists = equipment.GetCurrentAttackData(comboIndex + 1) != null;

        timer = attackData.Duration;
        animator.PlayAction(attackData.AttackID);
    }

    public override void Tick(float deltaTime)
    {
        timer -= deltaTime;

        if (timer <= attackData.ComboWindowStartTime)
        {
            canCombo = true;
        }

        if (canCombo && input.AttackInput)
        {
            // Chỉ chuyển sang đòn tiếp theo NẾU nó tồn tại
            if (nextAttackExists)
            {
                stateMachine.SwitchState(new PlayerAttackState(stateMachine, comboIndex + 1));
            }
            // Nếu không có đòn tiếp theo, ta có thể chọn không làm gì,
            // hoặc bắt đầu lại chuỗi combo từ đầu ngay lập tức
            // Ở đây tôi chọn không làm gì và để timer chạy hết
            return;
        }

        if (timer <= 0)
        {
            // Khi hết thời gian, luôn quay về GroundedState
            stateMachine.SwitchState(new PlayerGroundedState(stateMachine));
        }
    }

    public override void Exit() { }
}