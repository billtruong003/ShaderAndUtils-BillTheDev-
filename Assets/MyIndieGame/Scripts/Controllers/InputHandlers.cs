using UnityEngine;

public class InputHandler : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }
    public bool IsRunning { get; private set; }
    public bool JumpInput { get; private set; }
    public bool AttackInput { get; private set; }
    public bool DashInput { get; private set; }
    public bool StanceChangeInput { get; private set; }

    // --- THÊM CÁC DÒNG NÀY ĐỂ XỬ LÝ TARGET INPUT ---
    public bool TargetInput { get; private set; }

    private bool jumpInputConsumed;
    private bool attackInputConsumed;
    private bool dashInputConsumed;
    private bool stanceChangeConsumed;
    private bool targetInputConsumed; // Thêm cờ để "tiêu thụ" input

    void LateUpdate()
    {
        // Reset các cờ vào đầu mỗi frame
        if (jumpInputConsumed) { JumpInput = false; jumpInputConsumed = true; }
        if (attackInputConsumed) { AttackInput = false; attackInputConsumed = true; }
        if (dashInputConsumed) { DashInput = false; dashInputConsumed = true; }
        if (stanceChangeConsumed) { StanceChangeInput = false; stanceChangeConsumed = true; }
        if (targetInputConsumed) { TargetInput = false; targetInputConsumed = true; } // Thêm logic reset

        MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        IsRunning = Input.GetKey(KeyCode.LeftShift);

        if (Input.GetButtonDown("Jump")) { JumpInput = true; jumpInputConsumed = false; }
        if (Input.GetMouseButtonDown(0)) { AttackInput = true; attackInputConsumed = false; }
        if (Input.GetKeyDown(KeyCode.Z)) { DashInput = true; dashInputConsumed = false; }
        if (Input.GetKeyDown(KeyCode.R)) { StanceChangeInput = true; stanceChangeConsumed = false; }

        // Thêm logic đọc input cho Target (Nút chuột giữa)
        if (Input.GetMouseButtonDown(2)) { TargetInput = true; targetInputConsumed = false; }
    }

    public void ConsumeJumpInput() => jumpInputConsumed = true;
    public void ConsumeAttackInput() => attackInputConsumed = true;
    public void ConsumeDashInput() => dashInputConsumed = true;
    public void ConsumeStanceChangeInput() => stanceChangeConsumed = true;

    // --- THÊM HÀM NÀY ---
    public void ConsumeTargetInput() => targetInputConsumed = true;
}