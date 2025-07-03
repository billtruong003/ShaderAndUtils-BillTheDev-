// File: Assets/MyIndieGame/Scripts/Controllers/PlayerAnimator.cs
// Phiên bản đã cập nhật hoàn chỉnh

using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;

    // Tự động lấy Animator nếu chưa được gán
    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    // Cập nhật tốc độ cho Blend Tree (cả 1D và 2D)
    public void UpdateMoveSpeed(float verticalSpeed, float horizontalSpeed)
    {
        animator.SetFloat("VerticalSpeed", verticalSpeed, 0.1f, Time.deltaTime);
        animator.SetFloat("HorizontalSpeed", horizontalSpeed, 0.1f, Time.deltaTime);
    }

    // --- HÀM MỚI BẠN CẦN THÊM VÀO ---
    public void SetBool(string parameterName, bool value)
    {
        animator.SetBool(parameterName, value);
    }
    // --- KẾT THÚC HÀM MỚI ---
    
    // --- CÁC HÀM TIỆN ÍCH KHÁC ---
    public void SetTrigger(string parameterName)
    {
        animator.SetTrigger(parameterName);
    }

    public void SetGrounded(bool isGrounded)
    {
        // Có thể thay thế bằng hàm SetBool chung
        // animator.SetBool("IsGrounded", isGrounded);
        SetBool("IsGrounded", isGrounded);
    }

    public void SetWeaponDrawn(bool isDrawn)
    {
        // animator.SetBool("IsWeaponDrawn", isDrawn);
        SetBool("IsWeaponDrawn", isDrawn);
    }
    
    // --- Các hàm còn lại giữ nguyên ---

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
        // Có thể thay bằng hàm SetTrigger chung
        // animator.SetTrigger("PlayAction");
        SetTrigger("PlayAction");
    }
    
    public void SetAttackSpeedMultiplier(float multiplier)
    {
        animator.SetFloat("AttackSpeed", multiplier);
    }
}