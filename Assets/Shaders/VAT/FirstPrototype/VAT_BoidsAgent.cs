using UnityEngine;
using Sirenix.OdinInspector;

public class VAT_BoidsAgent : MonoBehaviour
{
    [Required]
    public VAT_AnimationData animationData;

    [ShowInInspector, ReadOnly]
    public int StateIndex { get; set; } = -1;

    [ShowInInspector, ReadOnly]
    public string CurrentAnimation { get; private set; }

    [SerializeField] private VAT_InstanceManager _manager;

    private void OnEnable()
    {
        if (_manager != null)
        {
            _manager.Register(this);
        }
    }

    private void OnDisable()
    {
        if (_manager != null)
        {
            _manager.Unregister(this);
        }
    }

    public void CrossFade(string clipName, float duration)
    {
        if (_manager != null && StateIndex != -1 && CurrentAnimation != clipName)
        {
            _manager.SetAnimationState(StateIndex, clipName, duration);
            CurrentAnimation = clipName;
        }
    }

    public void UpdateCurrentAnimationName(string clipName)
    {
        CurrentAnimation = clipName;
    }
}