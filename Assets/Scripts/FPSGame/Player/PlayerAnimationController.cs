using UnityEngine;
using Sirenix.OdinInspector;

namespace FPS
{
    public class PlayerAnimationController : MonoBehaviour
    {
        [Title("Dependencies")]
        [SerializeField] private Animator playerAnimator; // Bỏ [Required] để code tự tìm
        [SerializeField, Required] private Rigidbody playerRigidbody;

        // Animator Parameters
        private static readonly int AnimIDWeapon = Animator.StringToHash("WeaponID");
        private static readonly int AnimIDSpeed = Animator.StringToHash("Speed");
        private static readonly int AnimIDIsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int AnimIDIsSliding = Animator.StringToHash("IsSliding");
        private static readonly int AnimIDIsWallRunning = Animator.StringToHash("IsWallRunning");
        private static readonly int AnimIDWallRunDirection = Animator.StringToHash("WallRunDirection");
        private static readonly int AnimIDJump = Animator.StringToHash("JumpTrigger");

        private void Awake()
        {
            // Tự động tìm Animator trong các đối tượng con nếu chưa được gán
            if (playerAnimator == null)
            {
                playerAnimator = GetComponentInChildren<Animator>();
            }
        }

        private void Update()
        {
            if (playerAnimator == null) return;
            playerAnimator.SetFloat(AnimIDSpeed, new Vector3(playerRigidbody.linearVelocity.x, 0, playerRigidbody.linearVelocity.z).magnitude);
        }

        public void UpdateWeaponAnimation(Weapon weapon)
        {
            if (playerAnimator == null) return;
            int weaponId = weapon != null ? weapon.Data.weaponAnimationId : 0;
            playerAnimator.SetInteger(AnimIDWeapon, weaponId);
        }

        public void OnStateChanged(PlayerState newState, PlayerState oldState)
        {
            if (playerAnimator == null) return;

            if (oldState == PlayerState.Sliding) playerAnimator.SetBool(AnimIDIsSliding, false);
            if (oldState == PlayerState.WallRunning) playerAnimator.SetBool(AnimIDIsWallRunning, false);

            switch (newState)
            {
                case PlayerState.Jumping:
                    playerAnimator.SetTrigger(AnimIDJump);
                    playerAnimator.SetBool(AnimIDIsGrounded, false);
                    break;
                case PlayerState.Falling:
                    playerAnimator.SetBool(AnimIDIsGrounded, false);
                    break;
                case PlayerState.Sliding:
                    playerAnimator.SetBool(AnimIDIsSliding, true);
                    break;
                case PlayerState.WallRunning:
                    playerAnimator.SetBool(AnimIDIsWallRunning, true);
                    break;
                default:
                    playerAnimator.SetBool(AnimIDIsGrounded, true);
                    break;
            }
        }
    }
}