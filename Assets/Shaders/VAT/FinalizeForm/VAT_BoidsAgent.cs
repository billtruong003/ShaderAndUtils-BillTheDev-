using UnityEngine;
using Sirenix.OdinInspector;

namespace OptimizeVariousVAT
{
    public class VAT_BoidsAgent : MonoBehaviour
    {
        [ShowInInspector, ReadOnly]
        public int StateIndex { get; set; } = -1;

        [ShowInInspector, ReadOnly]
        public int AgentTypeIndex { get; set; } = -1;

        [ShowInInspector, ReadOnly]
        public string CurrentAnimation { get; private set; }

        [SerializeField, Required] private VAT_InstanceManager _manager;

        private void OnEnable()
        {
            if (_manager != null)
            {
                _manager.Register(this, AgentTypeIndex);
            }
        }

        private void OnDisable()
        {
            if (_manager != null)
            {
                _manager.Unregister(this, AgentTypeIndex);
            }
        }

        public void CrossFade(string clipName, float duration)
        {
            if (_manager != null && StateIndex != -1 && CurrentAnimation != clipName)
            {
                _manager.SetAnimationState(StateIndex, AgentTypeIndex, clipName, duration);
                CurrentAnimation = clipName;
            }
        }

        public void UpdateCurrentAnimationName(string clipName)
        {
            CurrentAnimation = clipName;
        }
    }
}