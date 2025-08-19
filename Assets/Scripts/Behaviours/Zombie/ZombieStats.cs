using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace ZombieAI
{
    [System.Serializable]
    public class AttackDefinition
    {
        [Tooltip("The name of the trigger in the Animator to activate this attack.")]
        public string AnimationTriggerName = "Attack1";

        [HorizontalGroup("Stats")]
        [BoxGroup("Stats/Damage", showLabel: false)]
        [Range(1, 200)]
        public int Damage = 10;

        [BoxGroup("Stats/Range", showLabel: false)]
        [Range(0.5f, 3.5f)]
        [SuffixLabel("m", Overlay = true)]
        public float Range = 2.0f;

        [BoxGroup("Stats/Cooldown", showLabel: false)]
        [Range(0.5f, 5f)]
        [SuffixLabel("s", Overlay = true)]
        public float Cooldown = 2f;
    }

    [CreateAssetMenu(fileName = "NewZombieStats", menuName = "ZombieAI/Zombie Stats")]
    public class ZombieStats : ScriptableObject
    {
        [TitleGroup("1. BASIC INFORMATION", boldTitle: true)]
        [BoxGroup("1. BASIC INFORMATION/General")]
        [Required("Zombie must have an identifier name.")]
        public string ZombieName = "Walker";

        [BoxGroup("1. BASIC INFORMATION/General")]
        [Range(50, 5000)]
        public int MaxHealth = 100;

        [TitleGroup("2. PERCEPTION SYSTEM", boldTitle: true)]
        [TabGroup("PerceptionTabs", "Vision")]
        [Range(5f, 50f)]
        [SuffixLabel("m", true)]
        [Tooltip("The maximum range at which the zombie can see the player.")]
        public float ViewRange = 20f;

        [TabGroup("PerceptionTabs", "Vision")]
        [Range(30f, 180f)]
        [SuffixLabel("degrees", true)]
        [Tooltip("The angle of the zombie's vision cone.")]
        public float ViewAngle = 90f;

        [TabGroup("PerceptionTabs", "Hearing")]
        [Range(10f, 60f)]
        [SuffixLabel("m", true)]
        [Tooltip("The maximum range at which the zombie can hear sounds.")]
        public float HearingRange = 30f;

        [TabGroup("PerceptionTabs", "Layers")]
        [Tooltip("Layers that are considered obstacles and block the zombie's line of sight.")]
        public LayerMask ObstacleLayer;

        [TabGroup("PerceptionTabs", "Layers")]
        [Tooltip("The layer for corpse objects that the zombie can detect and interact with.")]
        public LayerMask CorpseLayer;

        [TitleGroup("3. MOVEMENT SYSTEM", boldTitle: true)]
        [ValidateInput("IsChaseSpeedValid", "Chase Speed must be greater than or equal to Wander Speed.")]
        [Range(0.5f, 2.5f)]
        [SuffixLabel("m/s", true)]
        public float WanderSpeed = 1.5f;

        [Range(2f, 8f)]
        [SuffixLabel("m/s", true)]
        public float ChaseSpeed = 4f;

        [Range(5f, 50f)]
        [SuffixLabel("m", true)]
        [Tooltip("The maximum radius from its anchor point that the zombie will wander.")]
        public float WanderRadius = 15f;

        [Range(120f, 540f)]
        [SuffixLabel("deg/s", true)]
        [Tooltip("The rotational speed of the zombie when changing direction.")]
        public float TurnSpeed = 360f;

        [TitleGroup("4. COMBAT SYSTEM", boldTitle: true)]
        [ListDrawerSettings(NumberOfItemsPerPage = 3, ShowIndexLabels = true, DefaultExpandedState = true)]
        [Required("Zombie must have at least one attack definition.")]
        public List<AttackDefinition> Attacks;

        [TitleGroup("5. BEHAVIOR TIMINGS", boldTitle: true)]
        [InfoBox("These values control the timing of AI states and behaviors, helping to create a unique pace and feel for each zombie type.", InfoMessageType.Info)]

        [Range(1f, 10f)]
        [SuffixLabel("s", true)]
        [Tooltip("Duration the zombie stays in an alerted state after hearing a sound.")]
        public float WorriedDuration = 5f;

        [Range(0.5f, 5f)]
        [SuffixLabel("s", true)]
        [Tooltip("Duration the zombie stares at the player upon becoming aggressive before chasing.")]
        public float AggroStareDuration = 1.5f;

        [Range(3f, 20f)]
        [SuffixLabel("s", true)]
        [Tooltip("Duration the zombie will perform the 'eating corpse' behavior.")]
        public float BitingDuration = 8f;

        [Range(1f, 5f)]
        [SuffixLabel("s", true)]
        [Tooltip("Duration of the scream animation before transitioning to the chase state.")]
        public float ScreamDuration = 2.5f;

        [Range(0.2f, 3f)]
        [SuffixLabel("s", true)]
        [Tooltip("Time the zombie is stunned after taking damage.")]
        public float DamagedRecoveryTime = 0.5f;

        [Range(3f, 15f)]
        [SuffixLabel("s", true)]
        [Tooltip("Time until the zombie completely loses track of the player and returns to wandering.")]
        public float TimeToForgetPlayer = 5f;

        [Range(1f, 5f)]
        [SuffixLabel("s", true)]
        [Tooltip("How often (in seconds) the zombie scans the environment for corpses.")]
        public float SearchForCorpseInterval = 2.0f;

        [Range(5f, 60f)]
        [SuffixLabel("s", true)]
        [Tooltip("Time before the zombie's corpse is despawned and returned to a pool after death.")]
        public float DespawnTimeAfterDeath = 10f;

        [TitleGroup("6. AUDIO SYSTEM", "Manages all sound effects for the zombie", boldTitle: true)]

        [BoxGroup("6. AUDIO SYSTEM/Configuration")]
        [MinMaxSlider(0.8f, 1.2f, true)]
        [Tooltip("The random pitch variation for each sound played. 1 is normal pitch.")]
        public Vector2 PitchVariation = new Vector2(0.95f, 1.05f);

        [BoxGroup("6. AUDIO SYSTEM/Vocalizations")]
        public List<AudioClip> IdleSounds;

        [BoxGroup("6. AUDIO SYSTEM/Vocalizations")]
        [Required] public AudioClip ScreamSound;

        [BoxGroup("6. AUDIO SYSTEM/Vocalizations")]
        public List<AudioClip> ChaseSounds;

        [BoxGroup("6. AUDIO SYSTEM/Vocalizations")]
        [Required] public AudioClip AttackSound;

        [BoxGroup("6. AUDIO SYSTEM/SFX")]
        public List<AudioClip> HurtSounds;

        [BoxGroup("6. AUDIO SYSTEM/SFX")]
        public AudioClip DeathSound;

        [BoxGroup("6. AUDIO SYSTEM/SFX")]
        public List<AudioClip> FootstepSounds;

        [BoxGroup("6. AUDIO SYSTEM/SFX")]
        [Tooltip("Chewing/biting sounds for when the zombie is eating a corpse.")]
        public List<AudioClip> BitingSounds;

#if UNITY_EDITOR
        private bool IsChaseSpeedValid()
        {
            return this.ChaseSpeed >= this.WanderSpeed;
        }
#endif
    }
}