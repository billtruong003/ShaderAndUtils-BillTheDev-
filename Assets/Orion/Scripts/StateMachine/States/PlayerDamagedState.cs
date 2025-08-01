// Assets/Orion/Scripts/StateMachine/States/PlayerDamagedState.cs

using UnityEngine;

namespace Orion
{
    public class PlayerDamagedState : State
    {
        private float _hitDirection;
        private const float StateExitTime = 0.9f;
        private bool _hasTriggered;

        public PlayerDamagedState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine) { }

        public void SetHitDirection(float direction)
        {
            _hitDirection = direction;
        }

        public override void Enter()
        {
            base.Enter();
            player.AnimationController.TriggerTakeDamage(_hitDirection);
            player.SetVelocity(Vector3.zero);
            _hasTriggered = false;
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            AnimatorStateInfo stateInfo = player.AnimationController.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0);

            if (!_hasTriggered && stateInfo.IsTag("Damage"))
            {
                _hasTriggered = true;
            }

            if (_hasTriggered && stateInfo.normalizedTime >= StateExitTime)
            {
                if (player.IsGrounded)
                {
                    stateMachine.ChangeState(player.GroundedState);
                }
                else
                {
                    stateMachine.ChangeState(player.FallState);
                }
            }
        }
    }
}