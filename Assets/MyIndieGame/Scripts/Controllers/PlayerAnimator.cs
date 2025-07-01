using UnityEngine;
public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;

    void Awake()
    {
    }

    public void UpdateMoveSpeed(float speed)
    {
        animator.SetFloat("MoveSpeed", speed, 0.1f, Time.deltaTime);
    }

    public void SetGrounded(bool isGrounded)
    {
        animator.SetBool("IsGrounded", isGrounded);
    }

    public void PlayTargetAnimation(string stateName, float crossFadeDuration = 0.1f)
    {
        animator.CrossFade(stateName, crossFadeDuration);
    }

    public void SetWeaponType(int weaponTypeID)
    {
        animator.SetInteger("WeaponTypeID", weaponTypeID);
    }

    public void SetWeaponDrawn(bool isDrawn)
    {
        animator.SetBool("IsWeaponDrawn", isDrawn);
    }

    public void PlayAction(int actionID)
    {
        animator.SetInteger("ActionID", actionID);
        animator.SetTrigger("PlayAction");
    }

    public float GetAnimationLength(string stateName)
    {
        // NOTE: Cần một cách để lấy độ dài của clip từ override controller
        // Cách đơn giản là lưu nó trong AttackData
        return animator.GetCurrentAnimatorStateInfo(0).length;
    }
}