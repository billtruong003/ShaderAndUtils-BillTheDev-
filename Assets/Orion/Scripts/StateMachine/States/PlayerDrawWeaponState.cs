// Assets/Orion/Scripts/StateMachine/States/PlayerDrawWeaponState.cs

using UnityEngine;

namespace Orion
{
    public class PlayerDrawWeaponState : State
    {
        private const float StateExitTime = 0.9f;
        private bool _hasTriggered;

        public PlayerDrawWeaponState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine) { }

        public override void Enter()
        {
            base.Enter();
            player.Input.UseDrawWeaponInput();
            player.AnimationController.TriggerDrawWeapon();
            player.LockOrientation = true;
            _hasTriggered = false;
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            AnimatorStateInfo stateInfo = player.AnimationController.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0);

            if (!_hasTriggered && (stateInfo.IsName("DrawWeapon") || stateInfo.IsName("SheatheWeapon")))
            {
                _hasTriggered = true;
            }

            if (_hasTriggered && stateInfo.normalizedTime >= StateExitTime)
            {
                player.CurrentWeaponState = player.CurrentWeaponState == WeaponState.Sheathed ? WeaponState.Drawn : WeaponState.Sheathed;
                player.AnimationController.SetWeaponDrawn(player.CurrentWeaponState == WeaponState.Drawn);
                stateMachine.ChangeState(player.GroundedState);
            }
        }

        public override void Exit()
        {
            base.Exit();
            player.LockOrientation = false;
        }
    }
}