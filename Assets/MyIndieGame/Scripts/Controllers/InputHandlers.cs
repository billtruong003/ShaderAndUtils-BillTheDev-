using UnityEngine;

public class InputHandler : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }
    public bool IsRunning { get; private set; }
    public bool JumpInput { get; private set; }
    public bool AttackInput { get; private set; }
    public bool DashInput { get; private set; }
    public bool StanceChangeInput { get; private set; }

    private bool jumpInputConsumed;
    private bool attackInputConsumed;
    private bool dashInputConsumed;
    private bool stanceChangeConsumed;

    void Update()
    {
        // Reset các cờ vào đầu mỗi frame
        if (jumpInputConsumed) JumpInput = false;
        if (attackInputConsumed) AttackInput = false;
        if (dashInputConsumed) DashInput = false;
        if (stanceChangeConsumed) StanceChangeInput = false;

        MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        IsRunning = Input.GetKey(KeyCode.LeftShift);

        if (Input.GetButtonDown("Jump")) { JumpInput = true; jumpInputConsumed = false; }
        if (Input.GetMouseButtonDown(0)) { AttackInput = true; attackInputConsumed = false; }
        if (Input.GetKeyDown(KeyCode.Z)) { DashInput = true; dashInputConsumed = false; }
        if (Input.GetKeyDown(KeyCode.R)) { StanceChangeInput = true; stanceChangeConsumed = false; }
    }

    public void ConsumeJumpInput() => jumpInputConsumed = true;
    public void ConsumeAttackInput() => attackInputConsumed = true;
    public void ConsumeDashInput() => dashInputConsumed = true;
    public void ConsumeStanceChangeInput() => stanceChangeConsumed = true;
}