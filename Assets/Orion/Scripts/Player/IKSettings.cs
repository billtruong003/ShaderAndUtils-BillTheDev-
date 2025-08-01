using UnityEngine;
using Sirenix.OdinInspector;

namespace Orion
{
    // Interface chung cho tất cả các bộ cài đặt solver
    public interface IKSolverSettings
    {
        float PositionWeight { get; }
        float RotationWeight { get; }
        float RaycastDistance { get; }
    }

    [System.Serializable]
    public class FootIKSettings : IKSolverSettings
    {
        [Title("Blending Weights", Bold = false)]
        [Range(0f, 1f)] public float PositionWeight = 1f;
        [Range(0f, 1f)] public float RotationWeight = 1f;

        [Title("Raycasting", Bold = false)]
        [SuffixLabel("meters", true)]
        public float RaycastDistance = 1.2f;
        public Vector3 RaycastOriginOffset = new Vector3(0, 0.5f, 0);

        [Title("Positioning", Bold = false)]
        [SuffixLabel("meters", true)]
        [Tooltip("Đẩy vị trí bàn chân lên cao một chút so với mặt đất để tránh xuyên thấu.")]
        public float YOffset = 0.05f;
        [SuffixLabel("meters", true)]
        [Tooltip("Đẩy bàn chân về phía trước khi đứng trên dốc để trông tự nhiên hơn.")]
        public float ForwardOffsetOnSlopes = 0.15f;

        // Explicit interface implementation
        float IKSolverSettings.PositionWeight => PositionWeight;
        float IKSolverSettings.RotationWeight => RotationWeight;
        float IKSolverSettings.RaycastDistance => RaycastDistance;
    }

    [System.Serializable]
    public class HandIKSettings : IKSolverSettings
    {
        [Title("Blending Weights", Bold = false)]
        [Range(0f, 1f)] public float PositionWeight = 1f;
        [Range(0f, 1f)] public float RotationWeight = 1f;

        [Title("Raycasting", Bold = false)]
        [SuffixLabel("meters", true)]
        public float RaycastDistance = 1.5f;

        [Title("Positioning", Bold = false)]
        [SuffixLabel("meters", true)]
        [Tooltip("Khoảng cách từ bề mặt tường đến vị trí đặt bàn tay.")]
        public float PlacementOffset = 0.1f;

        // Explicit interface implementation
        float IKSolverSettings.PositionWeight => PositionWeight;
        float IKSolverSettings.RotationWeight => RotationWeight;
        float IKSolverSettings.RaycastDistance => RaycastDistance;
    }

    [System.Serializable]
    public class StateDrivenFootIKSettings
    {
        [BoxGroup("Grounded")]
        [InlineProperty]
        [HideLabel]
        public FootIKSettings Grounded = new FootIKSettings();

        [BoxGroup("WallRun")]
        [InlineProperty]
        [HideLabel]
        public FootIKSettings WallRun = new FootIKSettings { RaycastDistance = 1.5f, ForwardOffsetOnSlopes = 0.2f };
    }
}