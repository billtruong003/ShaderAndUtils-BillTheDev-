using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector; // Thêm namespace của Odin

namespace ZombieAI
{
    // Lớp AttackDefinition không cần thay đổi nhiều, nhưng vẫn có thể thêm các thuộc tính Odin
    [System.Serializable]
    public class AttackDefinition
    {
        [Tooltip("Tên Trigger trong Animator để kích hoạt đòn tấn công này.")]
        public string AnimationTriggerName = "SlapRight";

        [Range(0, 200)] public int Damage = 10;
        [Range(1f, 15f)] public float Range = 2.5f;
        [Range(0.5f, 10f)] public float Cooldown = 2f;
        public AudioClip AttackSound;
    }

    [CreateAssetMenu(fileName = "NewZombieStats", menuName = "BillTheDev/AI/Zombie Stats")]
    public class ZombieStats : ScriptableObject
    {
        [Title("CORE PROPERTIES", bold: true)]
        [TabGroup("Tabs", "Core")]
        [BoxGroup("Tabs/Core/General")]
        [Required("Phải có tên cho Zombie!")]
        public string ZombieName = "Walker";

        [BoxGroup("Tabs/Core/General")]
        [Range(50, 5000)]
        public int MaxHealth = 100;

        // --- Tab dành cho các thông số Perception ---
        [Title("AI PERCEPTION", bold: true)]
        [TabGroup("Tabs", "Perception")]
        [BoxGroup("Tabs/Perception/Vision")]
        [Range(5f, 50f)]
        [SuffixLabel("m", true)]
        public float ViewRange = 20f;

        [BoxGroup("Tabs/Perception/Vision")]
        [Range(30f, 180f)]
        [SuffixLabel("degrees", true)]
        public float ViewAngle = 90f;

        [BoxGroup("Tabs/Perception/Hearing")]
        [Range(0f, 60f)]
        [SuffixLabel("m", true)]
        public float HearingRange = 30f;

        [BoxGroup("Tabs/Perception/Layers")]
        [Tooltip("Các layer được coi là vật cản mà Zombie không thể nhìn xuyên qua.")]
        public LayerMask ObstacleLayer;

        [BoxGroup("Tabs/Perception/Layers")]
        [Tooltip("Layer của các đối tượng xác chết để Zombie có thể tương tác.")]
        public LayerMask CorpseLayer;

        // --- Tab dành cho các thông số Movement ---
        [Title("MOVEMENT", bold: true)]
        [TabGroup("Tabs", "Movement")]
        [Range(0.5f, 5f)] public float WanderSpeed = 1.5f;
        [Range(2f, 15f)] public float ChaseSpeed = 4f;
        [Range(5f, 50f)] public float WanderRadius = 15f;
        [Range(60f, 720f)] public float TurnSpeed = 120f;

        // --- Tab dành cho các thông số Combat ---
        [Title("COMBAT", bold: true)]
        [TabGroup("Tabs", "Combat")]
        [ListDrawerSettings(NumberOfItemsPerPage = 3, ShowIndexLabels = true)]
        [Required("Zombie phải có ít nhất một kiểu tấn công.")]
        public List<AttackDefinition> Attacks;

        // --- Tab dành cho các thông số hành vi (Behavior) ---
        [Title("AI BEHAVIOR TIMINGS", "Các giá trị này điều khiển thời gian của các hành vi AI", bold: true)]
        [TabGroup("Tabs", "Behavior")]
        [Range(1f, 10f)]
        [SuffixLabel("s", true)]
        [Tooltip("Thời gian (giây) Zombie sẽ ở trạng thái cảnh giác sau khi nghe thấy tiếng động.")]
        public float WorriedDuration = 5f;

        [Range(0.5f, 5f)]
        [SuffixLabel("s", true)]
        [Tooltip("Thời gian (giây) Zombie sẽ đứng thủ thế nhìn người chơi trước khi lao vào tấn công.")]
        public float AggroStareDuration = 1.5f;

        [Range(3f, 20f)]
        [SuffixLabel("s", true)]
        [Tooltip("Thời gian (giây) Zombie sẽ dành ra để thực hiện hành vi ăn xác.")]
        public float BitingDuration = 8f;

        [Range(1f, 5f)]
        [SuffixLabel("s", true)]
        [Tooltip("Thời gian (giây) của animation hét, trước khi chuyển sang đuổi theo.")]
        public float ScreamDuration = 2.5f;

        [Range(0.2f, 3f)]
        [SuffixLabel("s", true)]
        [Tooltip("Thời gian (giây) Zombie bị choáng sau khi nhận sát thương.")]
        public float DamagedRecoveryTime = 0.5f;

        [Range(1f, 10f)]
        [SuffixLabel("s", true)]
        [Tooltip("Thời gian (giây) Zombie mất dấu người chơi hoàn toàn và quay về trạng thái đi lang thang.")]
        public float TimeToForgetPlayer = 5f;

        [Range(1f, 5f)]
        [SuffixLabel("s", true)]
        [Tooltip("Tần suất (giây) Zombie quét môi trường xung quanh để tìm xác chết khi đang đi lang thang.")]
        public float SearchForCorpseInterval = 2.0f;

        [Range(3f, 60f)]
        [SuffixLabel("s", true)]
        [Tooltip("Thời gian (giây) xác chết của Zombie tồn tại trước khi được thu hồi về Pool.")]
        public float DespawnTimeAfterDeath = 10f;
    }
}