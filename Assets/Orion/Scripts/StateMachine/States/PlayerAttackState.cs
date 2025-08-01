// Assets/Orion/Scripts/StateMachine/States/PlayerAttackState.cs

using UnityEngine;

namespace Orion
{
    public class PlayerAttackState : State
    {
        private float _comboWindowTimer;
        private bool _canTransition;

        public PlayerAttackState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine) { }

        public override void Enter()
        {
            base.Enter();
            player.LockOrientation = true;
            _canTransition = false;

            if (player.Input.HeavyAttackWasPressed && player.HasMaxParryFocus())
            {
                player.Input.UseHeavyAttackInput();
                player.AnimationController.TriggerHeavyAttack();
                player.ResetParryFocus();
                player.AttackComboCounter = 0;
            }
            else
            {
                player.Input.UseAttackInput();
                player.AttackComboCounter++;
                if (player.AttackComboCounter > 3)
                {
                    player.AttackComboCounter = 1;
                }
                player.AnimationController.TriggerAttack(player.AttackComboCounter);
            }

            _comboWindowTimer = player.AttackComboWindow;
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();
            _comboWindowTimer -= Time.deltaTime;

            float normalizedTime = player.AnimationController.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime;

            if (normalizedTime > 0.4f && player.Input.AttackWasPressed)
            {
                stateMachine.ChangeState(player.AttackState);
                return;
            }

            if (normalizedTime > 0.9f)
            {
                _canTransition = true;
            }

            if (_canTransition)
            {
                if (_comboWindowTimer <= 0f)
                {
                    player.AttackComboCounter = 0;
                    stateMachine.ChangeState(player.GroundedState);
                }
            }
        }

        public override void Exit()
        {
            base.Exit();
            player.LockOrientation = false;
        }
    }
}