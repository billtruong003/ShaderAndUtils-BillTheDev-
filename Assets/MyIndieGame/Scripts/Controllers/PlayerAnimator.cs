// File: Assets/MyIndieGame/Scripts/Controllers/PlayerAnimator.cs
// PHIÊN BẢN CÓ TÍCH HỢP LEANTWEEN

using UnityEngine;
using DentedPixel; // Thêm thư viện LeanTween

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float lockOnTweenTime = 0.25f; // Thời gian chuyển đổi

    private int lockOnTweenId;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    // ===================================================================
    // === HÀM MỚI: TWEEN THAM SỐ LOCK-ON ================================
    // ===================================================================
    public void TweenLockOnParameter(bool isLockedOn)
    {
        // Hủy tween cũ nếu có để tránh xung đột
        LeanTween.cancel(gameObject, lockOnTweenId);

        float targetValue = isLockedOn ? 1.0f : 0.0f;
        float startValue = animator.GetFloat("IsLockedOn");

        // Sử dụng LeanTween.value để tween tham số
        var tween = LeanTween.value(gameObject, startValue, targetValue, lockOnTweenTime)
            .setEase(LeanTweenType.easeOutQuad)
            .setOnUpdate((float val) =>
            {
                animator.SetFloat("IsLockedOn", val);
            });

        // Lưu ID của tween để có thể hủy nó
        lockOnTweenId = tween.id;
    }
    // ===================================================================

    public void UpdateMoveSpeed(float verticalSpeed, float horizontalSpeed)
    {
        animator.SetFloat("VerticalSpeed", verticalSpeed, 0.1f, Time.deltaTime);
        animator.SetFloat("HorizontalSpeed", horizontalSpeed, 0.1f, Time.deltaTime);
    }

    public void SetBool(string parameterName, bool value)
    {
        animator.SetBool(parameterName, value);
    }

    public void SetTrigger(string parameterName)
    {
        animator.SetTrigger(parameterName);
    }

    public void SetGrounded(bool isGrounded)
    {
        SetBool("IsGrounded", isGrounded);
    }

    public void SetWeaponDrawn(bool isDrawn)
    {
        SetBool("IsWeaponDrawn", isDrawn);
    }

    public void PlayTargetAnimation(string stateName, float crossFadeDuration = 0.1f)
    {
        animator.CrossFade(stateName, crossFadeDuration);
    }

    public void SetWeaponType(int weaponTypeID)
    {
        animator.SetInteger("WeaponTypeID", weaponTypeID);
    }

    public void PlayAction(int actionID)
    {
        animator.SetFloat("ActionID", actionID);
        SetTrigger("PlayAction");
    }

    public void SetAttackSpeedMultiplier(float multiplier)
    {
        animator.SetFloat("AttackSpeed", multiplier);
    }
}