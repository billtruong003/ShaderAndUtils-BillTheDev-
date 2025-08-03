namespace FPS
{
    public enum PlayerState
    {
        Idle,
        Walking,
        Running,
        Jumping,
        Falling,
        Crouching, // Thêm trạng thái cúi
        Sliding,
        WallRunning
    }

    public enum WeaponSlot
    {
        Primary,   // Súng lớn
        Secondary, // Súng nhỏ
        Melee      // Cận chiến
    }
}