using UnityEngine;
using Sirenix.OdinInspector;

namespace FPS
{
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerController : MonoBehaviour
    {
        [Title("CORE DEPENDENCIES", bold: false)]
        [SerializeField, Required] private PlayerMovement playerMovement;
        [SerializeField, Required] private PlayerInventory playerInventory;
        [SerializeField, Required] private PlayerStateMachine stateMachine;

        private Vector2 moveInput;
        private bool jumpInput;
        private bool sprintInput;
        private bool crouchInput;

        private void OnValidate()
        {
            if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
            if (playerInventory == null) playerInventory = GetComponent<PlayerInventory>();
            if (stateMachine == null) stateMachine = GetComponent<PlayerStateMachine>();
        }

        private void Update()
        {
            ProcessPlayerInputs();
            ProcessWeaponInputs();

            // Yêu cầu State Machine xác định lại trạng thái dựa trên input và môi trường
            stateMachine.DetermineCurrentState(moveInput, sprintInput, crouchInput);
        }

        private void FixedUpdate()
        {
            // Gửi dữ liệu input đã xử lý đến PlayerMovement để áp dụng lực
            playerMovement.HandleMovement(moveInput, sprintInput, jumpInput, crouchInput);
            jumpInput = false; // Reset jump input sau khi đã xử lý trong FixedUpdate
        }

        private void ProcessPlayerInputs()
        {
            moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            sprintInput = Input.GetKey(KeyCode.LeftShift);
            crouchInput = Input.GetKey(KeyCode.LeftControl);

            if (Input.GetButtonDown("Jump"))
            {
                jumpInput = true;
            }
        }

        private void ProcessWeaponInputs()
        {
            if (Input.GetButton("Fire1")) // Dùng GetButton để có thể giữ chuột bắn
            {
                playerInventory.GetCurrentWeapon()?.Attack();
            }
            // ... các input vũ khí khác ...
        }
    }
}