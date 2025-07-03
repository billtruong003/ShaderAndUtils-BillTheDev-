// File: InputHandler.cs (Đã sửa lỗi và đơn giản hóa logic)
using UnityEngine;

public class InputHandler : MonoBehaviour
{
    // Các thuộc tính Input
    public Vector2 MoveInput { get; private set; }
    public bool IsRunning { get; private set; }
    public bool JumpInput { get; private set; }
    public bool AttackInput { get; private set; }
    public bool DashInput { get; private set; }
    public bool StanceChangeInput { get; private set; }
    public bool TargetInput { get; private set; }

    // Các cờ "tiêu thụ"
    private bool jumpInputConsumed;
    private bool attackInputConsumed;
    private bool dashInputConsumed;
    private bool stanceChangeConsumed;
    private bool targetInputConsumed;

    // Đổi sang Update để đảm bảo việc đọc input xảy ra trước khi các State.Tick() chạy
    void Update()
    {
        // --- LOGIC MỚI: ĐƠN GIẢN HÓA ---
        // Mỗi đầu frame, xóa các input "một lần" của frame trước nếu nó đã bị tiêu thụ
        if (jumpInputConsumed) { JumpInput = false; }
        if (attackInputConsumed) { AttackInput = false; }
        if (dashInputConsumed) { DashInput = false; }
        if (stanceChangeConsumed) { StanceChangeInput = false; }
        if (targetInputConsumed) { TargetInput = false; }

        // Đọc các input giữ (press & hold)
        MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        IsRunning = Input.GetKey(KeyCode.LeftShift);

        // Đọc các input bấm (press down)
        // Khi một nút được bấm, ta set input là true VÀ cờ consumed là false
        if (Input.GetButtonDown("Jump")) { JumpInput = true; jumpInputConsumed = false; }
        if (Input.GetMouseButtonDown(0)) { AttackInput = true; attackInputConsumed = false; }
        if (Input.GetKeyDown(KeyCode.Z)) { DashInput = true; dashInputConsumed = false; }
        if (Input.GetKeyDown(KeyCode.R)) { StanceChangeInput = true; stanceChangeConsumed = false; }
        if (Input.GetMouseButtonDown(2)) { TargetInput = true; targetInputConsumed = false; }
    }

    // Các hàm Consume chỉ đơn giản là set cờ thành true.
    // Logic reset sẽ được xử lý ở đầu frame tiếp theo.
    public void ConsumeJumpInput() => jumpInputConsumed = true;
    public void ConsumeAttackInput() => attackInputConsumed = true;
    public void ConsumeDashInput() => dashInputConsumed = true;
    public void ConsumeStanceChangeInput() => stanceChangeConsumed = true;
    public void ConsumeTargetInput() => targetInputConsumed = true;
}