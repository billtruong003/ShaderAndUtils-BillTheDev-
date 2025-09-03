using UnityEngine;
using Sirenix.OdinInspector;

namespace Kaelia.Data
{
    [CreateAssetMenu(fileName = "NewPlayerData", menuName = "Kaelia/Player Data")]
    public class PlayerDataSO : ScriptableObject
    {
        private const string MOVEMENT_TAB = "Movement";
        private const string JUMP_TAB = "Jump & Air";
        private const string SKILLS_TAB = "Skills";
        private const string COMBAT_TAB = "Combat";
        private const string DETECTION_TAB = "Detection";
        private const string CAMERA_TAB = "Camera";

        // Movement
        [TabGroup(MOVEMENT_TAB, "Ground")]
        [Title("Ground Movement", bold: false)]
        [SuffixLabel("m/s")] public float WalkSpeed = 7f;
        [SuffixLabel("m/s")] public float RunSpeed = 12f;
        public float RotationSmoothTime = 0.1f;
        public float GroundLinearDamping = 6f;

        // Jump & Air
        [TabGroup(JUMP_TAB, "Air Control")]
        [Title("Air Control", bold: false)]
        public float AirAcceleration = 30f;
        [SuffixLabel("m/s")] public float MaxAirSpeed = 10f;
        [Range(0f, 1f)] public float AirDamping = 0.98f;

        [TabGroup(JUMP_TAB, "Jump Mechanics")]
        [Title("Forces", bold: false)]
        [SuffixLabel("N")] public float JumpForce = 15f;
        [SuffixLabel("N")] public float WallJumpUpForce = 12f;
        [SuffixLabel("N")] public float WallJumpSideForce = 20f;

        [TabGroup(JUMP_TAB, "Jump Mechanics")]
        [Title("Timing Windows", bold: false)]
        [SuffixLabel("s")] public float CoyoteTime = 0.1f;
        [SuffixLabel("s")] public float JumpBufferTime = 0.1f;

        // Skills
        [TabGroup(SKILLS_TAB, "Dash")]
        [Title("Dash", bold: false)]
        [SuffixLabel("m/s")] public float DashSpeed = 30f;
        [SuffixLabel("s")] public float DashDuration = 0.2f;
        [SuffixLabel("s")] public float DashCooldown = 1f;

        [TabGroup(SKILLS_TAB, "Slide")]
        [Title("Slide", bold: false)]
        [SuffixLabel("N")] public float SlideStartBoost = 10f;
        [Range(0.9f, 1f)] public float SlideFriction = 0.985f;
        public float SlopeSlideMultiplier = 2f;
        [SuffixLabel("m")] public float SlideColliderHeight = 0.8f;
        [SuffixLabel("m/s")] public float MaxSlideSpeed = 25f;
        public float SlideSteeringControl = 5f;

        [TabGroup(SKILLS_TAB, "Wall Run")]
        [Title("Wall Run", bold: false)]
        [SuffixLabel("m/s")] public float WallRunSpeed = 15f;
        [SuffixLabel("N")] public float WallStickForce = 100f;
        public float WallRunGravity = 3f;
        [SuffixLabel("s")] public float MaxWallRunTime = 2.0f;

        // Combat
        [TabGroup(COMBAT_TAB)]
        [Title("Stance")]
        [SuffixLabel("s")] public float DrawWeaponDuration = 0.5f;
        [SuffixLabel("s")] public float SheatheWeaponDuration = 0.5f;

        [TabGroup(COMBAT_TAB)]
        [Title("Ground Combo")]
        [InfoBox("Thời gian tối đa giữa các đòn đánh để duy trì chuỗi combo.")]
        [SuffixLabel("s")] public float ComboWindow = 0.4f;
        [InfoBox("Lực đẩy tới cho mỗi đòn đánh trong combo.")]
        public Vector3[] ComboMoveForce = { new Vector3(0, 0, 10), new Vector3(0, 0, 12), new Vector3(0, 0, 15) };
        public float[] ComboDamage = { 10f, 15f, 25f };

        [TabGroup(COMBAT_TAB)]
        [Title("Dash Attack")]
        [SuffixLabel("m/s")] public float DashAttackSpeed = 40f;
        [SuffixLabel("s")] public float DashAttackDuration = 0.3f;
        public float DashAttackDamage = 20f;

        [TabGroup(COMBAT_TAB)]
        [Title("Airborne Attack")]
        [SuffixLabel("N")] public float AirborneAttackDownwardForce = 50f;
        public float AirborneAttackDamage = 18f;

        // Detection
        [TabGroup(DETECTION_TAB)]
        [Title("Layers")]
        public LayerMask GroundLayer;
        public LayerMask WallLayer;
        public int PlayerLayer = 6;
        public int InvincibleLayer = 7;

        [TabGroup(DETECTION_TAB)]
        [Title("Distances")]
        [SuffixLabel("m")] public float GroundCheckDistance = 0.1f;
        [SuffixLabel("m")] public float WallCheckDistance = 0.7f;

        // Camera
        [TabGroup(CAMERA_TAB)]
        [Range(0f, 30f)]
        [SuffixLabel("degrees")] public float WallRunCameraTilt = 10f;
    }
}