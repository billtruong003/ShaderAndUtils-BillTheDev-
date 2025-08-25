using UnityEngine;

namespace ZombieAI.VAT
{
    [RequireComponent(typeof(VAT_Animator))]
    public class VAT_ZombieAnimationManager : MonoBehaviour
    {
        private VAT_Animator _vatAnimator;
        [SerializeField, Range(0.1f, 0.5f)] private float _crossFadeDuration = 0.25f;

        // Định nghĩa tên clip dưới dạng hằng số để tránh lỗi chính tả
        private const string IDLE_CLIP = "Idle";
        private const string WALK_CLIP = "Walk";
        private const string CHASE_CLIP = "Chase";
        private const string ATTACK_CLIP = "Attack";
        private const string SCREAM_CLIP = "Scream";
        private const string DAMAGE_CLIP = "TakeDamage";
        private const string DEATH_CLIP = "Death";

        private void Awake()
        {
            _vatAnimator = GetComponent<VAT_Animator>();
        }

        public void PlayIdle()
        {
            _vatAnimator.CrossFade(IDLE_CLIP, _crossFadeDuration);
        }

        public void PlayWalk()
        {
            _vatAnimator.CrossFade(WALK_CLIP, _crossFadeDuration);
        }

        public void PlayChase()
        {
            _vatAnimator.CrossFade(CHASE_CLIP, _crossFadeDuration);
        }

        public void PlayAttack()
        {
            // Tấn công thường là hành động tức thời, không cần blend mượt
            _vatAnimator.CrossFade(ATTACK_CLIP, 0.1f);
        }

        public void PlayScream()
        {
            _vatAnimator.CrossFade(SCREAM_CLIP, 0.15f);
        }

        public void PlayTakeDamage()
        {
            _vatAnimator.CrossFade(DAMAGE_CLIP, 0.05f);
        }

        public void PlayDeath()
        {
            // Chết thì không cần blend
            _vatAnimator.Play(DEATH_CLIP);
        }
    }
}